//-----------------------------------------------------------------------------
// FILE:	    Connection.cs
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
using Microsoft.VisualStudio.Debugger.Breakpoints;
using Neon.Common;
using Neon.IO;
using Neon.Net;
using Neon.SSH;
using Renci.SshNet;

namespace RaspberryDebug
{
    /// <summary>
    /// Implements a SSH connection to the remote Raspberry Pi.
    /// </summary>
    internal class Connection : LinuxSshProxy
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
        public static async Task<Connection> ConnectAsync(ConnectionInfo connectionInfo)
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
                    throw new ConnectionException(connectionInfo.Host, "DNS lookup failed.");
                }

                SshCredentials credentials;

                if (string.IsNullOrEmpty(connectionInfo.KeyPath))
                {
                    credentials = SshCredentials.FromUserPassword(connectionInfo.User, connectionInfo.Password);
                }
                else
                {
                    credentials = SshCredentials.FromPrivateKey(connectionInfo.User, File.ReadAllText(connectionInfo.KeyPath));
                }

                var connection = new Connection(connectionInfo.Host, address, connectionInfo, credentials);

                connection.Connect(TimeSpan.Zero);
                await connection.InitializeAsync();

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

        private string host;
        private string username;
        private string password;
        private string keyPath;

        /// <summary>
        /// Constructs a connection using a password.
        /// </summary>
        /// <param name="name">The server name.</param>
        /// <param name="address">The IP address.</param>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="credentials">The SSH credentials.</param>
        private Connection(string name, IPAddress address, ConnectionInfo connectionInfo, SshCredentials credentials)
            : base(name, address, credentials, connectionInfo.Port, logWriter: null)
        {
            this.host     = connectionInfo.Host;
            this.username = connectionInfo.User;
            this.password = connectionInfo.Password;
            this.keyPath  = connectionInfo.KeyPath;

            // Disable connection level logging, etc.

            DefaultRunOptions = RunOptions.None;
        }

        /// <summary>
        /// Returns relevant status information for the remote Raspberry, including the
        /// chip architecture, <b>vsdbg</b> debugger status, as well as the installed
        /// .NET Core SDKs.
        /// </summary>
        public Status PiStatus { get; private set; }

        /// <summary>
        /// Logs info the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The error text.</param>
        private void LogInfo(string text)
        {
            RaspberryDebug.Log.WriteLine($"[{Name}]: {text}");
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
        /// Throws a <see cref="ConnectionException"/> if a remote command failed.
        /// </summary>
        /// <param name="commandResponse">The remote command response.</param>
        /// <returns>The <see cref="CommandResponse"/> on success.</returns>
        private CommandResponse ThrowOnError(CommandResponse commandResponse)
        {
            Covenant.Requires<ArgumentNullException>(commandResponse != null, nameof(commandResponse));

            if (commandResponse.ExitCode != 0)
            {
                throw new ConnectionException(this, commandResponse.ErrorText);
            }

            return commandResponse;
        }

        /// <summary>
        /// Initializes the connection by retrieving status from the remote Raspberry and ensuring
        /// that any packages required for executing remote commands are installed.  This will also
        /// create and configure a SSH key pair on both the workstation and remote Raspberry if one
        /// doesn't already exist so that subsequent connections can use key based authentication.
        /// </summary>
        /// <returns>Thr tracking <see cref="Task"/>.</returns>
        private async Task InitializeAsync()
        {
            // Disabling this because it looks like SUDO passwork prompting is disabled
            // by default for Raspberry Pi OS.
#if DISABLED
            // This call ensures that SUDO password prompting is disabled and the
            // the required hidden folders exist in the user's home directory.

            DisableSudoPrompt(password);
#endif
            // We need to ensure that [unzip] is installed so that [LinuxSshProxy] command
            // bundles will work.

            Log($"[{Name}]: Checking for: [unzip]");

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
# Chip Architecture
# PATH environment variable
# Unzip Installed (""unzip"" or ""unzip-missing"")
# Debugger Installed (""debugger-installed"" or ""debugger-missing"")
# Debugger Running (""debugger-running"" or ""debugger-not-running"")
# List of installed SDKs names (e.g. 3.1.108) separated by commas
#
# This script also ensures that the [/lib/dotnet] directory exists, that
# it has reasonable permissions, and that the folder exists on the system
# PATH and that DOTNET_ROOT points to the folder.

# Set the SDK and debugger installation paths.

DOTNET_ROOT={PackageHelper.RemoteDotnetRootPath}
DEBUGFOLDER={PackageHelper.RemoteDebugRoot}

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

# List the SDK folders.  These folder names are the same as the
# corresponding SDK name.  We'll list the files on one line
# with the SDK names separated by commas.  We'll return a blank
#line if the SDK directory doesn't exist.

if [ -d $DOTNET_ROOT/sdk ] ; then
    ls -m $DOTNET_ROOT/sdk
else
    echo ''
fi

# Ensure that the [/lib/dotnet] folder exists, that its on the 
# PATH and that DOTNET_ROOT is defined.

mkdir -p /lib/dotnet
chown root:root /lib/dotnet
chmod 755 /lib/dotnet

if ! grep --quiet DOTNET_ROOT /etc/profile ; then

    echo ''                                >> /etc/profile
    echo 'export DOTNET_ROOT=$DOTNET_ROOT' >> /etc/profile
    echo 'export PATH=$PATH:$DOTNET_ROOT'  >> /etc/profile

# Set these for the current session too:

    export DOTNET_ROOT=$DOTNET_ROOT
    export PATH=$PATH:$DOTNET_ROOT
fi
";
            Log($"[{Name}]: Fetching status");

            response = ThrowOnError(SudoCommand(CommandBundle.FromScript(script)));

            using (var reader = new StringReader(response.OutputText))
            {
                var architecture = await reader.ReadLineAsync();
                var path         = await reader.ReadLineAsync();
                var hasUnzip     = await reader.ReadLineAsync() == "unzip";
                var hasDebugger  = await reader.ReadLineAsync() == "debugger-installed";
                var sdkLine      = await reader.ReadLineAsync();

                Log($"[{Name}]: architecture: {architecture}");
                Log($"[{Name}]: path:         {path}");
                Log($"[{Name}]: unzip:        {hasUnzip}");
                Log($"[{Name}]: debugger:     {hasDebugger}");
                Log($"[{Name}]: sdks:         {sdkLine}");

                // Convert the comma separated SDK names into a [PiSdk] list.

                var sdks = new List<Sdk>();

                foreach (var sdkName in sdkLine.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(sdk => sdk.Trim()))
                {
                    // $todo(jefflill): We're only supporting 32-bit SDKs at this time.

                    var sdkCatalogItem = PackageHelper.SdkCatalog.Items.SingleOrDefault(item => item.Name == sdkName && item.Architecture == SdkArchitecture.ARM32);

                    if (sdkCatalogItem != null)
                    {
                        sdks.Add(new Sdk(sdkName, sdkCatalogItem.Version));
                    }
                    else
                    {
                        LogWarning($".NET SDK [{sdkName}] is present on [{Name}] but is not known to the RaspberryDebug extension.  Consider updating the extension.");
                    }
                }

                PiStatus = new Status(
                    architecture:  architecture,
                    path:          path,
                    hasUnzip:      hasUnzip,
                    hasDebugger:   hasDebugger,
                    installedSdks: sdks);
            }

            // Create and configure an SSH key for this connection if one doesn't already exist.

            if (string.IsNullOrEmpty(keyPath) || !File.Exists(keyPath))
            {
                await ProgressDialog.RunAsync("Create SSH Key Pair", 60,
                    async () =>
                    {
                        // Create a 2048-bit private key with no passphrase on the Raspberry
                        // and then download it to our keys folder.  The key file name will
                        // be the host name of the Raspberry.

                        LogInfo("Configuring SSH key pair");

                        var workstationUser    = Environment.GetEnvironmentVariable("USERNAME");
                        var workstationName    = Environment.GetEnvironmentVariable("COMPUTERNAME");
                        var keyName            = Guid.NewGuid().ToString("d");
                        var homeFolder         = LinuxPath.Combine("/", "home", username);
                        var tempPrivateKeyPath = LinuxPath.Combine(homeFolder, keyName);
                        var tempPublicKeyPath  = LinuxPath.Combine(homeFolder, $"{keyName}.pub");

                        try
                        {
                            var createKeyScript =
$@"
# Create the key pair

if ! ssh-keygen -t rsa -b 2048 -N '' -C '{workstationUser}@{workstationName}' -f {tempPrivateKeyPath} ; then
exit 1
fi

# Append the public key to the users [authorized_files].

touch {homeFolder}/.ssh/authorized_files
cat {tempPublicKeyPath} >> {homeFolder}/.ssh/authorized_files

exit 0
";
                            ThrowOnError(SudoCommand(CommandBundle.FromScript(createKeyScript)));

                            // Download the public key, persist it to the workstation and then update the
                            // workstation connections.

                            var connections = PackageHelper.ReadConnections();
                            var connection  = connections.SingleOrDefault(c => c.Host == host);

                            if (connection == null)
                            {
                                // Another instance of VS must have deleted this connection 
                                // out from under us.

                                throw new ConnectionException(this, $"The [{host}] connection no longer exists.  You'll need to recreate it.");
                            }

                            var privateKeyPath = Path.Combine(PackageHelper.KeysFolder, host);

                            File.WriteAllBytes(privateKeyPath, DownloadBytes(tempPrivateKeyPath));

                            connection.KeyPath  = privateKeyPath;
                            connection.Password = null;     // We don't need the password any longer

                            PackageHelper.WriteConnections(connections);
                        }
                        finally
                        {
                            // Delete the temporary key files on the Raspberry.

                            var removeKeyScript =
$@"
rm -f {tempPrivateKeyPath}
rm -f {tempPublicKeyPath}
";
                            ThrowOnError(SudoCommand(CommandBundle.FromScript(removeKeyScript)));
                        }

                        await Task.CompletedTask;
                    });
            }
        }

        /// <summary>
        /// Installs the specified .NET Core SDK on the Raspberry if it's not already installed.
        /// </summary>
        /// <param name="sdkItem">The SDK catalog information.</param>
        /// <returns><c>true</c> on success.</returns>
        public async Task<bool> InstallSdkAsync(SdkCatalogItem sdkItem)
        {
            Covenant.Requires<ArgumentNullException>(sdkItem != null, nameof(sdkItem));

            // $todo(jefflill):
            //
            // Note that we're going to install that standalone SDK for the SDK
            // version rather than the SDK that shipped with Visual Studio.  I'm 
            // assuming that the Visual Studio SDKs might have extra stuff we don't
            // need and it's also possible that the Visual Studio SDK for the SDK
            // version may not have shipped yet.
            //
            // We may want to re-evaluate this in the future.

            if (PiStatus.InstalledSdks.Any(sdk => sdk.Version == sdkItem.Version))
            {
                return true;    // Already installed
            }

            LogInfo($".NET Core SDK [v{sdkItem.Version}] is not installed.");

            // Locate the standalone SDK for the request .NET version.  Note that
            // standalone SDKs seem to have patch number is less than 200.

            var targetSdk = PackageHelper.SdkCatalog.Items.SingleOrDefault(item => item.Version == sdkItem.Version && SemanticVersion.Parse(sdkItem.Version).Patch < 200);

            if (targetSdk == null)
            {
                // Fall back to the Visual Studio SDK, if there is one.

                targetSdk = PackageHelper.SdkCatalog.Items.SingleOrDefault(item => item.Version == sdkItem.Version);
                LogInfo($"Cannot find SDK [{sdkItem.Name}] for falling back to [{targetSdk.Name}], version [v{targetSdk.Version}].");
            }

            if (targetSdk == null)
            {
                LogError($"RasberryDebug is unaware of the [{sdkItem.Name}/{sdkItem.Version}] .NET Core SDK.");
                LogError($"Try updating the RasberryDebug extension or report this issue at:");
                LogError($"https://github.com/jefflill/RaspberryDebug/issues/");

                return false;
            }

            // Install the SDK.

            LogInfo($"Installing SDK v{targetSdk.Version} on Raspberry");

            return await ProgressDialog.RunAsync<bool>($"Installing SDK v{targetSdk.Version}", 60,
                async () =>
                {
                    var installScript =
$@"
if ! rm $TMP/dotnet-sdk.tar.gz ; then
    exit 1
fi

if ! wget -O $TMP/dotnet-sdk.tar.gz {targetSdk.Link} ; then
    exit 1
fi

if ! tar zxf $TMP/dotnet-sdk.tar.gz -C $DOTNET_ROOT ; then
    exit 1
fi

if ! rm $TMP/dotnet-sdk.tar.gz ; then
    exit 1
fi

exit 0
";
                    try
                    {
                        return await Task<bool>.Run(() =>
                        {
                            var response = SudoCommand(CommandBundle.FromScript(installScript));

                            if (response.ExitCode == 0)
                            {
                                // Add the newly installed SDK to the list of installed SDKs.

                                PiStatus.InstalledSdks.Add(new Sdk(targetSdk.Name, targetSdk.Version));
                                return true;
                            }
                            else
                            {
                                LogError(response.AllText);
                                return false;
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        LogException(e);
                        return false;
                    }
                });
        }

        /// <summary>
        /// Installs the <b>vsdbg</b> debugger on the Raspberry if it's not already installed.
        /// </summary>
        /// <returns><c>true</c> on success.</returns>
        public async Task<bool> InstallDebuggerAsync()
        {
            if (PiStatus.HasDebugger)
            {
                return true;
            }

            return await ProgressDialog.RunAsync<bool>($"Installing [vsdbg] debugger", 60,
                async () =>
                {
                    var installScript =
$@"
if ! curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l /lib/dotnet/vsdbg ; then
    exit 1
fi

exit 0
";
                    try
                    {
                        return await Task<bool>.Run(() =>
                        {
                            var response = SudoCommand(CommandBundle.FromScript(installScript));

                            if (response.ExitCode == 0)
                            {
                                // Indicate that debugger is now installed.

                                PiStatus.HasDebugger = true;
                                return true;
                            }
                            else
                            {
                                LogError(response.AllText);
                                return false;
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        LogException(e);
                        return false;
                    }
                });
        }

        /// <summary>
        /// Uploads the files for the program being debugged to the Raspberry, replacing
        /// any existing files.
        /// </summary>
        /// <param name="programName">The program name</param>
        /// <param name="programFolder">Path to the folder holding the program files.</param>
        /// <returns><c>true</c> on success.</returns>
        public async Task<bool> UploadProgramAsync(string programName, string programFolder)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(programName), nameof(programName));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(programFolder), nameof(programFolder));
            Covenant.Requires<ArgumentNullException>(Directory.Exists(programFolder), nameof(programFolder));

            // Replace any spaces in the program name with underscores so we don't
            // have to worry about quoting.

            programName = programName.Replace(' ', '_');

            // We're going to ZIP the program files locally and then transfer the zipped
            // files to the Raspberry to be expanded there.

            var debugFolder  = LinuxPath.Combine(PackageHelper.RemoteDebugRoot, programName);
            var deployScript =
$@"
if ! rm -rf {debugFolder} ; then
    exit 1
fi

if ! mkdir -p {debugFolder} ; then
    exit 1
fi

if ! unzip program.zip -o -d {debugFolder} ; then
    exit 1
fi

exit 0
";
            // I'm not going to do a progress dialog because this should be fast.

            try
            {
                return await Task<bool>.Run(() =>
                {
                    LogInfo($"Uploading program to: [{debugFolder}]");

                    var bundle = new CommandBundle(deployScript);

                    bundle.AddZip("program.zip", programFolder);

                    var response = SudoCommand(bundle);

                    if (response.ExitCode == 0)
                    {
                        LogInfo($"Program uploaded");
                        return true;
                    }
                    else
                    {
                        LogError(response.AllText);
                        return false;
                    }
                });
            }
            catch (Exception e)
            {
                LogException(e);
                return false;
            }
        }
    }
}
