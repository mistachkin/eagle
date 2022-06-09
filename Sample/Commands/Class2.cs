/*
 * Class2.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if SAMPLE
using System.Reflection;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Commands = Eagle._Commands;

namespace Sample
{
    /// <summary>
    /// Declare a "custom command" class that inherits default functionality
    /// and implements the appropriate interface(s).
    /// </summary>
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("80546f26-3338-4c12-a165-ea5cef6ea879")]
    [CommandFlags(CommandFlags.Unsafe)]
    [ObjectGroup("example")]
    internal sealed class Class2 : _Commands.Default
    {
        #region Private Constants
#if SAMPLE
        /// <summary>
        /// The binding flags used to access the shared state field.
        /// </summary>
        private const BindingFlags bindingFlags =
            BindingFlags.Static | BindingFlags.NonPublic |
            BindingFlags.GetField;

        /// <summary>
        /// The example return code that indicates the shared state field is
        /// invalid.
        /// </summary>
        private const uint InvalidSharedState = 101;
#endif

        /// <summary>
        /// The example return code that indicates successful execution of the
        /// custom command.
        /// </summary>
        private const uint EverythingOk = 100;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// This value will be non-zero if the traced variable was added to the
        /// interpreter.
        /// </summary>
        private bool addedVariable = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructor (Required)
        /// <summary>
        /// Constructs an instance of this custom command class.
        /// </summary>
        /// <param name="commandData">
        /// An instance of the command data class containing the properties
        /// used to initialize the new instance of this custom command class.
        /// Typically, the only thing done with this parameter is to pass it
        /// along to the base class constructor.
        /// </param>
        public Class2(
            ICommandData commandData /* in */
            )
            : base(commandData)
        {
            //
            // NOTE: Typically, nothing much is done here.  Any non-trivial
            //       initialization should be done in IState.Initialize and
            //       cleaned up in IState.Terminate.
            //
            this.Flags |= Utility.GetCommandFlags(GetType().BaseType) |
                Utility.GetCommandFlags(this); /* HIGHLY RECOMMENDED */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods (Optional)
        /// <summary>
        /// This method is used to test how the custom script binder handles
        /// the marshalling of instances of this class.
        /// </summary>
        /// <param name="class2">
        /// The instance of this class being marshalled.
        /// </param>
        /// <returns>
        /// The same value as <paramref name="class2" />.
        /// </returns>
        public static Class2 TestMethod(
            Class2 class2
            )
        {
            return class2;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Custom Variable Trace Callback (Optional)
        /// <summary>
        /// This is a custom TraceCallback that intercepts attempts to get,
        /// set, or unset monitored variables.
        /// </summary>
        /// <param name="type">
        /// The type of variable operation we are being called in response to.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="traceInfo">
        /// An object containing detailed information about the variable
        /// operation in progress.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain the result of the callback.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        [MethodFlags(MethodFlags.VariableTrace | MethodFlags.NoAdd)]
        private ReturnCode PluginTraceCallback(
            BreakpointType breakpointType, /* in */
            Interpreter interpreter,       /* in */
            ITraceInfo traceInfo,          /* in */
            ref Result result              /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (traceInfo == null)
            {
                result = "invalid trace";
                return ReturnCode.Error;
            }

            //
            // NOTE: Intercept attempted get operations on variables
            //       that we are monitoring.
            //
            if (breakpointType == BreakpointType.BeforeVariableGet)
            {
                //
                // NOTE: Demonstrate that we can return a value that is
                //       totally dynamic and totally unrelated to the
                //       actual variable itself.
                //
                result = Utility.GetUtcNow();

                traceInfo.Cancel = true;
                traceInfo.ReturnCode = ReturnCode.Ok;
            }
            else if (breakpointType == BreakpointType.BeforeVariableUnset)
            {
                //
                // NOTE: Demonstrate that we can forbid operations from
                //       taking place.
                //
                result = String.Format(
                    "cannot unset \"{0}\", it is logically read-only",
                    traceInfo.Name);

                traceInfo.Cancel = true;
                traceInfo.ReturnCode = ReturnCode.Error;
            }

            return traceInfo.ReturnCode;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members (Optional)
        /// <summary>
        /// Optionally initializes any state information required by the
        /// command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied when this command was initially created, if
        /// any.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain the result of the command.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public override ReturnCode Initialize(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ref Result result        /* out */
            )
        {
            //
            // NOTE: We require a valid interpreter context.
            //
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // NOTE: Does the variable we are interested in already exist?
            //       If so, skip adding it; unfortunately, this means that
            //       we cannot add the variable trace.
            //
            VariableFlags varFlags = VariableFlags.GlobalOnly;
            string varName = GetType().Name;

            if (interpreter.DoesVariableExist(
                    varFlags, varName) != ReturnCode.Ok)
            {
                //
                // NOTE: Grab the plugin that owns us.
                //
                IPlugin plugin = this.Plugin;

                //
                // NOTE: Add a variable that has our custom variable trace
                //       callback.
                //
                TraceList traces = new TraceList(
                    clientData, TraceFlags.None, plugin,
                    new TraceCallback[] { PluginTraceCallback }
                );

                if (interpreter.AddVariable(
                        varFlags, varName, traces, true,
                        ref result) == ReturnCode.Ok)
                {
                    addedVariable = true;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }

            return base.Initialize(interpreter, clientData, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Optionally terminates state information required by the command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied when this command was initially created,
        /// if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain the result of the command.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public override ReturnCode Terminate(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ref Result result        /* out */
            )
        {
            //
            // NOTE: We require a valid interpreter context.
            //
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // NOTE: Skip unsetting our variable (and its custom trace) if we
            //       never actually added it -OR- if it no longer exists.
            //
            VariableFlags varFlags = VariableFlags.GlobalOnly;
            string varName = GetType().Name;

            if (addedVariable && (interpreter.DoesVariableExist(
                    varFlags, varName) == ReturnCode.Ok))
            {
                //
                // NOTE: Remove the variable we previously added to the
                //       interpreter with our custom trace callback.
                //
                if (interpreter.UnsetVariable(
                        VariableFlags.GlobalOnly | VariableFlags.NoTrace |
                        VariableFlags.Purge | VariableFlags.NoReady,
                        GetType().Name, ref result) == ReturnCode.Ok)
                {
                    addedVariable = false;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }

            return base.Terminate(interpreter, clientData, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members (Required)
        /// <summary>
        /// Execute the command and return the appropriate result and return
        /// code.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied when this command was initially created, if
        /// any.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments supplied to this command by the script being
        /// evaluated.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain the result of the command.
        /// Upon failure, this must contain an appropriate error message.
        /// If no result is explicitly set by the command, a default result
        /// of null will be used by the engine.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.  Other
        /// return codes may be used to implement custom control structures.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            //
            // NOTE: In truth, almost everything in this method is optional;
            //       however, there are certain conventions present in this
            //       example that custom commands are encouraged to follow.
            //
            if (interpreter == null)
            {
                //
                // NOTE: We require a valid interpreter context.  Most custom
                //       commands will want to do this because it is a fairly
                //       standard safety check (HIGHLY RECOMMENDED).
                //
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                //
                // NOTE: We require a valid argument list.  Most custom
                //       commands will want to do this because it is a fairly
                //       standard safety check (HIGHLY RECOMMENDED).
                //
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            // ****************** BEGIN CUSTOM COMMAND CODE *******************
            ///////////////////////////////////////////////////////////////////

            if (arguments.Count != 2)
            {
                //
                // TODO: Use the ISyntax interface to get this error string
                //       from the "resource manager".
                //
                result = Utility.WrongNumberOfArguments(
                    this, 1, arguments, "string");

                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            //
            // NOTE: This is where the command should actually do something
            //       useful (i.e. now that all the method parameters have
            //       passed at least basic validity checks).  The requirements
            //       imposed by Eagle for custom command (IExecute)
            //       implementations are:
            //
            //       1. Upon success the Execute method must return the
            //          "ReturnCode.Ok" enumeration value.
            //
            //       2. Upon success, the result of the command, if there is
            //          any, should be stored in the "result" parameter,
            //          passed to this method by reference.  If the command
            //          does not produce a result that is meaningful to the
            //          caller, the value "String.Empty" should be used
            //          instead.
            //
            //       3. Upon failure, the Execute method must return the
            //          "ReturnCode.Error" enumeration value.
            //
            //       4. Upon failure, the error message or exception, if there
            //          is any, must be stored in the "result" parameter,
            //          passed to this method by reference.  Do not use a null
            //          value or the value "String.Empty" for the error
            //          message.
            //
            //       5. All the other "ReturnCode" enumeration values are
            //          reserved for use by custom control structure commands
            //          implemented in managed code (e.g. "Return", "Break",
            //          and "Continue").
            //
            //       The try block here is optional; however, custom commands
            //       are "officially" discouraged from raising exceptions,
            //       especially if they are allowed to escape the Execute
            //       method.
            //
            ///////////////////////////////////////////////////////////////////

            ReturnCode code;

#if SAMPLE
            try
            {
                Type type = null;

                if (clientData != null)
                    type = clientData.Data as Type;
                else if (this.ClientData != null)
                    type = this.ClientData.Data as Type;

                if (type != null)
                {
                    //
                    // NOTE: Try to fetch the shared state from the class that
                    //       created us.
                    //
                    int sharedState = (int)type.InvokeMember(
                        "sharedState", bindingFlags, null, null, null);

                    //
                    // NOTE: Build the result using whatever methods are
                    //       appropriate for the particular command.
                    //
                    result = arguments[1] + Characters.Space +
                        Environment.TickCount.ToString() +
                        Characters.Space + sharedState.ToString();

                    //
                    // NOTE: Success, use our custom "Ok" return code.
                    //
                    code = Utility.CustomOkCode(EverythingOk);
                }
                else
                {
                    //
                    // TODO: Use "resource manager" to get this error string.
                    //
                    result = "invalid shared state type";

                    //
                    // NOTE: Failure, use our custom "Error" return code.
                    //
                    code = Utility.CustomErrorCode(InvalidSharedState);
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }
#else
            //
            // NOTE: Return their argument with the current system tick count
            //       appended to it.
            //
            result = arguments[1] + Characters.Space +
                Environment.TickCount.ToString();

            //
            // NOTE: Success, use our custom "Ok" return code.
            //
            code = Utility.CustomOkCode(EverythingOk);
#endif

            //
            // NOTE: Finally, return one of the standard status codes that
            //       indicate success / failure of this command OR a custom
            //       status code that indicates success / failure of this
            //       command.
            //
            return code;

            ///////////////////////////////////////////////////////////////////
            // ******************* END CUSTOM COMMAND CODE ********************
            ///////////////////////////////////////////////////////////////////
        }
        #endregion
    }
}
