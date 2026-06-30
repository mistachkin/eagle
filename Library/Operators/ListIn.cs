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
    /// <summary>
    /// This class implements the Eagle <c>in</c> (list membership) expression
    /// operator, which tests whether its left operand is an element of the
    /// list given by its right operand.  The evaluation itself is provided by
    /// the <see cref="List" /> base class, selected by the
    /// <see cref="Lexeme.ListIn" /> lexeme.  See <c>core_language.md</c> for
    /// expression and operator semantics.
    /// </summary>
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
        /// <summary>
        /// Constructs an instance of the <c>in</c> list membership operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
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
