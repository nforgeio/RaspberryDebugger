//-----------------------------------------------------------------------------
// FILE:	    RaspberryDebugPackage.cs
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
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using EnvDTE;
using EnvDTE80;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Neon.Common;
using Neon.Diagnostics;

using Task = System.Threading.Tasks.Task;

namespace RaspberryDebugger
{
    /// <summary>
    /// Implements a VSIX package that automates debugging C# .NET Core applications remotely
    /// on Raspberry Pi OS.
    /// </summary>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(RaspberryDebugPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideOptionPage(typeof(ConnectionsPage), "Raspberry Debugger", "Connections", 0, 0, true)]
    internal sealed class RaspberryDebugPackage : AsyncPackage
    {
        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Unique package ID.
        /// </summary>
        public const string PackageGuidString = "fed3a92c-c8e2-40a3-a38f-ce7d35088ea5";

        /// <summary>
        /// Command set ID for the package.
        /// </summary>
        public static readonly Guid CommandSet = new Guid("3e88353d-7372-44fb-a34f-502ec7453200");

        private static object               debugSyncLock = new object();
        private static IVsOutputWindowPane  debugPane     = null;
        private static Queue<string>        debugLogQueue = new Queue<string>();

        /// <summary>
        /// Returns the package instance.
        /// </summary>
        public static RaspberryDebugPackage Instance { get; private set; }

        /// <summary>
        /// Logs text to the Visual Studio debug panel.
        /// </summary>
        /// <param name="text">The output text.</param>
        public static void Log(string text)
        {
            if (Instance == null || debugPane == null)
            {
                return;     // Logging hasn't been initialized yet.
            }

            if (string.IsNullOrEmpty(text))
            {
                return;     // Nothing to log
            }

            // We're going to queue log messages in the current thread and 
            // then execute a fire-and-forget action on the UI thread to
            // write any queued log lines.  We'll use a lock to protect
            // the queue.
            // 
            // This pattern is nice because it ensures that the log lines
            // are written in the correct order while ensuring this all
            // happens on the UI thread in addition to not using a 
            // [Task.Run(...).Wait()] which would probably result in
            // background thread exhaustion.

            lock (debugSyncLock)
            {
                debugLogQueue.Enqueue(text);
            }

            Instance.JoinableTaskFactory.RunAsync(
                async () =>
                {
                    await Task.Yield();     // Get off of the callers stack
                    await Instance.JoinableTaskFactory.SwitchToMainThreadAsync(Instance.DisposalToken);

                    lock (debugSyncLock)
                    {
                        if (debugLogQueue.Count == 0)
                        {
                            return;     // Nothing to do
                        }

                        debugPane.Activate();

                        // Log any queued messages.

                        while (debugLogQueue.Count > 0)
                        {
                            debugPane.OutputString(debugLogQueue.Dequeue());
                        }
                    }
                });
        }

        //---------------------------------------------------------------------
        // Instance members

        private DTE2            dte;
        private CommandEvents   debugStartCommandEvent;
        private CommandEvents   debugStartWithoutDebuggingCommandEvent;
        private CommandEvents   debugStartDebugTargetCommandEvent;
        private CommandEvents   debugRestartCommandEvent;

        /// <summary>
        /// Initializes the package.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Instance = this;
            dte      = (DTE2)(await GetServiceAsync(typeof(SDTE)));

            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.

            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Initialize the log panel.

            var debugWindow     = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            var generalPaneGuid = VSConstants.GUID_OutWindowDebugPane;

            debugWindow.GetPane(ref generalPaneGuid, out debugPane);

            // Intercept the debugger commands and quickly decide whether the startup project is enabled
            // for Raspberry remote debugging so we can invoke our custom commands instead.  We'll just
            // let the default command implementations do their thing when we're not doing Raspberry
            // debugging.

