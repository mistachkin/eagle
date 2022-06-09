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
        public Error(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ReturnCode code;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if ((arguments.Count >= 2) && (arguments.Count <= 5))
                    {
                        code = ReturnCode.Ok;

                        if ((code == ReturnCode.Ok) && 
                            (arguments.Count >= 3) && 
                            !String.IsNullOrEmpty(arguments[2]))
                        {
                            //
                            // BUGFIX: The error line must be set manually now
                            //         because the engine itself will not set
                            //         it once the "error already logged" flag
                            //         has been set by this command (just below).
                            //
                            Engine.SetErrorLine(interpreter, true);

                            //
                            // BUGFIX: Prevent messing up custom errorInfo by passing 
                            //         an empty string for the eventual result here.
                            //
                            Engine.AddErrorInformation(
                                interpreter, String.Empty, arguments[2]);

                            Engine.SetErrorAlreadyLogged(interpreter, true);
                        }

                        if ((code == ReturnCode.Ok) && 
                            (arguments.Count >= 4) && 
                            !String.IsNullOrEmpty(arguments[3]))
                        {
                            /* IGNORED */
                            interpreter.SetVariableValue( /* EXEMPT */
                                Engine.ErrorCodeVariableFlags,
                                TclVars.Core.ErrorCode,
                                arguments[3], null);

                            Engine.SetErrorCodeSet(interpreter, true);
                        }

                        ReturnCode returnCode = ReturnCode.Error; /* default to the "normal" error code. */
                        
                        if ((code == ReturnCode.Ok) && (arguments.Count >= 5))
                            code = Value.GetReturnCode2(arguments[4], ValueFlags.AnyReturnCode, 
                                interpreter.InternalCultureInfo, ref returnCode, ref result);

                        //
                        // NOTE: If we managed to process all the arguments correctly, 
                        //       set the requested error message and return code.
                        //
                        if (code == ReturnCode.Ok)
                        {
                            result = arguments[1];
                            code = returnCode;
                        }                        
                    }
                    else
                    {
                        result = "wrong # args: should be \"error message ?errorInfo? ?errorCode? ?returnCode?\"";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    result = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
