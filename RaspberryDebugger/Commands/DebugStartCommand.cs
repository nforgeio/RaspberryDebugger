//-----------------------------------------------------------------------------
// FILE:	    DebugStartCommand.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2023 by neonFORGE, LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.ComponentModel.Design;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnvDTE;
using EnvDTE80;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Neon.Common;
using Neon.IO;
using Neon.SSH;

using Newtonsoft.Json.Linq;

using RaspberryDebugger.Extensions;
using RaspberryDebugger.Models.Connection;
using RaspberryDebugger.Models.Project;
using RaspberryDebugger.Models.VisualStudio;

using Task = System.Threading.Tasks.Task;

namespace RaspberryDebugger.Commands
{
    /// <summary>
    /// Handles the <b>Start Debugging</b> command for Raspberry enabled projects.
    /// </summary>
    internal sealed class DebugStartCommand
    {
        /// <summary>
        /// Provides access to Visual Studio commands, etc.
        /// </summary>
        private readonly DTE2 dte;

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = RaspberryDebuggerPackage.DebugStartCommandId;

        /// <summary>
        /// Package command set ID.
        /// </summary>
        public static readonly Guid CommandSet = RaspberryDebuggerPackage.CommandSet;

        /// Initializes a new instance of the <see cref="DebugStartCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private DebugStartCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            Covenant.Requires<ArgumentNullException>(package != null, nameof(package));
            Covenant.Requires<ArgumentNullException>(commandService != null, nameof(commandService));

            ThreadHelper.ThrowIfNotOnUIThread();

            dte = (DTE2)Package.GetGlobalService(typeof(SDTE));

            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem      = new MenuCommand(Execute, menuCommandId);
             
            commandService?.AddCommand(menuItem);
        }

        /// <summary>
        /// Returns the command instance.
        /// </summary>
#pragma warning disable IDE0052 // Remove unread private members
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private static DebugStartCommand Instance { get; set; }
#pragma warning restore IDE0052 // Remove unread private members

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

            Instance = new DebugStartCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Execute(object sender, EventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!await DebugHelper.EnsureOpenSshAsync())
            {
                return;
            }
            
            var project = DebugHelper.GetTargetProject(dte);

            if (project == null)
            {
                return;
            }

            var projectProperties = ProjectProperties.CopyFrom(dte.Solution, project);

            //-----------------------------------------------------------------

            if (!await DebugHelper.PublishProjectWithUiAsync(dte, dte.Solution, project, projectProperties))
            {
                return;
            }

            var connectionInfo = DebugHelper.GetDebugConnectionInfo(projectProperties);

            if (connectionInfo == null)
            {
                return;
            }

            var projectSettings = PackageHelper.GetProjectSettings(dte.Solution, project);

            // Establish a Raspberry connection to handle some things before we start the debugger.

            var connection = await DebugHelper.InitializeConnectionAsync(
                connectionInfo, 
                projectProperties, 
                projectSettings);

            if (connection == null)
            {
                return;
            }

