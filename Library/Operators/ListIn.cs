/*
 * ListIn.cs --
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
    [ObjectId("ad5c5e98-2cd0-4d20-81c7-f0a7d9dceeb1")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.List)]
    [Lexeme(Lexeme.ListIn)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("membership")]
    [ObjectName(Operators.ListIn)]
    internal sealed class ListIn : List
    {
        #region Public Constructors
        public ListIn(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
