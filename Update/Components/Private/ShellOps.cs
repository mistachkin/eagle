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
    [Guid("6c18af48-6051-4ddf-bc6f-c72c97db663c")]
    internal static class ShellOps
    {
        #region Private Constants
        private const string breakVariable = "Break";

        ///////////////////////////////////////////////////////////////////////

#if CONSOLE
        private const string debuggerPrompt =
            "Attach a debugger to process {0} and press any key to continue.";
#endif

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        private static readonly AssemblyName assemblyName =
            new AssemblyName("Eagle, Version=1.0, Culture=neutral");

        private const string typeName =
            "Eagle._Components.Public.Interpreter";

        private const string memberName = "ShellMain";

        private const BindingFlags bindingFlags =
            BindingFlags.Static | BindingFlags.Public |
            BindingFlags.InvokeMethod;
#else
        private const int UnsupportedExitCode = 0xDEAD;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Eagle Shell Support Methods
        public static long GetProcessId()
        {
            Process process = Process.GetCurrentProcess();

            if (process == null)
                return 0;

            return process.Id;
        }

        ///////////////////////////////////////////////////////////////////////

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
