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

#if EAGLE
using Eagle._Attributes;
#else
using System.Runtime.InteropServices;
#endif

namespace Eagle._Components.Shared
{
    /// <summary>
    /// This enumeration identifies the kind of build of the Eagle core
    /// library, distinguishing the targeted runtime (and platform) as well as
    /// certain optional feature configurations.
    /// </summary>
#if EAGLE
    [ObjectId("793e7b27-3390-4fcc-90dd-b91faf924f4e")]
#else
    [Guid("793e7b27-3390-4fcc-90dd-b91faf924f4e")]
#endif
    public enum BuildType
    {
        /// <summary>
        /// No build type; this value is a placeholder and should not be used.
        /// </summary>
        None = 0x0,              /* nop, do not use. */

        /// <summary>
        /// An invalid build type; this value is reserved and should not be
        /// used.
        /// </summary>
        Invalid = 0x1,           /* invalid, do not use. */

        /// <summary>
        /// A build targeting the .NET Framework 2.0 on Windows.
        /// </summary>
        NetFx20 = 0x2,           /* NetFx20 on Windows. */

        /// <summary>
        /// A build targeting the .NET Framework 3.5 on Windows.
        /// </summary>
        NetFx35 = 0x4,           /* NetFx35 on Windows. */

        /// <summary>
        /// A build targeting the .NET Framework 4.0 on Windows.
        /// </summary>
        NetFx40 = 0x8,           /* NetFx40 on Windows. */

        /// <summary>
        /// A build targeting the .NET Framework 4.5 on Windows.
        /// </summary>
        NetFx45 = 0x10,          /* NetFx45 on Windows. */

        /// <summary>
        /// A build targeting the .NET Framework 4.5.1 on Windows.
        /// </summary>
        NetFx451 = 0x20,         /* NetFx451 on Windows. */

        /// <summary>
        /// A build targeting the .NET Framework 4.5.2 on Windows.
        /// </summary>
        NetFx452 = 0x40,         /* NetFx452 on Windows. */

        /// <summary>
        /// A build targeting the .NET Framework 4.6 on Windows.
        /// </summary>
        NetFx46 = 0x80,          /* NetFx46 on Windows. */

        /// <summary>
        /// A build targeting the .NET Framework 4.6.1 on Windows.
        /// </summary>
        NetFx461 = 0x100,        /* NetFx461 on Windows. */

        /// <summary>
        /// A build targeting the .NET Framework 4.6.2 on Windows.
        /// </summary>
        NetFx462 = 0x200,        /* NetFx462 on Windows. */

        /// <summary>
        /// A build targeting the .NET Framework 4.7 on Windows.
        /// </summary>
        NetFx47 = 0x400,         /* NetFx47 on Windows. */

        /// <summary>
        /// A build targeting the .NET Framework 4.7.1 on Windows.
        /// </summary>
        NetFx471 = 0x800,        /* NetFx471 on Windows. */

        /// <summary>
        /// A build targeting the .NET Framework 4.7.2 on Windows.
        /// </summary>
        NetFx472 = 0x1000,       /* NetFx472 on Windows. */

        /// <summary>
        /// A build targeting the .NET Framework 4.8 on Windows.
        /// </summary>
        NetFx48 = 0x2000,        /* NetFx48 on Windows. */

        /// <summary>
        /// A build targeting the .NET Framework 4.8.1 on Windows.
        /// </summary>
        NetFx481 = 0x4000,       /* NetFx481 on Windows. */

        /// <summary>
        /// A build targeting .NET Standard 2.0 on Windows or Unix.
        /// </summary>
        NetStandard20 = 0x8000,  /* NetStandard20 on Windows or Unix. */

        /// <summary>
        /// A build targeting .NET Standard 2.1 on Windows or Unix.
        /// </summary>
        NetStandard21 = 0x10000, /* NetStandard21 on Windows or Unix. */

        /// <summary>
        /// A build with all optional features disabled.
        /// </summary>
        Bare = 0x20000,          /* all optional features disabled. */

