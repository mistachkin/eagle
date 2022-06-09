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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows.Forms;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Shell
{
    [ObjectId("05193919-e54c-448b-823d-e00c4573cd97")]
    internal static class Test
    {
        #region Private Constants
        private static readonly Assembly packageAssembly =
            Assembly.GetExecutingAssembly();

        private static readonly string resourceBaseName =
            packageAssembly.GetName().Name;

        //
        // NOTE: By default, we want a console?
        //
        private static readonly bool DefaultConsole = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal static _Forms.TestForm mainForm = null;

        ///////////////////////////////////////////////////////////////////////

        private static Thread interactiveLoopThread = null;
        private static IEnumerable<string> mainArguments = null;
        private static Interpreter interpreter = null;
        private static ExitCode exitCode = Utility.SuccessExitCode();
        private static long pluginToken = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        private static long GetProcessId()
        {
            Process process = Process.GetCurrentProcess();

            if (process == null)
                return 0;

            return process.Id;
        }

        ///////////////////////////////////////////////////////////////////////

        [STAThread()] /* WinForms */
        private static int Main(string[] args)
        {
            #region Shell Debugging Support (Optional)
            //
            // NOTE: Pause for them to attach a debugger, if requested.
            //       This cannot be done inside the Interpreter class
            //       because they may want to debug its initializers.
            //
            if (Environment.GetEnvironmentVariable(EnvVars.Break) != null)
            {
                //
                // NOTE: Prevent further breaks into the debugger.
                //
                Environment.SetEnvironmentVariable(EnvVars.Break, null);

#if CONSOLE
                //
                // NOTE: Display the prompt and then wait for the user to
                //       press a key.
                //
                Console.WriteLine(String.Format(
                    _Constants.Prompt.Debugger, GetProcessId()));

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
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Interpreter Creation Flags (Optional)
            //
            // NOTE: Start with default shell flags.
            //
            CreateFlags createFlags = CreateFlags.ShellUse;
            HostCreateFlags hostCreateFlags = HostCreateFlags.Default;

#if CONSOLE
            //
            // NOTE: The console is enabled and we are going to use our custom
            //       host which inherits from it; therefore, prevent a default
            //       host from being created for the interpreter.
            //
            hostCreateFlags |= HostCreateFlags.Disable;
#endif

            //
            // NOTE: Get the effective interpreter and host creation flags for
            //       the shell from the environment, etc.
            //
            createFlags = Interpreter.GetStartupCreateFlags(
                args, createFlags, OptionOriginFlags.Shell, true, true);

            hostCreateFlags = Interpreter.GetStartupHostCreateFlags(
                args, hostCreateFlags, OptionOriginFlags.Shell, true, true);
            #endregion

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: We need a return code and result variable now (in case
            //       querying the interpreter library path fails).
            //
            ReturnCode code = ReturnCode.Ok;
            Result result = null;

            ///////////////////////////////////////////////////////////////////

            #region Interpreter Pre-Initialize Text (Optional)
            //
            // NOTE: Start with the default pre-initialize text.
            //
            string text = null;

            //
            // NOTE: Get the effective interpreter pre-initialize text for the
            //       shell from the environment, etc.
            //
            if (code == ReturnCode.Ok)
            {
                code = Interpreter.GetStartupPreInitializeText(
                    args, createFlags, OptionOriginFlags.Shell, true,
                    true, ref text, ref result);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Interpreter Library Path (Optional)
            //
            // NOTE: Start with the default library path.
            //
            string libraryPath = null;

            //
            // NOTE: Get the effective interpreter library path for the shell
            //       from the environment, etc.
            //
            if (code == ReturnCode.Ok)
            {
                code = Interpreter.GetStartupLibraryPath(
                    args, createFlags, OptionOriginFlags.Shell, true,
                    true, ref libraryPath, ref result);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            if (code == ReturnCode.Ok)
            {
                //
                // NOTE: Create an interpreter now inside of a using block so
                //       that we can be relatively sure it will be finalized
                //       on this thread.
                //
                using (interpreter = Interpreter.Create(
                        args, createFlags, hostCreateFlags, text, libraryPath,
                        ref result))
                {
                    //
                    // NOTE: Make sure the interpreter was actually created.
                    //       This can, in theory, be a problem if the
                    //       ThrowOnError flag ends up getting removed somehow
                    //       prior to the call to create the interpreter.
                    //
                    if (interpreter != null)
                    {
                        //
                        // NOTE: Fetch the interpreter host now for error
                        //       reporting purposes.
                        //
                        IHost host = interpreter.Host;

                        ///////////////////////////////////////////////////////

                        #region Interpreter Startup Options (Optional)
                        //
                        // NOTE: By default, initialize the script library for
                        //       the interpreter.
                        //
                        bool initialize = true;

                        //
                        // NOTE: Process all the remaining startup options
                        //       (i.e. the ones that do not modify the
                        //       interpreter creation flags) now.
                        //
                        code = Interpreter.ProcessStartupOptions(
                            interpreter, args, createFlags, OptionOriginFlags.Shell,
                            true, true, ref initialize, ref result);
                        #endregion

                        ///////////////////////////////////////////////////////

                        if (code == ReturnCode.Ok)
                        {
                            #region Command Line Arguments (Optional)
#if CONSOLE
                            //
                            // NOTE: In debug mode, show the command line
                            //       arguments just as we received them.
                            //
                            if (interpreter.Debug)
                                Console.WriteLine("The command line is: {0}",
                                    Utility.BuildCommandLine(args, true));
#endif

                            //
                            // NOTE: Save the intial arguments for later use.
                            //
                            mainArguments = args;
                            #endregion

                            ///////////////////////////////////////////////////

                            #region Host - Window Preference (Optional)
                            //
                            // NOTE: By default, show the host window?
                            //
                            // TODO: Make this an argument?
                            //
                            bool console = DefaultConsole;

                            //
                            // NOTE: Do we want the initial (auto-created) host
                            //       window to be visible?
                            //
                            if (console)
                            {
                                if (Utility.GetEnvironmentVariable(
                                        EnvVars.NoConsole, true, false) != null)
                                {
#if CONSOLE
                                    Console.WriteLine(
                                        _Constants.Prompt.NoConsole);
#endif

                                    console = false;
                                }
                            }
                            else
                            {
                                if (Utility.GetEnvironmentVariable(
                                        EnvVars.Console, true, false) != null)
                                {
#if CONSOLE
                                    Console.WriteLine(
                                        _Constants.Prompt.Console);
#endif

                                    console = true;
                                }
                            }
                            #endregion

                            ///////////////////////////////////////////////////

                            #region Resource Manager Creation
                            //
                            // NOTE: Create our resource manager (the host and
                            //       the form will both use this).
                            //
                            ResourceManager resourceManager =
                                new ResourceManager(resourceBaseName,
                                    packageAssembly);
                            #endregion

                            ///////////////////////////////////////////////////

                            #region Host - Custom Creation (Optional)
#if CONSOLE
                            //
                            // NOTE: Create a custom IHost bound to the
                            //       interpreter.
                            //
                            host = new _Hosts.Custom(new HostData(null, null,
                                null, ClientData.Empty, typeof(_Hosts.Custom).Name,
                                interpreter, resourceManager, null, hostCreateFlags));

                            ///////////////////////////////////////////////////

                            interpreter.Host = host;
#endif
                            #endregion

                            ///////////////////////////////////////////////////

                            #region Interpreter Initialization
                            //
                            // NOTE: Attempt to initialize the interpreter.
                            //
                            if (initialize)
                                code = interpreter.Initialize(false, ref result);
                            else
                                code = ReturnCode.Ok;
                            #endregion

                            ///////////////////////////////////////////////////

                            #region Application-Specific Startup
                            //
                            // NOTE: If initialization failed, no point in
                            //       continuing.
                            //
                            if (code == ReturnCode.Ok)
                            {
                                Application.EnableVisualStyles();
                                Application.SetCompatibleTextRenderingDefault(false);

                                mainForm = new _Forms.TestForm(interpreter, args);

#if NOTIFY || NOTIFY_OBJECT
                                Assembly assembly = Assembly.GetExecutingAssembly();
                                AssemblyName assemblyName = assembly.GetName();
                                DateTime dateTime = Utility.GetAssemblyDateTime(assembly);
                                string fileName = assembly.Location;
                                string typeName = typeof(_Plugins.TestForm).FullName;
                                Uri uri = Utility.GetAssemblyUri(assembly);

                                IPlugin plugin = new _Plugins.TestForm(mainForm,
                                    new PluginData(Utility.FormatPluginName(
                                    assemblyName.FullName, typeName), null, null,
                                    ClientData.Empty, PluginFlags.None,
                                    assemblyName.Version, uri, null,
                                    interpreter.GetAppDomain(), assembly,
                                    assemblyName, dateTime, fileName, typeName,
                                    null, null, null, null, null, null,
                                    resourceManager, null, 0));

                                code = Utility.PopulatePluginEntities(
                                    interpreter, plugin, null, null,
                                    PluginFlags.None, null, false, false,
                                    ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    code = interpreter.AddPlugin(
                                        plugin, null, ref pluginToken, ref result);
                                }
#endif
                            }
                            #endregion

                            ///////////////////////////////////////////////////

                            #region Host - Window Startup
                            if (code == ReturnCode.Ok)
                            {
                                if (console)
                                {
                                    //
                                    // NOTE: Create and start the interpreter
                                    //       loop thread.
                                    //
                                    code = StartupInteractiveLoopThread(
                                        ref result);
                                }
                                else if (host.IsOpen())
                                {
                                    //
                                    // NOTE: Close the initial host window.
                                    //
                                    code = host.Close(ref result);
                                }
                            }
                            #endregion

                            ///////////////////////////////////////////////////

                            if (code == ReturnCode.Ok)
                            {
                                #region WinForms Specific
                                //
                                // NOTE: Show the primary user interface form
                                //       for the application.
                                //
                                Application.Run(mainForm);
                                #endregion

                                ///////////////////////////////////////////////

                                #region Host - Window Shutdown
                                //
                                // NOTE: If there is an interactive loop thread,
                                //       we do not want to exit until it is no
                                //       longer running.
                                //
                                if (interactiveLoopThread != null)
                                    interactiveLoopThread.Join();
                                #endregion
                            }

                            ///////////////////////////////////////////////////

                            #region Startup Error Handling
                            //
                            // NOTE: Was there any kind of failure above?
                            //
                            if (code != ReturnCode.Ok)
                            {
                                #region WinForms Specific Code
                                if (mainForm != null)
                                {
                                    mainForm.Dispose();
                                    mainForm = null;
                                }
                                #endregion

                                ///////////////////////////////////////////////

                                if (host != null)
                                    host.WriteResultLine(
                                        code, result, interpreter.ErrorLine);

                                CommonOps.Complain(code, result);

                                exitCode = Utility.ReturnCodeToExitCode(
                                    code, true);
                            }
                            #endregion
                        }
                        else
                        {
                            if (host != null)
                                host.WriteResultLine(code, result);

                            exitCode = Utility.ReturnCodeToExitCode(
                                code, true);
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
            }
            else
            {
#if CONSOLE
                //
                // NOTE: Querying the interpreter library path failed.
                //
                Console.WriteLine(Utility.FormatResult(code, result));
#endif

                exitCode = Utility.ReturnCodeToExitCode(code, true);
            }

            return (int)exitCode;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HaveInteractiveLoop()
        {
            try
            {
                if ((interactiveLoopThread != null) &&
                    interactiveLoopThread.IsAlive)
                {
                    return true;
                }
            }
            catch
            {
                // do nothing.
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode StartupInteractiveLoopThread(
            ref Result error
            )
        {
            if (interactiveLoopThread == null)
            {
                IHost host = interpreter.Host;

                //
                // NOTE: If there is no host window, open one now.
                //
                if (host.IsOpen() ||
                    (host.Open(ref error) == ReturnCode.Ok))
                {
                    //
                    // NOTE: Create the thread for the interactive loop
                    //       and start it.
                    //
                    interactiveLoopThread = Engine.CreateThread(
                        interpreter, InteractiveLoopThreadStart, 0, true,
                        false, true);

                    if (interactiveLoopThread != null)
                    {
                        interactiveLoopThread.Name = String.Format(
                            "interactiveLoopThread: {0}", interpreter);

                        interactiveLoopThread.Start(mainArguments);

                        //
                        // NOTE: Everything should be up and running now.
                        //
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = "could not create interactive loop thread";
                    }
                }
            }
            else
            {
                error = "interactive loop thread already started";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void InteractiveLoopThreadStart(object obj)
        {
#if MONO_BUILD
#pragma warning disable 219
#endif
            //
            // NOTE: Flagged by the Mono C# compiler.
            //
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
