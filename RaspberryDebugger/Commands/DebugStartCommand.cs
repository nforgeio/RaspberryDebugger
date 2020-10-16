//-----------------------------------------------------------------------------
// FILE:	    DebugStartCommand.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2020 by neonFORGE, LLC.  All rights reserved.
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
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using EnvDTE;
using EnvDTE80;

using Neon.Common;
using Neon.IO;
using Neon.Windows;

using Newtonsoft.Json.Linq;

using Task = System.Threading.Tasks.Task;

namespace RaspberryDebugger
{
    /// <summary>
    /// Handles the <b>Start Debugging</b> command for Raspberry enabled projects.
    /// </summary>
    internal sealed class DebugStartCommand
    {
        private DTE2    dte;

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = RaspberryDebugPackage.DebugStartCommandId;

        /// <summary>
        /// Package command set ID.
        /// </summary>
        public static readonly Guid CommandSet = RaspberryDebugPackage.CommandSet;

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugStartCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private DebugStartCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            Covenant.Requires<ArgumentNullException>(package != null, nameof(package));
            Covenant.Requires<ArgumentNullException>(commandService != null, nameof(commandService));

            ThreadHelper.ThrowIfNotOnUIThread();

            this.package = package;
            this.dte     = (DTE2)Package.GetGlobalService(typeof(SDTE));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem      = new MenuCommand(this.Execute, menuCommandID);
             
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Returns the command instance.
        /// </summary>
        public static DebugStartCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

            DebugStartCommand.Instance = new DebugStartCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
#pragma warning disable VSTHRD100
        private async void Execute(object sender, EventArgs e)
#pragma warning restore VSTHRD100 
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

            if (!await DebugHelper.PublishProjectWithUIAsync(dte, dte.Solution, project, projectProperties))
            {
                return;
            }

            var connectionInfo = DebugHelper.GetDebugConnectionInfo(projectProperties);

            if (connectionInfo == null)
            {
                return;
            }

            // Identify the most recent SDK installed on the workstation that has the same 
            // major and minor version numbers as the project.  We'll ensure that the same
            // SDK is installed on the Raspberry (further below).

            var targetSdk = DebugHelper.GetTargetSdk(projectProperties);

            if (targetSdk == null)
            {
                return;
            }

            // Establish a Raspberry connection to handle some things before we start the debugger.

            var connection = await DebugHelper.InitializeConnectionAsync(connectionInfo, targetSdk, projectProperties);

            if (connection == null)
            {
                return;
            }

            using (connection)
            {
                // Generate a temporary [launchSettings.json] file and launch the debugger.

                using (var tempFile = await CreateLaunchSettingsAsync(connectionInfo, projectProperties))
                {
                    dte.ExecuteCommand("DebugAdapterHost.Launch", $"/LaunchJson:\"{tempFile.Path}\"");
                }
            }
        }

        /// <summary>
        /// Creates the temporary launch settings file we'll use starting <b>vsdbg</b> on
        /// the Raspberry for this command.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="projectProperties">The project properties.</param>
        /// <returns>The <see cref="TempFile"/> referencing the created launch file.</returns>
        private async Task<TempFile> CreateLaunchSettingsAsync(ConnectionInfo connectionInfo, ProjectProperties projectProperties)
        {
            Covenant.Requires<ArgumentNullException>(connectionInfo != null, nameof(connectionInfo));
            Covenant.Requires<ArgumentNullException>(projectProperties != null, nameof(projectProperties));

            var systemRoot   = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var binaryFolder = LinuxPath.Combine(PackageHelper.RemoteDebugBinaryRoot(connectionInfo.User), projectProperties.Name);

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

            var args = new JArray();

            args.Add(LinuxPath.Combine(binaryFolder, projectProperties.AssemblyName + ".dll"));

            foreach (var arg in projectProperties.CommandLineArgs)
            {
                args.Add(arg);
            }

            var environmentVariables = new JObject();

            foreach (var variable in projectProperties.EnvironmentVariables)
            {
                environmentVariables.Add(variable.Key, variable.Value);
            }

            var settings = 
                new JObject
                (
                    new JProperty("version", "0.2.0"),
                    new JProperty("adapter", Path.Combine(systemRoot, "sysnative", "openssh", "ssh.exe")),
                    new JProperty("adapterArgs", $"-i \"{connectionInfo.PrivateKeyPath}\" {connectionInfo.User}@{connectionInfo.Host} {PackageHelper.RemoteDebuggerPath} --interpreter=vscode --engineLogging={binaryFolder}/log.txt"),
                    new JProperty("configurations",
                        new JArray
                        (
                            new JObject
                            (
                                new JProperty("name", "Debug on Raspberry"),
                                new JProperty("type", "coreclr"),
                                new JProperty("request", "launch"),
                                new JProperty("program", LinuxPath.Combine(PackageHelper.RemoteDotnetFolder, "dotnet")),
                                new JProperty("args", args),
                                new JProperty("cwd", binaryFolder),
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
