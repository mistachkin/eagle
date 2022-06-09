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
    [ObjectId("760936e9-3bcf-4d9c-8b89-55bccab3b3fd")]
    internal static class DefineConstants
    {
        public static readonly StringList OptionList = new StringList(new string[] {
#if APPROVED_VERBS
            "APPROVED_VERBS",
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

#if DEBUG
            "DEBUG",
#endif

#if ISOLATED_PLUGINS
            "ISOLATED_PLUGINS",
#endif

#if NATIVE
            "NATIVE",
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

#if TCL
            "TCL",
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
