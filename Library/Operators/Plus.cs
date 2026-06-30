/*
 * Plus.cs --
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
    /// This class implements the Eagle <c>+</c> (addition) expression
    /// operator, which adds its two numeric operands when used in binary form
    /// or yields its single numeric operand unchanged when used in unary form.
    /// The evaluation itself is provided by the <see cref="Math" /> base class,
    /// selected by the <see cref="Lexeme.Plus" /> lexeme.  See
    /// <c>core_language.md</c> for expression and operator semantics.
    /// </summary>
    [ObjectId("13aaf4ed-a901-48d3-a963-c98878778cab")]
    [OperatorFlags(
        OperatorFlags.Special | OperatorFlags.Standard |
        OperatorFlags.Arithmetic)]
    [Lexeme(Lexeme.Plus)]
    [Operands(Arity.UnaryAndBinary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("arithmetic")]
    [ObjectName(Operators.Plus)]
    internal sealed class Plus : Math
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>+</c> addition operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Plus(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
