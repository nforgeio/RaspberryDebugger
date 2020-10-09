//-----------------------------------------------------------------------------
// FILE:	    RootForm.cs
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
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RaspberryDebug
{
    /// <summary>
    /// This is a completely transparent form we'll display over Visual Studio to
    /// act as the target for <see cref="Control.Invoke(Delegate)"/> calls to
    /// execute things on the UI thread.
    /// </summary>
    public partial class RootForm : Form
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public RootForm()
        {
            InitializeComponent();

            // The borders are already disabled so we just need some magic to 
            // make the background transparent.

            //SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            //BackColor       = Color.Transparent;
            //TransparencyKey = Color.Transparent;
        }

#if DISABLED
        /// <summary>
        /// Override background painting and do nothing.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected override void OnPaintBackground(PaintEventArgs args)
        {
            // Don't paint anything
        }
#endif
    }
}
