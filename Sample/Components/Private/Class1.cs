/*
 * Class1.cs --
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
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Sample
{
    /// <summary>
    /// This class contains the main entry point for this assembly.
    /// </summary>
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("24642a67-9ef2-4e54-8511-6283669683af")]
    internal static class Class1
    {
        #region Private Data
        /// <summary>
        /// This represents some state information that Class2 may access (via
        /// the IClientData interface passed to it upon creation).
        /// </summary>
#pragma warning disable 414
        private static int sharedState = 42; // NOTE: Used via Reflection only.
#pragma warning restore 414

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This will contain a count of the number of read traces for all
        /// variables in the interpreter.
        /// </summary>
        private static int getTraces = 0;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This will contain a count of the number of write traces for all
        /// variables in the interpreter.
        /// </summary>
        private static int setTraces = 0;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This will contain a count of the number of delete traces for all
        /// variables in the interpreter.
        /// </summary>
        private static int unsetTraces = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This is the main entry point for this assembly.
        /// </summary>
        /// <param name="args">
        /// The command line arguments received from the calling assembly.
        /// </param>
        /// <returns>
        /// Zero for success, non-zero on error.
        /// </returns>
        [STAThread()] /* WinForms */
        private static int Main(
            string[] args /* in */
            )
        {
            return (int)Test(args);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called by the script variable subsystem when the
        /// associated variable(s) is/are accessed (e.g. read, set, or unset).
        /// It is allowed to cancel the operation outright, alter the returned
        /// value, and/or perform other arbitrary actions.  It is also possible
        /// to setup "interpreter-wide" variable traces that monitor and/or
        /// modify all script variable access performed by the interpreter.
        /// </summary>
        /// <param name="breakpointType">
        /// The type of variable access being requested (e.g. read, set, unset,
        /// etc).
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information pertaining to the requested variable access.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter should not be changed.  Upon failure,
        /// this parameter should be modified to contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        [MethodFlags(MethodFlags.VariableTrace | MethodFlags.NoAdd)]
        private static ReturnCode TraceCallback(
            BreakpointType breakpointType, /* in */
            Interpreter interpreter,       /* in */
            ITraceInfo traceInfo,          /* in */
            ref Result result              /* out */
            )
        {
            if (interpreter == null)
            {
                //
                // NOTE: We require a valid interpreter context.  Most custom
                //       variable trace methods will want to do this because it
                //       is a fairly standard safety check (HIGHLY RECOMMENDED).
                //
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (traceInfo == null)
            {
                //
                // NOTE: This parameter should contain the information about
                //       the current variable access being processed.  If it
                //       is null, we cannot continue (HIGHLY RECOMMENDED).
                //
                result = "invalid trace";
                return ReturnCode.Error;
            }

            //
            // NOTE: This trace method intercepts and show all variable access
            //       for the interpreter.  We could do practically anything
            //       inside this method, including monitoring and/or modifying
            //       the values to be fetched and/or set, changing the state of
            //       the interpreter, or aborting the operation altogether.
            //
            ///////////////////////////////////////////////////////////////////
            // ******************* BEGIN CUSTOM TRACE CODE ********************
            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This particular variable trace method requires a valid
            //       interpreter host; however, in practice, such a requirement
            //       would be very rare.
            //
            IHost host = interpreter.Host;

            if (host == null)
            {
                result = "interpreter host not available";
                return ReturnCode.Error;
            }

            //
            // NOTE: The try block here is optional; however, custom variable
            //       trace methods are "officially" discouraged from raising
            //       exceptions.
            //
            ReturnCode code = ReturnCode.Ok;

            switch (traceInfo.BreakpointType)
            {
                case BreakpointType.BeforeVariableUnset:
                    {
                        try
                        {
                            //
                            // NOTE: Query the configured foreground and
                            //       background colors used by the host
                            //       to display variable trace information.
                            //
                            ConsoleColor foregroundColor = _ConsoleColor.None;
                            ConsoleColor backgroundColor = _ConsoleColor.None;

                            code = host.GetColors(null,
                                "TraceInfo", true, true, ref foregroundColor,
                                ref backgroundColor, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                //
                                // NOTE: Clear the interpreter host and reset
                                //       its output position as well.
                                //
                                host.Clear();
                                host.ResetPosition();

                                //
                                // NOTE: Write the variable trace information
                                //       received by this method to the
                                //       interpreter host.
                                //
                                host.WriteTraceInfo(
                                    interpreter, traceInfo,
                                    DetailFlags.EmptyContent, true,
                                    foregroundColor, backgroundColor);

                                host.RestorePosition(false);

                                int left = 0;
                                int top = 0;

                                host.GetPosition(ref left, ref top);

                                left = 0; /* NOTE: Left aligned. */
                                top++;    /* NOTE: On next line. */

                                host.WriteBox(null,
                                    "Press any key to continue...", null,
                                    false, false, ref left, ref top);

                                host.Pause();
                            }
                        }
                        catch (Exception e)
                        {
                            result = e;
                            code = ReturnCode.Error;
                        }

                        unsetTraces++;
                        break;
                    }
                case BreakpointType.BeforeVariableGet:
                    {
                        getTraces++;
                        break;
                    }
                case BreakpointType.BeforeVariableSet:
                    {
                        setTraces++;
                        break;
                    }
            }

            //
            // NOTE: Finally, return one of the standard status codes that
            //       indicate success / failure of this variable access.
            //
            return code;

            ///////////////////////////////////////////////////////////////////
            // ******************** END CUSTOM TRACE CODE *********************
            ///////////////////////////////////////////////////////////////////
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates and initializes an interpreter, uses it to
        /// perform several tests, and then returns.  The created interpreter
        /// is disposed automatically before returning.
        /// </summary>
        /// <param name="args">
        /// The command line arguments received from the calling assembly.
        /// </param>
        /// <returns>
        /// Zero for success, non-zero on error.
        /// </returns>
        private static ExitCode Test(
            IEnumerable<string> args /* in */
            )
        {
            //
            // NOTE: This variable will contain the interpreter exit code
            //       (either via exiting the interactive loop or from an
            //       explicit call to the [exit] command).
            //
            ExitCode exitCode;

            //
            // NOTE: The "interpreter result" that is passed to various
            //       methods.  The interpreter itself does *NOT* store this
            //       result internally and that behavior is a significant
            //       difference from native Tcl.
            //
            Result result = null;

            //
            // NOTE: Do we want to intercept all variable accesses for the
            //       interpreter?
            //
            TraceList traces = new TraceList(new TraceCallback[] {
                TraceCallback
            });

            //
            // NOTE: First, we create a new interpreter, using some reasonable
            //       flags and passing in the command line arguments provided
            //       to this method.
            //
            using (Interpreter interpreter = Interpreter.Create(
                    args, CreateFlags.Default & ~CreateFlags.ThrowOnError,
                    HostCreateFlags.Default, traces, ref result))
            {
                //
                // NOTE: Was the interpreter created successfully?
                //
                if (interpreter != null)
                {
                    //
                    // NOTE: Override the default script binder with the one
                    //       implemented by this sample application.
                    //
                    interpreter.Binder = new Class9(
                        interpreter, interpreter.Binder as IScriptBinder);

                    //
                    // NOTE: Create instances of our custom command classes.
                    //       These instances will be used when registering the
                    //       custom commands in the interpreter.
                    //
                    Class2 class2 = new Class2(new CommandData(
                        "class2", null, "This would describe the command.",
                        new ClientData(typeof(Class1)), typeof(Class2).FullName,
                        CommandFlags.None, null, 0));

                    Class12 class12 = new Class12(new CommandData(
                        "class12", null, "This would describe the command.",
                        null, typeof(Class12).FullName, CommandFlags.None,
                        new Class3(null), 0));

                    //
                    // NOTE: These tokens will represent the custom command
                    //       registered with the interpreter.  These tokens can
                    //       later be used to remove the commands, even if they
                    //       have been renamed.
                    //
                    long token2 = 0;
                    long token12 = 0;

                    //
                    // NOTE: Next, we [optionally] register our custom commands
                    //       in the interpreter.  Normally, the return codes
                    //       should be checked here; however, in this example,
                    //       they are ignored.
                    //
                    /* IGNORED */
                    interpreter.AddCommand(
                        class2, null, ref token2, ref result);

                    /* IGNORED */
                    interpreter.AddCommand(
                        class12, null, ref token12, ref result);

                    //
                    // NOTE: Next, we can evaluate one or more scripts of our
                    //       choosing, which may or may not refer to custom
                    //       commands registered in the interpreter by us or
                    //       other parties.
                    //
                    // NOTE: To test the custom script binder, use the script:
                    //
                    //       object invoke Sample.Class2 TestMethod class2
                    //
                    ReturnCode code2;
                    Result result2 = null;
                    int errorLine2 = 0;

                    code2 = Engine.EvaluateScript(interpreter,
                        "set x 1; unset x; class2 test; # <-- script text",
                        ref result2, ref errorLine2);

                    ReturnCode code12;
                    Result result12 = null;
                    int errorLine12 = 0;

                    code12 = Engine.EvaluateScript(interpreter,
                        "list [class12 example1] [class12 example2] " +
                        "[class12 example3] [class12 options]; # ensemble",
                        ref result12, ref errorLine12);

                    //
                    // NOTE: Fetch the interpreter exit code (typically zero
                    //       for "success" and non-zero for "failure").  In the
                    //       event this indicates success and the return code
                    //       indicates failure, the return code will override
                    //       this value to produce the final exit code for this
                    //       method.  This is an example of what could be done
                    //       and is entirely optional.
                    //
                    exitCode = interpreter.ExitCode;

                    //
                    // NOTE: Grab the interpreter host now since we will need
                    //       it several times below to display various pieces
                    //       of information.
                    //
                    IHost host = interpreter.Host;

                    //
                    // NOTE: Show number of variable traces that were fired.
                    //
                    if ((getTraces > 0) || (setTraces > 0) || (unsetTraces > 0))
                    {
                        if (host != null)
                        {
                            host.Clear();

                            host.WriteLine(String.Format(
                                "read = {0}", getTraces));

                            host.WriteLine(String.Format(
                                "write = {0}", setTraces));

                            host.WriteLine(String.Format(
                                "delete = {0}", unsetTraces));

                            host.WriteLine();
                            host.WriteLine("Press any key to continue...");
                            host.Pause();
                        }
                        else
                        {
                            //
                            // NOTE: Do something else here...  Such as log the
                            //       trace counts.
                            //
                        }
                    }

                    //
                    // NOTE: This section of code attempts to demonstrate how
                    //       script errors *could* be handled.
                    //
                    if (Utility.IsSuccess(code2, false) &&
                        Utility.IsSuccess(code12, false))
                    {
                        //
                        // NOTE: Always check for a valid interpreter hosting
                        //       environment before using it as it is not
                        //       guaranteed to always be available.
                        //
                        if (host != null)
                        {
                            //
                            // NOTE: Display the script evaluation result along
                            //       with its return code using the interpreter
                            //       host.  This is an example of what could be
                            //       done and is entirely optional.
                            //
                            host.Clear();

                            host.WriteLine(String.Format(
                                "SUCCESS, code2 = {0}", code2));

                            host.WriteResultLine(code2, result2);

                            host.WriteLine(String.Format(
                                "SUCCESS, code12 = {0}", code12));

                            host.WriteResultLine(code12, result12);

                            host.WriteLine();
                            host.WriteLine("Press any key to continue...");
                            host.Pause();
                        }
                        else
                        {
                            //
                            // NOTE: Do something else here...  Such as log the
                            //       success.
                            //
                        }
                    }
                    else
                    {
                        //
                        // NOTE: Always check for a valid interpreter hosting
                        //       environment before using it as it is not
                        //       guaranteed to always be available.
                        //
                        if (host != null)
                        {
                            //
                            // NOTE: Display the script evaluation result along
                            //       with its return code using the interpreter
                            //       host.  This is an example of what could be
                            //       done and is entirely optional.
                            //
                            host.Clear();

                            host.WriteLine(String.Format(
                                "{0}, code2 = {1}", Utility.IsSuccess(code2,
                                false) ? "SUCCESS" : "FAILURE", code2));

                            host.WriteResultLine(code2, result2, errorLine2);

                            host.WriteLine(String.Format(
                                "{0}, code12 = {1}", Utility.IsSuccess(code12,
                                false) ? "SUCCESS" : "FAILURE", code12));

                            host.WriteResultLine(code12, result12, errorLine12);

                            host.WriteLine();
                            host.WriteLine("Press any key to continue...");
                            host.Pause();
                        }
                        else
                        {
                            //
                            // NOTE: Do something else here...  Such as log the
                            //       failure.
                            //
                        }

                        //
                        // NOTE: If the exit code is success and the return code
                        //       indicates an error, allow the return code to
                        //       override the exit code.  This is an example of
                        //       what could be done and is entirely optional.
                        //
                        if (exitCode == Utility.SuccessExitCode())
                        {
                            if (Utility.IsSuccess(code2, false))
                                exitCode = Utility.ReturnCodeToExitCode(code12);
                            else
                                exitCode = Utility.ReturnCodeToExitCode(code2);
                        }
                    }

                    //
                    // NOTE: Finally, if applicable, remove the custom commands
                    //       we previously registered in the interpreter.
                    //
                    if (token12 != 0)
                    {
                        /* IGNORED */
                        interpreter.RemoveCommand(token12, null, ref result);
                    }

                    if (token2 != 0)
                    {
                        /* IGNORED */
                        interpreter.RemoveCommand(token2, null, ref result);
                    }
                }
                else
                {
#if CONSOLE
                    //
                    // NOTE: Creation of the interpreter failed.  Attempt to
                    //       display the reason why to the system console.
                    //
                    Console.WriteLine(Utility.FormatResult(
                        ReturnCode.Error, result));
#endif

                    exitCode = Utility.FailureExitCode();
                }
            }

            return exitCode;
        }
    }
}
