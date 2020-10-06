//-----------------------------------------------------------------------------
// FILE:	    PiConnection.cs
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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using Neon.Common;
using Neon.Net;
using Neon.SSH;

namespace RaspberryDebug
{
    /// <summary>
    /// Implements a SSH connection to the remote Raspberry Pi.
    /// </summary>
    internal class PiConnection : LinuxSshProxy
    {
        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Establishes a SSH connection to the remote Raspberry Pi whose
        /// connection information is passed.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <returns>The connection.</returns>
        /// <exception cref="Exception">Thrown when the connection could not be established.</exception>
        public static PiConnection Connect(Connection connectionInfo)
        {
            Covenant.Requires<ArgumentNullException>(connectionInfo != null, nameof(connectionInfo));

            try
            {
                if (!NetHelper.TryParseIPv4Address(connectionInfo.Host, out var address))
                {
                    Log($"DNS lookup for: {connectionInfo.Host}");

                    address = Dns.GetHostAddresses(connectionInfo.Host).FirstOrDefault();
                }

                if (address == null)
                {
                    LogError("DNS lookup failed.");
                    return null;
                }

                var credentials = SshCredentials.FromUserPassword(connectionInfo.User, connectionInfo.Password);
                var connection  = new PiConnection(connectionInfo.Host, address, credentials, connectionInfo.Port);

                connection.Connect(TimeSpan.Zero);
                connection.GetPiStatus();

                return connection;
            }
            catch (Exception e)
            {
                LogException(e);
                throw;
            }
        }

        //----------------------------------------------------------------------
        // We need these methods to avoid conflicts with the base class methods.

        /// <summary>
        /// Logs a line of text to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The text.</param>
        private new static void Log(string text = "")
        {
            RaspberryDebug.Log.WriteLine(text);
        }

        /// <summary>
        /// Logs an error to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The error text.</param>
        private static void LogError(string text)
        {
            RaspberryDebug.Log.Error(text);
        }

        /// <summary>
        /// Logs a warning to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The error text.</param>
        private static void LogWarning(string text)
        {
            RaspberryDebug.Log.Warning(text);
        }

        /// <summary>
        /// Logs an exception to the Visual Studio debug pane.
        /// </summary>
        /// <param name="e">The exception.</param>
        private new static void LogException(Exception e)
        {
            RaspberryDebug.Log.Exception(e);
        }

        //---------------------------------------------------------------------
        // Instance members

        /// <summary>
        /// Private constructor.
        /// </summary>
        /// <param name="name">The server name.</param>
        /// <param name="address">The IP address.</param>
        /// <param name="credentials">The SSH credentials.</param>
        /// <param name="port">OPtionall overrides the default SSH port (22).</param>
        /// <param name="logWriter">Optional log writer.</param>
        private PiConnection(string name, IPAddress address, SshCredentials credentials, int port = NetworkPorts.SSH, TextWriter logWriter = null) 
            : base(name, address, credentials, port, logWriter)
        {
        }

        /// <summary>
        /// Returns relevant status information for the remote Raspberry, including the
        /// chip architecture, <b>vsdbg</b> debugger status, as well as the installed
        /// .NET Core SDKs.
        /// </summary>
        public PiStatus PiStatus { get; private set; }

        /// <summary>
        /// Retrieves status information from the remote Raspberry and updates <see cref="PiStatus"/>.
        /// </summary>
        private void GetPiStatus()
        {
            Log($"[{Name}]: Retrieving status");

            // We're going to execute a script the gathers everything in a single operation for speed.

            var script =
$@"
# This script will return the status information via STDOUT line-by-line
# in this order:
#
#       Sudo Capability (""sudo"" or ""no-sudo"")
#       Chip Architecture
#       Unzip Installed (""unzip"" or ""unzip-missing"")
#       Debugger Installed (""debugger-installed"" or ""debugger-missing"")
#       Debugger Running (""debugger-running"" or ""debugger-unavailable"")
#       List of installed SDKs names (e.g. 3.1.108) separated by commas

# Set the SDK and debugger installation paths.

DOTNET_ROOT={PackageHelper.RemoteDotnetRootPath}
DEBUGFOLDER={PackageHelper.RemoteDebugPath}

# Detect whether the user can act as root.

if sudu --non-interactive su ; then
    echo 'sudo'
else
    echo 'no-sudo'
fi

# Get the chip architecture

uname -m

# Detect whether the [vsdbg] debugger is installed.

if [ -d $DEBUGFOLDER ] ; then
    echo 'debugger-installed'
else
    echo 'debugger-missing'
fi

# Detect whether [unzip] is installed.

if `which unzip` ; then
    echo 'unzip'
else
    echo 'unzip-missing'
fi

# Detect whether the [vsdbg] debugger is running.

if pidof vsdbg &> /dev/nul ; then
    echo 'debugger-running'
else
    echo 'debugger-unavailable'
fi

# List the SDK folders.  These folder names are the same as the
# corresponding SDK name.

ls -m $SDKFOLDER\sdk
";
            Log($"[{Name}]: Fetching status");

            var result = SudoCommand(script, RunOptions.None);

            if (result.ExitCode != 0)
            {
                LogError($"[{Name}]: {result.ErrorText}");
                PiStatus = new PiStatus();
            }
            else
            {
                using (var reader = new StringReader(result.OutputText))
                {
                    var sudoAllowed       = reader.ReadLine() == "sudo";
                    var architecture      = reader.ReadLine();
                    var hasUnzip          = reader.ReadLine() == "unzip";
                    var debuggerInstalled = reader.ReadLine() == "debugger-installed";
                    var debuggerRunning   = reader.ReadLine() == "debugger-running";
                    var sdkLine           = reader.ReadLine();
                    var debuggerStatus    = PiDebuggerStatus.NotInstalled;

                    if (debuggerRunning)
                    {
                        debuggerStatus = PiDebuggerStatus.Running;
                    }
                    else if (debuggerInstalled)
                    {
                        debuggerStatus = PiDebuggerStatus.Installed;
                    }

                    Log($"[{Name}]: Status: sudo allowed    = {sudoAllowed}");
                    Log($"[{Name}]: Status: architecture    = {architecture}");
                    Log($"[{Name}]: Status: debugger status = {debuggerStatus}");
                    Log($"[{Name}]: Status: installed sdks  = {sdkLine}");

                    // Convert the comma separated SDK names into a [PiSdk] list.

                    var sdks = new List<PiSdk>();

                    foreach (var sdkName in sdkLine.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Select(sdk => sdk.Trim()))
                    {
                        // $todo(jefflill): We're only supporting 32-bit SDKs for now.

                        var sdkCatalogItem = PackageHelper.SdkCatalog.Items.Single(item => item.Name == sdkName && item.Architecture == SdkArchitecture.ARM32);

                        if (sdkCatalogItem != null)
                        {
                            sdks.Add(new PiSdk(sdkName, sdkCatalogItem.Version));
                        }
                        else
                        {
                            LogWarning($".NET SDK [{sdkName}] is present on [{Name}] but is not known to the RaspberryDebug extension.  Consider updating the extension.");
                        }
                    }

                    PiStatus = new PiStatus(
                        sudoAllowed:    sudoAllowed,
                        architecture:   architecture,
                        hasUnzip:       hasUnzip,
                        debugger:       debuggerStatus,
                        installedSdks:  sdks);
                }
            }
        }
    }
}
