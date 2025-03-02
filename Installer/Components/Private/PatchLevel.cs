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
[assembly: AssemblyVersion(null)]
#endif

///////////////////////////////////////////////////////////////////////////////

#if ASSEMBLY_DATETIME
[assembly: AssemblyDateTime(null)]
#endif

///////////////////////////////////////////////////////////////////////////////

#if ASSEMBLY_RELEASE
[assembly: AssemblyRelease(null)]
#endif

///////////////////////////////////////////////////////////////////////////////

#if SOURCE_ID
[assembly: AssemblySourceId(null)]
#endif

///////////////////////////////////////////////////////////////////////////////

#if SOURCE_TIMESTAMP
[assembly: AssemblySourceTimeStamp(null)]
#endif

///////////////////////////////////////////////////////////////////////////////

#if ASSEMBLY_TEXT
[assembly: AssemblyText(null)]
#endif

///////////////////////////////////////////////////////////////////////////////

#if ASSEMBLY_STRONG_NAME_TAG
[assembly: AssemblyStrongNameTag(null)]
#endif
