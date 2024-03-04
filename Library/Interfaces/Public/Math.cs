/*
 * Math.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("91ea6aa3-8646-43f0-bd93-9d8c846cdcc6")]
    public interface IMath : IConvert
    {
        ReturnCode Calculate(
            IIdentifierName identifierName,
            Lexeme lexeme,
            IConvert convert,
            ref Argument result,
            ref Result error
        );

        ReturnCode StringCompare(
            IIdentifierName identifierName,
            Lexeme lexeme,
            IConvert convert,
            StringComparison comparisonType,
            ref Argument result,
            ref Result error
        );

        ReturnCode ListMayContain(
            IIdentifierName identifierName,
            Lexeme lexeme,
            IConvert convert,
            StringComparison comparisonType,
            ref Argument result,
            ref Result error
        );
    }
}
