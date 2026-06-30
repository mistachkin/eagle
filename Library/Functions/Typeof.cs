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

#if NET_40
using System.Numerics;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    /// <summary>
    /// This class implements the Eagle <c>typeof</c> expression function, which
    /// returns the name of the underlying value type of its single argument.
    /// See <c>core_language.md</c> for expression and function semantics.
    /// </summary>
    [ObjectId("5fe20712-cd80-4329-b889-5233b3052c60")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("conversion")]
    internal sealed class Typeof : Arguments
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>typeof</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method evaluates the <c>typeof</c> function.  It validates the
        /// arguments using the base implementation, inspects the runtime type
        /// of the single argument's underlying value, and produces a string
        /// naming that type (for example, <c>int</c>, <c>double</c>,
        /// <c>string</c>, or <c>list</c>).  For values whose type is not one of
        /// the well-known intrinsic types, the formatted type name is returned;
        /// for a null underlying value, an empty string is returned.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this function is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, function-specific data supplied when this function was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// function name; element one is the value whose type name is
        /// determined.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the name of the underlying value type.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the argument is missing or
        /// invalid, with details placed in <paramref name="error" />.
        /// </returns>
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
#if NET_40
                else if (argumentValue is BigInteger)
                {
                    value = "entier";
                }
#endif
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
