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
