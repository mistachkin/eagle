/*
 * Multiply.cs --
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
    [ObjectId("7b59bbc7-4b52-4dae-8385-54903c554b9e")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Arithmetic)]
    [Lexeme(Lexeme.Multiply)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("arithmetic")]
    [ObjectName(Operators.Multiply)]
    internal sealed class Multiply : Math
    {
        #region Public Constructors
        public Multiply(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
