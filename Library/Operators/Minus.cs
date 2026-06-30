/*
 * Minus.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Interfaces.Private;

namespace Eagle._Operators
{
    /// <summary>
    /// This class implements the Eagle <c>-</c> (minus) expression operator,
    /// which negates its single numeric operand when used in its unary form
    /// and subtracts its second numeric operand from the first when used in
    /// its binary form.  The evaluation itself is provided by the
    /// <see cref="Math" /> base class, selected by the
    /// <see cref="Lexeme.Minus" /> lexeme.  See <c>core_language.md</c> for
    /// expression and operator semantics.
    /// </summary>
    [ObjectId("037ecf98-21d9-4a00-bfdf-c50581be276b")]
    [OperatorFlags(
        OperatorFlags.Special | OperatorFlags.Standard |
        OperatorFlags.Arithmetic)]
    [Lexeme(Lexeme.Minus)]
    [Operands(Arity.UnaryAndBinary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("arithmetic")]
    [ObjectName(Operators.Minus)]
    internal sealed class Minus : Math
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>-</c> minus operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Minus(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
