//-----------------------------------------------------------------------------
// FILE:	    ProjectProperties.cs
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
using EnvDTE80;

using Task = System.Threading.Tasks.Task;
using System.Diagnostics.Contracts;

namespace RaspberryDebug
{
    /// <summary>
    /// The Visual Studio <see cref="Project"/> class properties can only be
    /// accessed from the UI thread, so we'll use this class to capture the
    /// properties we need on a UI thread so we can use them later on other
    /// threads.
    /// </summary>
    internal class ProjectProperties
    {
        //--------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Creates a <see cref="ProjectProperties"/> instance holding the
        /// necessary properties from a <see cref="Project"/>.  This must
        /// be called on a UI thread.
        /// </summary>
        /// <param name="project">The source project.</param>
        /// <returns>The cloned <see cref="ProjectProperties"/>.</returns>
        public static ProjectProperties CopyFrom(Project project)
        {
            Covenant.Requires<ArgumentNullException>(project != null, nameof(project));
            ThreadHelper.ThrowIfNotOnUIThread();

            // $hack(jefflill): Read the project file to find out more about the project.

            var projectFile = File.ReadAllText(project.FullName);
            var isNetCore   = true;

            if (projectFile.Trim().StartsWith("<Project "))
            {
                isNetCore = true;
            }
            else
            {
                // This doesn't look like a .NET Core project so it won't be supported.

                isNetCore = false;
            }

            return new ProjectProperties()
            {
                Name          = project.Name,
                FullPath      = project.FullName,
                Configuration = project.ConfigurationManager.ActiveConfiguration.ConfigurationName,
                IsNetCore     = isNetCore,
                OutputFolder  = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project.FullName), project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString())),
                AssemblyName  = project.Properties.Item("AssemblyName").Value.ToString()
            };
        }

        //--------------------------------------------------------------------
        // Instance members

        /// <summary>
        /// Returns the project's friendly name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Returns the fullly qualfied path to the project file.
        /// </summary>
        public string FullPath { get; private set; }

        /// <summary>
        /// Indicates that the project targets .NET Core rather than the .NET Framework.
        /// </summary>
        public bool IsNetCore { get; private set; }

        /// <summary>
        /// Returns the project's build configuration.
        /// </summary>
        public string Configuration { get; private set; }

        /// <summary>
        /// Returns the fully qualified path to the project's output directory.
        /// </summary>
        public string OutputFolder { get; private set; }

        /// <summary>
        /// Returns the publish runtime.
        /// </summary>
        public string Runtime => "linux-arm";

        /// <summary>
        /// Returns the publication folder.
        /// </summary>
        public string PublishFolder => Path.Combine(OutputFolder, Runtime);

        /// <summary>
        /// Returns the name of the output assembly.
        /// </summary>
        public string AssemblyName { get; private set; }
    }
}
