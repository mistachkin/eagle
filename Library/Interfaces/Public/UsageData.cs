/*
 * UsageData.cs --
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
    [ObjectId("217ff750-08b8-49c1-a172-dd5c36a80a8f")]
    public interface IUsageData
    {
        //
        // WARNING: *EXPERIMENTAL* This interface may still change radically.
        //
        bool ResetUsage(UsageType type, ref long value);
        bool GetUsage(UsageType type, ref long value);
        bool SetUsage(UsageType type, ref long value);
        bool AddUsage(UsageType type, ref long value);

        //
        // WARNING: These methods are only intended for use by the core script
        //          engine itself (i.e. the "Engine" class).
        //
        bool CountUsage(ref long count);
        bool ProfileUsage(ref long microseconds);
    }
}
