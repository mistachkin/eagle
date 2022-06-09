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
    [ObjectId("0b9a849e-ce19-4e1c-b1ea-dc53c03d8143")]
    internal static class DefineConstants
    {
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
