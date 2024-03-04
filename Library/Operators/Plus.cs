/*
 * Plus.cs --
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
    [ObjectId("13aaf4ed-a901-48d3-a963-c98878778cab")]
    [OperatorFlags(
        OperatorFlags.Special | OperatorFlags.Standard |
        OperatorFlags.Arithmetic)]
    [Lexeme(Lexeme.Plus)]
    [Operands(Arity.UnaryAndBinary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("arithmetic")]
    [ObjectName(Operators.Plus)]
    internal sealed class Plus : Math
    {
        #region Public Constructors
        public Plus(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
