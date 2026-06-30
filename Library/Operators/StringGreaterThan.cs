/*
 * StringGreaterThan.cs --
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
    /// This class implements the Eagle <c>gt</c> string greater-than expression
    /// operator, which compares its two operands as strings and yields a boolean
    /// indicating whether the first operand is lexicographically greater than the
    /// second.  The evaluation itself is provided by the <see cref="_String" />
    /// base class, selected by the <see cref="Lexeme.StringGreaterThan" /> lexeme.
    /// See <c>core_language.md</c> for expression and operator semantics.
    /// </summary>
    [ObjectId("33eeb655-0b2f-4287-bb11-12206b7d11be")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.String)]
    [Lexeme(Lexeme.StringGreaterThan)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequality")]
    [ObjectName(Operators.StringGreaterThan)]
    internal sealed class StringGreaterThan : _String
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>gt</c> string greater-than operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public StringGreaterThan(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
