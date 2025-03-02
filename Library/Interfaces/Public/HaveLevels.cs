/*
 * HaveLevels.cs --
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
    [ObjectId("49b275c9-3098-44fb-bd91-a947f6b2b8e2")]
    public interface IHaveLevels
    {
        long Levels { get; }

        long EnterLevel();
        long ExitLevel();
    }
}
