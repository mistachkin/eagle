/*
 * ShellOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;

#if SHELL
using System.Reflection;
#endif

using System.Runtime.InteropServices;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the support methods used to start the Eagle shell
    /// from the update utility, including an optional facility for breaking
    /// into a debugger prior to startup.
    /// </summary>
    [Guid("6c18af48-6051-4ddf-bc6f-c72c97db663c")]
    internal static class ShellOps
    {
        #region Private Constants
        /// <summary>
        /// The name of the environment variable that, when present, requests
        /// that execution pause so a debugger can be attached.
        /// </summary>
        private const string breakVariable = "Break";

        ///////////////////////////////////////////////////////////////////////

#if CONSOLE
        /// <summary>
        /// The prompt displayed, when console support is available, asking the
        /// user to attach a debugger to the current process and press a key.
        /// </summary>
        private const string debuggerPrompt =
            "Attach a debugger to process {0} and press any key to continue.";
#endif

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        /// <summary>
        /// The name of the main Eagle assembly that is loaded in order to start
        /// the shell.
        /// </summary>
        private static readonly AssemblyName assemblyName =
            new AssemblyName("Eagle, Version=1.0, Culture=neutral");

        /// <summary>
        /// The fully qualified name of the type that exposes the shell entry
        /// point.
        /// </summary>
        private const string typeName =
            "Eagle._Components.Public.Interpreter";

        /// <summary>
        /// The name of the member that serves as the shell entry point.
        /// </summary>
        private const string memberName = "ShellMain";

        /// <summary>
        /// The reflection binding flags used to invoke the shell entry point.
        /// </summary>
        private const BindingFlags bindingFlags =
            BindingFlags.Static | BindingFlags.Public |
            BindingFlags.InvokeMethod;
#else
        /// <summary>
        /// The exit code returned when shell support is not available in this
        /// build.
        /// </summary>
        private const int UnsupportedExitCode = 0xDEAD;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Eagle Shell Support Methods
        /// <summary>
        /// This method returns the process identifier of the current process.
        /// </summary>
        /// <returns>
        /// The identifier of the current process, or zero if it could not be
        /// determined.
        /// </returns>
        public static long GetProcessId()
        {
            Process process = Process.GetCurrentProcess();

            if (process == null)
                return 0;

            return process.Id;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks whether a debugger break has been requested via
        /// the break environment variable and, if so, optionally prompts the
        /// user (when console support is available) and breaks into the
        /// debugger.  Any subsequent breaks are prevented.
        /// </summary>
        /// <param name="args">
        /// The command line arguments; this parameter is currently ignored.
        /// </param>
        public static void CheckBreak(
            IEnumerable<string> args /* IGNORED */
            )
        {
            //
            // NOTE: Pause for them to attach a debugger, if requested.
            //       This cannot be done inside the Interpreter class
            //       because they may want to debug its initializers.
            //
            if (Environment.GetEnvironmentVariable(breakVariable) != null)
            {
                //
                // NOTE: Prevent further breaks into the debugger.
                //
                Environment.SetEnvironmentVariable(breakVariable, null);

#if CONSOLE
                //
                // NOTE: Display the prompt and then wait for the user to
                //       press a key.
                //
                Console.WriteLine(String.Format(
                    debuggerPrompt, GetProcessId()));

                try
                {
                    Console.ReadKey(true); /* throw */
                }
                catch (InvalidOperationException) // Console.ReadKey
                {
                    // do nothing.
                }
#endif

                Debugger.Break(); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method serves as the entry point for starting the Eagle shell.
        /// It first checks for a requested debugger break and then, when shell
        /// support is available, loads the main Eagle assembly and invokes its
        /// shell entry point by reflection.
        /// </summary>
        /// <param name="args">
        /// The command line arguments to pass to the shell.
        /// </param>
        /// <returns>
        /// The exit code produced by the shell, or
        /// <see cref="UnsupportedExitCode" /> when shell support is not
        /// available in this build.
        /// </returns>
        [STAThread()] /* WinForms */
        public static int ShellMain(
            IEnumerable<string> args
            )
        {
            //
            // NOTE: Check and see if we need to break into the debugger
            //       before doing anything else.
            //
            CheckBreak(args); /* throw */

#if SHELL
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
#else
            return UnsupportedExitCode;
#endif
        }
        #endregion
    }
}
