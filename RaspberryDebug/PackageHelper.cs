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
using System.Drawing;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Neon.Common;
using Newtonsoft.Json;
using BrightIdeasSoftware;

namespace RaspberryDebug
{
    /// <summary>
    /// Package specific constants.
    /// </summary>
    public static class PackageHelper
    {
        /// <summary>
        /// The path to the <b>%USERPROFILE%\.pi-debug</b> folder where the package
        /// will persist its settings and other files.
        /// </summary>
        public static readonly string SettingsFolder;

        /// <summary>
        /// The path to the JSON file defining the Raspberry Pi connections.
        /// </summary>
        public static readonly string ConnectionsPath;

        /// <summary>
        /// Static constructor.
        /// </summary>
        static PackageHelper()
        {
            // Initialize the settings path and folder.

            SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pi-debug");

            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }

            ConnectionsPath = Path.Combine(SettingsFolder, "connections.json");
        }

        /// <summary>
        /// Reads the persisted connection settings.
        /// </summary>
        /// <returns>The connections.</returns>
        public static List<Connection> ReadConnections()
        {
            if (!File.Exists(ConnectionsPath))
            {
                return new List<Connection>();
            }

            try
            {
                var list = NeonHelper.JsonDeserialize<List<Connection>>(File.ReadAllText(ConnectionsPath));

                return list ?? new List<Connection>();
            }
            catch
            {
                // Something must be corrupted so just return an empty list.

                return new List<Connection>();
            }
        }

        /// <summary>
        /// Persists the connections passed.
        /// </summary>
        /// <param name="connections">The connections.</param>
        public static void WriteConnections(List<Connection> connections)
        {
            connections = connections ?? new List<Connection>();

            File.WriteAllText(ConnectionsPath, NeonHelper.JsonSerialize(connections, Formatting.Indented));
        }
    }
}
