/*
 * GarudaClr.h -- Eagle Package for Tcl (Garuda)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#ifndef _GARUDA_CLR_H_
#define _GARUDA_CLR_H_

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

/*
 * NOTE: This is the latest version of the CLR that we know about.  This
 *       is the value that will be passed to the GetRuntime method of the
 *       ICLRMetaHost interface (which will only be used when the USE_CLR_40
 *       compile-time option is enabled).  For now, only configure use of the
 *       latest version of the CLR if we are compiling with the MSVC compiler
 *       that shipped with Visual Studio 2010 or higher.
 */

#if defined(USE_CLR_40)
#  define CLR_MODULE_NAME			"MSCorEE"
#  define CLR_PROC_NAME				"CLRCreateInstance"
#  define CLR_MINIMUM_ENVVAR_NAME		"UseMinimumClr"
#  define CLR_VERSION_MINIMUM			CLR_VERSION_V2
#  define CLR_VERSION_LATEST			CLR_VERSION_V4
#endif

#endif /* _GARUDA_CLR_H_ */
