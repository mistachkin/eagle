/*
 * List.cs --
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
    /// This class provides the base implementation for the Eagle list
    /// membership expression operators, such as <c>in</c> and <c>ni</c>,
    /// which test whether the left operand is (or is not) an element of the
    /// list given by the right operand.  The specific membership test is
    /// selected by the derived operator via its <see cref="Lexeme" /> and
    /// comparison type.  See <c>core_language.md</c> for expression and
    /// operator semantics.
    /// </summary>
    [ObjectId("f191156e-9cc7-4e76-8ea1-9b29ca8f91a1")]
    [Operands(Arity.Binary)]
    [ObjectGroup("core")]
    internal class List : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the list membership operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public List(
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
        /// This method evaluates the list membership operator.  It extracts the
        /// two operands from the argument list, treating the first as a string
        /// element and the second as a list, and then tests whether the element
        /// is contained in the list according to this operator's
        /// <see cref="Lexeme" /> and comparison type.
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
        /// The list of arguments for this invocation, supplying the two
        /// operands to the membership test.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the boolean result of the membership
        /// test.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the interpreter or argument
        /// list is null, an operand cannot be obtained, or an exception occurs,
        /// with details placed in <paramref name="error" />.
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

                if (Value.GetOperandsFromArguments(interpreter,
                        this, arguments, ValueFlags.String, ValueFlags.List,
                        interpreter.InternalCultureInfo, true, ref operand1,
                        ref operand2, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                return operand1.ListMayContain(
                    this, this.Lexeme, operand2, this.ComparisonType,
                    ref value, ref error);
            }
            catch (Exception e)
            {
                Engine.SetExceptionErrorCode(interpreter, e);

                error = String.Format(
                    "caught math exception: {0}",
                    e);

                return ReturnCode.Error;
            }
        }
        #endregion
    }
}
