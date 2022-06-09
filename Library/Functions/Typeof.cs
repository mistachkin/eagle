/*
 * Typeof.cs --
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

namespace Eagle._Functions
{
    [ObjectId("5fe20712-cd80-4329-b889-5233b3052c60")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("conversion")]
    internal sealed class Typeof : Core
    {
        public Typeof(
            IFunctionData functionData
            )
            : base(functionData)
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
                    if (arguments.Count == (this.Arguments + 1))
                    {
                        if (arguments[1] != null)
                        {
                            object argumentValue = arguments[1].Value;

                            if (argumentValue is bool)
                            {
                                value = "bool";
                            }
                            else if (argumentValue is sbyte)
                            {
                                value = "sbyte";
                            }
                            else if (argumentValue is byte)
                            {
                                value = "byte";
                            }
                            else if (argumentValue is short)
                            {
                                value = "short";
                            }
                            else if (argumentValue is ushort)
                            {
                                value = "ushort";
                            }
                            else if (argumentValue is char)
                            {
                                value = "char";
                            }
                            else if (argumentValue is int)
                            {
                                value = "int";
                            }
                            else if (argumentValue is uint)
                            {
                                value = "uint";
                            }
                            else if (argumentValue is long)
                            {
                                value = "wide";
                            }
                            else if (argumentValue is ulong)
                            {
                                value = "ulong";
                            }
                            else if (argumentValue is ReturnCode)
                            {
                                value = "returnCode";
                            }
                            else if (argumentValue is decimal)
                            {
                                value = "decimal";
                            }
                            else if (argumentValue is double)
                            {
                                value = "double";
                            }
                            else if (argumentValue is float)
                            {
                                value = "float";
                            }
                            else if (argumentValue is DateTime)
                            {
                                value = "dateTime";
                            }
                            else if (argumentValue is TimeSpan)
                            {
                                value = "timeSpan";
                            }
                            else if (argumentValue is Guid)
                            {
                                value = "guid";
                            }
                            else if (argumentValue is Version)
                            {
                                value = "version";
                            }
                            else if (argumentValue is Uri)
                            {
                                value = "uri";
                            }
                            else if (argumentValue is string)
                            {
                                value = "string";
                            }
                            else if (argumentValue is StringList)
                            {
                                value = "list";
                            }
                            else if (argumentValue is StringDictionary)
                            {
                                value = "dictionary";
                            }
                            else if (argumentValue is Argument)
                            {
                                value = "argument";
                            }
                            else if (argumentValue is Number)
                            {
                                value = "number";
                            }
                            else if (argumentValue is Variant)
                            {
                                value = "variant";
                            }
                            else if (argumentValue is Result)
                            {
                                value = "result";
                            }
                            else if (argumentValue != null)
                            {
                                value = FormatOps.TypeName(
                                    argumentValue, String.Empty,
                                    String.Empty, false);
                            }
                            else
                            {
                                value = String.Empty;
                            }
                        }
                        else
                        {
                            error = "invalid argument";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        if (arguments.Count > (this.Arguments + 1))
                            error = String.Format(
                                "too many arguments for math function {0}",
                                FormatOps.WrapOrNull(base.Name));
                        else
                            error = String.Format(
                                "too few arguments for math function {0}",
                                FormatOps.WrapOrNull(base.Name));

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
