//-----------------------------------------------------------------------------
// FILE:	    PackageHelper.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2021 by neonFORGE, LLC.  All rights reserved.
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
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;

using Newtonsoft.Json;

using Neon.Common;
using Neon.IO;
using RaspberryDebugger.Connection;
using RaspberryDebugger.Models.Connection;
using RaspberryDebugger.Models.Project;
using RaspberryDebugger.Models.Raspberry;
using RaspberryDebugger.Models.Sdk;
using RaspberryDebugger.Models.VisualStudio;

namespace RaspberryDebugger
{
    /// <summary>
    /// Package specific constants.
    /// </summary>
    internal static class PackageHelper
    {
        private static readonly object  SyncLock = new object();
        private static SdkCatalog       _cachedSdkCatalog;
        private static SdkCatalog       _cachedGoodSdkCatalog;
        private static RaspberryCatalog _cachedRaspberryCatalog;
        private static List<Sdk>        _cachedWorkstationSdks;

        /// <summary>
        /// The path to the <b>%USERPROFILE%\.raspberry</b> folder where the package
        /// will persist its settings and other files.
        /// </summary>
        public static readonly string SettingsFolder;

        /// <summary>
        /// The path to the folder holding the Raspberry SSH private keys.
        /// </summary>
        public static readonly string KeysFolder;

        /// <summary>
        /// The path to the JSON file defining the Raspberry Pi connections.
        /// </summary>
        public static readonly string ConnectionsPath;

        /// <summary>
        /// The name used to prefix logged output and status bar text.
        /// </summary>
        public const string LogName = "raspberry";

        /// <summary>
        /// Directory on the Raspberry Pi where .NET Core SDKs will be installed along with the
        /// <b>vsdbg</b> remote debugger.
        /// </summary>
        public const string RemoteDotnetFolder = "/lib/dotnet";

        /// <summary>
        /// Fully qualified path to the <b>dotnet</b> executable on the Raspberry.
        /// </summary>
        public const string RemoteDotnetCommand = "/lib/dotnet/dotnet";

        /// <summary>
        /// Directory on the Raspberry Pi where the <b>vsdbg</b> remote debugger will be installed.
        /// </summary>
        public const string RemoteDebuggerFolder = RemoteDotnetFolder + "/vsdbg";

        /// <summary>
        /// Path to the <b>vsdbg</b> program on the remote machine.
        /// </summary>
        public const string RemoteDebuggerPath = RemoteDebuggerFolder + "/vsdbg";

        /// <summary>
        /// URI for the project's GitHub issues.
        /// </summary>
        public const string GitHubIssuesUri = "https://github.com/nforgeio/RaspberryDebugger/issues/";

        /// <summary>
        /// Returns the root directory on the Raspberry Pi where the folder where 
        /// program binaries will be uploaded for the named user.  Each program will
        /// have a sub directory named for the program.
        /// </summary>
        public static string RemoteDebugBinaryRoot(string username)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(username), nameof(username));

