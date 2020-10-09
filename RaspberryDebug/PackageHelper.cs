//-----------------------------------------------------------------------------
// FILE:	    PackageHelper.cs
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
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

using Neon.Common;
using Newtonsoft.Json;

using Task = System.Threading.Tasks.Task;
using Neon.Time;
using Microsoft.VisualStudio.Threading;
using System.Threading;

namespace RaspberryDebug
{
    /// <summary>
    /// Package specific constants.
    /// </summary>
    internal static class PackageHelper
    {
        /// <summary>
        /// The path to the <b>%USERPROFILE%\.pi-debug</b> folder where the package
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
        public const string LogName = "pi-debug";

        /// <summary>
        /// Directory on the Raspberry Pi where .NET Core SDKs will be installed along with the
        /// <b>vsdbg</b> remote debugger.
        /// </summary>
        public const string RemoteDotnetRootPath = "/lib/dotnet";

        /// <summary>
        /// Directory on the Raspberry Pi where the <b>vsdbg</b> remote debugger will be installed.
        /// </summary>
        public const string RemoteDebugRoot = RemoteDotnetRootPath + "/vsdbg";

        /// <summary>
        /// Returns information about the known .NET Core SDKs,
        /// </summary>
        public static SdkCatalog SdkCatalog { get; private set; }

        /// <summary>
        /// Static constructor.
        /// </summary>
        static PackageHelper()
        {
            // Initialize the settings path and folders.

            SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pi-debug");

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

            // Parse the embedded SDK catalog JSON.

            var assembly = Assembly.GetExecutingAssembly();

            using (var catalogStream = assembly.GetManifestResourceStream("RaspberryDebug.sdk-catalog.json"))
            {
                var catalogJson = Encoding.UTF8.GetString(catalogStream.ReadToEnd());

                SdkCatalog = NeonHelper.JsonDeserialize<SdkCatalog>(catalogJson);
            }
        }

        /// <summary>
        /// Reads the persisted connection settings.
        /// </summary>
        /// <returns>The connections.</returns>
        public static List<ConnectionInfo> ReadConnections()
        {
            Log.Info("Reading connections");

            try
            {
                if (!File.Exists(ConnectionsPath))
                {
                    return new List<ConnectionInfo>();
                }

                var list = NeonHelper.JsonDeserialize<List<ConnectionInfo>>(File.ReadAllText(ConnectionsPath));

                return list ?? new List<ConnectionInfo>();
            }
            catch (Exception e)
            {
                Log.Exception(e);
                throw;
            }
        }

        /// <summary>
        /// Persists the connections passed.
        /// </summary>
        /// <param name="connections">The connections.</param>
        public static void WriteConnections(List<ConnectionInfo> connections)
        {
            Log.Info("Writing connections");

            try
            {
                connections = connections ?? new List<ConnectionInfo>();

                File.WriteAllText(ConnectionsPath, NeonHelper.JsonSerialize(connections, Formatting.Indented));
            }
            catch (Exception e)
            {
                Log.Exception(e);
                throw;
            }
        }

        //---------------------------------------------------------------------
        // Progress related code

        private static IVsThreadedWaitDialog2   progressDialog     = null;
        private static Stack<string>            operationStack = new Stack<string>();
        private static string                   rootDescription;

