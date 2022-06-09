/*
 * VariableAssignment.cs --
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
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Operators
{
    [ObjectId("e46b3b67-cae1-4142-8c4f-af6cd776ced6")]
    [OperatorFlags(OperatorFlags.NonStandard | OperatorFlags.Assignment)]
    [Lexeme(Lexeme.VariableAssignment)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("assignment")]
    [ObjectName(Operators.VariableAssignment)]
    internal sealed class VariableAssignment : Core
    {
        public VariableAssignment(
            IOperatorData operatorData
            )
            : base(operatorData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
                if (!interpreter.InternalIsSafeOrSdk())
                {
                    if (arguments != null)
                    {
                        try
                        {
                            string operatorName = null; /* NOT USED */
                            Variant operand1 = null;
                            Variant operand2 = null;

                            code = Value.GetOperandsFromArguments(
                                interpreter, this, arguments,
                                ValueFlags.String, ValueFlags.None,
                                interpreter.InternalCultureInfo,
                                false, ref operatorName, ref operand1,
                                ref operand2, ref error);

                            if (code == ReturnCode.Ok)
                            {
                                code = interpreter.SetVariableValue2(
                                    VariableFlags.None, null,
                                    (string)operand1.Value,
                                    operand2.Value, ref error);

                                if (code == ReturnCode.Ok)
                                    value = operand2;
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
                    //
                    // BUGBUG: Technically, this error message is not 100%
                    //         accurate.  If the interpreter was created
                    //         for use with the security SDK, it may not
                    //         be "safe"; however, to keep things simple,
                    //         just use that error message.
                    //
                    error = String.Format(
                        "permission denied: safe interpreter cannot use operator {0}",
                        FormatOps.WrapOrNull(base.Name));

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
