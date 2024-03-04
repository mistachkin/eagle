/*
 * Minus.cs --
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
    [ObjectId("037ecf98-21d9-4a00-bfdf-c50581be276b")]
    [OperatorFlags(
        OperatorFlags.Special | OperatorFlags.Standard |
        OperatorFlags.Arithmetic)]
    [Lexeme(Lexeme.Minus)]
    [Operands(Arity.UnaryAndBinary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("arithmetic")]
    [ObjectName(Operators.Minus)]
    internal sealed class Minus : Math
    {
        #region Public Constructors
        public Minus(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
