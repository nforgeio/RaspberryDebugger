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
using System.Threading.Tasks;

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
        public static async Task<PiConnection> ConnectAsync(Connection connectionInfo)
        {
            Covenant.Requires<ArgumentNullException>(connectionInfo != null, nameof(connectionInfo));

            try
            {
                if (!NetHelper.TryParseIPv4Address(connectionInfo.Host, out var address))
                {
                    Log($"DNS lookup for: {connectionInfo.Host}");

                    address = (await Dns.GetHostAddressesAsync(connectionInfo.Host)).FirstOrDefault();
                }

                if (address == null)
                {
                    throw new ConnectException(connectionInfo.Host, "DNS lookup failed.");
                }

                var connection = new PiConnection(connectionInfo.Host, address, connectionInfo.User, connectionInfo.Password, connectionInfo.Port);

                connection.Connect(TimeSpan.Zero);
                connection.Initialize();

                return connection;
            }
            catch (Exception e)
            {
                RaspberryDebug.Log.Exception(e, $"[{connectionInfo.Host}]");
                throw;
            }
        }

        /// <summary>
        /// Logs a line of text to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The text.</param>
        private new static void Log(string text = "")
        {
            RaspberryDebug.Log.WriteLine(text);
        }

        //---------------------------------------------------------------------
        // Instance members

        private string username;
        private string password;

        /// <summary>
        /// Private constructor.
        /// </summary>
        /// <param name="name">The server name.</param>
        /// <param name="address">The IP address.</param>
        /// <param name="username">The SSH username.</param>
        /// <param name="password">The SSH password.</param>
        /// <param name="port">OPtionall overrides the default SSH port (22).</param>
        private PiConnection(string name, IPAddress address, string username, string password, int port = NetworkPorts.SSH) 
            : base(name, address, SshCredentials.FromUserPassword(username, password), port, logWriter: null)
        {
            this.username = username;
            this.password = password;

            // Disable connection level logging, etc.

            DefaultRunOptions = RunOptions.None;
        }

        /// <summary>
        /// Returns relevant status information for the remote Raspberry, including the
        /// chip architecture, <b>vsdbg</b> debugger status, as well as the installed
        /// .NET Core SDKs.
        /// </summary>
        public PiStatus PiStatus { get; private set; }

        /// <summary>
        /// Logs info the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The error text.</param>
        private void LogInfo(string text)
        {
            RaspberryDebug.Log.Write($"[{Name}]: {text}");
        }

        /// <summary>
        /// Logs an error to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The error text.</param>
        private void LogError(string text)
        {
            RaspberryDebug.Log.Error($"[{Name}]: {text}");
        }

        /// <summary>
        /// Logs a warning to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The error text.</param>
        private void LogWarning(string text)
        {
            RaspberryDebug.Log.Warning($"[{Name}]: {text}");
        }

        /// <summary>
        /// Logs an exception to the Visual Studio debug pane.
        /// </summary>
        /// <param name="e">The exception.</param>
        private new void LogException(Exception e)
        {
            RaspberryDebug.Log.Exception(e, $"[{Name}]");
        }

        /// <summary>
        /// Throws a <see cref="ConnectException"/> if a remote command failed.
        /// </summary>
        /// <param name="commandResponse">The remote command response.</param>
        /// <returns>The <see cref="CommandResponse"/> on success.</returns>
        private CommandResponse ThrowOnError(CommandResponse commandResponse)
        {
            Covenant.Requires<ArgumentNullException>(commandResponse != null, nameof(commandResponse));

            if (commandResponse.ExitCode != 0)
            {
                throw new ConnectException(this, commandResponse.ErrorText);
            }

            return commandResponse;
        }

        /// <summary>
        /// Initializes the connection by retrieving status from the remote Raspberry and ensuring
        /// that any packages required for executing remote commands are installed.
        /// </summary>
        private void Initialize()
        {
            // This call ensures that SUDO password prompting is disabled and the
            // the required hidden folders exist in the user's home directory.

            DisableSudoPrompt(password);

            // We need to ensure that [unzip] is installed so that [LinuxSshProxy] command
            // bundles will work.

            Log($"[{Name}]: Checking: [unzip]");

            var response = SudoCommand("which unzip");

            if (response.ExitCode != 0)
            {
                Log($"[{Name}]: Installing: [unzip]");

                ThrowOnError(SudoCommand("sudo apt-get update"));
                ThrowOnError(SudoCommand("sudo apt-get install -yq unzip"));
            }

            // We're going to execute a script the gathers everything in a single operation for speed.

            Log($"[{Name}]: Retrieving status");

            var script =
$@"
# This script will return the status information via STDOUT line-by-line
# in this order:
#
#       Chip Architecture
#       PATH environment variable
#       Unzip Installed (""unzip"" or ""unzip-missing"")
#       Debugger Installed (""debugger-installed"" or ""debugger-missing"")
#       Debugger Running (""debugger-running"" or ""debugger-not-running"")
#       List of installed SDKs names (e.g. 3.1.108) separated by commas
#
# This script also ensures that the [/lib/dotnet] directory exists, that
# it has reasonable permissions, and that the folder exists on the system
# PATH and that DOTNET_ROOT points to the folder.

# Set the SDK and debugger installation paths.

DOTNET_ROOT={PackageHelper.RemoteDotnetRootPath}
DEBUGFOLDER={PackageHelper.RemoteDebugPath}

# Get the chip architecture

uname -m

# Get the current PATH

echo $PATH

# Detect whether [unzip] is installed.

if which unzip &> /dev/nul ; then
    echo 'unzip'
else
    echo 'unzip-missing'
fi

# Detect whether the [vsdbg] debugger is installed.

if [ -d $DEBUGFOLDER ] ; then
    echo 'debugger-installed'
else
    echo 'debugger-missing'
fi

# Detect whether the [vsdbg] debugger is running.

if pidof vsdbg &> /dev/nul ; then
    echo 'debugger-running'
else
    echo 'debugger-unavailable'
fi

# Ensure that the [/lib/dotnet] folder exists, is on the PATH
# and DOTNET_ROOT is defined.

sudo mkdir -p /lib/dotnet
sudo chown root:root /lib/dotnet
sudo chmod 755 /lib/dotnet

if ! sudo grep DOTNET_ROOT /etc/profile ; then

    sudo echo ''                                >> /etc/profile
    sudo echo 'export DOTNET_ROOT=$DOTNET_ROOT' >> /etc/profile
    sudo echo 'export PATH=$PATH:$DOTNET_ROOT'  >> /etc/profile

    # Set these for the current session too:

    export DOTNET_ROOT=$DOTNET_ROOT
    export PATH=$PATH:$DOTNET_ROOT
fi

# List the SDK folders.  These folder names are the same as the
# corresponding SDK name.  We'll list the files on one line
# with the SDK names separated by commas.  We'll return a blank
# line if the SDK directory doesn't exist.

if [ -d $DOTNET_ROOT/sdk ] ; then
    ls -m $DOTNET_ROOT/sdk
else
    echo ''
fi
";
            Log($"[{Name}]: Fetching status");

            response = ThrowOnError(SudoCommand(CommandBundle.FromScript(script)));

            using (var reader = new StringReader(response.OutputText))
            {
                var architecture      = reader.ReadLine();
                var path              = reader.ReadLine();
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

                Log($"[{Name}]: architecture    = {architecture}");
                Log($"[{Name}]: PATH            = {path}");
                Log($"[{Name}]: debugger status = {debuggerStatus}");
                Log($"[{Name}]: installed sdks  = {sdkLine}");

                // Convert the comma separated SDK names into a [PiSdk] list.

                var sdks = new List<PiSdk>();

                foreach (var sdkName in sdkLine.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(sdk => sdk.Trim()))
                {
                    // $todo(jefflill): We're only supporting 32-bit SDKs at this time.

                    var sdkCatalogItem = PackageHelper.SdkCatalog.Items.SingleOrDefault(item => item.Name == sdkName && item.Architecture == SdkArchitecture.ARM32);

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
                    architecture:   architecture,
                    path:           path,
                    hasUnzip:       hasUnzip,
                    debugger:       debuggerStatus,
                    installedSdks:  sdks);
            }
        }
    }
}
