/*
 * LogicalXor.cs --
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
    [ObjectId("403adc7b-f116-4ad2-b5ec-c35307664f1b")]
    [OperatorFlags(
        OperatorFlags.NonStandard | OperatorFlags.Logical)]
    [Lexeme(Lexeme.LogicalXor)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("logical")]
    [ObjectName(Operators.LogicalXor)]
    internal sealed class LogicalXor : Logic
    {
        #region Public Constructors
        public LogicalXor(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
