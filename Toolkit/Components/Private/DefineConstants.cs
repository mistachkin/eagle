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
    /// active when this component was built.
    /// </summary>
    [ObjectId("0b9a849e-ce19-4e1c-b1ea-dc53c03d8143")]
    internal static class DefineConstants
    {
        /// <summary>
        /// The list of conditional compilation symbol names that were defined
        /// at build time, terminated by a null element.
        /// </summary>
        public static readonly StringList OptionList = new StringList(new string[] {
#if CONSOLE
            "CONSOLE",
#endif

#if DEAD_CODE
            "DEAD_CODE",
#endif

#if DEBUG
            "DEBUG",
#endif

#if DEBUGGER
            "DEBUGGER",
#endif

#if DEBUGGER_ARGUMENTS
            "DEBUGGER_ARGUMENTS",
#endif

#if MONO_BUILD
            "MONO_BUILD",
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

#if OFFICIAL_BINARY
            "OFFICIAL_BINARY",
#endif

#if SHELL
            "SHELL",
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
