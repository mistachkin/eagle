/*
 * BitwiseNot.cs --
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
    /// This class implements the Eagle <c>~</c> (bitwise complement)
    /// expression operator, which produces the bitwise NOT of its single
    /// integral operand.  The evaluation itself is provided by the
    /// <see cref="Math" /> base class, selected by the
    /// <see cref="Lexeme.BitwiseNot" /> lexeme.  See <c>core_language.md</c>
    /// for expression and operator semantics.
    /// </summary>
    [ObjectId("1e9331a3-b6be-45c1-aa04-5063410d3f09")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Bitwise)]
    [Lexeme(Lexeme.BitwiseNot)]
    [Operands(Arity.Unary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("bitwise")]
    [ObjectName(Operators.BitwiseNot)]
    internal sealed class BitwiseNot : Math
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>~</c> bitwise complement operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public BitwiseNot(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
