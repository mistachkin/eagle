/*
 * NewHostCallback.cs --
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
    [ObjectId("54578e21-6a4c-440a-8435-61efb9ec2a06")]
    public interface INewHostCallback
    {
        IHost NewHost(IHostData hostData);
    }
}
