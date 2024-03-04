/*
 * LeftRotate.cs --
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
    [ObjectId("1586ae75-31f4-4d9b-921c-82b79bcd8b5d")]
    [OperatorFlags(
        OperatorFlags.NonStandard | OperatorFlags.Bitwise)]
    [Lexeme(Lexeme.LeftRotate)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("bitwise")]
    [ObjectName(Operators.LeftRotate)]
    internal sealed class LeftRotate : Math
    {
        #region Public Constructors
        public LeftRotate(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
