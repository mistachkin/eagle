/*
 * StringEqual.cs --
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
    [ObjectId("b1b8bfdd-28ad-4d6b-9228-5dfaabac3790")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.String)]
    [Lexeme(Lexeme.StringEqual)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("equality")]
    [ObjectName(Operators.StringEqual)]
    internal sealed class StringEqual : _String
    {
        #region Public Constructors
        public StringEqual(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
