/*
 * Exponent.cs --
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
    [ObjectId("3a0efea2-4c30-43b2-b63a-b3c7d0f0dbc9")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Arithmetic)]
    [Lexeme(Lexeme.Exponent)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("exponential")]
    [ObjectName(Operators.Exponent)]
    internal sealed class Exponent : Math
    {
        #region Public Constructors
        public Exponent(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
