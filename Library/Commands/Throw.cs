/*
 * Throw.cs --
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

namespace Eagle._Commands
{
    [ObjectId("c51886a9-e2d6-4cb9-ab39-9258bc2baeb9")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("control")]
    internal sealed class Throw : Core
    {
        public Throw(
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
                    if ((arguments.Count >= 2) && (arguments.Count <= 4))
                    {
                        code = ReturnCode.Ok;

                        //
                        // NOTE: Default to the "normal" error return code.
                        //
                        ReturnCode returnCode = ReturnCode.Error;

                        if ((code == ReturnCode.Ok) && (arguments.Count >= 3))
                        {
                            code = Value.GetReturnCode2(arguments[2], ValueFlags.AnyReturnCode, 
                                interpreter.InternalCultureInfo, ref returnCode, ref result);
                        }

                        Exception innerException = null;

                        if ((code == ReturnCode.Ok) && (arguments.Count >= 4))
                        {
                            IObject @object = null;

                            code = interpreter.GetObject(
                                arguments[3], LookupFlags.Default, ref @object, ref result);

                            if ((code == ReturnCode.Ok) && (@object != null))
                            {
                                innerException = @object.Value as Exception;

                                if (innerException == null)
                                {
                                    result = String.Format(
                                        "object \"{0}\" is not an exception", 
                                        arguments[3]);

                                    code = ReturnCode.Error;
                                }
                            }
                        }
                        
                        //
                        // NOTE: If we managed to process all the arguments correctly, 
                        //       set the requested error message and throw a script 
                        //       exception.  This exception will not escape the script 
                        //       engine; however, it will be properly stored in the 
                        //       result.
                        //
                        if (code == ReturnCode.Ok)
                            throw new ScriptException(returnCode, arguments[1], innerException);
                    }
                    else
                    {
                        result = "wrong # args: should be \"throw message ?returnCode? ?innerException?\"";
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
