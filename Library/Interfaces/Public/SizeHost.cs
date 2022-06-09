/*
 * SizeHost.cs --
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
    [ObjectId("9d10e1ab-e22b-4f51-acd7-4328dbc42889")]
    public interface ISizeHost : IInteractiveHost
    {
        bool ResetSize(HostSizeType hostSizeType);
        bool GetSize(HostSizeType hostSizeType, ref int width, ref int height);
        bool SetSize(HostSizeType hostSizeType, int width, int height);
    }
}
