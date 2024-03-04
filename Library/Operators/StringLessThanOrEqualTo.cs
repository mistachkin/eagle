/*
 * StringLessThanOrEqualTo.cs --
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
    [ObjectId("2b25af98-8ff9-48e3-8772-b97cc805479b")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.String)]
    [Lexeme(Lexeme.StringLessThanOrEqualTo)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequality")]
    [ObjectName(Operators.StringLessThanOrEqualTo)]
    internal sealed class StringLessThanOrEqualTo : _String
    {
        #region Public Constructors
        public StringLessThanOrEqualTo(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
