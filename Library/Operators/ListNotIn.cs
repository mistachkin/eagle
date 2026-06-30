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
    /// <summary>
    /// This class implements the Eagle <c>ni</c> (list not-in) expression
    /// operator, which evaluates to true when its left operand is not an
    /// element of the list given by its right operand.  The evaluation itself
    /// is provided by the <see cref="List" /> base class, selected by the
    /// <see cref="Lexeme.ListNotIn" /> lexeme.  See <c>core_language.md</c> for
    /// expression and operator semantics.
    /// </summary>
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
        /// <summary>
        /// Constructs an instance of the <c>ni</c> list not-in operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
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
