/*
 * BitwiseXor.cs --
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
    [ObjectId("5b602c6b-1621-4d00-9f01-8d93e1c15cc4")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Bitwise)]
    [Lexeme(Lexeme.BitwiseXor)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("bitwise")]
    [ObjectName(Operators.BitwiseXor)]
    internal sealed class BitwiseXor : Math
    {
        #region Public Constructors
        public BitwiseXor(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