            return LinuxPath.Combine("/", "home", username, "vsdbg");
        }

        /// <summary>
        /// Returns information about the all good .NET Core SDKs, including the unusable ones.
        /// </summary>
        public static SdkCatalog SdkCatalog
        {
            get
            {
                lock (SyncLock)
                {
                    if (_cachedSdkCatalog != null) return _cachedSdkCatalog;
                    
                    var assembly = Assembly.GetExecutingAssembly();

                    using (var catalogStream = assembly.GetManifestResourceStream("RaspberryDebugger.sdk-catalog.json"))
                    {
                        var catalogJson = Encoding.UTF8.GetString(catalogStream.ReadToEnd());

                        _cachedSdkCatalog = NeonHelper.JsonDeserialize<SdkCatalog>(catalogJson);
                    }

                    return _cachedSdkCatalog;
                }
            }
        }

        /// <summary>
        /// Returns information about the known good .NET Core SDKs,
        /// </summary>
        public static SdkCatalog SdkGoodCatalog
        {
            get
            {
                lock (SyncLock)
                {
                    if (_cachedGoodSdkCatalog != null) return _cachedGoodSdkCatalog;

                    var assembly = Assembly.GetExecutingAssembly();

                    using (var catalogStream = assembly.GetManifestResourceStream("RaspberryDebugger.sdk-catalog.json"))
                    {
                        var catalogJson = Encoding.UTF8.GetString(catalogStream.ReadToEnd());

                        _cachedGoodSdkCatalog = NeonHelper.JsonDeserialize<SdkCatalog>(catalogJson);

                        // Remove any unusable SDKs from the catalog.

                        foreach (var unusable in _cachedGoodSdkCatalog.Items.Where(item => item.IsUnusable).ToArray())
                        {
                            _cachedGoodSdkCatalog.Items.Remove(unusable);
                        }
                    }

                    return _cachedGoodSdkCatalog;
                }
            }
        }

        /// <summary>
        /// Returns information about the known Raspberry Pie models.
        /// </summary>
        public static RaspberryCatalog RaspberryCatalog
        {
            get
            {
                lock (SyncLock)
                {
                    if (_cachedRaspberryCatalog != null)
                    {
                        return _cachedRaspberryCatalog;
                    }

                    var assembly = Assembly.GetExecutingAssembly();

                    using (var catalogStream = assembly.GetManifestResourceStream("RaspberryDebugger.raspberry-catalog.json"))
                    {
                        var catalogJson = Encoding.UTF8.GetString(catalogStream.ReadToEnd());

                        _cachedRaspberryCatalog = NeonHelper.JsonDeserialize<RaspberryCatalog>(catalogJson);
                    }
                }

                return _cachedRaspberryCatalog;
            }
        }

        /// <summary>
        /// Static constructor.
        /// </summary>
        static PackageHelper()
        {
            // Initialize the settings path and folders.

            SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".raspberry");

            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }

            KeysFolder = Path.Combine(SettingsFolder, "keys");

            if (!Directory.Exists(KeysFolder))
            {
                Directory.CreateDirectory(KeysFolder);
            }

            ConnectionsPath = Path.Combine(SettingsFolder, "connections.json");
        }

        /// <summary>
        /// Reads the persisted connection settings.
        /// </summary>
        /// <param name="disableLogging">Optionally disable logging.</param>
        /// <returns>The connections.</returns>
        public static List<ConnectionInfo> ReadConnections(bool disableLogging = false)
        {
            if (!disableLogging)
            {
                Log.Info("Reading connections");
            }

            try
            {
                if (!File.Exists(ConnectionsPath))
                {
                    return new List<ConnectionInfo>();
                }

                var connections = NeonHelper.JsonDeserialize<List<ConnectionInfo>>(File.ReadAllText(ConnectionsPath));

                if (connections == null)
                {
                    connections = new List<ConnectionInfo>();
                }

                // Ensure that at least one connection is marked as default.  We'll
                // select the first one as sorted by name if necessary.

                if (connections.Count > 0 && !connections.Any(connection => connection.IsDefault))
                {
                    connections.OrderBy(connection => connection.Name.ToLowerInvariant()).Single().IsDefault = true;
                }
                
                return connections;
            }
            catch (Exception e)
            {
                if (!disableLogging)
                {
                    Log.Exception(e);
                }

                throw;
            }
        }

        /// <summary>
        /// Persists the connections passed.
        /// </summary>
        /// <param name="connections">The connections.</param>
        /// <param name="disableLogging">Optionally disable logging.</param>
        public static void WriteConnections(List<ConnectionInfo> connections, bool disableLogging = false)
        {
            if (!disableLogging)
            {
                Log.Info("Writing connections");
            }

            try
            {
                connections = connections ?? new List<ConnectionInfo>();

                // Ensure that at least one connection is marked as default.  We'll
                // select the first one as sorted by name if necessary.

                if (connections.Count > 0 && !connections.Any(connection => connection.IsDefault))
                {
                    connections.OrderBy(connection => connection.Name.ToLowerInvariant()).First().IsDefault = true;
                }

                File.WriteAllText(ConnectionsPath, NeonHelper.JsonSerialize(connections, Formatting.Indented));
            }
            catch (Exception e)
            {
                if (!disableLogging)
                {
                    Log.Exception(e);
                }

                throw;
            }
        }

        /// <summary>
        /// Updates the Visual Studio user interface.  This is performed asynchronously
        /// by default which will result in better performance.
        /// </summary>
        /// <param name="synchronously">
        /// Optionally specifies that the update should be performed immediately,
        /// before the method returns.
        /// </param>
        internal static void UpdateVisualStudioUi(bool synchronously = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var vsShell = (IVsUIShell)Package.GetGlobalService(typeof(IVsUIShell));

            if (vsShell == null) return;

            var hr = vsShell.UpdateCommandUI(synchronously ? 1 : 0);
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);
        }

        /// <summary>
        /// Returns the current Visual Studio startup project for a solution.
        /// </summary>
        /// <param name="solution">The current solution (or <c>null</c>).</param>
        /// <returns>The startup project or <c>null</c>.</returns>
        /// <remarks>
        /// <note>
        /// The active project may be different from the startup project.  Users select
        /// the startup project explicitly and that project will remain selected until
        /// the user selects another.  The active project is determined by the current
        /// document.
        /// </note>
        /// </remarks>
        public static Project GetStartupProject(Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (solution?.SolutionBuild?.StartupProjects == null)
            {
                return null;
            }

            var projectName    = (string)((object[])solution.SolutionBuild.StartupProjects).FirstOrDefault();
            var startupProject = (Project)null;

            foreach (Project project in solution.Projects)
            {
                if (project.UniqueName == projectName)
                {
                    startupProject = project;
                }
                else if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
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
        /// Returns a solution's active project.
        /// </summary>
        /// <returns>The active <see cref="Project"/> or <c>null</c> for none.</returns>
        /// <remarks>
        /// <note>
        /// The active project may be different from the startup project.  Users select
        /// the startup project explicitly and that project will remain selected until
        /// the user selects another.  The active project is determined by the current
        /// document.
        /// </note>
        /// </remarks>
        public static Project GetActiveProject(DTE2 dte)
        {
            Covenant.Requires<ArgumentNullException>(dte != null, nameof(dte));
            ThreadHelper.ThrowIfNotOnUIThread();

            var activeSolutionProjects = (Array)dte?.ActiveSolutionProjects;

            if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
            {
                return (Project)activeSolutionProjects.GetValue(0);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Determines whether a project is capable of being debugged on a Raspberry.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="project">The project being tested.</param>
        /// <returns><c>true</c> when the project may be debugged on a Raspberry.</returns>
        public static bool IsProjectRaspberryCompatible(Solution solution, Project project)
        {
            Covenant.Requires<ArgumentNullException>(solution != null, nameof(solution));
            Covenant.Requires<ArgumentNullException>(project != null, nameof(project));
            ThreadHelper.ThrowIfNotOnUIThread();

            var projectProperties = ProjectProperties.CopyFrom(solution, project);

            return projectProperties.IsRaspberryCompatible;
        }

        /// <summary>
        /// Determines whether the active project is a candidate for debugging on
        /// a Raspberry.  Currently, the project must target .NET Core 3.1 or
        /// greater and be an executable.
        /// </summary>
        /// <param name="dte"></param>
        /// <returns>
        /// <c>true</c> if there's an active project and it satisfies the criterion.
        /// </returns>
        public static bool IsActiveProjectRaspberryCompatible(DTE2 dte)
        {
            Covenant.Requires<ArgumentNullException>(dte != null, nameof(dte));
            ThreadHelper.ThrowIfNotOnUIThread();

            var activeProject = GetActiveProject(dte);

            if (activeProject == null)
            {
                return false;
            }

            var projectProperties = ProjectProperties.CopyFrom(dte?.Solution, activeProject);

            return projectProperties.IsRaspberryCompatible;
        }

        /// <summary>
        /// Searches a project's subproject for a project matching a path.
        /// </summary>
        /// <param name="parentProject">The parent project.</param>
        /// <param name="projectName">The desired project name.</param>
        /// <returns>The <see cref="Project"/> or <c>null</c>.</returns>
        public static Project FindInSubprojects(Project parentProject, string projectName)
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

            if (parentProject.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
            {
                // The project is actually a solution folder so recursively
                // search any subprojects.

                foreach (ProjectItem projectItem in parentProject.ProjectItems)
                {
                    if (projectItem.SubProject != null)
                    {
                        project = FindInSubprojects(projectItem.SubProject, projectName);

                        if (project != null)
                        {
                            break;
                        }
                    }
                }
            }

            return project;
        }

        /// <summary>
        /// Adds any projects suitable for debugging on a Raspberry to the <paramref name="solutionProjects"/>
        /// list, recursing into projects that are actually solution folders as required.
        /// </summary>
        /// <param name="solutionProjects">The list where discovered projects will be added.</param>
        /// <param name="solution">The parent solution.</param>
        /// <param name="project">The project or solution folder.</param>
        public static void GetSolutionProjects(List<Project> solutionProjects, Solution solution, Project project)
        {
            Covenant.Requires<ArgumentNullException>(solutionProjects != null, nameof(solutionProjects));
            Covenant.Requires<ArgumentNullException>(solution != null, nameof(solution));
            Covenant.Requires<ArgumentNullException>(project != null, nameof(project));

            ThreadHelper.ThrowIfNotOnUIThread();

            if (project?.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
            {
                foreach (ProjectItem projectItem in project.ProjectItems)
                {
                    GetSolutionProjects(solutionProjects, solution, projectItem.SubProject);
                }
            }
            else
            {
                var projectProperties = ProjectProperties.CopyFrom(solution, project);

                if (projectProperties.IsRaspberryCompatible)
                {
                    solutionProjects?.Add(project);
                }
            }
        }

        /// <summary>
        /// Returns the path to the <b>$/.vs/raspberry-projects.json</b> file for
        /// the current solution.
        /// </summary>
        /// <param name="solution">The current solution.</param>
        /// <returns>The file path.</returns>
        private static string GetRaspberryProjectsPath(Solution solution)
        {
            Covenant.Requires<ArgumentNullException>(solution != null);

            ThreadHelper.ThrowIfNotOnUIThread();

            return Path.Combine(Path.GetDirectoryName(solution?.FullName) ?? string.Empty, ".vs", "raspberry-projects.json");
        }

        /// <summary>
        /// Reads the <b>$/.vs/raspberry-projects.json</b> file from the current
        /// solution's directory.
        /// </summary>
        /// <param name="solution">The current solution.</param>
        /// <returns>The projects read or an object with no projects if the file doesn't exist.</returns>
        public static RaspberryProjects ReadRaspberryProjects(Solution solution)
        {
            Covenant.Requires<ArgumentNullException>(solution != null);

            ThreadHelper.ThrowIfNotOnUIThread();

            var path = GetRaspberryProjectsPath(solution);

            if (File.Exists(path))
            {
                return NeonHelper.JsonDeserialize<RaspberryProjects>(File.ReadAllText(path));
            }
            else
            {
                return new RaspberryProjects();
            }
        }

        /// <summary>
        /// Persists the project information passed to the <b>$/.vs/raspberry-projects.json</b> file.
        /// </summary>
        /// <param name="solution">The current solution.</param>
        /// <param name="projects">The projects.</param>
        public static void WriteRaspberryProjects(Solution solution, RaspberryProjects projects)
        {
            Covenant.Requires<ArgumentNullException>(solution != null);
            Covenant.Requires<ArgumentNullException>(projects != null);

            ThreadHelper.ThrowIfNotOnUIThread();

            // Prune any projects with GUIDs that are no longer present in
            // the solution so these don't accumulate.  Note that we need to
            // recurse into solution folders to look for any projects there.

            var solutionProjects = new List<Project>();

            if (solution?.Projects != null)
            {
                foreach (Project project in solution.Projects)
                {
                    GetSolutionProjects(solutionProjects, solution, project);
                }
            }

            var solutionProjectIds = new HashSet<string>();
            var delList            = new List<string>();

            foreach (var project in solutionProjects)
            {
                solutionProjectIds.Add(project.UniqueName);
            }

            if (projects?.Keys != null)
            {
                delList.AddRange((projects.Keys).Where(projectId => !solutionProjectIds.Contains(projectId)));
            }

            foreach (var projectId in delList)
            {
                projects?.Remove(projectId);
            }

            // Write the file, ensuring that the parent directories exist.

            var path = GetRaspberryProjectsPath(solution);

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
            File.WriteAllText(path, NeonHelper.JsonSerialize(projects, Formatting.Indented));
        }

        /// <summary>
        /// Returns the project settings for a specific project.
        /// </summary>
        /// <param name="solution">The current solution.</param>
        /// <param name="project">The target project.</param>
        /// <returns>The project settings.</returns>
        public static ProjectSettings GetProjectSettings(Solution solution, Project project)
        {
            Covenant.Requires<ArgumentNullException>(solution != null);
            Covenant.Requires<ArgumentNullException>(project != null);

            ThreadHelper.ThrowIfNotOnUIThread();

            var raspberryProjects = ReadRaspberryProjects(solution);

            return raspberryProjects[project?.UniqueName];
        }

        //---------------------------------------------------------------------
        // Progress related code

        private const string ProgressCaption = "Raspberry Debugger";

        private static IVsThreadedWaitDialog2 _progressDialog;
        private static readonly Stack<string> OperationStack = new Stack<string>();
        private static string                 _rootDescription;

        /// <summary>
        /// Executes an asynchronous action that does not return a result within the context of a 
        /// Visual Studio progress dialog.  You may make nested calls and this may also be called
        /// from any thread.
        /// </summary>
        /// <param name="description">The operation description.</param>
        /// <param name="action">The action.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public static async Task ExecuteWithProgressAsync(string description, Func<Task> action)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(description), nameof(description));
            Covenant.Requires<ArgumentNullException>(action != null, nameof(action));

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (_progressDialog == null)
            {
                Covenant.Assert(OperationStack.Count == 0);

                _rootDescription = description;
                OperationStack.Push(description);

                var dialogFactory = (IVsThreadedWaitDialogFactory)Package.GetGlobalService((typeof(SVsThreadedWaitDialogFactory)));

                dialogFactory.CreateInstance(out _progressDialog);

                _progressDialog.StartWaitDialog(
                    szWaitCaption:          ProgressCaption, 
                    szWaitMessage:          description,
                    szProgressText:         null, 
                    varStatusBmpAnim:       null, 
                    szStatusBarText:        null, 
                    iDelayToShowDialog:     0,
                    fIsCancelable:          false, 
                    fShowMarqueeProgress:   true);
            }
            else
            {
                Covenant.Assert(OperationStack.Count > 0);

                OperationStack.Push(description);

                _progressDialog.UpdateProgress(
                    szUpdatedWaitMessage:   ProgressCaption,
                    szProgressText:         description,
                    szStatusBarText:        null,
                    iCurrentStep:           0,
                    iTotalSteps:            0,
                    fDisableCancel:         true,
                    pfCanceled:             out _);
            }

            var orgCursor = Cursor.Current;

            try
            {
                Cursor.Current = Cursors.WaitCursor;

                if (action != null) await action().ConfigureAwait(false);
            }
            finally
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                Cursor.Current = orgCursor;

                OperationStack.Pop();

                if (OperationStack.Count == 0)
                {
                    _progressDialog.EndWaitDialog(out _);

                    _progressDialog  = null;
                    _rootDescription = null;
                }
                else
                {
                    _progressDialog.UpdateProgress(
                        szUpdatedWaitMessage:   ProgressCaption,
                        szProgressText:         description,
                        szStatusBarText:        null,
                        iCurrentStep:           0,
                        iTotalSteps:            0,
                        fDisableCancel:         true,
                        pfCanceled:             out _);
                }
            }
        }

        /// <summary>
        /// Executes an asynchronous action that does not return a result within the context of a 
        /// Visual Studio progress dialog.  You may make nested calls and this may also be called
        /// from any thread.
        /// </summary>
        /// <typeparam name="TResult">The action result type.</typeparam>
        /// <param name="description">The operation description.</param>
        /// <param name="action">The action.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public static async Task<TResult> ExecuteWithProgressAsync<TResult>(string description, Func<Task<TResult>> action)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(description), nameof(description));
            Covenant.Requires<ArgumentNullException>(action != null, nameof(action));

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (_progressDialog == null)
            {
                Covenant.Assert(OperationStack.Count == 0);

                _rootDescription = description;
                OperationStack.Push(description);

                var dialogFactory = (IVsThreadedWaitDialogFactory)Package.GetGlobalService((typeof(SVsThreadedWaitDialogFactory)));

                dialogFactory.CreateInstance(out _progressDialog);

                _progressDialog.StartWaitDialog(
                    szWaitCaption:          ProgressCaption, 
                    szWaitMessage:          description,
                    szProgressText:         null, 
                    varStatusBmpAnim:       null, 
                    szStatusBarText:        $"[{LogName}]{description}", 
                    iDelayToShowDialog:     0,
                    fIsCancelable:          false, 
                    fShowMarqueeProgress:   true);
            }
            else
            {
                Covenant.Assert(OperationStack.Count > 0);

                OperationStack.Push(description);

                _progressDialog.UpdateProgress(
                    szUpdatedWaitMessage:   ProgressCaption,
                    szProgressText:         description,
                    szStatusBarText:        null,
                    iCurrentStep:           0,
                    iTotalSteps:            0,
                    fDisableCancel:         true,
                    pfCanceled:             out _);
            }

            var orgCursor = Cursor.Current;

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                Debug.Assert(action != null, nameof(action) + " != null");
                return await action().ConfigureAwait(false);
            }
            finally
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                Cursor.Current = orgCursor;

                var currentDescription = OperationStack.Pop();

                if (OperationStack.Count == 0)
                {
                    _progressDialog.EndWaitDialog(out _);

                    _progressDialog = null;
                    _rootDescription = null;
                }
                else
                {
                    _progressDialog.UpdateProgress(
                        szUpdatedWaitMessage:   currentDescription,
                        szProgressText:         null,
                        szStatusBarText:        _rootDescription,
                        iCurrentStep:           0,
                        iTotalSteps:            0,
                        fDisableCancel:         true,
                        pfCanceled:             out _);
                }
            }
        }
    }
}