/*
 * LessThan.cs --
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
    [ObjectId("8d293a7d-99ae-43fa-b466-81dfd362cbe6")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Relational)]
    [Lexeme(Lexeme.LessThan)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequality")]
    [ObjectName(Operators.LessThan)]
    internal sealed class LessThan : MaybeString
    {
        #region Public Constructors
        public LessThan(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
