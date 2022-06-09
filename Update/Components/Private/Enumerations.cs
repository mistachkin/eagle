/*
 * Enumerations.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Runtime.InteropServices;

namespace Eagle._Components.Private
{
    #region Strong Name Flags Enumeration
    [Flags()]
    [Guid("e668e128-b5fb-4ff6-880e-eece1db14351")]
    public enum StrongNameExFlags
    {
        None = 0x0,    /* nop, do not use. */
        Invalid = 0x1, /* invalid, do not use. */
        Self = 0x2,    /* verify signature on the assembly for the updater
                        * itself. */
        Core = 0x4,    /* verify signature on the core release assembly (e.g.
                        * Eagle.dll). */
        Other = 0x8,   /* verify signatures on other assemblies (e.g.
                        * EagleShell.exe). */

        All = Self | Core | Other,
        Default = All
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Signature Flags Enumeration
    [Flags()]
    [Guid("ede72b3d-7316-440e-8f31-c7b8e7231787")]
    public enum SignatureFlags
    {
        None = 0x0,    /* nop, do not use. */
        Invalid = 0x1, /* invalid, do not use. */
        Self = 0x2,    /* verify signatures on the file(s) for the updater
                        * itself. */
        Release = 0x4, /* verify signatures on all downloaded
                        * self-extracting files. */
        Core = 0x8,    /* verify signatures on the core release file (e.g.
                        * Eagle.dll). */
        Other = 0x10,  /* verify signatures on other EXE and DLL files
                        * (e.g. EagleShell.exe). */

        All = Self | Release | Core | Other,
        Default = All
    }
    #endregion
}
