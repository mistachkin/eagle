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

#if DYNAMIC
using System.Reflection;
using System.Runtime.InteropServices;
#endif

#if STATIC
using Eagle._Attributes;
using Eagle._Components.Public;
#endif

namespace Eagle._Shell
{
#if STATIC
    [ObjectId("ef2b0d77-82ca-4d06-a8db-ef6b41e891c3")]
    internal static class StaticCommandLine
    {
        #region Assembly Entry Point Method
        [STAThread()] /* WinForms */
        private static int Main(string[] args)
        {
            //
            // NOTE: Check and see if we need to break into the debugger
            //       before doing anything else.
            //
            ShellOps.CheckBreak(args); /* throw */

            //
            // NOTE: The Interpreter class now handles all the default
            //       behavior of the shell.
            //
            return (int)Interpreter.ShellMain(args);
        }
        #endregion
    }
#endif

    ///////////////////////////////////////////////////////////////////////////

#if DYNAMIC
#if STATIC
    [ObjectId("421b83c0-4238-44cf-84d6-f66b9fd71efb")]
#else
    [Guid("421b83c0-4238-44cf-84d6-f66b9fd71efb")]
#endif
    internal static class DynamicCommandLine
    {
        #region Private Constants
        private static readonly AssemblyName assemblyName =
            new AssemblyName("Eagle, Version=1.0, Culture=neutral");

        ///////////////////////////////////////////////////////////////////////

        private static readonly string typeName =
            "Eagle._Components.Public.Interpreter";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string memberName = "ShellMain";

        ///////////////////////////////////////////////////////////////////////

        private static readonly BindingFlags bindingFlags =
            BindingFlags.Static | BindingFlags.Public |
            BindingFlags.InvokeMethod;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Entry Point Method
        [STAThread()] /* WinForms */
        private static int Main(string[] args)
        {
            //
            // NOTE: Check and see if we need to break into the debugger
            //       before doing anything else.
            //
            ShellOps.CheckBreak(args); /* throw */

            //
            // NOTE: Attempt to load the main Eagle assembly by name.
            //
            Assembly assembly = Assembly.Load(assemblyName); /* throw */

            //
            // NOTE: Attempt to locate the Interpreter type by name.
            //
            Type type = assembly.GetType(typeName); /* throw */

            //
            // NOTE: Attempt to invoke the shell entry point by name.
            //
            return (int)type.InvokeMember(memberName, bindingFlags,
                null, null, new object[] { args }); /* throw */
        }
        #endregion
    }
#endif
}
