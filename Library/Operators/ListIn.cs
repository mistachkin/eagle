/*
 * ListIn.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Operators
{
    [ObjectId("ad5c5e98-2cd0-4d20-81c7-f0a7d9dceeb1")]
    [OperatorFlags(OperatorFlags.Standard | OperatorFlags.List)]
    [Lexeme(Lexeme.ListIn)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("membership")]
    [ObjectName(Operators.ListIn)]
    internal sealed class ListIn : Core
    {
        public ListIn(
            IOperatorData operatorData
            )
            : base(operatorData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Argument value,
            ref Result error
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    try
                    {
                        string operatorName = null; /* NOT USED */
                        Variant operand1 = null;
                        Variant operand2 = null;

                        code = Value.GetOperandsFromArguments(
                            interpreter, this, arguments, ValueFlags.String,
                            ValueFlags.List, interpreter.InternalCultureInfo,
                            true, ref operatorName, ref operand1, ref operand2,
                            ref error);

                        if (code == ReturnCode.Ok)
                        {
                            value = ((StringList)operand2.Value).Contains(
                                (string)operand1.Value);
                        }
                    }
                    catch (Exception e)
                    {
                        Engine.SetExceptionErrorCode(interpreter, e);

                        error = String.Format(
                            "caught math exception: {0}",
                            e);

                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    error = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                error = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}

