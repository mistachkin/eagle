/*
 * DefineConstants.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    [ObjectId("976e4dbd-f384-4f14-be61-eb6eb1012f78")]
    internal static class DefineConstants
    {
        public static readonly StringList OptionList = new StringList(new string[] {
#if CONSOLE
            "CONSOLE",
#endif

#if DAEMON
            "DAEMON",
#endif

#if DEBUG
            "DEBUG",
#endif

#if MONO
            "MONO",
#endif

#if MONO_BUILD
            "MONO_BUILD",
#endif

#if MONO_HACKS
            "MONO_HACKS",
#endif

#if MONO_LEGACY
            "MONO_LEGACY",
#endif

#if NET_20
            "NET_20",
#endif

#if NET_20_ONLY
            "NET_20_ONLY",
#endif

#if NET_20_SP1
            "NET_20_SP1",
#endif

#if NET_20_SP2
            "NET_20_SP2",
#endif

#if NET_30
            "NET_30",
#endif

#if NET_35
            "NET_35",
#endif

#if NET_40
            "NET_40",
#endif

#if NET_45
            "NET_45",
#endif

#if NET_451
            "NET_451",
#endif

#if NET_452
            "NET_452",
#endif

#if NET_46
            "NET_46",
#endif

#if NET_461
            "NET_461",
#endif

#if NET_462
            "NET_462",
#endif

#if NET_47
            "NET_47",
#endif

#if NET_471
            "NET_471",
#endif

#if NET_472
            "NET_472",
#endif

#if NET_48
            "NET_48",
#endif

#if NOTIFY
            "NOTIFY",
#endif

#if NOTIFY_OBJECT
            "NOTIFY_OBJECT",
#endif

#if SHELL
            "SHELL",
#endif

#if THROW_ON_DISPOSED
            "THROW_ON_DISPOSED",
#endif

#if TRACE
            "TRACE",
#endif

            null
        });
    }
}
