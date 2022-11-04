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

namespace Sample
{
    [ObjectId("7a3fdc54-8976-4c3e-a5de-a88b92f83dbe")]
    internal static class DefineConstants
    {
        public static readonly StringList OptionList = new StringList(new string[] {
#if ASSEMBLY_DATETIME
            "ASSEMBLY_DATETIME",
#endif

#if ASSEMBLY_RELEASE
            "ASSEMBLY_RELEASE",
#endif

#if ASSEMBLY_STRONG_NAME_TAG
            "ASSEMBLY_STRONG_NAME_TAG",
#endif

#if ASSEMBLY_TEXT
            "ASSEMBLY_TEXT",
#endif

#if CONSOLE
            "CONSOLE",
#endif

#if DEBUG
            "DEBUG",
#endif

#if ISOLATED_INTERPRETERS
            "ISOLATED_INTERPRETERS",
#endif

#if ISOLATED_PLUGINS
            "ISOLATED_PLUGINS",
#endif

#if NETWORK
            "NETWORK",
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

#if NET_481
            "NET_481",
#endif

#if NET_CORE_REFERENCES
            "NET_CORE_REFERENCES",
#endif

#if NET_CORE_20
            "NET_CORE_20",
#endif

#if NET_CORE_30
            "NET_CORE_30",
#endif

#if NET_STANDARD_20
            "NET_STANDARD_20",
#endif

#if NET_STANDARD_21
            "NET_STANDARD_21",
#endif

#if NOTIFY
            "NOTIFY",
#endif

#if NOTIFY_OBJECT
            "NOTIFY_OBJECT",
#endif

#if OFFICIAL
            "OFFICIAL",
#endif

#if PATCHLEVEL
            "PATCHLEVEL",
#endif

#if SAMPLE
            "SAMPLE",
#endif

#if SOURCE_ID
            "SOURCE_ID",
#endif

#if SOURCE_TIMESTAMP
            "SOURCE_TIMESTAMP",
#endif

#if STABLE
            "STABLE",
#endif

#if THROW_ON_DISPOSED
            "THROW_ON_DISPOSED",
#endif

#if TCL
            "TCL",
#endif

#if TEST
            "TEST",
#endif

#if TRACE
            "TRACE",
#endif

#if WEB
            "WEB",
#endif

            null
        });
    }
}
