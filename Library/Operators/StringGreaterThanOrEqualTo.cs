/*
 * StringGreaterThanOrEqualTo.cs --
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
    [ObjectId("bfc6940f-200d-44f9-bf4e-d4142e21daf6")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.String)]
    [Lexeme(Lexeme.StringGreaterThanOrEqualTo)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequality")]
    [ObjectName(Operators.StringGreaterThanOrEqualTo)]
    internal sealed class StringGreaterThanOrEqualTo : _String
    {
        #region Public Constructors
        public StringGreaterThanOrEqualTo(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
