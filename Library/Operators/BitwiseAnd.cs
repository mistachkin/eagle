/*
 * BitwiseAnd.cs --
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
    /// This class implements the Eagle <c>&amp;</c> (bitwise AND) expression
    /// operator, which computes the bitwise AND of its two integral operands.
    /// The evaluation itself is provided by the <see cref="Math" /> base class,
    /// selected by the <see cref="Lexeme.BitwiseAnd" /> lexeme.  See
    /// <c>core_language.md</c> for expression and operator semantics.
    /// </summary>
    [ObjectId("282737fd-f974-478c-86c0-36149d83028a")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Bitwise)]
    [Lexeme(Lexeme.BitwiseAnd)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("bitwise")]
    [ObjectName(Operators.BitwiseAnd)]
    internal sealed class BitwiseAnd : Math
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>&amp;</c> bitwise AND operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public BitwiseAnd(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
