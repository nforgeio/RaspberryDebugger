//-----------------------------------------------------------------------------
// FILE:	    ProjectProperties.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2020 by neonFORGE, LLC.  All rights reserved.
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
using System.Diagnostics.Contracts;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using EnvDTE;
using EnvDTE80;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Neon.Common;
using Newtonsoft.Json.Linq;

using Task = System.Threading.Tasks.Task;

namespace RaspberryDebugger
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
        /// <param name="solution">The current solution.</param>
        /// <param name="project">The source project.</param>
        /// <returns>The cloned <see cref="ProjectProperties"/>.</returns>
        public static ProjectProperties CopyFrom(Solution solution, Project project)
        {
            Covenant.Requires<ArgumentNullException>(solution != null, nameof(solution));
            Covenant.Requires<ArgumentNullException>(project != null, nameof(project));
            ThreadHelper.ThrowIfNotOnUIThread();

            var projectFolder = Path.GetDirectoryName(project.FullName);
            var projectFile   = File.ReadAllText(project.FullName);
            var isNetCore     = true;
            var sdkVersion    = (string)null;

            // Read the properties we care about from the project.

            var targetFrameworkMonikers = (string)project.Properties.Item("TargetFrameworkMoniker").Value;
            var outputType              = (int)project.Properties.Item("OutputType").Value;

            var monikers = targetFrameworkMonikers.Split(',');

            isNetCore  = monikers[0] == ".NETCoreApp";
            sdkVersion = monikers[1].StartsWith("Version=v") ? monikers[1].Substring("Version=v".Length) : null;

            // Load [Properties/launchSettings.json] if present to obtain the command line
            // arguments and environment variables as well as the target connection.  Note
            // that we're going to use the profile named for the project and ignore any others.

            var launchSettingsPath   = Path.Combine(projectFolder, "Properties", "launchSettings.json");
            var commandLineArgs      = new List<string>();
            var environmentVariables = new Dictionary<string, string>();

            if (File.Exists(launchSettingsPath))
            {
                var settings = JObject.Parse(File.ReadAllText(launchSettingsPath));
                var profiles = settings.Property("profiles");

                if (profiles != null)
                {
                    foreach (var profile in ((JObject)profiles.Value).Properties())
                    {
                        if (profile.Name == project.Name)
                        {
                            var profileObject              = (JObject)profile.Value;
                            var environmentVariablesObject = (JObject)profileObject.Property("environmentVariables")?.Value;

                            commandLineArgs = ParseArgs((string)profileObject.Property("commandLineArgs")?.Value);

                            if (environmentVariablesObject != null)
                            {
                                foreach (var variable in environmentVariablesObject.Properties())
                                {
                                    environmentVariables[variable.Name] = (string)variable.Value;
                                }
                            }

                            break;
                        }
                    }
                }
            }

            // Get the target Raspberry from the debug settings.

            var projects            = PackageHelper.ReadRaspberryProjects(solution);
            var projectSettings     = projects[project.UniqueName];
            var debugEnabled        = projectSettings.EnableRemoteDebugging;
            var debugConnectionName = projectSettings.RemoteDebugTarget;

            // Determine whether the project is Raspberry compatible.

            var isRaspberryCompatible = isNetCore &&
                                        outputType == 1 && // 1=EXE
                                        !string.IsNullOrEmpty(sdkVersion) &&
                                        SemanticVersion.Parse(sdkVersion) >= SemanticVersion.Parse("3.1");

            // Return the properties.

            return new ProjectProperties()
            {
                Name                  = project.Name,
                FullPath              = project.FullName,
                Configuration         = project.ConfigurationManager.ActiveConfiguration.ConfigurationName,
                IsNetCore             = isNetCore,
                SdkVersion            = sdkVersion,
                OutputFolder          = Path.Combine(projectFolder, project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString()),
                OutputFileName        = (string)project.Properties.Item("OutputFileName").Value,
                IsExecutable          = outputType == 1,     // 1=EXE
                AssemblyName          = project.Properties.Item("AssemblyName").Value.ToString(),
                DebugEnabled          = debugEnabled,
                DebugConnectionName   = debugConnectionName,
                CommandLineArgs       = commandLineArgs,
                EnvironmentVariables  = environmentVariables,
                IsRaspberryCompatible = isRaspberryCompatible
            };
        }

        /// <summary>
        /// Parses command line arguments from a string, trying to handle things like 
        /// double and single quotes as well as escaped characters.
        /// </summary>
        /// <param name="commandLine">The source command line or <c>null</c>.</param>
        /// <returns>The list of parsed arguments.</returns>
        private static List<string> ParseArgs(string commandLine)
        {
            commandLine = commandLine ?? string.Empty;
            commandLine = commandLine.Trim();

            var args = new List<string>();

            if (string.IsNullOrWhiteSpace(commandLine))
            {
                return args;
            }

            var pos = 0;

            while (pos < commandLine.Length)
            {
                // Skip past any whitespace.

                if (char.IsWhiteSpace(commandLine[pos]))
                {
                    while (pos < commandLine.Length && char.IsWhiteSpace(commandLine[pos]))
                    {
                        pos++;
                    }

                    continue;
                }

                var arg = string.Empty;

                switch (commandLine[pos])
                {
                    case '\'':

                        // Single quoted string argument: scan for the terminating single quote.

                        pos++;

                        while (pos < commandLine.Length && commandLine[pos] != '\'')
                        {
                            if (commandLine[pos] == '\\')
                            {
                                // Escaped character

                                arg += commandLine[pos++];

                                if (pos >= commandLine.Length)
                                {
                                    throw new ArgumentException($"Invalid escape in: [{commandLine}]");
                                }

                                arg += commandLine[pos++];
                            }
                            else
                            {
                                arg += commandLine[pos++];
                            }
                        }

                        pos++;
                        break;

                    case '"':

                        // Double quoted string argument: scan for the terminating double quote.

                        pos++;

                        while (pos < commandLine.Length && commandLine[pos] != '"')
                        {
                            if (commandLine[pos] == '\\')
                            {
                                // Escaped character

                                arg += commandLine[pos++];

                                if (pos >= commandLine.Length)
                                {
                                    throw new ArgumentException($"Invalid escape in: [{commandLine}]");
                                }

                                arg += commandLine[pos++];
                            }
                            else
                            {
                                arg += commandLine[pos++];
                            }
                        }

                        pos++;
                        break;

                    default:

                        // Space delimited argument: scan for the terminating whitespace.

                        while (pos < commandLine.Length && !char.IsWhiteSpace(commandLine[pos]))
                        {
                            arg += commandLine[pos++];
                        }

                        pos++;
                        break;
                }

                if (arg.Length > 0)
                {
                    args.Add(Regex.Unescape(arg));
                }
            }
            
            return args;
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
        /// Returns the projects .NET Core SDK version.  Note that this will probably include
        /// just the major and minor versions of the SDK.  This may also return <c>null</c>
        /// if the SSK version could not be identified.
        /// </summary>
        public string SdkVersion { get; private set; }

        /// <summary>
        /// Returns the project's build configuration.
        /// </summary>
        public string Configuration { get; private set; }

        /// <summary>
        /// Returns the fully qualified path to the project's output directory.
        /// </summary>
        public string OutputFolder { get; private set; }

        /// <summary>
        /// Indicates that the program is an execuable as opposed to something
        /// else, like a DLL.
        /// </summary>
        public bool IsExecutable { get; private set; }

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

        /// <summary>
        /// Returns the name of the output binary file.
        /// </summary>
        public string OutputFileName { get; private set; }

        /// <summary>
        /// Indicates whether Raspberry debugging is enabled for this project.
        /// </summary>
        public bool DebugEnabled { get; private set; }

        /// <summary>
        /// Returns the connection name identifying the target Raspberry or <c>null</c> 
        /// when the default Raspberry connection should be used.
        /// </summary>
        public string DebugConnectionName { get; private set; }

        /// <summary>
        /// Returns the command line arguments to be passed to the debugged program.
        /// </summary>
        public List<string> CommandLineArgs { get; private set; }

        /// <summary>
        /// Returns the environment variables to be passed to the debugged program.
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; private set; }

        /// <summary>
        /// Indicates whether the project is capable of being deployed and debugged
        /// on a Raspberry.
        /// </summary>
        public bool IsRaspberryCompatible { get; private set; }
    }
}
