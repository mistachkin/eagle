/*
 * StringLessThan.cs --
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
    /// This class implements the Eagle <c>lt</c> (string less-than) expression
    /// operator, which compares its two operands as strings and yields a
    /// boolean indicating whether the first operand is lexicographically less
    /// than the second.  The evaluation itself is provided by the
    /// <see cref="_String" /> base class, selected by the
    /// <see cref="Lexeme.StringLessThan" /> lexeme.  See <c>core_language.md</c>
    /// for expression and operator semantics.
    /// </summary>
    [ObjectId("76967830-ce3a-41c0-ae62-6662c4a09b59")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.String)]
    [Lexeme(Lexeme.StringLessThan)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequality")]
    [ObjectName(Operators.StringLessThan)]
    internal sealed class StringLessThan : _String
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>lt</c> string less-than operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public StringLessThan(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
