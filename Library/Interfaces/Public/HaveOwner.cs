/*
 * HaveOwner.cs --
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
    [ObjectId("3b6ad1c6-4890-4691-b347-8d1c8a396c46")]
    public interface IHaveOwner
    {
        object Owner { get; set; }

        bool HasOwner();
        bool IsOwnerBusy(object owner);
    }
}
