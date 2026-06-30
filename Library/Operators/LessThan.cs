/*
 * LessThan.cs --
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
    /// This class implements the Eagle <c>&lt;</c> (less-than) expression
    /// operator, which compares its two operands and yields a boolean
    /// indicating whether the first operand is strictly less than the second.
    /// The comparison may be performed numerically or as strings, as
    /// appropriate for the operands.  The evaluation itself is provided by the
    /// <see cref="MaybeString" /> base class, selected by the
    /// <see cref="Lexeme.LessThan" /> lexeme.  See <c>core_language.md</c> for
    /// expression and operator semantics.
    /// </summary>
    [ObjectId("8d293a7d-99ae-43fa-b466-81dfd362cbe6")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Relational)]
    [Lexeme(Lexeme.LessThan)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequality")]
    [ObjectName(Operators.LessThan)]
    internal sealed class LessThan : MaybeString
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>&lt;</c> less-than operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public LessThan(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
