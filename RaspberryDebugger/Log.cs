//-----------------------------------------------------------------------------
// FILE:	    Log.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2024 by neonFORGE, LLC.  All rights reserved.
//
// Obtained from a Microsoft samples project:
//
//      https://github.com/microsoft/VSSDK-Extensibility-Samples/blob/master/Options/src/Options/BaseOptionModel.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

using Task = System.Threading.Tasks.Task;

namespace RaspberryDebugger
{
    /// <summary>
    /// Logs to the Visual Studio debug log output.  This class is a convenient wrapper around the
    /// <see cref="RaspberryDebuggerPackage.Log(string)"/> method.
    /// </summary>
    internal static class Log
    {
        /// <summary>
        /// Writes text to the debug pane.
        /// </summary>
        /// <param name="text">The text text.</param>
        public static void Write(string text)
        {
            RaspberryDebuggerPackage.Log(text);
        }

        /// <summary>
        /// Writes a line of text to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">Optionally specifies the log text.</param>
        public static void WriteLine(string text = "")
        {
            RaspberryDebuggerPackage.Log(text + "\n");
        }

        /// <summary>
        /// Writes an information line to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The information text.</param>
        public static void Info(string text)
        {
            WriteLine($"[{PackageHelper.LogName}]: INFO: {text}");
        }

        /// <summary>
        /// Writes an error line to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The error text.</param>
        public static void Error(string text)
        {
            WriteLine($"[{PackageHelper.LogName}]: ERROR: {text}");
        }

        /// <summary>
        /// Writes a warning line to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The error text.</param>
        public static void Warning(string text)
        {
            WriteLine($"[{PackageHelper.LogName}]: WARNING: {text}");
        }

        /// <summary>
        /// Writes an exception to the Visual Studio debug pane.
        /// </summary>
        /// <param name="e">The exception.</param>
        /// <param name="message">Optional text to include before the exception information.</param>
        public static void Exception(Exception e, string message = null)
        {
            if (e == null)
            {
                return;
            }

            // We're going to build a multi-line message to reduce pressure
            // on the task/threading in [RaspberryDebugPackage.Log()].

            var sb = new StringBuilder();

            sb.Append("\n");

            if (string.IsNullOrEmpty(message))
            {
                sb.Append($"EXCEPTION: {e.GetType().FullName}: {e.Message}\n");
            }
            else
            {
                sb.Append($"EXCEPTION: {e.GetType().FullName}: {message} {e.Message}\n");
            }

            sb.Append(e.StackTrace);
            sb.Append("\n");

            RaspberryDebuggerPackage.Log(sb.ToString());
        }
    }
}
