//-----------------------------------------------------------------------------
// FILE:	    RaspberryProjects.cs
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

using System.Collections.Generic;

namespace RaspberryDebugger.Models.Project
{
    /// <summary>
    /// Holds the Raspberry related settings for projects in a solution.  This is
    /// persisted to <b>$/.vs/raspberry-projects.json</b> in the solution directory.
    /// </summary>
    /// <remarks>
    /// This is simply a dictionary mapping a project's unique name to the settings 
    /// for the project.
    /// </remarks>
    internal class RaspberryProjects : Dictionary<string, ProjectSettings>
    {
        /// <summary>
        /// Accesses the project settings for a project based on its GUID.
        /// </summary>
        /// <param name="projectUnqueName">The project's unique name.</param>
        /// <returns>
        /// The <see cref="ProjectSettings"/> for the project, initializing default 
        /// (disabled) settings if when the project doesn't exist.
        /// </returns>
        public new ProjectSettings this[string projectUnqueName]
        {
            get
            {
                if (base.TryGetValue(projectUnqueName, out var settings))
                {
                    return settings;
                }
                else
                {
                    settings = new ProjectSettings();

                    this[projectUnqueName] = settings;

                    return settings;
                }
            }

            set => base[projectUnqueName] = value;
        }
    }
}
