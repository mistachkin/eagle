/*
 * GetValue.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("291117b5-f1ef-4945-a983-fc01a1b5447f")]
    public interface IGetValue
    {
        object Value { get; }
        int Length { get; }
        string String { get; }
    }
}
