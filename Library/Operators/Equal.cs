/*
 * Equal.cs --
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
    [ObjectId("7ed209e6-b614-4922-8e3f-de5f5855dbcc")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Relational |
        OperatorFlags.Initialize | OperatorFlags.SecuritySdk)]
    [Lexeme(Lexeme.Equal)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("equality")]
    [ObjectName(Operators.Equal)]
    internal sealed class Equal : MaybeString
    {
        #region Public Constructors
        public Equal(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
