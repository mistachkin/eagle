/*
 * Shell.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Shell
{
    /// <summary>
    /// This class provides the managed assembly entry point used when the
    /// Eagle core library is executed directly as a stand-alone shell
    /// application.
    /// </summary>
    [ObjectId("2a3e66d9-28fb-4c31-95e6-0132aab6c127")]
    internal static class Shell
    {
        #region Assembly Entry Point Method
        /// <summary>
        /// This method is the managed entry point for the stand-alone shell.
        /// It forwards the command line arguments to the interpreter shell and
        /// returns the resulting process exit code.
        /// </summary>
        /// <param name="args">
        /// The array of command line arguments passed to the process.
        /// </param>
        /// <returns>
        /// The integer process exit code produced by the interpreter shell.
        /// </returns>
        [STAThread()] /* WinForms */
        private static int Main(string[] args)
        {
            return (int)Interpreter.ShellMain(args);
        }
        #endregion
    }
}
