//-----------------------------------------------------------------------------
// FILE:	    ProgressDialog.cs
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
using System.Drawing;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;
using System.Diagnostics.Contracts;

namespace RaspberryDebug
{
    /// <summary>
    /// <para>
    /// Displays a form with a progress indicator.  This accepts a title and
    /// integer minimum/maximum values.  The progress bar will advance the progress
    /// every second until gets within a specified value of the maximum.
    /// </para>
    /// <para>
    /// This is intended for situations where an operation will take some time but
    /// we're won't know how long in advance. 
    /// </para>
    /// </summary>
    public partial class ProgressDialog : Form
    {
        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Executes an action on a background thread while displaying progress on
        /// the UI thread.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="seconds">The estimated maximum duration of the operation in seconds.</param>
        /// <param name="action">The async action.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public static async Task RunAsync(string title, int seconds, Func<Task> action)
        {
            Covenant.Requires<ArgumentNullException>(action != null, nameof(action));

            var dialog = (ProgressDialog)null;

            PackageHelper.InvokeOnUIThread(
                () =>
                {
                    dialog = new ProgressDialog(title, seconds, Math.Max(seconds - 5, 5));
                    dialog.ShowDialog();
                });

            try
            {
                await action();
            }
            finally
            {
                dialog.isDone = true;
                dialog.WaitUntilClosed();
            }
        }

        /// <summary>
        /// Executes an action that returns a result on a background thread while
        /// displaying progress on the UI thread.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="title">The dialog title.</param>
        /// <param name="seconds">The estimated maximum duration of the operation in seconds.</param>
        /// <param name="action"></param>
        /// <returns>The action result.</returns>
        public static async Task<T> RunAsync<T>(string title, int seconds, Func<Task<T>> action)
        {
            Covenant.Requires<ArgumentNullException>(action != null, nameof(action));

            var dialog = (ProgressDialog)null;

            PackageHelper.InvokeOnUIThread(
                () =>
                {
                    dialog = new ProgressDialog(title, seconds, Math.Max(seconds - 5, 5));
                    dialog.ShowDialog();
                });

            try
            {
                return await action();
            }
            finally
            {
                dialog.isDone = true;
                dialog.WaitUntilClosed();
            }
        }

        //---------------------------------------------------------------------
        // Instance members

        private bool isDone   = false;
        private bool isClosed = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="seconds">The estimated maximum duration of the operation in seconds.</param>
        /// <param name="stopSeconds">The number of seconds past which the progress bar will stop advancing.</param>
        private ProgressDialog(string title, int seconds, int stopSeconds)
        {
            InitializeComponent();

            this.Text        = title;
            this.ControlBox  = false;    // Hides the min/max/X title bar buttons
            this.FormClosed += (s, a) => isClosed = true;

            progressBar.Minimum = 0;
            progressBar.Maximum = seconds;

            timer.Interval = 1000;
            timer.Tick    +=
                (s, a) =>
                {
                    if (isDone)
                    {
                        Close();
                        return;
                    }

                    progressBar.Value = Math.Min(progressBar.Value + 1, Math.Min(seconds, stopSeconds));
                };
            
            timer.Start();
        }

        /// <summary>
        /// Waits for the dialog to close itself.
        /// </summary>
        private void WaitUntilClosed()
        {
            while (!isClosed)
            {
                Thread.Sleep(100);
            }
        }
    }
}
