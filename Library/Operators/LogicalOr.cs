/*
 * LogicalOr.cs --
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
    /// This class implements the Eagle <c>||</c> (logical OR) expression
    /// operator, which yields a boolean result that is true when either of its
    /// two operands is non-zero.  The evaluation itself is provided by the
    /// <see cref="Logic" /> base class, selected by the
    /// <see cref="Lexeme.LogicalOr" /> lexeme.  See <c>core_language.md</c> for
    /// expression and operator semantics.
    /// </summary>
    [ObjectId("60581f56-b46d-400b-a20d-10b8dfdb420a")]
    [OperatorFlags(
        OperatorFlags.Special | OperatorFlags.Standard |
        OperatorFlags.Logical)]
    [Lexeme(Lexeme.LogicalOr)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("logical")]
    [ObjectName(Operators.LogicalOr)]
    internal sealed class LogicalOr : Logic
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>||</c> logical OR operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public LogicalOr(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
