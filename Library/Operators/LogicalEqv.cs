/*
 * LogicalEqv.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Operators
{
    /// <summary>
    /// This class implements the Eagle <c>&lt;=&gt;</c> (logical
    /// equivalence) expression operator, which yields a true result when both
    /// of its two boolean operands have the same truth value.  The evaluation
    /// itself is provided by the <see cref="Logic" /> base class, selected by
    /// the <see cref="Lexeme.LogicalEqv" /> lexeme.  See <c>core_language.md</c>
    /// for expression and operator semantics.
    /// </summary>
    [ObjectId("a6e180b8-cb52-4ec1-9830-39cd8493d97b")]
    [OperatorFlags(
        OperatorFlags.NonStandard | OperatorFlags.Logical)]
    [Lexeme(Lexeme.LogicalEqv)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("logical")]
    [ObjectName(Operators.LogicalEqv)]
    internal sealed class LogicalEqv : Logic
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>&lt;=&gt;</c> logical equivalence
        /// operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public LogicalEqv(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
