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
    [ObjectId("c2c3065b-9b9e-4d26-81a0-8260884c7dbb")]
    public interface IExpressionToken : IToken
    {
        Lexeme Lexeme { get; set; }
        Variant Variant { get; set; }
    }
}
