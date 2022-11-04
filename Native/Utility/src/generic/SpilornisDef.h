/*
 * SpilornisDef.h -- Eagle Native Utility Library (Spilornis)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#ifndef _SPILORNIS_DEF_H_
#define _SPILORNIS_DEF_H_

/*
 * NOTE: Attempt to determine if the "wchar_t" data type is greater than
 *       what the CLR and Mono, et al, can handle.  Both the CLR and the
 *       Mono runtime only support two byte characters in their P/Invoke
 *       marshalling subsystems.  Unfortunately, it appears that various
 *       compiler runtimes on non-Windows platforms (e.g. gcc on Linux)
 *       define "wchar_t" data type to be four bytes.  Most functions in
 *       this file cannot adapt to this difference; however, the version
 *       introspection entry point (i.e. "Eagle_GetVersion") can, since
 *       it is limited to output only.  This helps to enable the managed
 *       integration code to detect that the library is not suitable for
 *       use on those platforms (i.e. by detecting the SIZE_OF_WCHAR_T=4
 *       datum in the returned version string).
 */

#if defined(WCHAR_MAX) && defined(USHRT_MAX) && (WCHAR_MAX > USHRT_MAX)
#define USE_WCHARSTRTOUSHORTSTR			1
#endif

/*
 * NOTE: Attempt to determine if we can use the Win32 specific stuff that
 *       we need.  If not, alternative stuff will be used.
 */

#if !defined(USE_SYSSTRINGLEN) && defined(_MSC_VER) && defined(_WIN32) && \
    (defined(_M_IX86) || defined(_M_IA64) || defined(_M_X64) || \
     defined(_M_ARM) || defined(_M_ARM64))
#define USE_SYSSTRINGLEN			1
#endif

/*
 * NOTE: Attempt to determine if we can use the Win32 API for heap memory
 *	 allocation.
 */

#if !defined(USE_HEAPAPI) && defined(_MSC_VER) && defined(_WIN32)
#define USE_HEAPAPI				1
#endif

#endif /* _SPILORNIS_DEF_H_ */
