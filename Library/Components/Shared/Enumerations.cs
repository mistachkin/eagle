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
#if EAGLE
    [ObjectId("793e7b27-3390-4fcc-90dd-b91faf924f4e")]
#else
    [Guid("793e7b27-3390-4fcc-90dd-b91faf924f4e")]
#endif
    public enum BuildType
    {
        None = 0x0,             /* nop, do not use. */
        Invalid = 0x1,          /* invalid, do not use. */
        NetFx20 = 0x2,          /* NetFx20 on Windows. */
        NetFx35 = 0x4,          /* NetFx35 on Windows. */
        NetFx40 = 0x8,          /* NetFx40 on Windows. */
        NetFx45 = 0x10,         /* NetFx45 on Windows. */
        NetFx451 = 0x20,        /* NetFx451 on Windows. */
        NetFx452 = 0x40,        /* NetFx452 on Windows. */
        NetFx46 = 0x80,         /* NetFx46 on Windows. */
        NetFx461 = 0x100,       /* NetFx461 on Windows. */
        NetFx462 = 0x200,       /* NetFx462 on Windows. */
        NetFx47 = 0x400,        /* NetFx47 on Windows. */
        NetFx471 = 0x800,       /* NetFx471 on Windows. */
        NetFx472 = 0x1000,      /* NetFx472 on Windows. */
        NetFx48 = 0x2000,       /* NetFx48 on Windows. */
        NetStandard20 = 0x4000, /* NetStandard20 on Windows or Unix. */
        NetStandard21 = 0x8000, /* NetStandard21 on Windows or Unix. */
        Bare = 0x10000,         /* all optional features disabled. */
        LeanAndMean = 0x20000,  /* most speed-impacting optional features
                                 * disabled. */
        Database = 0x40000,     /* for SQL Server 2005+ embedding. */
        MonoOnUnix = 0x80000,   /* Mono on Unix. */
        Development = 0x100000, /* development and testing use. */
        Default = NetFx20       /* the default build type, has no suffix. */
    }

    ///////////////////////////////////////////////////////////////////////////

#if EAGLE
    [ObjectId("065e1563-2765-4b25-b1a5-a290ab9e6019")]
#else
    [Guid("065e1563-2765-4b25-b1a5-a290ab9e6019")]
#endif
    public enum UpdateType /* ToLower */
    {
        None = 0x0,      /* nop, do not use. */
        Invalid = 0x1,   /* invalid, do not use. */
        Engine = 0x2,    /* the (Eagle) script engine itself. */
        Plugin = 0x4,    /* a binary plugin (i.e. think "IPlugin"). */
        Script = 0x8,    /* a script file. */
        Other = 0x10,    /* something else, reserved, do not use. */
        Default = Engine /* the default update type. */
    }

    ///////////////////////////////////////////////////////////////////////////

#if EAGLE
    [ObjectId("5239484a-dc56-45b1-9e07-ab48c792bfeb")]
#else
    [Guid("5239484a-dc56-45b1-9e07-ab48c792bfeb")]
#endif
    public enum ReleaseType
    {
        None = 0x0,         /* nop, do not use. */
        Invalid = 0x1,      /* invalid, do not use. */
        Automatic = 0x2,    /* Attempt to detect release type. */
        Source = 0x4,       /* source code release. */
        Setup = 0x8,        /* Windows setup release. */
        Binary = 0x10,      /* binary release (not setup). */
        Runtime = 0x20,     /* runtime release (core + shell). */
        Core = 0x40,        /* runtime release (core only). */
        Plugin = 0x80,      /* plugin release (plugin only). */
        Default = Automatic /* the default release type. */
    }
}
