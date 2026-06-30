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
    /// <summary>
    /// This class provides the assembly entry point for the statically-linked
    /// variant of the Eagle shell, which references the Eagle assembly directly.
    /// </summary>
    [ObjectId("ef2b0d77-82ca-4d06-a8db-ef6b41e891c3")]
    internal static class StaticCommandLine
    {
        #region Assembly Entry Point Method
        /// <summary>
        /// This method is the managed entry point for the shell process.  It
        /// optionally breaks into the debugger and then delegates to the Eagle
        /// interpreter shell.
        /// </summary>
        /// <param name="args">
        /// The command line arguments passed to the process.
        /// </param>
        /// <returns>
        /// The process exit code produced by the shell.
        /// </returns>
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
    /// <summary>
    /// This class provides the assembly entry point for the dynamically-linked
    /// variant of the Eagle shell, which loads the Eagle assembly by name at
    /// runtime and invokes its shell entry point via reflection.
    /// </summary>
#if STATIC
    [ObjectId("421b83c0-4238-44cf-84d6-f66b9fd71efb")]
#else
    [Guid("421b83c0-4238-44cf-84d6-f66b9fd71efb")]
#endif
    internal static class DynamicCommandLine
    {
        #region Private Constants
        /// <summary>
        /// The (partial) display name of the Eagle assembly to load.
        /// </summary>
        private static readonly string assemblyName =
            "Eagle, Version=1.0, Culture=neutral";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The fully-qualified name of the interpreter type that exposes the
        /// shell entry point.
        /// </summary>
        private static readonly string typeName =
            "Eagle._Components.Public.Interpreter";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the static method that serves as the shell entry point.
        /// </summary>
        private static readonly string memberName = "ShellMain";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The binding flags used to locate and invoke the shell entry point
        /// method via reflection.
        /// </summary>
        private static readonly BindingFlags bindingFlags =
            BindingFlags.Static | BindingFlags.Public |
            BindingFlags.InvokeMethod;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Entry Point Method
        /// <summary>
        /// This method is the managed entry point for the shell process.  It
        /// optionally breaks into the debugger, loads the Eagle assembly by
        /// name, and invokes its shell entry point via reflection.
        /// </summary>
        /// <param name="args">
        /// The command line arguments passed to the process.
        /// </param>
        /// <returns>
        /// The process exit code produced by the shell.
        /// </returns>
        [STAThread()] /* WinForms */
        private static int Main(string[] args)
        {
            //
            // NOTE: Check and see if we need to break into the debugger
            //       before doing anything else.
            //
            ShellOps.CheckBreak(args); /* throw */

            //
            // NOTE: Check for applicable assembly public key token.  If
            //       there is one, it will be formatted as a hexadecimal
            //       string.
            //
            string publicKeyTokenString = ShellOps.GetPublicKeyTokenAsString();

            //
            // NOTE: Attempt to load the main Eagle assembly by name.
            //
            Assembly assembly;

            if (!String.IsNullOrEmpty(publicKeyTokenString))
            {
                assembly = Assembly.Load(String.Format(
                    "{0}, PublicKeyToken={1}", assemblyName,
                    publicKeyTokenString)); /* throw */
            }
            else
            {
                assembly = Assembly.Load(assemblyName); /* throw */
            }

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
