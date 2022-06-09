/*
 * BitwiseNot.cs --
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
    [ObjectId("1e9331a3-b6be-45c1-aa04-5063410d3f09")]
    [OperatorFlags(OperatorFlags.Standard | OperatorFlags.Bitwise)]
    [Lexeme(Lexeme.BitwiseNot)]
    [Operands(Arity.Unary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("bitwise")]
    [ObjectName(Operators.BitwiseNot)]
    internal sealed class BitwiseNot : Core
    {
        public BitwiseNot(
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
                        string operatorName = null;
                        Variant operand1 = null;
                        Variant operand2 = null;

                        code = Value.GetOperandsFromArguments(
                            interpreter, this, arguments, ValueFlags.AnyVariant,
                            interpreter.InternalCultureInfo, false, ref operatorName,
                            ref operand1, ref operand2, ref error);

                        if (code == ReturnCode.Ok)
                        {
                            if (operand1 != null)
                            {
                                //
                                // BUGBUG: This is not correct.  We need to use the smallest type
                                //         possible here (this will have to be corrected in
                                //         GetVariant).
                                //
                                if (operand1.IsWideInteger())
                                {
                                    value = ~(long)operand1.Value;
                                }
                                else if (operand1.IsInteger())
                                {
                                    value = ~(int)operand1.Value;
                                }
                                else if (operand1.IsBoolean())
                                {
                                    value = ~ConversionOps.ToInt((bool)operand1.Value);
                                }
                                else
                                {
                                    error = String.Format(
                                        "unsupported operand type for operator {0}",
                                        FormatOps.OperatorName(operatorName, this.Lexeme));

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = String.Format(
                                    "operand for operator {0} is invalid",
                                    FormatOps.OperatorName(operatorName, this.Lexeme));

                                code = ReturnCode.Error;
                            }
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

