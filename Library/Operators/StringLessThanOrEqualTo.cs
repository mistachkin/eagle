/*
 * StringLessThanOrEqualTo.cs --
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
    /// This class implements the Eagle <c>le</c> (string less-than-or-equal-to)
    /// expression operator, which compares its two operands as strings and
    /// yields true when the left operand is lexically less than or equal to the
    /// right operand.  The evaluation itself is provided by the
    /// <see cref="_String" /> base class, selected by the
    /// <see cref="Lexeme.StringLessThanOrEqualTo" /> lexeme.  See
    /// <c>core_language.md</c> for expression and operator semantics.
    /// </summary>
    [ObjectId("2b25af98-8ff9-48e3-8772-b97cc805479b")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.String)]
    [Lexeme(Lexeme.StringLessThanOrEqualTo)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequality")]
    [ObjectName(Operators.StringLessThanOrEqualTo)]
    internal sealed class StringLessThanOrEqualTo : _String
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>le</c> string less-than-or-equal-to
        /// operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public StringLessThanOrEqualTo(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
