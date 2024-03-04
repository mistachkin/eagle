/*
 * BitwiseOr.cs --
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
    [ObjectId("9b87f17b-0992-4a70-9611-2e08fca9d2bf")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Bitwise)]
    [Lexeme(Lexeme.BitwiseOr)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("bitwise")]
    [ObjectName(Operators.BitwiseOr)]
    internal sealed class BitwiseOr : Math
    {
        #region Public Constructors
        public BitwiseOr(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
