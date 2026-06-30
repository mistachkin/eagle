/*
 * Modulus.cs --
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
    /// This class implements the Eagle <c>%</c> (modulus) expression operator,
    /// which computes the remainder of dividing its first integral operand by
    /// its second integral operand.  The evaluation itself is provided by the
    /// <see cref="Math" /> base class, selected by the
    /// <see cref="Lexeme.Modulus" /> lexeme.  See <c>core_language.md</c> for
    /// expression and operator semantics.
    /// </summary>
    [ObjectId("93eff400-bbf3-4478-b778-61244b460eea")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Arithmetic)]
    [Lexeme(Lexeme.Modulus)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("arithmetic")]
    [ObjectName(Operators.Modulus)]
    internal sealed class Modulus : Math
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>%</c> modulus operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Modulus(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
