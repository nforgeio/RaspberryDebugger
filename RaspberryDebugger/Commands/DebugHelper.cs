//-----------------------------------------------------------------------------
// FILE:	    DebugHelper.cs
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
using Microsoft.VisualStudio.Threading;

namespace RaspberryDebugger
{
    /// <summary>
    /// Remote debugger related utilties.
    /// </summary>
    internal static class DebugHelper
    {
        /// <summary>
        /// Ensures that the native Windows OpenSSH client is installed, prompting
        /// the user to install it if necessary.
        /// </summary>
        /// <returns><c>true</c> if OpenSSH is installed.</returns>
        public static async Task<bool> EnsureOpenSshAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

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
                    return false;
                }

                // Install via Powershell: https://techcommunity.microsoft.com/t5/itops-talk-blog/installing-and-configuring-openssh-on-windows-server-2019/ba-p/309540

                await PackageHelper.ExecuteWithProgressAsync("Installing OpenSSH Client",
                    async () =>
                    {
                        using (var powershell = new PowerShell())
                        {
                            Log.Info("Installing OpenSSH");
                            Log.Info(powershell.Execute("Add-WindowsCapability -Online -Name OpenSSH.Client~~~~0.0.1.0"));
                        }

                        await Task.CompletedTask;
                    });

