/*
 * StringGreaterThanOrEqualTo.cs --
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
    /// This class implements the Eagle <c>ge</c> (string greater-than-or-equal-to)
    /// expression operator, which compares its two operands as strings and yields
    /// a boolean indicating whether the first is lexically greater than or equal to
    /// the second.  The evaluation itself is provided by the <see cref="_String" />
    /// base class, selected by the <see cref="Lexeme.StringGreaterThanOrEqualTo" />
    /// lexeme.  See <c>core_language.md</c> for expression and operator semantics.
    /// </summary>
    [ObjectId("bfc6940f-200d-44f9-bf4e-d4142e21daf6")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.String)]
    [Lexeme(Lexeme.StringGreaterThanOrEqualTo)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequality")]
    [ObjectName(Operators.StringGreaterThanOrEqualTo)]
    internal sealed class StringGreaterThanOrEqualTo : _String
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>ge</c> string
        /// greater-than-or-equal-to operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public StringGreaterThanOrEqualTo(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
