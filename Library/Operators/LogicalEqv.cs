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
