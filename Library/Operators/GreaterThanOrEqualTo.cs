/*
 * GreaterThanOrEqualTo.cs --
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
    [ObjectId("3a75021c-ca94-4766-b2ec-b56b18f10bfd")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Relational)]
    [Lexeme(Lexeme.GreaterThanOrEqualTo)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequality")]
    [ObjectName(Operators.GreaterThanOrEqualTo)]
    internal sealed class GreaterThanOrEqualTo : MaybeString
    {
        #region Public Constructors
        public GreaterThanOrEqualTo(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
