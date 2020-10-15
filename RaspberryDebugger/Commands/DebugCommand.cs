//-----------------------------------------------------------------------------
// FILE:	    DebugCommand.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Open Source
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
    /// Handles the <b>Start Debugging on Raspberry</b> command.
    /// </summary>
    internal sealed class DebugCommand
    {
        private DTE2    dte;

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Package command set ID.
        /// </summary>
        public static readonly Guid CommandSet = RaspberryDebugPackage.CommandSet;

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private DebugCommand(AsyncPackage package, OleMenuCommandService commandService)
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
        /// Gets the instance of the command.
        /// </summary>
        public static DebugCommand Instance { get; private set; }

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

            DebugCommand.Instance = new DebugCommand(package, commandService);
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

            // We need Windows native SSH to be installed.

            Log.Info("Checking for native OpenSSH client");

            var openSshPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "sysnative", "openssh", "ssh.exe");

            if (!File.Exists(openSshPath))
            {
                Log.WriteLine("Raspberry debugging requires the native OpenSSH client.  See this:");
                Log.WriteLine("https://techcommunity.microsoft.com/t5/itops-talk-blog/installing-and-configuring-openssh-on-windows-server-2019/ba-p/309540");

                var button = MessageBox.Show(
                    "Raspberry debugging requires the Windows OpenSSH client.\r\n\r\nWould you like to install this now (restart required)?",
                    "Windows OpenSSH Client Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (button != DialogResult.Yes)
                {
                    return;
                }

                // Install via Powershell: https://techcommunity.microsoft.com/t5/itops-talk-blog/installing-and-configuring-openssh-on-windows-server-2019/ba-p/309540

                await PackageHelper.ExecuteWithProgressAsync("Installing OpenSSH Client", 
                    async () =>
                    {
                        using (var powershell = new PowerShell())
                        {
                            Log.Info("Installing OpenSSH");

                            for (int i = 0; i < 50; i++)
                            {
                                System.Threading.Thread.Sleep(1000);
                            }

                            Log.Info(powershell.Execute("Add-WindowsCapability -Online -Name OpenSSH.Client~~~~0.0.1.0"));
                        }

                        MessageBox.Show(
                            "Restart Windows to complete the OpenSSH Client installation.",
                            "Restart Required",
                            MessageBoxButtons.OK);

                        await Task.CompletedTask;
                    });
            }

            // Identify the current startup project (if any).

            if (dte.Solution == null)
            {
                MessageBox.Show(
                    "Please open a Visual Studio solution.",
                    "Solution Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            var project = PackageHelper.GetStartupProject(dte.Solution);

            if (project == null)
            {
                MessageBox.Show(
                    "Please select a startup project.",
                    "Startup Project Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            // We need to capture the relevant project properties while we're still
            // on the UI thread so we'll have them on background threads.

            var projectProperties = ProjectProperties.CopyFrom(dte.Solution, project);

            if (!projectProperties.IsNetCore)
            {
                MessageBox.Show(
                    "Only .NETCoreApp v3.1 projects are supported for Raspberry debugging.",
                    "Invalid Project Type",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            if (!projectProperties.IsExecutable)
            {
                MessageBox.Show(
                    "Only projects types that generate an executable program are supported for Raspberry debugging.",
                    "Invalid Project Type",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            if (string.IsNullOrEmpty(projectProperties.SdkVersion))
            {
                MessageBox.Show(
                    "The .NET Core SDK version could not be identified.",
                    "Invalid Project Type",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            var sdkVersion = Version.Parse(projectProperties.SdkVersion);

            if (sdkVersion < Version.Parse("3.1") || Version.Parse("4.0") < sdkVersion)
            {
                MessageBox.Show(
                    $"The .NET Core SDK [{sdkVersion}] is not supported.  Only .NET Core [3.1.x] is supported at this time.",
                    "SDK Not Supported",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            if (projectProperties.AssemblyName.Contains(' '))
            {
                MessageBox.Show(
                    $"Your assembly name [{projectProperties.AssemblyName}] includes a space.  This isn't supported.",
                    "SDK Not Supported",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            // Publish the project locally.  We're publishing, not building so all
            // required binaries and files will be generated.

            if (!await PublishProjectAsync(dte.Solution, project, projectProperties))
            {
                MessageBox.Show(
                    "[dotnet publish] failed for the project.\r\n\r\nLook at the Debug Output for more details.",
                    "Build Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            // Map the debug connection name we got from the project properties (if any) to
            // one of our Raspberry connections.  If no name is specified, we'll
            // use the default connection or prompt the user to create a connection.
            // We'll display an error if a connection is specified and but doesn't exist.

            var existingConnections = PackageHelper.ReadConnections();
            var connectionInfo      = (ConnectionInfo)null;

            if (string.IsNullOrEmpty(projectProperties.DebugConnectionName))
            {
                connectionInfo = existingConnections.SingleOrDefault(info => info.IsDefault);

                if (connectionInfo == null)
                {
                    if (MessageBoxEx.Show(
                        $"Raspberry connection information required.  Would you like to create a connection now?",
                        "Raspberry Connection Required",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1) == DialogResult.No)
                    {
                        return;
                    }

                    connectionInfo = new ConnectionInfo();

                    var connectionDialog = new ConnectionDialog(connectionInfo, edit: false, existingConnections: existingConnections);

                    if (connectionDialog.ShowDialog() == DialogResult.OK)
                    {
                        existingConnections.Add(connectionInfo);
                        PackageHelper.WriteConnections(existingConnections, disableLogging: true);
                    }
                    else
                    {
                        return;
                    }
                }
            }
            else
            {
                connectionInfo = existingConnections.SingleOrDefault(info => info.Name.Equals(projectProperties.DebugConnectionName, StringComparison.InvariantCultureIgnoreCase));

                if (connectionInfo == null)
                {
                    MessageBoxEx.Show(
                        $"The [{projectProperties.DebugConnectionName}] Raspberry connection does not exist.\r\n\r\nPlease add the connection via:\r\n\r\nTools/Options/Raspberry Debugger",
                        "Cannot Find Raspberry Connection",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }
            }

            // Identify the most recent SDK installed on the workstation that has the same 
            // major and minor version numbers as the project.  We'll ensure that the same
            // SDK is installed on the Raspberry (further below).

            var targetSdk = (Sdk)null;

            foreach (var workstationSdk in PackageHelper.InstalledSdks
                .Where(sdk => sdk.Version != null && sdk.Version.StartsWith(projectProperties.SdkVersion + ".")))
            {
                if (targetSdk == null)
                {
                    targetSdk = workstationSdk;
                }
                else if (SemanticVersion.Parse(targetSdk.Version) < SemanticVersion.Parse(workstationSdk.Version))
                {
                    targetSdk = workstationSdk;
                }
            }

            if (targetSdk == null)
            {
                MessageBoxEx.Show(
                    $"We cannot find a .NET SDK implementing v[{projectProperties.SdkVersion}] on this workstation.",
                    "Cannot Find .NET SDK",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            // Establish a Raspberry connection to handle some things before we start the debugger.

            using (var connection = await Connection.ConnectAsync(connectionInfo))
            {
                // Ensure that the SDK is installed.

                if (!await connection.InstallSdkAsync(targetSdk.Version))
                {
                    MessageBoxEx.Show(
                        $"Cannot install the .NET SDK [v{targetSdk.Version}] on the Raspberry.  Check the Debug Output for more details.",
                        "SDK Installation Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                // Ensure that the debugger is installed.

                if (!await connection.InstallDebuggerAsync())
                {
                    MessageBoxEx.Show(
                        $"Cannot install the VSDBG debugger on the Raspberry.  Check the Debug Output for more details.",
                        "Debugger Installation Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                // Upload the program binaries.

                if (!await connection.UploadProgramAsync(projectProperties.Name, projectProperties.AssemblyName, projectProperties.PublishFolder))
                {
                    MessageBoxEx.Show(
                        $"Cannot upload the program binaries to the Raspberry.  Check the Debug Output for more details.",
                        "Debugger Installation Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                // Generate a temporary [launchSettings.json] file and launch the debugger.

                using (var tempFile = await CreateLaunchSettingsAsync(connectionInfo, projectProperties))
                {
                    dte.ExecuteCommand("DebugAdapterHost.Launch", $"/LaunchJson:\"{tempFile.Path}\"");
                }
            }
        }

        /// <summary>
        /// Builds a project.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="project">The project.</param>
        /// <param name="projectProperties">The project properties.</param>
        /// <returns><c>true</c> on success.</returns>
        private async Task<bool> PublishProjectAsync(Solution solution, Project project, ProjectProperties projectProperties)
        {
            Covenant.Requires<ArgumentNullException>(solution != null, nameof(solution));
            Covenant.Requires<ArgumentNullException>(project != null, nameof(project));
            Covenant.Requires<ArgumentNullException>(projectProperties != null, nameof(projectProperties));

            // Build the project within the context of VS to ensure that all changed
            // files are saved and all dependencies are built first.  Then we'll
            // verify that there were no errors before proceeding.

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            solution.SolutionBuild.BuildProject(solution.SolutionBuild.ActiveConfiguration.Name, project.UniqueName, WaitForBuildToFinish: true);

            var errorList = dte.ToolWindows.ErrorList.ErrorItems;

            if (errorList.Count > 0)
            {
                return false;
            }

            await Task.Yield();

            // Publish the project so all required binaries and assets end up
            // in the output folder.

            Log.Info($"Publishing: {projectProperties.FullPath}");

            var response = await NeonHelper.ExecuteCaptureAsync(
                "dotnet",
                new object[]
                {
                    "publish",
                    "--configuration", projectProperties.Configuration,
                    "--runtime", projectProperties.Runtime,
                    "--no-self-contained",
                    "--output", projectProperties.PublishFolder,
                    projectProperties.FullPath
                });

            if (response.ExitCode == 0)
            {
                return true;
            }

            Log.Error("Build Failed!");
            Log.WriteLine(response.AllText);

            return false;
        }

        /// <summary>
        /// Debugs a project.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns><c>true</c> on success.</returns>
        private bool DebugProject(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Log.Info($"Debugging: {project.FullName}");

            return false;
        }

        /// <summary>
        /// Creates a temporary launch settings file for the application that allows
        /// it to be launched and debugged on the remote Raspberry using the connection
        /// information and project properties passed.
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
