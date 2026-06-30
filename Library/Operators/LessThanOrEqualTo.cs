/*
 * LessThanOrEqualTo.cs --
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
    /// This class implements the Eagle <c>&lt;=</c> (less than or equal to)
    /// expression operator, which compares its two operands and yields a
    /// boolean result that is true when the left operand is less than or equal
    /// to the right operand.  The evaluation itself is provided by the
    /// <see cref="MaybeString" /> base class, selected by the
    /// <see cref="Lexeme.LessThanOrEqualTo" /> lexeme, so that the comparison
    /// may be performed numerically or as a string depending on the operands.
    /// See <c>core_language.md</c> for expression and operator semantics.
    /// </summary>
    [ObjectId("79406242-64b3-4483-a248-7ad2f48bd6f4")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Relational)]
    [Lexeme(Lexeme.LessThanOrEqualTo)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequality")]
    [ObjectName(Operators.LessThanOrEqualTo)]
    internal sealed class LessThanOrEqualTo : MaybeString
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>&lt;=</c> less than or equal to
        /// operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public LessThanOrEqualTo(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
