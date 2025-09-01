/*
 * GarudaPre.h -- Eagle Package for Tcl (Garuda)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#ifndef _GARUDA_PRE_H_
#define _GARUDA_PRE_H_

/*
 * NOTE: The following define is needed to prevent the a Tcl-related compiler
 *       error on 64-bit platforms when including the file "tclInt.h":
 *
 *       error C2371: 'ptrdiff_t' : redefinition; different basic types
 *
 *       This define is currently limited to the MSVC compilers that shipped
 *       with Visual Studio 2005 or higher because this project does not
 *       formally support previous compilers; however, other compilers may
 *       also need this define.
 */

#if !defined(STDC_HEADERS) && defined(_MSC_VER) && _MSC_VER >= 1400
#  define STDC_HEADERS
#endif

/*
 * NOTE: For now, only enable use of the latest version of the CLR if we are
 *       compiling with the MSVC compiler that shipped with Visual Studio 2010
 *       or higher -AND- the CLR_40 compile-time option is enabled; otherwise,
 *       we probably want to use the CoreCLR.
 */

#if defined(CORE_CLR)
#  define USE_CORE_CLR
#elif defined(CLR_40)
#  if defined(_MSC_VER) && _MSC_VER >= 1600
#    define USE_CLR_40
#  elif defined(RC_MSC_VER) && RC_MSC_VER >= 1600
#    define USE_CLR_40
#  endif
#endif

/*
 * NOTE: These are the primary (major) versions of the CLR and CoreCLR that
 *       this package knows about.
 */

#ifndef CLR_VERSION_V2
#  define CLR_VERSION_V2			"v2.0.50727"
#endif

#ifndef CLR_VERSION_V4
#  define CLR_VERSION_V4			"v4.0.30319"
#endif

#ifndef CORE_CLR_VERSION_V3
#  define CORE_CLR_VERSION_V3			"v3.0+"
#endif

/*
 * HACK: The following three typedef's are required due to a misfeature of
 *       the "hostfxr.h" header file.  It appears to assume that including
 *       files are being compiled in C++, which would allow a typedef name
 *       to be used as a complete type before its own declaration.
 */

#if defined(USE_CORE_CLR) && !defined(NO_HOSTFXR_TYPEDEF_HACK)
typedef struct hostfxr_dotnet_environment_info
hostfxr_dotnet_environment_info;

typedef struct hostfxr_dotnet_environment_sdk_info
hostfxr_dotnet_environment_sdk_info;

typedef struct hostfxr_dotnet_environment_framework_info
hostfxr_dotnet_environment_framework_info;
#endif

#endif /* _GARUDA_PRE_H_ */