            using (connection)
            {
                // Generate a temporary [launch.json] file and launch the debugger.

                using (var tempFile = await CreateLaunchSettingsAsync(connectionInfo, projectProperties, projectSettings))
                {
                    try
                    {
                        dte.ExecuteCommand("DebugAdapterHost.Launch", $"/LaunchJson:\"{tempFile.Path}\"");
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }
                }

                // Launch the browser for ASPNET apps if requested.  Note that we're going to do this
                // on a background task to poll the Raspberry, waiting for the app to create the create
                // the LISTENING socket.

                if (!projectProperties.IsAspNet || !projectProperties.AspLaunchBrowser)
                    return;
                
                var launchReady    = false;
                var foundWebServer = WebServer.None;

                await NeonHelper.WaitForAsync(
                    async () =>
                    {
                        // The developer must have stopped debugging before the 
                        // ASPNET application was able to begin servicing requests.
                        if (dte.Mode != vsIDEMode.vsIDEModeDebug) return true;

                        using (new CursorWait())
                        {
                            try
                            {
                                var (found, webServer) =
                                    await SearchForRunningWebServerAsync(projectProperties, projectSettings, connection);

                                // web server not found

                                if (!found) return false;

                                // take the found web server

                                foundWebServer = webServer;
                                launchReady    = true;

                                return true;
                            }
                            catch
                            {
                                return false;
                            }
                        }
                    },
                    timeout:      TimeSpan.FromSeconds(60),
                    pollInterval: TimeSpan.FromSeconds(0.5));

                if (!launchReady) return;

                OpenWebBrowser(projectProperties, foundWebServer, connection);
            }
        }

        /// <summary>
        /// Open web browser for debugging
        /// </summary>
        /// <param name="projectProperties">Related project properties</param>
        /// <param name="foundWebServer">Active WebServer: Kestrel or Other (NGiNX, Apache, etc.)</param>
        /// <param name="connection">LinuxSshProxy connection</param>
        private static void OpenWebBrowser(ProjectProperties projectProperties, WebServer foundWebServer, LinuxSshProxy connection)
        {
            // only '/' present or full relative uri

            const int fullRelativeUri = 2;

            var baseUri = $"http://{connection.Name}.local";

            var relativeBrowserUri = projectProperties.AspRelativeBrowserUri.FirstOrDefault() == '/'
                ? projectProperties.AspRelativeBrowserUri
                : $"/{projectProperties.AspRelativeBrowserUri}";

            var port = projectProperties.AspPort;

            switch (foundWebServer)
            {
                // Apache, NGiNX, Kestrel and more... 
                case WebServer.Other:
                case WebServer.Kestrel:
                    NeonHelper.OpenBrowser(relativeBrowserUri.Length < fullRelativeUri 
                        ? foundWebServer == WebServer.Kestrel 
                            ? $"{baseUri}:{port}" 
                            : $"{baseUri}"
                        : foundWebServer == WebServer.Kestrel 
                            ? $"{baseUri}:{port}{relativeBrowserUri}" 
                            : $"{baseUri}{relativeBrowserUri}");
                    break;

                // no running web server found
                case WebServer.None:
                default:
                    break;
            }
        }

        /// <summary>
        /// Search for running web server
        /// </summary>
        /// <param name="projectProperties">ProjectProperties</param>
        /// <param name="projectSettings"></param>
        /// <param name="connection">LinuxSshProxy</param>
        /// <returns>true if running</returns>
        private static async Task<(bool, WebServer)> SearchForRunningWebServerAsync(
            ProjectProperties   projectProperties,
            ProjectSettings     projectSettings,
            LinuxSshProxy       connection)
        {
            // Wait just a bit longer to give the application a 
            // chance to perform any additional initialization.

            await Task.Delay(TimeSpan.FromMilliseconds(125));

            return projectSettings.UseWebServerProxy
                ? ProxyWebServer.ListenFor(projectProperties.AspPort, connection, WebServer.Other)
                : ProxyWebServer.ListenFor(projectProperties.AspPort, connection, WebServer.Kestrel);
        }

        /// <summary>
        /// Creates the temporary launch settings file we'll use for starting <b>vsdbg</b> on
        /// the Raspberry for this command.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="projectProperties">The project properties.</param>
        /// <param name="projectSettings"></param>
        /// <returns>The <see cref="TempFile"/> referencing the created launch file.</returns>
        private async Task<TempFile> CreateLaunchSettingsAsync(
            ConnectionInfo      connectionInfo,
            ProjectProperties   projectProperties, 
            ProjectSettings     projectSettings)
        {
            Covenant.Requires<ArgumentNullException>(connectionInfo != null, nameof(connectionInfo));
            Covenant.Requires<ArgumentNullException>(projectProperties != null, nameof(projectProperties));

            var systemRoot  = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var debugFolder = LinuxPath.Combine(PackageHelper.RemoteDebugBinaryRoot(connectionInfo?.User), projectProperties?.Name);

            // NOTE:
            //
            // We're having the remote [vsdbg] debugger launch our program as:
            //
            //      dotnet program.dll args
            //
            // where:
            //
            //      dotnet          - is the fully qualified path to the dotnet SDK tool on the remote machine
            //      program.dll     - is the fully qualified path to our program DLL
            //      args            - are the arguments to be passed to our program
            //
            // This means that we need add [program.dll] as the first argument, followed 
            // by the program arguments.

            var args = new JArray
            {
                LinuxPath.Combine(debugFolder, projectProperties?.AssemblyName + ".dll")
            };

            if (projectProperties?.CommandLineArgs != null)
            {
                foreach (var arg in projectProperties.CommandLineArgs)
                {
                    args.Add(arg);
                }
            }

            var environmentVariables = new JObject();

            if (projectProperties?.EnvironmentVariables != null)
            {
                foreach (var variable in projectProperties.EnvironmentVariables)
                {
                    environmentVariables.Add(variable.Key, variable.Value);
                }
            }

            // For ASPNET apps, set the [ASPNETCORE_URLS] environment variable
            // to [http://0.0.0.0:PORT] so that the app running on the Raspberry will
            // be reachable from the development workstation.  Note that we don't
            // support HTTPS at this time.

            if (projectProperties?.IsAspNet == true)
            {
                if (projectSettings.UseWebServerProxy)
                {
                    environmentVariables["ASPNETCORE_URLS"] = $"http://127.0.0.1:{projectProperties.AspPort}";
                }
                else
                {
                    environmentVariables["ASPNETCORE_URLS"] = $"http://0.0.0.0:{projectProperties.AspPort}";
                }
            }

            // Construct the debug launch JSON file.

            var engineLogging = string.Empty;
#if DISABLED
            // Uncomment this to have the remote debugger log the traffic it
            // sees from Visual Studio for debugging purposes.  The log file
            // is persisted to the program folder on the Raspberry.

            engineLogging = $"--engineLogging={debugFolder}/__vsdbg-log.txt";
#endif
            var settings =
                new JObject
                (
                    new JProperty("version", "0.2.1"),
                    new JProperty("adapter", Path.Combine(systemRoot, "System32", "OpenSSH", "ssh.exe")),
                    new JProperty("adapterArgs", $"-i \"{connectionInfo?.PrivateKeyPath}\" -o \"StrictHostKeyChecking no\" {connectionInfo?.User}@{connectionInfo?.Host} {PackageHelper.RemoteDebuggerPath} --interpreter=vscode {engineLogging}"),
                    new JProperty("configurations",
                        new JArray
                        (
                            new JObject
                            (
                                new JProperty("name", "Debug on Raspberry"),
                                new JProperty("type", "coreclr"),
                                new JProperty("request", "launch"),
                                new JProperty("program", PackageHelper.RemoteDotnetCommand),
                                new JProperty("args", args),
                                new JProperty("cwd", debugFolder),
                                new JProperty("stopAtEntry", "false"),
                                new JProperty("console", "internalConsole"),
                                new JProperty("env", environmentVariables)
                            )
                        )
                    )
                );

            var tempFile = new TempFile(".launch.json");

            using (var stream = new FileStream(tempFile.Path, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes(settings.ToString()));
            }

            return tempFile;
        }
    }
}
