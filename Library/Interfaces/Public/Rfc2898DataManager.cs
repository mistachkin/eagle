/*
 * Rfc2898DataManager.cs --
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
    [ObjectId("0f334a93-1b7c-4e16-bfc3-45100a9b75b9")]
    public interface IRfc2898DataManager
    {
        IRfc2898Data Rfc2898Data { get; set; }
        IRfc2898DataProvider Rfc2898DataProvider { get; set; }
    }
}
