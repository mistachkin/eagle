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
    [ObjectId("2a3e66d9-28fb-4c31-95e6-0132aab6c127")]
    internal static class Shell
    {
        #region Assembly Entry Point Method
        [STAThread()] /* WinForms */
        private static int Main(string[] args)
        {
            return (int)Interpreter.ShellMain(args);
        }
        #endregion
    }
}
