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
    /// <summary>
    /// This enumeration specifies which assemblies should have their strong
    /// name signatures verified by the updater.
    /// </summary>
    [Flags()]
    [Guid("e668e128-b5fb-4ff6-880e-eece1db14351")]
    public enum StrongNameExFlags
    {
        /// <summary>
        /// No flags are set.  This value is reserved and should not be used.
        /// </summary>
        None = 0x0,    /* nop, do not use. */

        /// <summary>
        /// This value is invalid and should not be used.
        /// </summary>
        Invalid = 0x1, /* invalid, do not use. */

        /// <summary>
        /// Verify the strong name signature on the assembly for the updater
        /// itself.
        /// </summary>
        Self = 0x2,    /* verify signature on the assembly for the updater
                        * itself. */

        /// <summary>
        /// Verify the strong name signature on the core release assembly (e.g.
        /// Eagle.dll).
        /// </summary>
        Core = 0x4,    /* verify signature on the core release assembly (e.g.
                        * Eagle.dll). */

        /// <summary>
        /// Verify the strong name signatures on other assemblies (e.g.
        /// EagleShell.exe).
        /// </summary>
        Other = 0x8,   /* verify signatures on other assemblies (e.g.
                        * EagleShell.exe). */

        /// <summary>
        /// All of the supported strong name verification flags are set.
        /// </summary>
        All = Self | Core | Other,

        /// <summary>
        /// The default set of strong name verification flags.
        /// </summary>
        Default = All
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Signature Flags Enumeration
    /// <summary>
    /// This enumeration specifies which files should have their digital
    /// signatures verified by the updater.
    /// </summary>
    [Flags()]
    [Guid("ede72b3d-7316-440e-8f31-c7b8e7231787")]
    public enum SignatureFlags
    {
        /// <summary>
        /// No flags are set.  This value is reserved and should not be used.
        /// </summary>
        None = 0x0,    /* nop, do not use. */

        /// <summary>
        /// This value is invalid and should not be used.
        /// </summary>
        Invalid = 0x1, /* invalid, do not use. */

        /// <summary>
        /// Verify the digital signatures on the file(s) for the updater
        /// itself.
        /// </summary>
        Self = 0x2,    /* verify signatures on the file(s) for the updater
                        * itself. */

        /// <summary>
        /// Verify the digital signatures on all downloaded self-extracting
        /// files.
        /// </summary>
        Release = 0x4, /* verify signatures on all downloaded
                        * self-extracting files. */

        /// <summary>
        /// Verify the digital signature on the core release file (e.g.
        /// Eagle.dll).
        /// </summary>
        Core = 0x8,    /* verify signatures on the core release file (e.g.
                        * Eagle.dll). */

        /// <summary>
        /// Verify the digital signatures on other EXE and DLL files (e.g.
        /// EagleShell.exe).
        /// </summary>
        Other = 0x10,  /* verify signatures on other EXE and DLL files
                        * (e.g. EagleShell.exe). */

        /// <summary>
        /// All of the supported signature verification flags are set.
        /// </summary>
        All = Self | Release | Core | Other,

        /// <summary>
        /// The default set of signature verification flags.
        /// </summary>
        Default = All
    }
    #endregion
}