        /// <summary>
        /// Used to fake progress in in a <see cref="IVsThreadedWaitDialog2"/>.  I would have
        /// preferred to use an indeterminate progress dialog, but couldn't find one.
        /// </summary>
        private class ProgressUpdater : IDisposable
        {
            private bool    isDisposed = false;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="progressDialog">The associated progress dialog.</param>
            /// <param name="targetSeconds">The maximum seconds to display in the progress bar.</param>
            /// <param name="maxTargetSeconds">
            /// The maximum seconds to actually set in the progress bar.  This defaults to
            /// something a bit less than <paramref name="maxTargetSeconds"/> so the progress
            /// bar won't reach 100%, as suggested for indeterminate progress situations.
            /// </param>
            public ProgressUpdater(IVsThreadedWaitDialog2 progressDialog, int targetSeconds, int maxTargetSeconds = -1)
            {
                Covenant.Requires<ArgumentNullException>(progressDialog != null, nameof(progressDialog));
                Covenant.Requires(targetSeconds >= 0, nameof(targetSeconds));
                Covenant.Requires(maxTargetSeconds <= targetSeconds, nameof(maxTargetSeconds));

                if (maxTargetSeconds < 0)
                {
                    if (targetSeconds == 0)
                    {
                        maxTargetSeconds = targetSeconds;
                    }
                    else if (targetSeconds >= 30)
                    {
                        maxTargetSeconds = targetSeconds - 5;
                    }
                    else
                    {
                        maxTargetSeconds = targetSeconds - 1;
                    }
                }

                CurrentStep = 1;
                TotalSteps  = targetSeconds;

#if DISABLED
                // $todo(jefflill):
                //
                // I couldn't get this to work.  It always blocks on:
                //
                //      ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                //
                // until the operation has completed.  Not a big deal.  I was just
                // trying to move the progress bar, but the user will still see
                // the operation status message updates.

                // We're going to increment the progress every second until the current
                // step reaches [maxTargetSeconds].

                _ = Task.Run(async () =>
                {
                    while (!isDisposed)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));

                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        if (CurrentStep < maxTargetSeconds)
                        {
                            CurrentStep++;
                        }

                        progressDialog?.UpdateProgress(
                            szUpdatedWaitMessage:   operationStack.Peek(),
                            szProgressText:         null,
                            szStatusBarText:        null,
                            iCurrentStep:           CurrentStep,
                            iTotalSteps:            TotalSteps,
                            fDisableCancel:         true,
                            pfCanceled: out var cancelled);

                        await Task.Yield();
                    }

                }).ConfigureAwait(false);
#endif
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                isDisposed = true;
            }

            /// <summary>
            /// Returns the current progress step index.
            /// </summary>
            public int CurrentStep { get; private set; }

            /// <summary>
            /// Returns the current number progress of steps.
            /// </summary>
            public int TotalSteps { get; private set; }
        }

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

            var progressUpdater = (ProgressUpdater)null;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (progressDialog == null)
            {
                Covenant.Assert(operationStack.Count == 0);

                rootDescription = description;
                operationStack.Push(description);

                var dialogFactory = (IVsThreadedWaitDialogFactory)RaspberryDebugPackage.GetGlobalService((typeof(SVsThreadedWaitDialogFactory)));

                dialogFactory.CreateInstance(out progressDialog);

                progressDialog.StartWaitDialog(
                    szWaitCaption:          description, 
                    szWaitMessage:          " ",    // We need this otherwise "Visual Studio" gets displayed
                    szProgressText:         null, 
                    varStatusBmpAnim:       null, 
                    szStatusBarText:        description, 
                    iDelayToShowDialog:     0,
                    fIsCancelable:          false, 
                    fShowMarqueeProgress:   false);
            }
            else
            {
                Covenant.Assert(operationStack.Count > 0);

                operationStack.Push(description);

                progressDialog.UpdateProgress(
                    szUpdatedWaitMessage:   description,
                    szProgressText:         null,
                    szStatusBarText:        null,
                    iCurrentStep:           progressUpdater != null ? progressUpdater.CurrentStep : 0,
                    iTotalSteps:            progressUpdater != null ? progressUpdater.TotalSteps : 0,
                    fDisableCancel:         true,
                    pfCanceled:             out var cancelled);

                progressUpdater = new ProgressUpdater(progressDialog, 60);  // Hardcoding to 60 second target progress
            }

            try
            {
                await action().ConfigureAwait(false);
            }
            finally
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                progressUpdater?.Dispose();
                progressUpdater = null;

                var currentDescription = operationStack.Pop();

                if (operationStack.Count == 0)
                {
                    progressDialog.EndWaitDialog(out var cancelled);

                    progressDialog  = null;
                    rootDescription = null;
                }
                else
                {
                    progressDialog.UpdateProgress(
                        szUpdatedWaitMessage:   currentDescription,
                        szProgressText:         null,
                        szStatusBarText:        rootDescription,
                        iCurrentStep:           0,
                        iTotalSteps:            0,
                        fDisableCancel:         true,
                        pfCanceled:             out var cancelled);
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

            var progressUpdater = (ProgressUpdater)null;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (progressDialog == null)
            {
                Covenant.Assert(operationStack.Count == 0);

                rootDescription = description;
                operationStack.Push(description);

                var dialogFactory = (IVsThreadedWaitDialogFactory)RaspberryDebugPackage.GetGlobalService((typeof(SVsThreadedWaitDialogFactory)));

                dialogFactory.CreateInstance(out progressDialog);

                progressDialog.StartWaitDialog(
                    szWaitCaption:          description, 
                    szWaitMessage:          " ",    // We need this otherwise "Visual Studio" gets displayed
                    szProgressText:         null, 
                    varStatusBmpAnim:       null, 
                    szStatusBarText:        $"[{LogName}]{description}", 
                    iDelayToShowDialog:     0,
                    fIsCancelable:          false, 
                    fShowMarqueeProgress:   false);

                progressUpdater = new ProgressUpdater(progressDialog, 60);  // Hardcoding to 60 second target progress
            }
            else
            {
                Covenant.Assert(operationStack.Count > 0);

                operationStack.Push(description);

                progressDialog.UpdateProgress(
                    szUpdatedWaitMessage:   description,
                    szProgressText:         null,
                    szStatusBarText:        null,
                    iCurrentStep:           progressUpdater != null ? progressUpdater.CurrentStep : 0,
                    iTotalSteps:            progressUpdater != null ? progressUpdater.TotalSteps : 0,
                    fDisableCancel:         true,
                    pfCanceled: out var cancelled);
            }

            try
            {
                return await action().ConfigureAwait(false);
            }
            finally
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                progressUpdater?.Dispose();
                progressUpdater = null;

                var currentDescription = operationStack.Pop();

                if (operationStack.Count == 0)
                {
                    progressDialog.EndWaitDialog(out var cancelled);

                    progressDialog = null;
                    rootDescription = null;
                }
                else
                {
                    progressDialog.UpdateProgress(
                        szUpdatedWaitMessage:   currentDescription,
                        szProgressText:         null,
                        szStatusBarText:        rootDescription,
                        iCurrentStep:           0,
                        iTotalSteps:            0,
                        fDisableCancel:         true,
                        pfCanceled:             out var cancelled);
                }
            }
        }
    }
}
