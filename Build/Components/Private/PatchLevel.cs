/*
 * PatchLevel.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if PATCHLEVEL
using System.Reflection;
#endif

#if ASSEMBLY_DATETIME || ASSEMBLY_RELEASE || SOURCE_ID || SOURCE_TIMESTAMP || ASSEMBLY_TEXT || ASSEMBLY_STRONG_NAME_TAG
using Eagle._Attributes;
#endif

///////////////////////////////////////////////////////////////////////////////

#if PATCHLEVEL
[assembly: AssemblyVersion("1.0.8192.54321")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if ASSEMBLY_DATETIME
[assembly: AssemblyDateTime("2022.05.06T12:42:00.000 +0000")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if ASSEMBLY_RELEASE
[assembly: AssemblyRelease("Fire Dragon Series, Beta 50")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if SOURCE_ID
[assembly: AssemblySourceId("48b99035ca39c1100f84abd801d0309f6ce25106")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if SOURCE_TIMESTAMP
[assembly: AssemblySourceTimeStamp("2022-05-26 21:44:39 UTC")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if ASSEMBLY_TEXT
[assembly: AssemblyText("MonoOnUnix")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if ASSEMBLY_STRONG_NAME_TAG
[assembly: AssemblyStrongNameTag("EagleFast")]
#endif
