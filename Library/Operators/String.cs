/*
 * String.cs --
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
    /// <summary>
    /// This class provides the base implementation for the Eagle string
    /// comparison expression operators (e.g. <c>eq</c>, <c>ne</c>, <c>lt</c>,
    /// <c>gt</c>, <c>le</c>, <c>ge</c>).  Derived operators evaluate their two
    /// operands as strings and compare them, with the specific comparison
    /// selected by the operator <see cref="Lexeme" />.  See
    /// <c>core_language.md</c> for expression and operator semantics.
    /// </summary>
    [ObjectId("bc45ebda-e05b-4750-afdc-7dc42409e87b")]
    [Operands(Arity.Binary)]
    [ObjectGroup("core")]
    internal class _String : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of a string comparison operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public _String(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            if ((operatorData == null) || !FlagOps.HasFlags(
                    operatorData.Flags, OperatorFlags.NoAttributes, true))
            {
                this.Flags |=
                    AttributeOps.GetOperatorFlags(GetType().BaseType) |
                    AttributeOps.GetOperatorFlags(this);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        /// <summary>
        /// This method evaluates a string comparison operator.  It obtains the
        /// two operands from the argument list, converts them to strings, and
        /// compares them according to the operator <see cref="Lexeme" /> and
        /// the configured comparison type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this operator is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, operator-specific data supplied when this operator was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation, which supplies the two
        /// operands to be compared.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the result of the string comparison.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the interpreter or argument
        /// list is invalid, an operand cannot be obtained, or an exception
        /// occurs, with details placed in <paramref name="error" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Argument value,      /* out */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                error = "invalid argument list";
                return ReturnCode.Error;
            }

            try
            {
                IVariant operand1 = null;
                IVariant operand2 = null;

                if (Value.GetOperandsFromArguments(
                        interpreter, this, arguments, ValueFlags.String,
                        interpreter.InternalCultureInfo, false, ref operand1,
                        ref operand2, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                return operand1.StringCompare(
                    this, this.Lexeme, operand2, this.ComparisonType,
                    ref value, ref error);
            }
            catch (Exception e)
            {
                Engine.SetExceptionErrorCode(interpreter, e);

                error = String.Format("caught math exception: {0}", e);

                return ReturnCode.Error;
            }
        }
        #endregion
    }
}
