//-----------------------------------------------------------------------------
// FILE:	    ProgressForm.cs
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
    public partial class ProgressForm : Form
    {
        private bool isClosed = false;

        /// <summary>
        /// Set this to true when the operation is complete.
        /// </summary>
        public bool Done { get; set; } = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="min">The minimum progress value.</param>
        /// <param name="max">The maximum progress value.</param>
        /// <param name="stop">Optionally specifies a value less than <see cref="Max"/> where progress will stop.</param>
        public ProgressForm(string title, int min, int max, int stop = int.MaxValue)
        {
            InitializeComponent();

            this.Text       = title;
            this.ControlBox = false;    // Hides the min/max/X title bar buttons

            progressBar.Minimum = min;
            progressBar.Maximum = max;

            timer.Interval = 1000;
            timer.Tick    +=
                (s, a) =>
                {
                    if (Done)
                    {
                        Close();
                        isClosed = true;
                        return;
                    }

                    progressBar.Value = Math.Min(progressBar.Value + 1, Math.Min(max, stop));
                };
            
            timer.Start();
        }

        /// <summary>
        /// Waits for the dialog to close itself.
        /// </summary>
        public void WaitUntilClosed()
        {
            while (!isClosed)
            {
                Thread.Sleep(100);
            }
        }
    }
}
