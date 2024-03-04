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
    internal sealed class Typeof : Arguments
    {
        #region Public Constructors
        public Typeof(
            IFunctionData functionData /* in */
            )
            : base(functionData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Argument value,      /* out */
            ref Result error         /* out */
            )
        {
            if (base.Execute(
                    interpreter, clientData, arguments, ref value,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            Argument argument = arguments[1];

            if (argument != null)
            {
                object argumentValue = argument.Value;

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
                else if (argumentValue is IVariant)
                {
                    value = "variant";
                }
                else if (argumentValue is INumber)
                {
                    value = "number";
                }
                else if (argumentValue is Result)
                {
                    value = "result";
                }
                else if (argumentValue is IValue)
                {
                    value = "value";
                }
                else if (argumentValue is IGetValue)
                {
                    value = "getValue";
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
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }
        #endregion
    }
}
