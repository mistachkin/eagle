/*
 * Example.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Example
{
    /// <summary>
    /// This class contains the main entry point for this assembly.
    /// </summary>
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("6b4f29fc-c7e9-4dbc-b838-909ae6708f5d")]
    internal static class Program
    {
        #region Private Constants
        /// <summary>
        /// The name of the field in this class to use when creating the
        /// example linked variable.  This is also used for the name of the
        /// script variable to create; however, this is not a requirement and
        /// the name of the created script variable could be any valid script
        /// variable name.
        /// </summary>
        private static readonly string fieldName = "mainArgs";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The random number generator used by the example command execution
        /// policy for making "random" policy decisions.
        /// </summary>
        private static Random random;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Data
        /// <summary>
        /// The string array used to back the example linked variable.
        /// </summary>
        public static string[] mainArgs;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Application Entry Point
        /// <summary>
        /// This is the main entry point for this assembly.
        /// </summary>
        /// <param name="args">
        /// The command line arguments received from the calling assembly.
        /// </param>
        /// <returns>
        /// Zero for success, non-zero on error.
        /// </returns>
        private static int Main(
            string[] args /* in */
            )
        {
            //
            // NOTE: The integer exit code to return to the caller (parent
            //       process, etc).
            //
            ExitCode exitCode = Utility.SuccessExitCode();

            //
            // NOTE: Save the command line arguments for use by the
            //       interpreter via the linked variable (optional).
            //
            mainArgs = args;

            //
            // NOTE: The "interpreter result" that is passed to various
            //       methods.
            //
            Result result = null;

            //
            // NOTE: First, we create a new interpreter (with the default
            //       options).
            //
            using (Interpreter interpreter = Interpreter.Create(
                    args, ref result))
            {
                if (interpreter != null)
                {
                    ReturnCode code = interpreter.SetVariableLink(
                        VariableFlags.None, fieldName, typeof(Program).
                        GetField(fieldName), null, ref result);

                    if (code != ReturnCode.Ok)
                    {
                        //
                        // NOTE: Handle variable linking error.
                        //
                        exitCode = Utility.ReturnCodeToExitCode(code);
                    }

                    //
                    // NOTE: Create an instance of the example custom command.
                    //
                    Class0 class0 = new Class0(new CommandData("class0", null,
                        null, ClientData.Empty, typeof(Class0).FullName,
                        CommandFlags.None, null, 0));

                    //
                    // NOTE: The token that will represent the custom command
                    //       we add.
                    //
                    long commandToken = 0;

                    //
                    // NOTE: Next, we can optionally add one or more custom
                    //       commands.
                    //
                    if (code == ReturnCode.Ok)
                    {
                        code = interpreter.AddCommand(
                            class0, null, ref commandToken, ref result);
                    }

                    //
                    // NOTE: The token that will represent the custom command
                    //       policy we add.
                    //
                    long policyToken = 0;

                    //
                    // NOTE: Next, add our custom command execution policy (for
                    //       use in "safe" mode).
                    //
                    if (code == ReturnCode.Ok)
                    {
                        code = interpreter.AddPolicy(
                            Class0PolicyCallback, null, null, ref policyToken,
                            ref result);
                    }

                    //
                    // NOTE: The error line number that is passed to various
                    //       script evaluation methods.
                    //
                    int errorLine = 0;

                    //
                    // NOTE: Check for a successful return code.
                    //
                    if (code == ReturnCode.Ok) // OR: Utility.IsSuccess(code, true)
                    {
#if SHELL
                        result = null;

                        code = Interpreter.InteractiveLoop(
                            interpreter, args, ref result);
#else
                        //
                        // NOTE: Next, evaluate one or more scripts of your
                        //       choosing (which may or may not reference any
                        //       custom commands you may have added in the
                        //       previous step).
                        //
                        code = Engine.EvaluateScript(
                            interpreter, "class0 test; # <-- script text",
                            ref result, ref errorLine);
#endif

                        //
                        // NOTE: Check for an unsuccessful return code.
                        //
                        if (code != ReturnCode.Ok) // OR: !Utility.IsSuccess(code, true)
                        {
                            //
                            // NOTE: Handle script error.
                            //
                            exitCode = Utility.ReturnCodeToExitCode(code);
                        }

                        //
                        // NOTE: Next, we can optionally remove one or more of
                        //       the custom command policies we added earlier.
                        //
                        code = interpreter.RemovePolicy(policyToken, null,
                            ref result);

                        //
                        // NOTE: Check for an unsuccessful return code.
                        //
                        if (code != ReturnCode.Ok)
                        {
                            //
                            // NOTE: Handle policy removal error.
                            //
                            exitCode = Utility.ReturnCodeToExitCode(code);
                        }

                        //
                        // NOTE: Next, we can optionally remove one or more of
                        //       the custom commands we added earlier.
                        //
                        code = interpreter.RemoveCommand(commandToken, null,
                            ref result);

                        //
                        // NOTE: Check for an unsuccessful return code.
                        //
                        if (code != ReturnCode.Ok)
                        {
                            //
                            // NOTE: Handle command removal error.
                            //
                            exitCode = Utility.ReturnCodeToExitCode(code);
                        }

                        code = interpreter.UnsetVariable(VariableFlags.None,
                            fieldName, ref result);

                        if (code != ReturnCode.Ok)
                        {
                            //
                            // NOTE: Handle variable unlinking error.
                            //
                            exitCode = Utility.ReturnCodeToExitCode(code);
                        }
                    }
                    else
                    {
                        //
                        // NOTE: Handle failure to add the custom command
                        //       or policy.
                        //
                        exitCode = Utility.ReturnCodeToExitCode(code);
                    }

                    //
                    // NOTE: Always check for a valid interpreter hosting
                    //       environment before using it as it is not
                    //       guaranteed to always be available.
                    //
                    IInteractiveHost interactiveHost = interpreter.Host;

                    if (interactiveHost != null)
                    {
                        interactiveHost.WriteResultLine(
                            code, result, errorLine);
                    }
                    else
                    {
                        Console.WriteLine(Utility.FormatResult(
                            code, result, errorLine));
                    }
                }
                else
                {
                    //
                    // NOTE: Creation of the interpreter failed.
                    //
                    Console.WriteLine(Utility.FormatResult(
                        ReturnCode.Error, result));

                    exitCode = Utility.FailureExitCode();
                }
            }

            return (int)exitCode;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Example Command Policy Implementation
        /// <summary>
        /// This method is an example command policy.  It will be called by
        /// the evaluation engine before any hidden command is allowed to be
        /// executed.  Determine if the command with the specified arguments
        /// and context should be allowed to execute.  The "Approved" method of
        /// the policy context object should be called if the command should be
        /// allowed to execute; however, this does not guarantee that the
        /// command will execute.  If any other active policies call the
        /// "Denied" method of the policy context object, execution of the
        /// command will be vetoed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="clientData">
        /// The policy context data will be provided by the evaluation engine
        /// using this parameter.  To extract the <see cref="IPolicyContext" />
        /// instance, the <see cref="Utility.ExtractPolicyContextAndCommand" />
        /// method must be used.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments supplied to the command by the script being
        /// evaluated.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter should not be changed.  Upon failure,
        /// this parameter should be modified to contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// The value <see cref="ReturnCode.Ok" /> on success,
        /// <see cref="ReturnCode.Error" />  on failure.  Other return codes
        /// may be used to implement custom control structures.  If the policy
        /// wishes to approve of the command execution, the
        /// <see cref="IPolicyContext.Approved" /> method should be called
        /// prior to returning; otherwise, the
        /// <see cref="IPolicyContext.Denied" /> method should be called.  If
        /// neither of these methods is called, the default command execution
        /// decision will be used instead.
        /// </returns>
        [MethodFlags(MethodFlags.CommandPolicy)]
        private static ReturnCode Class0PolicyCallback( /* POLICY */
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            //
            // NOTE: We only want to make policy decisions for our own command;
            //       therefore, use the appropriate helper method provided by
            //       the interpreter.  This method also allows us to easily
            //       extract the policy context data required to make command
            //       execution decisions.
            //
            IPolicyContext policyContext = null;
            bool match = false;

            if (Utility.ExtractPolicyContextAndCommand(interpreter,
                    clientData, typeof(Class0), 0, ref policyContext,
                    ref match, ref result) == ReturnCode.Ok)
            {
                if (match)
                {
                    //
                    // NOTE: If necessary, create a new random policy decision
                    //       maker to allow this policy to easily demonstrate
                    //       the effects of both the "approved" and "denied"
                    //       decisions.
                    //
                    if (random == null)
                        random = new Random();

                    //
                    // NOTE: Get a random integer; even numbers result in a
                    //       policy approval and odd numbers result in policy
                    //       denial.
                    //
                    if ((random.Next() % 2) == 0)
                        policyContext.Approved("random number is even");
                    else
                        policyContext.Denied("random number is odd");
                }

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }
        #endregion
    }
}
