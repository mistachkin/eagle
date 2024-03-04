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
