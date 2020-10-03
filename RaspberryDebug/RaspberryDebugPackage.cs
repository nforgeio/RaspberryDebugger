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
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace RaspberryDebug
{
    /// <summary>
    /// Implements a VSIX package that automates debugging C# .NET Core applications remotely
    /// on Raspberry Pi OS.
    /// </summary>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(RaspberryDebugPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideOptionPage(typeof(PiDebugOptionsPage), "Raspberry Debug", "Settings", 0, 0, true)]
    public sealed class RaspberryDebugPackage : AsyncPackage
    {
        /// <summary>
        /// Unique package ID.
        /// </summary>
        public const string PackageGuidString = "fed3a92c-c8e2-40a3-a38f-ce7d35088ea5";

        /// <summary>
        /// Initializes the package.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.

            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await DebugRaspberryCommand.InitializeAsync(this);
        }
    }
}
