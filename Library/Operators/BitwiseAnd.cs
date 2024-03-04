/*
 * BitwiseAnd.cs --
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
    [ObjectId("282737fd-f974-478c-86c0-36149d83028a")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Bitwise)]
    [Lexeme(Lexeme.BitwiseAnd)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("bitwise")]
    [ObjectName(Operators.BitwiseAnd)]
    internal sealed class BitwiseAnd : Math
    {
        #region Public Constructors
        public BitwiseAnd(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
