//-----------------------------------------------------------------------------
// FILE:	    Log.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Open Source
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

using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace RaspberryDebug
{
    /// <summary>
    /// Logs to the Visual Studio debug log output.  This class is a convenient wrapper around the
    /// <see cref="RaspberryDebugPackage.Log(string)"/> method.
    /// </summary>
    internal static class Log
    {
        /// <summary>
        /// Writes text to the debug pane.
        /// </summary>
        /// <param name="text">The text text.</param>
        public static void Write(string text)
        {
            RaspberryDebugPackage.Log(text);
        }

        /// <summary>
        /// Writes a line of text to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">Optionally specifies the log text.</param>
        public static void WriteLine(string text = "")
        {
            RaspberryDebugPackage.Log(text + "\n");
        }

        /// <summary>
        /// Writes an error line to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The error text.</param>
        public static void Error(string text)
        {
            WriteLine($"ERROR: {text}");
        }

        /// <summary>
        /// Writes a warning line to the Visual Studio debug pane.
        /// </summary>
        /// <param name="text">The error text.</param>
        public static void Warning(string text)
        {
            WriteLine($"WARNING: {text}");
        }

        /// <summary>
        /// Writes an exception to the Visual Studio debug pane.
        /// </summary>
        /// <param name="e">The exception.</param>
        public static void Exception(Exception e)
        {
            if (e == null)
            {
                return;
            }

            // We're going to build a multi-line message to reduce pressure
            // on the task/threading in [RaspberryDebugPackage.Log()].

            var sb = new StringBuilder();

            sb.Append("\n");
            sb.Append($"EXCEPTION: {e.GetType().FullName}: {e.Message}\n");
            sb.Append(e.StackTrace);
            sb.Append("\n");

            RaspberryDebugPackage.Log(sb.ToString());
        }
    }
}
