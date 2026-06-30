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
    /// <summary>
    /// This class implements the Eagle <c>ne</c> (string inequality)
    /// expression operator, which compares its two operands as strings and
    /// yields a boolean indicating whether they are not equal.  The
    /// evaluation itself is provided by the <see cref="_String" /> base
    /// class, selected by the <see cref="Lexeme.StringNotEqual" /> lexeme.
    /// See <c>core_language.md</c> for expression and operator semantics.
    /// </summary>
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
        /// <summary>
        /// Constructs an instance of the <c>ne</c> string inequality
        /// operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
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
