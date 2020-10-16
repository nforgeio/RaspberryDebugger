//-----------------------------------------------------------------------------
// FILE:	    ConnectionException.cs
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
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using Neon.Common;
using Neon.Net;
using Neon.SSH;

namespace RaspberryDebugger
{
    /// <summary>
    /// Used to report connection issues.
    /// </summary>
    internal class ConnectionException : Exception
    {
        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Converts a connection and error message into an exception message.
        /// </summary>
        /// <param name="connection">The offending connection.</param>
        /// <param name="error">The error message.</param>
        /// <returns>The exception message.</returns>
        private static string GetMessage(Connection connection, string error)
        {
            var name = connection?.Name ?? "????";
            
            error = error ?? "unspecified error";

            return $"[{name}]: {error}";
        }

        /// <summary>
        /// Converts a connection name and error message into an exception message.
        /// </summary>
        /// <param name="name">The offending connection name.</param>
        /// <param name="error">The error message.</param>
        /// <returns>The exception message.</returns>
        private static string GetMessage(string name, string error)
        {
            name  = name ?? "????";
            error = error ?? "unspecified error";

            return $"[{name}]: {error}";
        }

        //---------------------------------------------------------------------
        // Instance members

        /// <summary>
        /// Constructs an instance based on a <see cref="Connection"/> and
        /// an error message.
        /// </summary>
        /// <param name="connection">The offending connection.</param>
        /// <param name="error">The error message.</param>
        public ConnectionException(Connection connection, string error)
            : base(GetMessage(connection, error))
        {
        }

        /// <summary>
        /// Constructs an instance based on a connection name and
        /// error message.
        /// </summary>
        /// <param name="name">The offending connection name.</param>
        /// <param name="error">The error message.</param>
        public ConnectionException(string name, string error)
            : base(GetMessage(name,error))
        {
        }
    }
}
