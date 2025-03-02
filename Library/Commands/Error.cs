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
    [ObjectId("fe60e0b8-3d89-4c48-9115-c02b9917424b")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("control")]
    internal sealed class Error : Core
    {
        #region Public Constructors
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
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
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
                    result = "invalid arguments";
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
