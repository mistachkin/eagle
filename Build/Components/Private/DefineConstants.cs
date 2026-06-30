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
    /// <summary>
    /// This class exposes the set of conditional compilation symbols that were
    /// active when this assembly was built, as a list of their names.
    /// </summary>
    [ObjectId("bd546b57-162e-44c5-9c9a-339e9adb92bd")]
    internal static class DefineConstants
    {
        /// <summary>
        /// The list of conditional compilation symbol names that were defined
        /// for this build.  The exact contents depend on which symbols were
        /// active at compile time; the list is terminated with a null element.
        /// </summary>
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

#if OFFICIAL_BINARY
            "OFFICIAL_BINARY",
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

            null
        });
    }
}
