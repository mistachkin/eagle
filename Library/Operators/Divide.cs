/*
 * Divide.cs --
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
    [ObjectId("99261dd9-726c-4559-b2e4-e80aff0f2f6d")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Arithmetic)]
    [Lexeme(Lexeme.Divide)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("arithmetic")]
    [ObjectName(Operators.Divide)]
    internal sealed class Divide : Math
    {
        #region Public Constructors
        public Divide(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
