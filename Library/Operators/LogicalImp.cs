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
