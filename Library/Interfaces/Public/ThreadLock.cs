/*
 * ThreadLock.cs --
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
    [ObjectId("723276de-6bb2-4067-9fb8-e54e73b23ac2")]
    public interface IThreadLock
    {
        long? ThreadId { get; set; }

        bool Lock(ref Result error);
        bool Unlock(ref Result error);

        bool IsUsable();
        bool IsUsable(ref Result error);
    }
}
