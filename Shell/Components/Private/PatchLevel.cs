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

#if STATIC && (ASSEMBLY_DATETIME || ASSEMBLY_RELEASE || SOURCE_ID || SOURCE_TIMESTAMP || ASSEMBLY_TEXT || ASSEMBLY_STRONG_NAME_TAG)
using Eagle._Attributes;
#endif

///////////////////////////////////////////////////////////////////////////////

#if PATCHLEVEL
[assembly: AssemblyVersion("1.0.8734.30319")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if STATIC && ASSEMBLY_DATETIME
[assembly: AssemblyDateTime("2024.07.20T00:00:00.000 +0000")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if STATIC && ASSEMBLY_RELEASE
[assembly: AssemblyRelease("Fire DRAGON Series, Beta 55")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if STATIC && SOURCE_ID
[assembly: AssemblySourceId("29187b7247d15136ccc8142f89e70b0406be61cf")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if STATIC && SOURCE_TIMESTAMP
[assembly: AssemblySourceTimeStamp("2024-08-13 17:22:12 UTC")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if STATIC && ASSEMBLY_TEXT
[assembly: AssemblyText("MonoOnUnix")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if STATIC && ASSEMBLY_STRONG_NAME_TAG
[assembly: AssemblyStrongNameTag("EagleFast")]
#endif
