/*
 * LogicalNot.cs --
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
    /// This class implements the Eagle <c>!</c> (logical not) expression
    /// operator, which negates the boolean value of its single numeric
    /// operand.  The evaluation itself is provided by the
    /// <see cref="Logic" /> base class, selected by the
    /// <see cref="Lexeme.LogicalNot" /> lexeme.  See <c>core_language.md</c>
    /// for expression and operator semantics.
    /// </summary>
    [ObjectId("1896ddee-7435-4a0a-b628-0ce851a87880")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Logical |
        OperatorFlags.Initialize | OperatorFlags.SecuritySdk)]
    [Lexeme(Lexeme.LogicalNot)]
    [Operands(Arity.Unary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("logical")]
    [ObjectName(Operators.LogicalNot)]
    internal sealed class LogicalNot : Logic
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>!</c> logical not operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public LogicalNot(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
