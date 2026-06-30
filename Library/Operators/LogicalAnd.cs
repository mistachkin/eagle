/*
 * LogicalAnd.cs --
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
    /// This class implements the Eagle <c>&amp;&amp;</c> (logical AND) expression
    /// operator, which evaluates to a boolean indicating whether both of its
    /// operands are true.  The evaluation itself is provided by the
    /// <see cref="Logic" /> base class, selected by the
    /// <see cref="Lexeme.LogicalAnd" /> lexeme.  See <c>core_language.md</c>
    /// for expression and operator semantics.
    /// </summary>
    [ObjectId("9d11a005-8ea1-4c30-85b0-17f8c5bc5cb1")]
    [OperatorFlags(
        OperatorFlags.Special | OperatorFlags.Standard |
        OperatorFlags.Logical | OperatorFlags.Initialize)]
    [Lexeme(Lexeme.LogicalAnd)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("logical")]
    [ObjectName(Operators.LogicalAnd)]
    internal sealed class LogicalAnd : Logic
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>&amp;&amp;</c> logical AND operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public LogicalAnd(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
