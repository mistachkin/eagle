/*
 * Modulus.cs --
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
    [ObjectId("93eff400-bbf3-4478-b778-61244b460eea")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Arithmetic)]
    [Lexeme(Lexeme.Modulus)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("arithmetic")]
    [ObjectName(Operators.Modulus)]
    internal sealed class Modulus : Math
    {
        #region Public Constructors
        public Modulus(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
