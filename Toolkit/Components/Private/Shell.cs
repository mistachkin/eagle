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

///////////////////////////////////////////////////////////////////////////////////////////////
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* 
//
// Please do not use this code, it is a proof-of-concept only.  It is not production ready.
//
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* 
///////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Forms;
using Eagle._Interfaces.Public;

namespace Eagle._Shell
{
    /// <summary>
    /// This class implements a graphical (Windows Forms) Eagle shell that
    /// creates an interpreter, adds the toolkit plugin, runs the interactive
    /// loop on a background thread, and displays a top-level window.  It is a
    /// proof-of-concept and is not production ready.
    /// </summary>
    [ObjectId("aa0f9c46-2fca-4be4-a9af-1ec1bcc34d90")]
    internal static class Graphical
    {
        /// <summary>
        /// The interpreter created and used by this shell.
        /// </summary>
        private static Interpreter interpreter;
        /// <summary>
        /// The exit code to be returned to the operating system when this shell
        /// terminates.
        /// </summary>
        private static ExitCode exitCode;
        /// <summary>
        /// The token identifying the toolkit plugin added to the interpreter.
        /// </summary>
        private static long toolkitPluginToken;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the application entry point for the graphical shell.
        /// It creates and initializes the interpreter, registers the toolkit
        /// plugin, starts the interactive loop on a background thread, and runs
        /// the Windows Forms message loop for the top-level window.
        /// </summary>
        /// <param name="args">
        /// The command line arguments.
        /// </param>
        /// <returns>
        /// The process exit code.
        /// </returns>
        [STAThread()] /* WinForms */
        private static int Main(string[] args)
        {
            ReturnCode code;
            Result result = null;

            using (interpreter = Interpreter.Create(
                    args, CreateFlags.ShellUse, HostCreateFlags.ShellUse,
                    ref result))
            {
                if (interpreter != null)
                {
                    code = interpreter.Initialize(false, ref result);

                    if (code == ReturnCode.Ok)
                    {
                        Assembly assembly = Assembly.GetExecutingAssembly();
                        AssemblyName assemblyName = assembly.GetName();
                        DateTime dateTime = Utility.GetAssemblyDateTime(assembly);
                        string fileName = assembly.Location;
                        string typeName = typeof(_Plugins.Toolkit).FullName;
                        Uri uri = Utility.GetAssemblyUri(assembly);

                        IPlugin plugin = new _Plugins.Toolkit(new PluginData(
                            Utility.FormatPluginName(assemblyName.FullName,
                            typeName), null, null, ClientData.Empty,
                            PluginFlags.None, assemblyName.Version, uri, null,
                            interpreter.GetAppDomain(), assembly, assemblyName,
                            dateTime, fileName, typeName, null, null, null,
                            null, null, null, null, null, 0));

                        code = Utility.PopulatePluginEntities(
                            interpreter, plugin, null, null, null,
                            null, false, false, false, ref result);

                        if (code == ReturnCode.Ok)
                            code = interpreter.AddPlugin(plugin, null,
                                ref toolkitPluginToken, ref result);
                    }

                    if (code == ReturnCode.Ok)
                    {
                        Thread thread = Engine.CreateThread(
                            interpreter, InteractiveLoopThreadStart, 0, true,
                            false, true);

                        if (thread != null)
                        {
                            thread.Name = String.Format(
                                "interactiveLoopThread: {0}", interpreter);

                            thread.Start(args);

                            Application.EnableVisualStyles();
                            Application.SetCompatibleTextRenderingDefault(false);

                            Toplevel toplevel = new Toplevel(interpreter, ".");

                            Application.Run(toplevel);

                            if (thread != null)
                                thread.Join();

                            exitCode = interpreter.ExitCode;
                        }
                        else
                        {
                            result = "could not create interactive loop thread";
                            code = ReturnCode.Error;
                        }
                    }

                    if (code != ReturnCode.Ok)
                    {
                        IInteractiveHost interactiveHost = interpreter.Host;

                        if (interactiveHost != null)
                            interactiveHost.WriteResultLine(
                                code, result, interpreter.ErrorLine);

                        exitCode = Utility.ReturnCodeToExitCode(code, true);
                    }
                }
                else
                {
#if CONSOLE
                    //
                    // NOTE: Creation of the interpreter failed.
                    //
                    Console.WriteLine(Utility.FormatResult(
                        ReturnCode.Error, result));
#endif

                    exitCode = Utility.FailureExitCode();
                }
            }

            return (int)exitCode;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the thread start routine for the background thread
        /// that runs the interpreter interactive loop.  It records the
        /// resulting exit code when the loop completes.
        /// </summary>
        /// <param name="obj">
        /// The thread parameter, which is the enumerable of command line
        /// arguments to pass to the interactive loop.
        /// </param>
        private static void InteractiveLoopThreadStart(object obj)
        {
#if MONO_BUILD
#pragma warning disable 219
#endif
            ReturnCode code;
#if MONO_BUILD
#pragma warning restore 219
#endif

            Result result = null;

#if SHELL
            code = Interpreter.InteractiveLoop(
                interpreter, obj as IEnumerable<string>, ref result);
#else
            result = "not implemented";
            code = ReturnCode.Error;
#endif

#if CONSOLE
            if (code != ReturnCode.Ok)
                Console.WriteLine(Utility.FormatResult(code, result));
#endif

            if (code == ReturnCode.Ok)
                exitCode = interpreter.ExitCode;
            else
                exitCode = Utility.ReturnCodeToExitCode(code);
        }
    }
}