        /// <summary>
        /// A build with most speed-impacting optional features disabled.
        /// </summary>
        LeanAndMean = 0x40000,   /* most speed-impacting optional features
                                  * disabled. */

        /// <summary>
        /// A build intended for SQL Server 2005 (or later) embedding.
        /// </summary>
        Database = 0x80000,      /* for SQL Server 2005+ embedding. */

        /// <summary>
        /// A build targeting Mono on Unix.
        /// </summary>
        MonoOnUnix = 0x100000,   /* Mono on Unix. */

        /// <summary>
        /// A build intended for development and testing use.
        /// </summary>
        Development = 0x200000,  /* development and testing use. */

        /// <summary>
        /// The default build type, which has no associated file name suffix.
        /// </summary>
        Default = NetFx20        /* the default build type, has no suffix. */
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration identifies the kind of software component being
    /// checked for, or targeted by, an update operation.
    /// </summary>
#if EAGLE
    [ObjectId("065e1563-2765-4b25-b1a5-a290ab9e6019")]
#else
    [Guid("065e1563-2765-4b25-b1a5-a290ab9e6019")]
#endif
    public enum UpdateType /* ToLower */
    {
        /// <summary>
        /// No update type; this value is a placeholder and should not be used.
        /// </summary>
        None = 0x0,      /* nop, do not use. */

        /// <summary>
        /// An invalid update type; this value is reserved and should not be
        /// used.
        /// </summary>
        Invalid = 0x1,   /* invalid, do not use. */

        /// <summary>
        /// An update to the Eagle script engine itself.
        /// </summary>
        Engine = 0x2,    /* the (Eagle) script engine itself. */

        /// <summary>
        /// An update to a binary plugin (i.e. an implementation of the
        /// <c>IPlugin</c> interface).
        /// </summary>
        Plugin = 0x4,    /* a binary plugin (i.e. think "IPlugin"). */

        /// <summary>
        /// An update to a script file.
        /// </summary>
        Script = 0x8,    /* a script file. */

        /// <summary>
        /// An update to something else; this value is reserved and should not
        /// be used.
        /// </summary>
        Other = 0x10,    /* something else, reserved, do not use. */

        /// <summary>
        /// The default update type.
        /// </summary>
        Default = Engine /* the default update type. */
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration identifies the kind of release (or distribution
    /// packaging) of the Eagle software.
    /// </summary>
#if EAGLE
    [ObjectId("5239484a-dc56-45b1-9e07-ab48c792bfeb")]
#else
    [Guid("5239484a-dc56-45b1-9e07-ab48c792bfeb")]
#endif
    public enum ReleaseType
    {
        /// <summary>
        /// No release type; this value is a placeholder and should not be used.
        /// </summary>
        None = 0x0,         /* nop, do not use. */

        /// <summary>
        /// An invalid release type; this value is reserved and should not be
        /// used.
        /// </summary>
        Invalid = 0x1,      /* invalid, do not use. */

        /// <summary>
        /// Indicates that the release type should be detected automatically.
        /// </summary>
        Automatic = 0x2,    /* Attempt to detect release type. */

        /// <summary>
        /// A source code release.
        /// </summary>
        Source = 0x4,       /* source code release. */

        /// <summary>
        /// A Windows setup (installer) release.
        /// </summary>
        Setup = 0x8,        /* Windows setup release. */

        /// <summary>
        /// A binary release (not packaged as a setup installer).
        /// </summary>
        Binary = 0x10,      /* binary release (not setup). */

        /// <summary>
        /// A runtime release containing both the core library and the shell.
        /// </summary>
        Runtime = 0x20,     /* runtime release (core + shell). */

        /// <summary>
        /// A runtime release containing the core library only.
        /// </summary>
        Core = 0x40,        /* runtime release (core only). */

        /// <summary>
        /// A release containing a plugin only.
        /// </summary>
        Plugin = 0x80,      /* plugin release (plugin only). */

        /// <summary>
        /// The default release type.
        /// </summary>
        Default = Automatic /* the default release type. */
    }
}
