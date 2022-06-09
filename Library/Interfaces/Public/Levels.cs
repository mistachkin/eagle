/*
 * Levels.cs --
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
    [ObjectId("d998fcd4-340d-4c0b-9b80-413001bb1cd7")]
    public interface ILevels
    {
        int Levels { get; }

        int EnterLevel();
        int ExitLevel();
    }
}
