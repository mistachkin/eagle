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
  #define STDC_HEADERS
#endif

/*
 * NOTE: For now, only enable use of the latest version of the CLR if we are
 *       compiling with the MSVC compiler that shipped with Visual Studio 2010
 *       or higher -AND- the CLR_40 compile-time option is enabled.
 */

#if defined(_MSC_VER) && _MSC_VER >= 1600 && defined(CLR_40)
  #define USE_CLR_40
#elif defined(RC_MSC_VER) && RC_MSC_VER >= 1600 && defined(CLR_40)
  #define USE_CLR_40
#endif

/*
 * NOTE: These are the primary (major) versions of the CLR that this package
 *       knows about.
 */

#ifndef CLR_VERSION_V2
#  define CLR_VERSION_V2			"v2.0.50727"
#endif

#ifndef CLR_VERSION_V4
#  define CLR_VERSION_V4			"v4.0.30319"
#endif

#endif /* _GARUDA_PRE_H_ */
