/*
 * StringNotEqual.cs --
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
    [ObjectId("7f159bce-39f5-471b-8332-6772b658cf8b")]
    [OperatorFlags(
        OperatorFlags.Standard | OperatorFlags.String)]
    [Lexeme(Lexeme.StringNotEqual)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequation")]
    [ObjectName(Operators.StringNotEqual)]
    internal sealed class StringNotEqual : _String
    {
        #region Public Constructors
        public StringNotEqual(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion
    }
}
