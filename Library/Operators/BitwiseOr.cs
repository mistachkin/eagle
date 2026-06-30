/*
 * BitwiseOr.cs --
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
    /// This class implements the Eagle <c>|</c> (bitwise OR) expression
    /// operator, which computes the bitwise inclusive OR of its two integral
    /// operands.  The evaluation itself is provided by the
    /// <see cref="Math" /> base class, selected by the
    /// <see cref="Lexeme.BitwiseOr" /> lexeme.  See <c>core_language.md</c>
    /// for expression and operator semantics.
    /// </summary>
    [ObjectId("9b87f17b-0992-4a70-9611-2e08fca9d2bf")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Bitwise)]
    [Lexeme(Lexeme.BitwiseOr)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("bitwise")]
    [ObjectName(Operators.BitwiseOr)]
    internal sealed class BitwiseOr : Math
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>|</c> bitwise OR operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public BitwiseOr(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
