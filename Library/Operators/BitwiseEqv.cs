/*
 * BitwiseEqv.cs --
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
    [ObjectId("c76c4a7b-1190-497c-b0a1-001c77f49e99")]
    [OperatorFlags(
        OperatorFlags.NonStandard | OperatorFlags.Bitwise)]
    [Lexeme(Lexeme.BitwiseEqv)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("bitwise")]
    [ObjectName(Operators.BitwiseEqv)]
    internal sealed class BitwiseEqv : Math
    {
        #region Public Constructors
        public BitwiseEqv(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