                MessageBox.Show(
                    "Restart Windows to complete the OpenSSH Client installation.",
                    "Restart Required",
                    MessageBoxButtons.OK);

                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Attempts to locate the startup project to be debugged, ensuring that it's 
        /// eligable for Raspberry debugging.
        /// </summary>
        /// <param name="dte">The IDE.</param>
        /// <returns>The target project or <c>null</c> if there isn't a startup project or it wasn't eligible.</returns>
        public static Project GetTargetProject(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Identify the current startup project (if any).

            if (dte.Solution == null)
            {
                MessageBox.Show(
                    "Please open a Visual Studio solution.",
                    "Solution Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return null;
            }

            var project = PackageHelper.GetStartupProject(dte.Solution);

            if (project == null)
            {
                MessageBox.Show(
                    "Please select a startup project.",
                    "Startup Project Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return null;
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

                return null;
            }

            if (!projectProperties.IsExecutable)
            {
                MessageBox.Show(
                    "Only projects types that generate an executable program are supported for Raspberry debugging.",
                    "Invalid Project Type",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return null;
            }

            if (string.IsNullOrEmpty(projectProperties.SdkVersion))
            {
                MessageBox.Show(
                    "The .NET Core SDK version could not be identified.",
                    "Invalid Project Type",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return null;
            }

            var sdkVersion = Version.Parse(projectProperties.SdkVersion);

            if (!projectProperties.IsSupportedSdkVersion)
            {
                MessageBox.Show(
                    $"The .NET Core SDK [{sdkVersion}] is not currently supported.  Only .NET Core versions [v3.1] or later will ever be supported\r\n\r\nNote that we currently support only offical SDKs (not previews or release candidates) and we check for new .NET Core SDKs every week or two.  Submit an issue if you really need support for a new SDK ASAP:\r\n\t\nhttps://github.com/nforgeio/RaspberryDebugger/issues",
                    "SDK Not Supported",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return null;
            }

            if (projectProperties.AssemblyName.Contains(' '))
            {
                MessageBox.Show(
                    $"Your assembly name [{projectProperties.AssemblyName}] includes a space.  This isn't supported.",
                    "Unsupported Assembly Name",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return null;
            }

            return project;
        }

        /// <summary>
        /// Builds and publishes a project locally to prepare it for being uploaded to the Raspberry.  This method
        /// will display an error message box to the user on failures
        /// </summary>
        /// <param name="dte"></param>
        /// <param name="solution"></param>
        /// <param name="project"></param>
        /// <param name="projectProperties"></param>
        /// <returns></returns>
        public static async Task<bool> PublishProjectWithUIAsync(DTE2 dte, Solution solution, Project project, ProjectProperties projectProperties)
        {
            Covenant.Requires<ArgumentNullException>(dte != null, nameof(dte));
            Covenant.Requires<ArgumentNullException>(solution != null, nameof(solution));
            Covenant.Requires<ArgumentNullException>(project != null, nameof(project));
            Covenant.Requires<ArgumentNullException>(projectProperties != null, nameof(projectProperties));

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!await PublishProjectAsync(dte, solution, project, projectProperties))
            {
                MessageBox.Show(
                    "[dotnet publish] failed for the project.\r\n\r\nLook at the Debug Output for more details.",
                    "Build Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Builds and publishes a project locally to prepare it for being uploaded to the Raspberry.  This method
        /// does not display error message box to the user on failures
        /// </summary>
        /// <param name="dte">The DTE.</param>
        /// <param name="solution">The solution.</param>
        /// <param name="project">The project.</param>
        /// <param name="projectProperties">The project properties.</param>
        /// <returns><c>true</c> on success.</returns>
        public static async Task<bool> PublishProjectAsync(DTE2 dte, Solution solution, Project project, ProjectProperties projectProperties)
        {
            Covenant.Requires<ArgumentNullException>(dte != null, nameof(dte));
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
        /// Maps the debug connection name we got from the project properties (if any) to
        /// one of our Raspberry connections.  If no name is specified, we'll
        /// use the default connection or prompt the user to create a connection.
        /// We'll display an error if a connection is specified and but doesn't exist.
        /// </summary>
        /// <param name="projectProperties">The project properties.</param>
        /// <returns>The connection or <c>null</c> when one couldn't be located.</returns>
        public static ConnectionInfo GetDebugConnectionInfo(ProjectProperties projectProperties)
        {
            Covenant.Requires<ArgumentNullException>(projectProperties != null, nameof(projectProperties));

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
                        return null;
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
                        return null;
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

                    return null;
                }
            }

            return connectionInfo;
        }

        /// <summary>
        /// Determine which .NET SDK we should target for the project.
        /// </summary>
        /// <param name="projectProperties">The project properties.</param>
        /// <returns>The target <see cref="SDK"/> or <c>null</c> when one could not be located.</returns>
        public static Sdk GetTargetSdk(ProjectProperties projectProperties)
        {
            Covenant.Requires<ArgumentNullException>(projectProperties != null, nameof(projectProperties));

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

                return null;
            }
            else
            {
                return targetSdk;
            }
        }

        /// <summary>
        /// Establishes a connection to the Raspberry and ensures that the Raspberry has
        /// the target SDK, <b>vsdbg</b> installed and also handles uploading of the project
        /// binaries.
        /// </summary>
        /// <param name="connectionInfo">The connection info.</param>
        /// <param name="targetSdk">The target SDK.</param>
        /// <returns>The <see cref="Connection"/> or <c>null</c> if there was an error.</returns>
        public static async Task<Connection> InitializeConnectionAsync(ConnectionInfo connectionInfo, Sdk targetSdk, ProjectProperties projectProperties)
        {
            var connection = await Connection.ConnectAsync(connectionInfo);

            // Ensure that the SDK is installed.

            if (!await connection.InstallSdkAsync(targetSdk.Version))
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                MessageBoxEx.Show(
                    $"Cannot install the .NET SDK [v{targetSdk.Version}] on the Raspberry.\r\n\r\nCheck the Debug Output for more details.",
                    "SDK Installation Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                connection.Dispose();
                return null;
            }

            // Ensure that the debugger is installed.

            if (!await connection.InstallDebuggerAsync())
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                MessageBoxEx.Show(
                    $"Cannot install the VSDBG debugger on the Raspberry.\r\n\r\nCheck the Debug Output for more details.",
                    "Debugger Installation Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                connection.Dispose();
                return null;
            }

            // Upload the program binaries.

            if (!await connection.UploadProgramAsync(projectProperties.Name, projectProperties.AssemblyName, projectProperties.PublishFolder))
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                MessageBoxEx.Show(
                    $"Cannot upload the program binaries to the Raspberry.\r\n\r\nCheck the Debug Output for more details.",
                    "Debugger Installation Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                connection.Dispose();
                return null;
            }

            return connection;
        }
    }
}