            debugStartCommandEvent                 = dte.Events.CommandEvents["{5EFC7975-14BC-11CF-9B2B-00AA00573819}", 0x127];
            debugStartWithoutDebuggingCommandEvent = dte.Events.CommandEvents["{5EFC7975-14BC-11CF-9B2B-00AA00573819}", 0x170];
            debugStartDebugTargetCommandEvent      = dte.Events.CommandEvents["{6E87CFAD-6C05-4ADF-9CD7-3B7943875B7C}", 0x101];
            debugRestartCommandEvent               = dte.Events.CommandEvents["{5EFC7975-14BC-11CF-9B2B-00AA00573819}", 0x128];

            debugStartCommandEvent.BeforeExecute                 += DebugStartCommandEvent_BeforeExecute;
            debugStartWithoutDebuggingCommandEvent.BeforeExecute += DebugStartWithoutDebuggingCommandEvent_BeforeExecute;
            debugStartDebugTargetCommandEvent.BeforeExecute      += DebugStartDebugTargetCommandEvent_BeforeExecute;
            debugRestartCommandEvent.BeforeExecute               += DebugRestartCommandEvent_BeforeExecute;

            // Initialize the new commands.

            await DebugCommand.InitializeAsync(this);
            await SettingsCommand.InitializeAsync(this);
        }

        //---------------------------------------------------------------------
        // DEBUG Command interceptors

        /// <summary>
        /// Executes a command by command set GUID and command ID.
        /// </summary>
        /// <param name="commandSet">The command set GUID.</param>
        /// <param name="commandId">The command ID.</param>
        /// <param name="arg">Optionall command argument.</param>
        /// <returns>The command result.</returns>
        private object ExecuteCommand(Guid commandSet, int commandId, object arg = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var result = (object)null;

            dte.Commands.Raise(commandSet.ToString(), commandId, ref arg, ref result);

            return result;
        }

        /// <summary>
        /// Determines whether the current project has Raspberry project settings and returns 
        /// the name of the target connection when it does.
        /// </summary>
        /// <returns>
        /// <c>null</c> if the project does not have Raspberry settings or is not an eligible
        /// .NET Core project, <b>"[default]"</b> when the project targets the default Raspberry 
        /// connection, otherwise the name of  the specific target connection will be returned.
        /// </returns>
        private string GetConnectionName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (dte.Solution == null)
            {
                return null;
            }

            var project = PackageHelper.GetStartupProject(dte.Solution);

            if (project == null)
            {
                return null;
            }

            var projectSettings = PackageHelper.GetProjectSettings(dte.Solution, project);

            if (projectSettings == null || !projectSettings.EnableRemoteDebugging)
            {
                return null;
            }

            return projectSettings.RemoteDebugTarget ?? "[default]";
        }

        /// <summary>
        /// Debug.Start
        /// </summary>
        private void DebugStartCommandEvent_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var connectionName = GetConnectionName();

            if (connectionName == null)
            {
                return;
            }

            CancelDefault = true;
            ExecuteCommand(DebugCommand.CommandSet, DebugCommand.CommandId); 
        }

        /// <summary>
        /// Debug.StartWithoutDebugging
        /// </summary>
        private void DebugStartWithoutDebuggingCommandEvent_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var connectionName = GetConnectionName();

            if (connectionName == null)
            {
                return;
            }

            CancelDefault = true;
            ExecuteCommand(DebugCommand.CommandSet, DebugCommand.CommandId);
        }

        /// <summary>
        /// Debug.Attach
        /// </summary>
        private void DebugStartDebugTargetCommandEvent_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var connectionName = GetConnectionName();

            if (connectionName == null)
            {
                return;
            }

            CancelDefault = true;
            ExecuteCommand(DebugCommand.CommandSet, DebugCommand.CommandId);
        }

        /// <summary>
        /// Debug.Restart
        /// </summary>
        private void DebugRestartCommandEvent_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var connectionName = GetConnectionName();

            if (connectionName == null)
            {
                return;
            }

            CancelDefault = true;
            ExecuteCommand(DebugCommand.CommandSet, DebugCommand.CommandId);
        }
    }
}
