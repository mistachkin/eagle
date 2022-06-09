/*
 * String.cs --
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

namespace Eagle._Interfaces.Private
{
    [ObjectId("4c20e212-18ea-4321-802d-11f636e4c6e4")]
    internal interface IString
    {
        //
        // NOTE: This interface only contains the bare minimum requirements 
        //       common to the Argument and Result classes.  It may need to 
        //       be added to later.
        //
        int IndexOf(string value, StringComparison comparisonType);
        int IndexOf(string value, int startIndex, StringComparison comparisonType);
        int LastIndexOf(string value, StringComparison comparisonType);
        int LastIndexOf(string value, int startIndex, StringComparison comparisonType);

        bool StartsWith(string value, StringComparison comparisonType);
        bool EndsWith(string value, StringComparison comparisonType);

        string Substring(int startIndex);
        string Substring(int startIndex, int length);

        bool Contains(string value, StringComparison comparisonType);
        string Replace(string oldValue, string newValue);

        string Trim();
        string Trim(char[] trimChars);
        string TrimStart(char[] trimChars);
        string TrimEnd(char[] trimChars);

        char[] ToCharArray();
    }
}
