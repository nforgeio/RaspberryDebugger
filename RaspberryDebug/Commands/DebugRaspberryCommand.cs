//-----------------------------------------------------------------------------
// FILE:	    DebugRaspberryCommand.cs
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using EnvDTE;
using Neon.Windows;

using Task = System.Threading.Tasks.Task;
using EnvDTE80;

namespace RaspberryDebug
{
    /// <summary>
    /// Handles the <b>Start Raspberry Debugging</b> command.
    /// </summary>
    internal sealed class DebugRaspberryCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("3e88353d-7372-44fb-a34f-502ec7453200");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugRaspberryCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private DebugRaspberryCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem      = new MenuCommand(this.Execute, menuCommandID);
             
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static DebugRaspberryCommand Instance { get; private set; }

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
            // Switch to the main thread - the call to AddCommand in DebugRaspberryCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

            Instance = new DebugRaspberryCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // We need Windows native SSH to be installed.

            Log.WriteLine("Checking for native OpenSSH client");

            var openSshPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "sysnative", "openssh", "ssh.exe");
                                
            if (!File.Exists(openSshPath))
            {
                Log.WriteLine("Raspberry debugging requires the native OpenSSH client.  See this:");
                Log.WriteLine("https://techcommunity.microsoft.com/t5/itops-talk-blog/installing-and-configuring-openssh-on-windows-server-2019/ba-p/309540");

                var button = MessageBox.Show(
                    "Raspberry debugging requires the Windows OpenSSH client.\r\n\r\nWould you like to install this now (restart required)?",
                    "OpenSSH Client Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (button != DialogResult.Yes)
                {
                    return;
                }

                // Install via Powershell: https://techcommunity.microsoft.com/t5/itops-talk-blog/installing-and-configuring-openssh-on-windows-server-2019/ba-p/309540

                var installingForm = new ProgressForm("Installing OpenSSH Client", min: 0, max: 90, stop: 85);

                _ = Task.Run(() =>
                {
                    try
                    {
                        using (var powershell = new PowerShell())
                        {
                            Log.WriteLine("Installing OpenSSH");

                            for (int i = 0; i < 50; i++)
                            {
                                System.Threading.Thread.Sleep(1000);
                            }

                            Log.WriteLine(powershell.Execute("Add-WindowsCapability -Online -Name OpenSSH.Client~~~~0.0.1.0"));
                        }
                    }
                    finally
                    {
                        installingForm.Done = true;
                    }

                    installingForm.WaitUntilClosed();

                    MessageBox.Show(
                        "Restart Windows to complete the OpenSSH Client installation.",
                        "Restart Required",
                        MessageBoxButtons.OK);

                    return;
                });

                installingForm.ShowDialog();
            }

            // Identify the startup project.

            var solution = GetSolution();

            if (solution == null)
            {
                MessageBox.Show(
                    "Please open a Visual Studio solution.",
                    "Solution Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            var project = GetStartupProject(solution);

            if (project == null)
            {
                MessageBox.Show(
                    "Please select a startup project.",
                    "Startup Project Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            Log.WriteLine("Start Raspberry Pi Debugging");
        }

        /// <summary>
        /// Returns the current root solution.
        /// </summary>
        /// <returns>The current solution or <c>null</c>.</returns>
        private Solution GetSolution()
        {
            var dte = (DTE2)Package.GetGlobalService(typeof(SDTE));

            return dte.Solution;
        }

        /// <summary>
        /// Returns the current Visual Studio startup project.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <returns>The current project or <c>null</c>.</returns>
        private Project GetStartupProject(Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (solution?.SolutionBuild?.StartupProjects == null)
            {
                return null;
            }

            var projectName = (string)((object[])solution.SolutionBuild.StartupProjects).FirstOrDefault();

            var startupProject = (Project)null;

            foreach (Project project in solution.Projects)
            {
                if (project.UniqueName == projectName)
                {
                    startupProject = project;
                }
                else if (project.Kind == EnvDTE.Constants.vsProjectItemKindSolutionItems)
                {
                    startupProject = FindInSubprojects(project, projectName);
                }

                if (startupProject != null)
                {
                    break;
                }
            }

            return startupProject;
        }

        /// <summary>
        /// Searches a project's subprojects for a project matching a path.
        /// </summary>
        /// <param name="parentProject">The parent project.</param>
        /// <param name="projectName">The desired project name.</param>
        /// <returns>The <see cref="Project"/> or <c>null</c>.</returns>
        private Project FindInSubprojects(Project parentProject, string projectName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (parentProject == null)
            {
                return null;
            }

            if (parentProject.UniqueName == projectName)
            {
                return parentProject;
            }

            var project = (Project)null;

            if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
            {
                // The project is actually a solution folder so recursively
                // search any subprojects.

                foreach (ProjectItem projectItem in project.ProjectItems)
                {
                    project = FindInSubprojects(projectItem.SubProject, projectName);

                    if (project != null)
                    {
                        break;
                    }
                }
            }

            return project;
        }
    }
}
