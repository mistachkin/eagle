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
[assembly: AssemblyVersion("1.0.8503.24499")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if ASSEMBLY_DATETIME
[assembly: AssemblyDateTime("2023.04.15T00:00:00.000 +0000")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if ASSEMBLY_RELEASE
[assembly: AssemblyRelease("Fire Dragon Series, Beta 53")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if SOURCE_ID
[assembly: AssemblySourceId("3408f6a4276cac14fcd30d0c0d9bc8afd3929c83")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if SOURCE_TIMESTAMP
[assembly: AssemblySourceTimeStamp("2023-06-06 14:43:06 UTC")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if ASSEMBLY_TEXT
[assembly: AssemblyText("MonoOnUnix")]
#endif

///////////////////////////////////////////////////////////////////////////////

#if ASSEMBLY_STRONG_NAME_TAG
[assembly: AssemblyStrongNameTag("EagleFast")]
#endif
