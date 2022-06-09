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

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Eagle._Components.Private
{
    [Guid("08e75a58-f9d4-4236-99ef-d25b7ec1a1c9")]
    internal static class DefineConstants
    {
        public static readonly IList<string> OptionList = new List<string>(new string[] {
#if ASSEMBLY_DATETIME
            "ASSEMBLY_DATETIME",
#endif

#if ASSEMBLY_RELEASE
            "ASSEMBLY_RELEASE",
#endif

#if ASSEMBLY_TAG
            "ASSEMBLY_TAG",
#endif

#if ASSEMBLY_STRONG_NAME_TAG
            "ASSEMBLY_STRONG_NAME_TAG",
#endif

#if ASSEMBLY_TEXT
            "ASSEMBLY_TEXT",
#endif

#if ASSEMBLY_URI
            "ASSEMBLY_URI",
#endif

#if CONSOLE
            "CONSOLE",
#endif

#if DEAD_CODE
            "DEAD_CODE",
#endif

#if DEBUG
            "DEBUG",
#endif

#if MONO
            "MONO",
#endif

#if NATIVE
            "NATIVE",
#endif

#if NET_20_ONLY
            "NET_20_ONLY",
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

#if OFFICIAL
            "OFFICIAL",
#endif

#if SHELL
            "SHELL",
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

#if TRACE
            "TRACE",
#endif

#if WINDOWS
            "WINDOWS",
#endif

            null
        });
    }
}
