/*
 * CanHashValue.cs --
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
    [ObjectId("9e271f2a-8a46-44fe-8b6e-43dd11eb7559")]
    internal interface ICanHashValue
    {
        byte[] GetHashValue(ref Result error);
        byte[] HashValue { get; set; }
    }
}
