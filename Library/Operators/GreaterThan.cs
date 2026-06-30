/*
 * GreaterThan.cs --
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
    /// This class implements the Eagle <c>&gt;</c> (greater-than) expression
    /// operator, which compares its two operands and yields a boolean result
    /// indicating whether the left operand is greater than the right operand.
    /// The evaluation itself is provided by the <see cref="MaybeString" /> base
    /// class, selected by the <see cref="Lexeme.GreaterThan" /> lexeme, and
    /// supports both numeric and string operands.  See <c>core_language.md</c>
    /// for expression and operator semantics.
    /// </summary>
    [ObjectId("12161625-71ff-4150-bcf4-7b1b6b015915")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Relational |
        OperatorFlags.Initialize)]
    [Lexeme(Lexeme.GreaterThan)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequality")]
    [ObjectName(Operators.GreaterThan)]
    internal sealed class GreaterThan : MaybeString
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>&gt;</c> greater-than operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public GreaterThan(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
