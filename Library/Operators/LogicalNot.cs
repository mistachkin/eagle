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
