/*
 * Setup.cs --
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
    [ObjectId("61a0ed57-55db-4ce2-94dd-a8a93a290da3")]
    public interface ISetup
    {
        //
        // WARNING: This method may not throw exceptions.
        //
        [Throw(false)]
        ReturnCode Setup(ref Result error);
    }
}
