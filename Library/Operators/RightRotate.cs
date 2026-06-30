/*
 * RightRotate.cs --
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
    /// This class implements the Eagle <c>&gt;&gt;&gt;</c> (bitwise right-rotate)
    /// expression operator, which rotates the bits of its left integral operand
    /// to the right by the number of bit positions given by its right operand.
    /// The evaluation itself is provided by the <see cref="Math" /> base class,
    /// selected by the <see cref="Lexeme.RightRotate" /> lexeme.  See
    /// <c>core_language.md</c> for expression and operator semantics.
    /// </summary>
    [ObjectId("f14c5673-bb8d-4eca-8321-3aaa4932fd45")]
    [OperatorFlags(
        OperatorFlags.NonStandard | OperatorFlags.Bitwise)]
    [Lexeme(Lexeme.RightRotate)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("bitwise")]
    [ObjectName(Operators.RightRotate)]
    internal sealed class RightRotate : Math
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>&gt;&gt;&gt;</c> right-rotate operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public RightRotate(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
