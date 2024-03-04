/*
 * RightRotate.cs --
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
    [ObjectId("f14c5673-bb8d-4eca-8321-3aaa4932fd45")]
    [OperatorFlags(
        OperatorFlags.NonStandard | OperatorFlags.Bitwise)]
    [Lexeme(Lexeme.RightRotate)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("bitwise")]
    [ObjectName(Operators.RightRotate)]
    internal sealed class RightRotate : Math
    {
        #region Public Constructors
        public RightRotate(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
