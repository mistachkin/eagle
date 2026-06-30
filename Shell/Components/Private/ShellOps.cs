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
    /// <summary>
    /// This class provides static helper methods used by the Eagle shell,
    /// including debugger-attach support during startup and, when built
    /// dynamically, retrieval of the executing assembly public key token.
    /// </summary>
#if STATIC
    [ObjectId("34dddef4-def9-483c-a299-6ee2bd92a739")]
#else
    [Guid("34dddef4-def9-483c-a299-6ee2bd92a739")]
#endif
    internal static class ShellOps
    {
        #region Private Constants
        /// <summary>
        /// The name of the environment variable that, when present, causes the
        /// shell to pause for a debugger to be attached during startup.
        /// </summary>
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
        /// <summary>
        /// The prompt format string displayed, when console support is
        /// available, instructing the user to attach a debugger to the
        /// current process and press a key to continue.
        /// </summary>
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
        /// <summary>
        /// This method gets the process identifier of the current process.
        /// </summary>
        /// <returns>
        /// The process identifier of the current process, or zero if it could
        /// not be determined.
        /// </returns>
        private static long GetProcessId()
        {
            Process process = Process.GetCurrentProcess();

            if (process == null)
                return 0;

            return process.Id;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks for the presence of the configured break
        /// environment variable and, if it is set, pauses (optionally prompting
        /// at the console) so a debugger can be attached, then breaks into the
        /// debugger.
        /// </summary>
        /// <param name="args">
        /// The command line arguments; this parameter is ignored.
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
        /// <summary>
        /// This method gets the public key token of the executing assembly,
        /// formatted as a lower-case hexadecimal string.
        /// </summary>
        /// <returns>
        /// The public key token as a hexadecimal string, or null if the
        /// assembly is not strong-named or the token could not be obtained.
        /// </returns>
        public static string GetPublicKeyTokenAsString()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            if (assembly == null)
                return null;

            AssemblyName assemblyName = assembly.GetName();

            if (assemblyName == null)
                return null;

            byte[] publicKeyToken = assemblyName.GetPublicKeyToken();

            if (publicKeyToken == null)
                return null;

            int length = publicKeyToken.Length;

            if (length == 0)
                return null;

            StringBuilder builder = new StringBuilder();

            for (int index = 0; index < length; index++)
                builder.AppendFormat("{0:x2}", publicKeyToken[index]);

            return builder.ToString();
        }
#endif
        #endregion
    }
}
