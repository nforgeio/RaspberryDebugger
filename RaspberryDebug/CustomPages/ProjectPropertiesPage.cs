//-----------------------------------------------------------------------------
// FILE:	    ProjectPropertiesPage.cs
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
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Microsoft.VisualStudio.Editors.PropertyPages;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;

namespace RaspberryDebug
{
    /// <summary>
    /// Implements the custom Raspberry project debug properties page.
    /// </summary>
    [ComVisible(true)]
    [Guid("8b37874b-9f3f-4634-9f43-427d1d08ff5f")]
    [ProvideObject(typeof(ProjectPropertiesPage))]
    public class ProjectPropertiesPage : PropPageBase
    {
        /// <inheritdoc/>/>
        protected override Type ControlType
        {
            get { return typeof(ProjectPropertiesPanel); }
        }

        /// <inheritdoc/>/>
        protected override string Title
        {
            get { return "Debug Raspberry"; }
        }

        /// <inheritdoc/>/>
        protected override Control CreateControl()
        {
            return new ProjectPropertiesPanel();
        }
    }
}
