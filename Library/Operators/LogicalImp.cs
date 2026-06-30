/*
 * LogicalImp.cs --
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
    /// This class implements the Eagle <c>=&gt;</c> (logical implication)
    /// expression operator, which evaluates to a boolean result representing
    /// the logical implication of its two operands.  The evaluation itself is
    /// provided by the <see cref="Logic" /> base class, selected by the
    /// <see cref="Lexeme.LogicalImp" /> lexeme.  See <c>core_language.md</c>
    /// for expression and operator semantics.
    /// </summary>
    [ObjectId("1af81a08-1df4-4bd6-b9fa-963142257b04")]
    [OperatorFlags(
        OperatorFlags.Special | OperatorFlags.NonStandard |
        OperatorFlags.Logical)]
    [Lexeme(Lexeme.LogicalImp)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("logical")]
    [ObjectName(Operators.LogicalImp)]
    internal sealed class LogicalImp : Logic
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>=&gt;</c> logical implication
        /// operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public LogicalImp(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
