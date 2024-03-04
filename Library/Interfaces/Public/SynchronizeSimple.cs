/*
 * SynchronizeSimple.cs --
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
    //
    // WARNING: This interface is being deprecated, please do not use it, or
    //          any methods from it, for new code.
    //
    [ObjectId("66e23dba-6d3a-4e6a-b215-300f1d2d1c82")]
    public interface ISynchronizeSimple
    {
        bool TryLock();
        void Lock();
        void Unlock();
    }
}
