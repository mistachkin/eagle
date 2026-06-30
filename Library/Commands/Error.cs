/*
 * Error.cs --
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
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>error</c> command, which raises a
    /// script error, optionally supplying the error message, the
    /// <c>errorInfo</c> stack trace, the <c>errorCode</c> value, and the
    /// return code carried out of the failure.  See <c>core_language.md</c>
    /// for the command syntax and semantics.
    /// </summary>
    [ObjectId("fe60e0b8-3d89-4c48-9115-c02b9917424b")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("control")]
    internal sealed class Error : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>error</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Error(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>error</c> command.  It accepts an
        /// optional error message together with optional <c>errorInfo</c>,
        /// <c>errorCode</c>, and return code arguments, records the requested
        /// error information on the interpreter, and returns the requested
        /// return code (defaulting to <see cref="ReturnCode.Error" />) so the
        /// failure propagates through the engine.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// command name; an optional element one supplies the error message,
        /// element two the <c>errorInfo</c> stack-trace text, element three
        /// the <c>errorCode</c> value, and element four the return code to
        /// raise.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the error message (either the supplied
        /// message or the interpreter's last error when none was given).  Upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The requested return code (defaulting to
        /// <see cref="ReturnCode.Error" />) when the arguments are processed
        /// successfully; otherwise, <see cref="ReturnCode.Error" /> when the
        /// wrong number of arguments is supplied, the interpreter is null, the
        /// argument list is null, the return code cannot be parsed, or no last
        /// error is available, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            bool setLastError = true;

            try
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (arguments == null)
                {
                    result = "invalid argument list";
                    return ReturnCode.Error;
                }

                int argumentCount = arguments.Count;

                if ((argumentCount < 1) || (argumentCount > 5))
                {
                    result = String.Format(
                        "wrong # args: should be \"{0} ?message? ?errorInfo? ?errorCode? ?returnCode?\"",
                        this.Name);

                    return ReturnCode.Error;
                }

                if ((argumentCount >= 3) &&
                    !String.IsNullOrEmpty(arguments[2]))
                {
                    //
                    // BUGFIX: The error line must be set manually now
                    //         because the engine itself will not set
                    //         it once the "error already logged" flag
                    //         has been set by this command (just below).
                    //
                    /* IGNORED */
                    Engine.SetErrorLine(interpreter, true);

                    //
                    // BUGFIX: Prevent messing up custom info by passing
                    //         empty string for the eventual result here.
                    //
                    /* IGNORED */
                    Engine.AddErrorInformation(
                        interpreter, String.Empty, arguments[2]);

                    /* IGNORED */
                    Engine.SetErrorAlreadyLogged(interpreter, true);
                }

                if ((argumentCount >= 4) &&
                    !String.IsNullOrEmpty(arguments[3]))
                {
                    /* IGNORED */
                    interpreter.SetVariableValue( /* EXEMPT */
                        Engine.ErrorCodeVariableFlags,
                        TclVars.Core.ErrorCode,
                        arguments[3], null);

                    /* IGNORED */
                    Engine.SetErrorCodeSet(interpreter, true);
                }

                //
                // NOTE: Default to the "normal" error return code.
                //
                ReturnCode returnCode = ReturnCode.Error;

                if ((argumentCount >= 5) && (Value.GetReturnCode2(
                        arguments[4], ValueFlags.AnyReturnCode,
                        interpreter.InternalCultureInfo, ref returnCode,
                        ref result) != ReturnCode.Ok))
                {
                    return ReturnCode.Error;
                }

                //
                // NOTE: If we managed to process all arguments correctly,
                //       set the requested error message and return code.
                //
                if (argumentCount > 1)
                {
                    Argument error = arguments[1];

                    if (!String.IsNullOrEmpty(error))
                    {
                        result = error;
                    }
                    else
                    {
                        setLastError = false;

                        if (!interpreter.TryUseLastError(ref result))
                            return ReturnCode.Error;
                    }
                }
                else
                {
                    setLastError = false;

                    if (!interpreter.TryUseLastError(ref result))
                        return ReturnCode.Error;
                }

                return returnCode;
            }
            finally
            {
                if (setLastError && (interpreter != null))
                {
                    /* IGNORED */
                    interpreter.MaybeSetLastError(result);
                }
            }
        }
        #endregion
    }
}
