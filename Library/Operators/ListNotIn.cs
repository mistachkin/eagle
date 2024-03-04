/*
 * ListNotIn.cs --
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
    [ObjectId("945819a3-1415-41a2-9c57-65641ccf98e9")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.List)]
    [Lexeme(Lexeme.ListNotIn)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("membership")]
    [ObjectName(Operators.ListNotIn)]
    internal sealed class ListNotIn : List
    {
        #region Public Constructors
        public ListNotIn(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
