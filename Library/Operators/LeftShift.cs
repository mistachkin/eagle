/*
 * LeftShift.cs --
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
    [ObjectId("13823c03-46f6-483c-bb2f-075f0f76724e")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Bitwise)]
    [Lexeme(Lexeme.LeftShift)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("bitwise")]
    [ObjectName(Operators.LeftShift)]
    internal sealed class LeftShift : Math
    {
        #region Public Constructors
        public LeftShift(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
