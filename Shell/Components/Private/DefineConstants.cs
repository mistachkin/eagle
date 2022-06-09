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

#if !STATIC
using System.Collections.Generic;
using System.Runtime.InteropServices;
#endif

#if STATIC
using Eagle._Attributes;
using Eagle._Containers.Public;
#endif

namespace Eagle._Shell
{
#if STATIC
    [ObjectId("d09f93fb-3de0-484c-bffa-50a5f26fb00c")]
#else
    [Guid("d09f93fb-3de0-484c-bffa-50a5f26fb00c")]
#endif
    internal static class DefineConstants
    {
#if STATIC
        public static readonly StringList OptionList = new StringList(new string[] {
#else
        public static readonly List<string> OptionList = new List<string>(new string[] {
#endif
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

#if DEAD_CODE
            "DEAD_CODE",
#endif

#if DEBUG
            "DEBUG",
#endif

#if DYNAMIC
            "DYNAMIC",
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

#if STATIC
            "STATIC",
#endif

#if TEST
            "TEST",
#endif

#if TRACE
            "TRACE",
#endif

            null
        });
    }
}
