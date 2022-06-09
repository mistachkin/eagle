/*
 * Return.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("a54f8720-ba76-476c-91b1-3f140d587c70")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard |
        CommandFlags.Initialize)]
    [ObjectGroup("control")]
    internal sealed class Return : Core
    {
        public Return(
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
                    if (arguments.Count >= 1)
                    {
                        OptionDictionary options = new OptionDictionary(
                            new IOption[] { 
                            new Option(null, OptionFlags.MustHaveReturnCodeValue, Index.Invalid, Index.Invalid, "-code", null),
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-errorinfo", null),
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-errorcode", null)
                        });

                        int argumentIndex = Index.Invalid;

                        if (arguments.Count > 1)
                            code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, false, ref argumentIndex, ref result);
                        else
                            code = ReturnCode.Ok;
                        
                        if (code == ReturnCode.Ok)
                        {
                            if ((argumentIndex == Index.Invalid) || ((argumentIndex + 1) == arguments.Count))
                            {
                                //
                                // NOTE: Always reset these first.  They may be set to something
                                //       below.
                                //
                                interpreter.ErrorInfo = null;
                                interpreter.ErrorCode = null;

                                //
                                // NOTE: Initialize the variable that will hold the supplied option 
                                //       values, if any.
                                //
                                Variant value = null;

                                //
                                // NOTE: Process the -code option.
                                //
                                if (options.IsPresent("-code", ref value))
                                    interpreter.ReturnCode = (ReturnCode)value.Value;
                                else
                                    interpreter.ReturnCode = ReturnCode.Ok;

                                //
                                // NOTE: If the -code option indicates an error then we also want to 
                                //       process the values for the -errorinfo and -errorcode options.
                                //
                                if (interpreter.ReturnCode == ReturnCode.Error)
                                {
                                    if (options.IsPresent("-errorcode", ref value))
                                    {
                                        interpreter.ErrorCode = value.ToString();
                                        Engine.SetErrorCodeSet(interpreter, true);
                                    }

                                    if (options.IsPresent("-errorinfo", ref value))
                                    {
                                        interpreter.ErrorInfo = value.ToString();
                                        Engine.SetErrorInProgress(interpreter, true);
                                    }
                                }

                                //
                                // NOTE: Is an actual string value being returned?
                                //
                                if (argumentIndex != Index.Invalid)
                                    result = arguments[argumentIndex];
                                else
                                    result = String.Empty;

                                code = ReturnCode.Return;
                            }
                            else
                            {
                                if ((argumentIndex != Index.Invalid) &&
                                    Option.LooksLikeOption(arguments[argumentIndex]))
                                {
                                    result = OptionDictionary.BadOption(
                                        options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                }
                                else
                                {
                                    result = "wrong # args: should be \"return ?-code code? ?-errorinfo info? ?-errorcode code? ?string?\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"return ?-code code? ?-errorinfo info? ?-errorcode code? ?string?\"";
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
