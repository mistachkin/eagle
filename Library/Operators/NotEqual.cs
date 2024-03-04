/*
 * NotEqual.cs --
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
    [ObjectId("ab93e922-b5e8-48b2-a5ec-6d576dac8ba8")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.Relational)]
    [Lexeme(Lexeme.NotEqual)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequation")]
    [ObjectName(Operators.NotEqual)]
    internal sealed class NotEqual : MaybeString
    {
        #region Public Constructors
        public NotEqual(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
