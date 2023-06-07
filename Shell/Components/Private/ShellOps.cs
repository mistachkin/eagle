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

#if DYNAMIC
using System.Reflection;
#endif

#if !STATIC
using System.Runtime.InteropServices;
#endif

#if DYNAMIC
using System.Text;
#endif

#if STATIC
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
#endif

namespace Eagle._Shell
{
#if STATIC
    [ObjectId("34dddef4-def9-483c-a299-6ee2bd92a739")]
#else
    [Guid("34dddef4-def9-483c-a299-6ee2bd92a739")]
#endif
    internal static class ShellOps
    {
        #region Private Constants
        private static readonly string breakVariable =
#if STATIC
            //
            // NOTE: Use the environment variable name defined in the Eagle
            //       assembly itself.
            //
            EnvVars.Break;
#else
            //
            // NOTE: *FALLBACK* The Eagle assembly is not available; therefore,
            //       we hard-code the string value here.
            //
            "Break";
#endif

        ///////////////////////////////////////////////////////////////////////

#if CONSOLE
        private static readonly string debuggerPrompt =
#if STATIC
            //
            // NOTE: Use the debugger prompt string defined in the Eagle
            //       assembly itself.
            //
            Prompt.Debugger;
#else
            //
            // NOTE: *FALLBACK* The Eagle assembly is not available; therefore,
            //       we hard-code the string value here.
            //
            "Attach a debugger to process {0} and press any key to continue.";
#endif
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Debugging Support Methods
        private static long GetProcessId()
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
                Console.WriteLine(String.Format(debuggerPrompt,
                    GetProcessId()));

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
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Dynamic Support Methods
#if DYNAMIC
        public static string GetPublicKeyTokenAsString()
        {
            StringBuilder builder = new StringBuilder();

            byte[] publicKeyToken = Assembly.GetExecutingAssembly().
                    GetName().GetPublicKeyToken();

            int length = publicKeyToken.Length;

            for (int index = 0; index < length; index++)
                builder.AppendFormat("{0:x2}", publicKeyToken[index]);

            return builder.ToString();
        }
#endif
        #endregion
    }
}
