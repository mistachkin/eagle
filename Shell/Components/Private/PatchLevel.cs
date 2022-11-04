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
[assembly: AssemblyVersion("1.0.8251.52012")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if STATIC && ASSEMBLY_DATETIME
[assembly: AssemblyDateTime("2022.11.01T01:00:00.000 +0000")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if STATIC && ASSEMBLY_RELEASE
[assembly: AssemblyRelease("15th Anniversary Special Edition, Beta 51")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if STATIC && SOURCE_ID
[assembly: AssemblySourceId("a9d911e9e2b184473fd1fe1825d7ce962fc6f5ce")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if STATIC && SOURCE_TIMESTAMP
[assembly: AssemblySourceTimeStamp("2022-11-04 01:43:04 UTC")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if STATIC && ASSEMBLY_TEXT
[assembly: AssemblyText("MonoOnUnix")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if STATIC && ASSEMBLY_STRONG_NAME_TAG
[assembly: AssemblyStrongNameTag("EagleFast")]
#endif
