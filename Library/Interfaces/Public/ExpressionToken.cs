/*
 * ExpressionToken.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface represents a single token produced while parsing an
    /// expression.  It extends the general token contract
    /// (<see cref="IToken" />) with the expression-specific lexeme and the
    /// variant value associated with the token.
    /// </summary>
    [ObjectId("c2c3065b-9b9e-4d26-81a0-8260884c7dbb")]
    public interface IExpressionToken : IToken
    {
        /// <summary>
        /// Gets or sets the lexeme that classifies this expression token.
        /// </summary>
        Lexeme Lexeme { get; set; }
        /// <summary>
        /// Gets or sets the variant value associated with this expression
        /// token, if any.
        /// </summary>
        IVariant Variant { get; set; }
    }
}
