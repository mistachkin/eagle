/*
 * BitwiseImp.cs --
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
    /// This class implements the Eagle <c>-&gt;</c> (bitwise implication)
    /// expression operator, which computes the bitwise logical implication of
    /// its two integral operands.  The evaluation itself is provided by the
    /// <see cref="Math" /> base class, selected by the
    /// <see cref="Lexeme.BitwiseImp" /> lexeme.  See <c>core_language.md</c>
    /// for expression and operator semantics.
    /// </summary>
    [ObjectId("571cbd10-c710-41b6-8532-0ba61c5e9fa0")]
    [OperatorFlags(
        OperatorFlags.NonStandard | OperatorFlags.Bitwise)]
    [Lexeme(Lexeme.BitwiseImp)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("bitwise")]
    [ObjectName(Operators.BitwiseImp)]
    internal sealed class BitwiseImp : Math
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>-&gt;</c> bitwise implication
        /// operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public BitwiseImp(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
