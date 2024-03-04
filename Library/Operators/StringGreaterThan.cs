/*
 * StringGreaterThan.cs --
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
    [ObjectId("33eeb655-0b2f-4287-bb11-12206b7d11be")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.String)]
    [Lexeme(Lexeme.StringGreaterThan)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequality")]
    [ObjectName(Operators.StringGreaterThan)]
    internal sealed class StringGreaterThan : _String
    {
        #region Public Constructors
        public StringGreaterThan(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
