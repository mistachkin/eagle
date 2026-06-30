/*
 * RightShift.cs --
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
    /// This class implements the Eagle <c>&gt;&gt;</c> (right shift) expression
    /// operator, which shifts the bits of its left integral operand to the
    /// right by the number of positions given by its right integral operand.
    /// The evaluation itself is provided by the <see cref="Math" /> base class,
    /// selected by the <see cref="Lexeme.RightShift" /> lexeme.  See
    /// <c>core_language.md</c> for expression and operator semantics.
    /// </summary>
    [ObjectId("4064ada2-b1b2-4695-b099-f4daebb57da2")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Bitwise)]
    [Lexeme(Lexeme.RightShift)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("bitwise")]
    [ObjectName(Operators.RightShift)]
    internal sealed class RightShift : Math
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>&gt;&gt;</c> right shift operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public RightShift(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
