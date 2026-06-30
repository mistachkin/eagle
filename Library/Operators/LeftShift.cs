/*
 * LeftShift.cs --
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
    /// This class implements the Eagle <c>&lt;&lt;</c> (left shift) expression
    /// operator, which shifts the bits of its left integral operand to the left
    /// by the number of positions given by its right integral operand.  The
    /// evaluation itself is provided by the <see cref="Math" /> base class,
    /// selected by the <see cref="Lexeme.LeftShift" /> lexeme.  See
    /// <c>core_language.md</c> for expression and operator semantics.
    /// </summary>
    [ObjectId("13823c03-46f6-483c-bb2f-075f0f76724e")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Bitwise)]
    [Lexeme(Lexeme.LeftShift)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("bitwise")]
    [ObjectName(Operators.LeftShift)]
    internal sealed class LeftShift : Math
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>&lt;&lt;</c> left shift operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public LeftShift(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
