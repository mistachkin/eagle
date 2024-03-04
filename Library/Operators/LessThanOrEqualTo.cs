/*
 * LessThanOrEqualTo.cs --
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
    [ObjectId("79406242-64b3-4483-a248-7ad2f48bd6f4")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Relational)]
    [Lexeme(Lexeme.LessThanOrEqualTo)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequality")]
    [ObjectName(Operators.LessThanOrEqualTo)]
    internal sealed class LessThanOrEqualTo : MaybeString
    {
        #region Public Constructors
        public LessThanOrEqualTo(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
