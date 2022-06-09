/*
 * Wrapper.cs --
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
    [ObjectId("166e10d1-381d-434d-b3c3-34ad9372ffd5")]
    public interface IWrapper : IWrapperData
    {
        bool IsDisposable { get; }
        object Object { get; }
    }
}
