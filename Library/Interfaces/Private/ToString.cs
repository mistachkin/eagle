/*
 * ToString.cs --
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

namespace Eagle._Interfaces.Private
{
    [ObjectId("235f14fd-0a1a-4f86-a5b1-a3237f1bfc88")]
    internal interface IToString
    {
        string ToString(ToStringFlags flags);
        string ToString(ToStringFlags flags, string @default);
        string ToString(string format);
        string ToString(string format, int limit, bool strict);
    }
}
