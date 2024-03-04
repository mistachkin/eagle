/*
 * GreaterThan.cs --
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
    [ObjectId("12161625-71ff-4150-bcf4-7b1b6b015915")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Relational |
        OperatorFlags.Initialize)]
    [Lexeme(Lexeme.GreaterThan)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequality")]
    [ObjectName(Operators.GreaterThan)]
    internal sealed class GreaterThan : MaybeString
    {
        #region Public Constructors
        public GreaterThan(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
