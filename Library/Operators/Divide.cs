/*
 * Divide.cs --
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
    /// This class implements the Eagle <c>/</c> (division) expression operator,
    /// which divides its first numeric operand by its second numeric operand.
    /// The evaluation itself is provided by the <see cref="Math" /> base class,
    /// selected by the <see cref="Lexeme.Divide" /> lexeme.  See
    /// <c>core_language.md</c> for expression and operator semantics.
    /// </summary>
    [ObjectId("99261dd9-726c-4559-b2e4-e80aff0f2f6d")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Arithmetic)]
    [Lexeme(Lexeme.Divide)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("arithmetic")]
    [ObjectName(Operators.Divide)]
    internal sealed class Divide : Math
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>/</c> division operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Divide(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
