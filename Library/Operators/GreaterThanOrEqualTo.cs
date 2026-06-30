/*
 * GreaterThanOrEqualTo.cs --
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
    /// This class implements the Eagle <c>&gt;=</c> (greater-than-or-equal-to)
    /// expression operator, which compares its two operands and returns a
    /// boolean indicating whether the left operand is greater than or equal to
    /// the right operand.  The evaluation itself is provided by the
    /// <see cref="MaybeString" /> base class, selected by the
    /// <see cref="Lexeme.GreaterThanOrEqualTo" /> lexeme, which permits both
    /// numeric and string comparisons.  See <c>core_language.md</c> for
    /// expression and operator semantics.
    /// </summary>
    [ObjectId("3a75021c-ca94-4766-b2ec-b56b18f10bfd")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Relational)]
    [Lexeme(Lexeme.GreaterThanOrEqualTo)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequality")]
    [ObjectName(Operators.GreaterThanOrEqualTo)]
    internal sealed class GreaterThanOrEqualTo : MaybeString
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>&gt;=</c> greater-than-or-equal-to
        /// operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public GreaterThanOrEqualTo(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
