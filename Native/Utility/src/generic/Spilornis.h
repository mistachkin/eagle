/*
 * Spilornis.h -- Eagle Native Utility Library (Spilornis)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#ifndef _SPILORNIS_H_
#define _SPILORNIS_H_

/*****************************************************************************/

/*
 * WARNING: These values must match those used by the ReturnCode enumeration
 *          from the Eagle core library.
 */

#ifndef EAGLE_OK
#  define EAGLE_OK				(0)
#endif

#ifndef EAGLE_ERROR
#  define EAGLE_ERROR				(1)
#endif

/*****************************************************************************/

#ifndef _CONST_DEFINED
#define _CONST_DEFINED
#define CONST const
#endif

#ifndef _VOID_DEFINED
#define _VOID_DEFINED
#define VOID void
#endif

#ifndef _LPVOID_DEFINED
#define _LPVOID_DEFINED
typedef VOID *LPVOID;
#endif

#ifndef __SIZE_T_DEFINED
#define __SIZE_T_DEFINED
/*
 * NOTE: If USE_32BIT_SIZE_T is defined and non-zero, use a 32-bit signed
 *       integer to represent all element counts and sizes (i.e. to better
 *       interoperate with managed code).  Technically, this SIZE_T type
 *       definition should be unsigned; however, for some reason, the CLR
 *       really wants string lengths and array element counts to be signed.
 */
#if defined(USE_32BIT_SIZE_T) && USE_32BIT_SIZE_T
typedef int SIZE_T;
#else
typedef size_t SIZE_T;
#endif
#endif

#ifndef _LPSIZE_T_DEFINED
#define _LPSIZE_T_DEFINED
typedef SIZE_T *LPSIZE_T;
#endif

#ifndef _LPCSIZE_T_DEFINED
#define _LPCSIZE_T_DEFINED
typedef CONST SIZE_T *LPCSIZE_T;
#endif

#ifndef _WCHAR_DEFINED
#define _WCHAR_DEFINED
typedef wchar_t WCHAR;
#endif

#ifndef _LPWSTR_DEFINED
#define _LPWSTR_DEFINED
typedef WCHAR *LPWSTR;
#endif

#ifndef _LPCWSTR_DEFINED
#define _LPCWSTR_DEFINED
typedef CONST WCHAR *LPCWSTR;
#endif

#ifndef _RETURNCODE_DEFINED
#define _RETURNCODE_DEFINED
typedef int RETURNCODE;
#endif

/*****************************************************************************/

/*
 * NOTE: These are the public functions exported by this library.
 */

#ifndef EAGLE_EXTERN
#  define EAGLE_EXTERN
#endif

EAGLE_EXTERN LPCWSTR	Eagle_GetVersion(VOID);
EAGLE_EXTERN LPVOID	Eagle_AllocateMemory(SIZE_T size);
EAGLE_EXTERN VOID	Eagle_FreeMemory(LPVOID pMemory);
EAGLE_EXTERN VOID	Eagle_FreeElements(SIZE_T elementCount,
			    LPWSTR *ppElements);
EAGLE_EXTERN RETURNCODE	Eagle_SplitList(SIZE_T length, LPCWSTR pText,
			    LPSIZE_T pElementCount,
			    LPSIZE_T *ppElementLengths,
			    LPCWSTR **pppElements,
			    LPCWSTR *ppError);
EAGLE_EXTERN RETURNCODE	Eagle_JoinList(SIZE_T elementCount,
			    LPCSIZE_T pElementLengths,
			    LPCWSTR *ppElements,
			    LPSIZE_T pLength, LPCWSTR *ppText,
			    LPCWSTR *ppError);

/*****************************************************************************/

#endif /* _SPILORNIS_H_ */
