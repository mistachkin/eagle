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
    [ObjectId("9a05b468-8763-45f4-8bc3-af445b6b2ed4")]
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

#if DEBUG
            "DEBUG",
#endif

#if NET_20_ONLY
            "NET_20_ONLY",
#endif

#if OFFICIAL
            "OFFICIAL",
#endif

#if PATCHLEVEL
            "PATCHLEVEL",
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

#if TRACE
            "TRACE",
#endif

#if WIX_30
            "WIX_30",
#endif

#if WIX_35
            "WIX_35",
#endif

#if WIX_36
            "WIX_36",
#endif

#if WIX_37
            "WIX_37",
#endif

#if WIX_38
            "WIX_38",
#endif

#if WIX_39
            "WIX_39",
#endif

#if WIX_310
            "WIX_310",
#endif

#if WIX_311
            "WIX_311",
#endif

            null
        });
    }
}
