//-----------------------------------------------------------------------------
// FILE:	    Connection.cs
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;

using Neon.Common;
using Neon.IO;
using Neon.Net;
using Neon.SSH;
using RaspberryDebugger.Extensions;
using Renci.SshNet.Common;

namespace RaspberryDebugger
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
        /// <param name="usePassword">Optionally forces use of the password instead of the public key.</param>
        /// <param name="projectSettings">
        /// Optionally specifies the project settings.  This must be specified for connections that
        /// will be used for remote debugging but may be omitted for connections just used for setting
        /// things up like SSH keys, etc.
        /// </param>
        /// <returns>The connection.</returns>
        /// <exception cref="Exception">Thrown when the connection could not be established.</exception>
        public static async Task<Connection> ConnectAsync(ConnectionInfo connectionInfo, bool usePassword = false, ProjectSettings projectSettings = null)
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

                if (string.IsNullOrEmpty(connectionInfo.PrivateKeyPath) || usePassword)
                {
                    Log($"[{connectionInfo.Host}]: Auth via username/password");

                    credentials = SshCredentials.FromUserPassword(connectionInfo.User, connectionInfo.Password);
                }
                else
                {
                    Log($"[{connectionInfo.Host}]: Auth via SSH keys");

                    credentials = SshCredentials.FromPrivateKey(connectionInfo.User, File.ReadAllText(connectionInfo.PrivateKeyPath));
                }

                var connection = new Connection(connectionInfo.Host, address, connectionInfo, credentials, projectSettings);

                connection.Connect(TimeSpan.Zero);
                await connection.InitializeAsync();

                return connection;
            }
            catch (SshProxyException e)
            {
                if (usePassword ||
                    e.InnerException == null ||
                    e.InnerException.GetType() != typeof(SshAuthenticationException))
                {
                    RaspberryDebugger.Log.Exception(e, $"[{connectionInfo.Host}]");
                    throw;
                }

                if (string.IsNullOrEmpty(connectionInfo.PrivateKeyPath) ||
                    string.IsNullOrEmpty(connectionInfo.Password))
                {
                    RaspberryDebugger.Log.Exception(e, $"[{connectionInfo.Host}]: The connection must have a password or SSH private key.");
                    throw;
                }

                RaspberryDebugger.Log.Warning($"[{connectionInfo.Host}]: SSH auth failed: Try using the password and reauthorizing the public key");

                // SSH private key authentication didn't work.  This commonly happens
                // after the user has reimaged the Raspberry.  It's likely that the
                // user has setup the same username/password, so we'll try logging in
                // with those and just configure the current public key to be accepted
                // on the Raspberry.

                try
                {
                    var connection = await ConnectAsync(connectionInfo, usePassword: true);

                    // Append the public key to the user's [authorized_keys] file if it's
                    // not already present.

                    RaspberryDebugger.Log.Info($"[{connectionInfo.Host}]: Reauthorizing the public key");

                    var homeFolder = LinuxPath.Combine("/", "home", connectionInfo.User);
                    var publicKey  = File.ReadAllText(connectionInfo.PublicKeyPath).Trim();
                    var keyScript  =
                    $@"
                    mkdir -p {homeFolder}/.ssh
                    touch {homeFolder}/.ssh/authorized_keys

                    if ! grep --quiet '{publicKey}' {homeFolder}/.ssh/authorized_keys ; then
                        echo '{publicKey}' >> {homeFolder}/.ssh/authorized_keys
                        exit $?
                    fi

                    exit 0
                    ";

                    connection.ThrowOnError(connection.RunCommand(CommandBundle.FromScript(keyScript)));
                    return connection;
                }
                catch (Exception e2)
                {
                    // We've done all we can.

                    RaspberryDebugger.Log.Exception(e2, $"[{connectionInfo.Host}]");
                    throw;
                }
            }
            catch (Exception e)
            {
                RaspberryDebugger.Log.Exception(e, $"[{connectionInfo.Host}]");
                throw;
            }
        }

        /// <summary>
        /// Logs a line of text to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The text.</param>
        private new static void Log(string text = "")
        {
            if (string.IsNullOrEmpty(text))
            {
                RaspberryDebugger.Log.WriteLine(text);
            }
            else
            {
                RaspberryDebugger.Log.Info(text);
            }
        }

        //---------------------------------------------------------------------
        // Instance members

        private ConnectionInfo      connectionInfo;
        private ProjectSettings     projectSettings;

        /// <summary>
        /// Constructs a connection using a password.
        /// </summary>
        /// <param name="name">The server name.</param>
        /// <param name="address">The IP address.</param>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="credentials">The SSH credentials.</param>
        /// <param name="projectSettings">
        /// Optionally specifies the project settings.  This must be specified for connections that
        /// will be used for remote debugging but may be omitted for connections just used for setting
        /// things up like SSH keys, etc.
        /// </param>
        private Connection(string name, IPAddress address, ConnectionInfo connectionInfo, SshCredentials credentials, ProjectSettings projectSettings)
            : base(name, address, credentials, connectionInfo.Port, logWriter: null)
        {
            this.connectionInfo  = connectionInfo;
            this.projectSettings = projectSettings;

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
            RaspberryDebugger.Log.Info($"[{Name}]: {text}");
        }

        /// <summary>
        /// Logs an error to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The error text.</param>
        private void LogError(string text)
        {
            RaspberryDebugger.Log.Error($"[{Name}]: {text}");
        }

        /// <summary>
        /// Logs a warning to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The error text.</param>
        private void LogWarning(string text)
        {
            RaspberryDebugger.Log.Warning($"[{Name}]: {text}");
        }

        /// <summary>
        /// Logs an exception to the Visual Studio debug pane.
        /// </summary>
        /// <param name="e">The exception.</param>
        private new void LogException(Exception e)
        {
            RaspberryDebugger.Log.Exception(e, $"[{Name}]");
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
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task InitializeAsync()
        {
            await PackageHelper.ExecuteWithProgressAsync(
                $"Connecting to [{Name}]...",
                async () =>
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

                    var statusScript =
                    $@"
                    # This script will return the status information via STDOUT line-by-line
                    # in this order:
                    #
                    # Chip Architecture
                    # PATH environment variable
                    # Unzip Installed (""unzip"" or ""unzip-missing"")
                    # Debugger Installed (""debugger-installed"" or ""debugger-missing"")
                    # List of installed SDKs names (e.g. 3.1.108) separated by commas
                    # Raspberry Model like:     Raspberry Pi 4 Model B Rev 1.2
                    # Raspberry Revision like:  c03112
                    #
                    # This script also ensures that the [/lib/dotnet] directory exists, that
                    # it has reasonable permissions, and that the folder exists on the system
                    # PATH and that DOTNET_ROOT points to the folder.

                    # Set the SDK and debugger installation paths.

                    DOTNET_ROOT={PackageHelper.RemoteDotnetFolder}
                    DEBUGFOLDER={PackageHelper.RemoteDebuggerFolder}

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
                    # line if the SDK directory doesn't exist.

                    if [ -d $DOTNET_ROOT/sdk ] ; then
                        ls -m $DOTNET_ROOT/sdk
                    else
                        echo ''
                    fi

                    # Output the Raspberry board model.

                    cat /proc/cpuinfo | grep '^Model\s' | grep -o 'Raspberry.*$'

                    # Output the Raspberry board revision.

                    cat /proc/cpuinfo | grep 'Revision\s' | grep -o '[0-9a-fA-F]*$'

                    # Ensure that the [/lib/dotnet] folder exists, that it's on the
                    # PATH and that DOTNET_ROOT are defined.

                    mkdir -p /lib/dotnet
                    chown root:root /lib/dotnet
                    chmod 755 /lib/dotnet

                    # Set these for the current session:

                    export DOTNET_ROOT={PackageHelper.RemoteDotnetFolder}
                    export PATH=$PATH:$DOTNET_ROOT

                    # and for future sessions too:

                    if ! grep --quiet DOTNET_ROOT /etc/profile ; then

                        echo """"                                >> /etc/profile
                        echo ""#------------------------------"" >> /etc/profile
                        echo ""# Raspberry Debugger:""           >> /etc/profile
                        echo ""export DOTNET_ROOT=$DOTNET_ROOT"" >> /etc/profile
                        echo ""export PATH=$PATH""               >> /etc/profile
                        echo ""#------------------------------"" >> /etc/profile
                    fi
                    ";

                    Log($"[{Name}]: Fetching status");

                    response = ThrowOnError(SudoCommand(CommandBundle.FromScript(statusScript)));

                    using (var reader = new StringReader(response.OutputText))
                    {
                        var processor    = await reader.ReadLineAsync();
                        var path         = await reader.ReadLineAsync();
                        var hasUnzip     = await reader.ReadLineAsync() == "unzip";
                        var hasDebugger  = await reader.ReadLineAsync() == "debugger-installed";
                        var sdkLine      = await reader.ReadLineAsync();
                        var model        = await reader.ReadLineAsync();
                        var revision     = await reader.ReadToEndAsync();

                        revision = revision.Trim();     // Remove any whitespace at the end.

                        Log($"[{Name}]: processor: {processor}");
                        Log($"[{Name}]: path:      {path}");
                        Log($"[{Name}]: unzip:     {hasUnzip}");
                        Log($"[{Name}]: debugger:  {hasDebugger}");
                        Log($"[{Name}]: sdks:      {sdkLine}");
                        Log($"[{Name}]: model:     {model}");
                        Log($"[{Name}]: revision:  {revision}");

                        // raspberry pi platform architecture
                        var architecture = processor.Contains(Platform.Bitness32.GetAttributeOfType<EnumMemberAttribute>().Value) 
                            ? SdkArchitecture.ARM32 
                            : SdkArchitecture.ARM64;

                        // Convert the comma separated SDK names into a [PiSdk] list.
                        var sdks = new List<Sdk>();

                        foreach (var sdkName in sdkLine.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(sdk => sdk.Trim()))
                        {
                            var sdkCatalogItem = PackageHelper.SdkCatalog.Items.SingleOrDefault(item => item.Name == sdkName && item.Architecture == architecture);

                            if (sdkCatalogItem != null)
                            {
                                sdks.Add(new Sdk(sdkName, sdkCatalogItem.Version, architecture));
                            }
                            else
                            {
                                LogWarning($".NET SDK [{sdkName}] is present on [{Name}] but is not known to the RaspberryDebugger extension. Consider updating the extension.");
                            }
                        }

                        PiStatus = new Status(
                            processor:     processor,
                            path:          path,
                            hasUnzip:      hasUnzip,
                            hasDebugger:   hasDebugger,
                            installedSdks: sdks,
                            model:         model,
                            revision:      revision,
                            architecture:  architecture
                        );
                    }
                });

            // Create and configure an SSH key for this connection if one doesn't already exist.
            if (string.IsNullOrEmpty(connectionInfo.PrivateKeyPath) || !File.Exists(connectionInfo.PrivateKeyPath))
            {
                await PackageHelper.ExecuteWithProgressAsync("Creating SSH keys...",
                    async () =>
                    {
                        // Create a 2048-bit private key with no passphrase on the Raspberry
                        // and then download it to our keys folder.  The key file name will
                        // be the host name of the Raspberry.

                        LogInfo("Creating SSH keys");

                        var workstationUser    = Environment.GetEnvironmentVariable("USERNAME");
                        var workstationName    = Environment.GetEnvironmentVariable("COMPUTERNAME");
                        var keyName            = Guid.NewGuid().ToString("d");
                        var homeFolder         = LinuxPath.Combine("/", "home", connectionInfo.User);
                        var tempPrivateKeyPath = LinuxPath.Combine(homeFolder, keyName);
                        var tempPublicKeyPath  = LinuxPath.Combine(homeFolder, $"{keyName}.pub");

                        try
                        {
                            var createKeyScript =
                            $@"
                            # Create the key pair

                            if ! ssh-keygen -t rsa -b 2048 -P '' -C '{workstationUser}@{workstationName}' -f {tempPrivateKeyPath} -m pem ; then
                                exit 1
                            fi

                            # Append the public key to the user's [authorized_keys] file to enable it.

                            mkdir -p {homeFolder}/.ssh
                            touch {homeFolder}/.ssh/authorized_keys
                            cat {tempPublicKeyPath} >> {homeFolder}/.ssh/authorized_keys

                            exit 0
                            ";

                            ThrowOnError(RunCommand(CommandBundle.FromScript(createKeyScript)));

                            // Download the public and private keys, persist them to the workstation
                            // and then update the connection info.

                            var connections            = PackageHelper.ReadConnections();
                            var existingConnectionInfo = connections.SingleOrDefault(c => c.Name == connectionInfo.Name);
                            var publicKeyPath          = Path.Combine(PackageHelper.KeysFolder, $"{connectionInfo.Name}.pub");
                            var privateKeyPath         = Path.Combine(PackageHelper.KeysFolder, connectionInfo.Name);

                            File.WriteAllBytes(publicKeyPath, DownloadBytes(tempPublicKeyPath));
                            File.WriteAllBytes(privateKeyPath, DownloadBytes(tempPrivateKeyPath));

                            connectionInfo.PrivateKeyPath = privateKeyPath;
                            connectionInfo.PublicKeyPath  = publicKeyPath;

                            if (existingConnectionInfo != null)
                            {
                                existingConnectionInfo.PrivateKeyPath = privateKeyPath;
                                existingConnectionInfo.PublicKeyPath  = publicKeyPath;

                                PackageHelper.WriteConnections(connections, disableLogging: true);
                            }
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
        /// <param name="sdkVersion">The SDK version.</param>
        /// <returns><c>true</c> on success.</returns>
        public async Task<bool> InstallSdkAsync()
        {
            var sdkOnPi = PiStatus.InstalledSdks.FirstOrDefault();
            var sdkVersion = sdkOnPi?.Version ?? String.Empty;
            var sdkArchitecture = sdkOnPi?.Architecture ?? SdkArchitecture.ARM32;

            if (PiStatus.InstalledSdks.Any(sdk => sdk.Version == sdkVersion && sdk.Architecture == sdkArchitecture))
            {
                return await Task.FromResult(true);    // Already installed
            }
         
            // Locate the standalone SDK for the request .NET version.
            // Figure out the latest SDK version - Microsoft versioning: the highest number
            var targetSdk = PackageHelper.SdkGoodCatalog.Items
                .OrderByDescending(item => item.Version)
                .FirstOrDefault(item => item.Architecture == PiStatus.PiArchitecture);

            if (targetSdk == null)
            {
                LogError($"RasberryDebug is unaware of .NET Core SDK.");
                LogError($"Try updating the RasberryDebug extension or report this issue at:");
                LogError($"https://github.com/nforgeio/RaspberryDebugger/issues");

                return await Task.FromResult(false);
            }
            else
            {
                LogInfo($".NET Core SDK [v{targetSdk.Version}] is not installed.");
            }

            // Install the SDK.
            LogInfo($"Installing SDK v{targetSdk.Version}");

            return await PackageHelper.ExecuteWithProgressAsync<bool>($"Download and install SDK for .NET v{targetSdk.Version} on Raspberry...",
                async () =>
                {
                    var installScript =
                    $@"
                    export DOTNET_ROOT={PackageHelper.RemoteDotnetFolder}

                    # Ensure that the packages required by .NET Core are installed:
                    #
                    #       https://docs.microsoft.com/en-us/dotnet/core/install/linux-debian#dependencies

                    if ! apt-get update ; then
                        exit 1
                    fi

                    if ! apt-get install -yq libc6 libgcc1 libgssapi-krb5-2 libicu-dev libssl1.1 libstdc++6 zlib1g libgdiplus ; then
                        exit 1
                    fi

                    # Remove any existing SDK download.  This might be present if a
                    # previous installation attempt failed.

                    if ! rm -f /tmp/dotnet-sdk.tar.gz ; then
                        exit 1
                    fi

                    # Download the SDK installation file to a temporary file.

                    if ! wget --quiet -O /tmp/dotnet-sdk.tar.gz {targetSdk.Link} ; then
                        exit 1
                    fi

                    # Verify the SHA512.

                    orgDir=$cwd
                    cd /tmp

                    if ! echo '{targetSdk.SHA512}  dotnet-sdk.tar.gz' | sha512sum --check - ; then
                        cd $orgDir
                        exit 1
                    fi

                    cd $orgDir

                    # Make sure the installation directory exists.

                    if ! mkdir -p $DOTNET_ROOT ; then
                        exit 1
                    fi

                    # Unpack the SDK to the installation directory.

                    if ! tar -zxf /tmp/dotnet-sdk.tar.gz -C $DOTNET_ROOT --no-same-owner ; then
                        exit 1
                    fi

                    # Remove the temporary installation file.

                    if ! rm /tmp/dotnet-sdk.tar.gz ; then
                        exit 1
                    fi

                    exit 0
                    ";

                    try
                    {
                        var response = SudoCommand(CommandBundle.FromScript(installScript));

                        if (response.ExitCode == 0)
                        {
                            // Add the newly installed SDK to the list of installed SDKs.

                            PiStatus.InstalledSdks.Add(new Sdk(targetSdk.Name, targetSdk.Version, targetSdk.Architecture));
                            return await Task.FromResult(true);
                        }
                        else
                        {
                            LogError(response.AllText);
                            return await Task.FromResult(false);
                        }
                    }
                    catch (Exception e)
                    {
                        LogException(e);
                        return await Task.FromResult(false);
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
                return await Task.FromResult(true);
            }

            LogInfo($"Installing VSDBG to: [{PackageHelper.RemoteDebuggerFolder}]");

            return await PackageHelper.ExecuteWithProgressAsync<bool>($"Installing [vsdbg] debugger...",
                async () =>
                {
                    var installScript =
                    $@"
                    if ! curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l {PackageHelper.RemoteDebuggerFolder} ; then
                        exit 1
                    fi

                    exit 0
                    ";

                    try
                    {
                        var response = SudoCommand(CommandBundle.FromScript(installScript));

                        if (response.ExitCode == 0)
                        {
                            // Indicate that debugger is now installed.

                            PiStatus.HasDebugger = true;
                            return await Task.FromResult(true);
                        }
                        else
                        {
                            LogError(response.AllText);
                            return await Task.FromResult(false);
                        }
                    }
                    catch (Exception e)
                    {
                        LogException(e);
                        return await Task.FromResult(false);
                    }
                });
        }

        /// <summary>
        /// Uploads the files for the program being debugged to the Raspberry, replacing
        /// any existing files.
        /// </summary>
        /// <param name="programName">The program name</param>
        /// <param name="assemblyName">The addembly name.</param>
        /// <param name="publishedBinaryFolder">Path to the workstation folder holding the program files.</param>
        /// <returns><c>true</c> on success.</returns>
        public async Task<bool> UploadProgramAsync(string programName, string assemblyName, string publishedBinaryFolder)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(programName), nameof(programName));
            Covenant.Requires<ArgumentException>(!programName.Contains(' '), nameof(programName));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(assemblyName), nameof(assemblyName));
            Covenant.Requires<ArgumentException>(!assemblyName.Contains(' '), nameof(assemblyName));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(publishedBinaryFolder), nameof(publishedBinaryFolder));
            Covenant.Requires<ArgumentNullException>(Directory.Exists(publishedBinaryFolder), nameof(publishedBinaryFolder));

            // We're going to ZIP the program files locally and then transfer the zipped
            // files to the Raspberry to be expanded there.

            var debugFolder = LinuxPath.Combine(PackageHelper.RemoteDebugBinaryRoot(Username), programName);
            var groupScript = string.Empty;

            if (!string.IsNullOrEmpty(projectSettings.TargetGroup))
            {
                groupScript =
                $@"
                # Add the program assembly to the user specified target group (if any).  This
                # defaults to [gpio] so users will be able to access the GPIO pins.

                if ! chgrp {projectSettings.TargetGroup} {debugFolder}/{assemblyName} ; then
                    exit 1
                fi
                ";
            }

            var uploadScript =
            $@"

            # Ensure that the debug folder exists.

            if ! mkdir -p {debugFolder} ; then
                exit 1
            fi

            # Clear all existing program files.

            if ! rm -rf {debugFolder}/* ; then
                exit 1
            fi

            # Unzip the binary and other files to the debug folder.

            if ! unzip program.zip -d {debugFolder} ; then
                exit 1
            fi

            # The program assembly needs execute permissions.

            if ! chmod 770 {debugFolder}/{assemblyName} ; then
                exit 1
            fi
            {groupScript}
            exit 0
            ";

            // I'm not going to do a progress dialog because this should be fast.
            try
            {
                LogInfo($"Uploading program to: [{debugFolder}]");

                var bundle = new CommandBundle(uploadScript);

                bundle.AddZip("program.zip", publishedBinaryFolder);

                var response = RunCommand(bundle);

                if (response.ExitCode == 0)
                {
                    LogInfo($"Program uploaded");
                    return await Task.FromResult(true);
                }
                else
                {
                    LogError(response.AllText);
                    return await Task.FromResult(false);
                }
            }
            catch (Exception e)
            {
                LogException(e);
                return await Task.FromResult(false);
            }
        }
    }
}
