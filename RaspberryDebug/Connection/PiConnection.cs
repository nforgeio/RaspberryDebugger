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
        /// <returns>The connection or <c>null</c> if the connection could not be established..</returns>
        public static PiConnection Connect(Connection connectionInfo)
        {
            Covenant.Requires<ArgumentNullException>(connectionInfo != null, nameof(connectionInfo));

            try
            {
                if (!IPAddress.TryParse(connectionInfo.Host, out var address))
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

                connection.Connect();

                return connection;
            }
            catch (Exception e)
            {
                LogException(e);

                return null;
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

        /// <summary>
        /// Logs an error to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The error text.</param>
        private static void LogError(string text)
        {
            RaspberryDebug.Log.Error(text);
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


    }
}
