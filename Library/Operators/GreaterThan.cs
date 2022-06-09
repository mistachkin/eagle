/*
 * GreaterThan.cs --
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
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Operators
{
    [ObjectId("12161625-71ff-4150-bcf4-7b1b6b015915")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Relational |
        OperatorFlags.Initialize)]
    [Lexeme(Lexeme.GreaterThan)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequality")]
    [ObjectName(Operators.GreaterThan)]
    internal sealed class GreaterThan : Core
    {
        public GreaterThan(
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
                            code = Value.FixupVariants(
                                this, operand1, operand2, null, null, false, false);

                            if (code == ReturnCode.Ok)
                            {
                                if (operand1.IsDouble())
                                {
                                    value = ((double)operand1.Value > (double)operand2.Value);
                                }
                                else if (operand1.IsDecimal())
                                {
                                    value = ((decimal)operand1.Value > (decimal)operand2.Value);
                                }
                                else if (operand1.IsWideInteger())
                                {
                                    value = ((long)operand1.Value > (long)operand2.Value);
                                }
                                else if (operand1.IsInteger())
                                {
                                    value = ((int)operand1.Value > (int)operand2.Value);
                                }
                                else if (operand1.IsBoolean())
                                {
                                    value = (ConversionOps.ToInt((bool)operand1.Value) >
                                        ConversionOps.ToInt((bool)operand2.Value));
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
                                //
                                // NOTE: Fine, try to treat the operands as strings.
                                //
                                code = Value.FixupStringVariants(
                                    this, operand1, operand2, ref error);

                                if (code == ReturnCode.Ok)
                                {
                                    if (operand1.IsString())
                                    {
                                        value = (SharedStringOps.Compare(
                                            (string)operand1.Value, (string)operand2.Value,
                                            this.ComparisonType) > 0);
                                    }
                                    else
                                    {
                                        error = String.Format(
                                            "unsupported operand type for operator {0}",
                                            FormatOps.OperatorName(operatorName, this.Lexeme));

                                        code = ReturnCode.Error;
                                    }
                                }
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

