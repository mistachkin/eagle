/*
 * GarudaPal.h -- Eagle Package for Tcl (Garuda)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#ifndef _GARUDA_PAL_H_
#define _GARUDA_PAL_H_

/*
 * NOTE: These type defines are defined only for non-Windows platforms as they
 *       are normally defined by the Windows SDK.
 */

#if !defined(_WIN32)

#ifndef _Pal_Atomic
#define _Pal_Atomic _Atomic
#endif

#ifndef _CONST_DEFINED
#define _CONST_DEFINED
#define CONST const
#endif

#ifndef _LPVOID_DEFINED
#define _LPVOID_DEFINED
typedef void *LPVOID;
#endif

#ifndef _BOOL_DEFINED
#define _BOOL_DEFINED
typedef int BOOL;
#endif

#ifndef _DWORD_DEFINED
#define _DWORD_DEFINED
typedef unsigned int DWORD;
#endif

#ifndef _LPDWORD_DEFINED
#define _LPDWORD_DEFINED
typedef DWORD *LPDWORD;
#endif

#ifndef _LONG_DEFINED
#define _LONG_DEFINED
typedef long LONG;
#endif

#ifndef _HRESULT_DEFINED
#define _HRESULT_DEFINED
typedef int HRESULT;
#endif

#ifndef _CHAR_DEFINED
#define _CHAR_DEFINED
typedef char CHAR;
#endif

#ifndef _LPSTR_DEFINED
#define _LPSTR_DEFINED
typedef CHAR *LPSTR;
#endif

#ifndef _LPCSTR_DEFINED
#define _LPCSTR_DEFINED
typedef CONST CHAR *LPCSTR;
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

#ifndef _HMODULE_DEFINED
#define _HMODULE_DEFINED
typedef void *HMODULE;
#endif

#ifndef FALSE
#define FALSE				(0)
#endif

#ifndef TRUE
#define TRUE				(1)
#endif

#ifndef ERROR_SERVICE_NEVER_STARTED
#define ERROR_SERVICE_NEVER_STARTED	(1077)
#endif

#ifndef ERROR_FUNCTION_NOT_CALLED
#define ERROR_FUNCTION_NOT_CALLED	(1626)
#endif

#ifndef S_OK
#define S_OK				(0)
#endif

#ifndef S_FALSE
#define S_FALSE				(1)
#endif

#ifndef E_NOTIMPL
#define E_NOTIMPL			(0x80000001)
#endif

#ifndef E_POINTER
#define E_POINTER			(0x80004003)
#endif

#ifndef E_FAIL
#define E_FAIL				(0x80004005)
#endif

#ifndef DISP_E_OVERFLOW
#define DISP_E_OVERFLOW			(0x8002000A)
#endif

#ifndef CO_E_PATHTOOLONG
#define CO_E_PATHTOOLONG		(0x80040116)
#endif

#ifndef E_OUTOFMEMORY
#define E_OUTOFMEMORY			(0x8007000E)
#endif

#ifndef E_INVALIDARG
#define E_INVALIDARG			(0x80070057)
#endif

#ifndef UNICODE_STRING_MAX_CHARS
#define UNICODE_STRING_MAX_CHARS	(32767)
#endif

#if !defined(SUCCEEDED)
#define SUCCEEDED(x)			(((HRESULT)(x)) >= 0)
#endif

#if !defined(FAILED)
#define FAILED(x)			(((HRESULT)(x)) < 0)
#endif

#if !defined(SEVERITY_ERROR)
#define SEVERITY_ERROR			(1)
#endif

#if !defined(FACILITY_CUSTOMER_BIT)
#define FACILITY_CUSTOMER_BIT		(0x20000000)
#endif

#if !defined(FACILITY_WIN32)
#define FACILITY_WIN32			(7)
#endif

#if !defined(FACILITY_CRT)
#define FACILITY_CRT			(76)
#endif

#if defined(__STDC_NO_ATOMICS__)
#if !defined(InterlockedIncrement)
#define InterlockedIncrement(a)		((*(a))++)
#endif

#if !defined(InterlockedDecrement)
#define InterlockedDecrement(a)		((*(a))--)
#endif

#if !defined(InterlockedCompareExchange)
#define InterlockedCompareExchange(a, b, c) \
			(((*(a)) == (c)) ? ((*(a)) = (b), (c)) : (*(a)))
#endif
#else
#if !defined(InterlockedIncrement)
#define InterlockedIncrement(a)		atomic_fetch_add((a), 1)
#endif

#if !defined(InterlockedDecrement)
#define InterlockedDecrement(a)		atomic_fetch_sub((a), 1)
#endif

#if !defined(InterlockedCompareExchange)
#define InterlockedCompareExchange(a, b, c) \
			atomic_compare_exchange_strong((a), (c), (b))
#endif
#endif

#if !defined(FACILITY_CUSTOMER_CRT)
#define FACILITY_CUSTOMER_CRT \
			(((unsigned int)(FACILITY_CUSTOMER_BIT)) | \
			(((unsigned int)(FACILITY_CRT)) << 16))
#endif

#if !defined(HRESULT_FROM_WIN32)
#define HRESULT_FROM_WIN32(x) \
		((HRESULT)((((unsigned int)(SEVERITY_ERROR)) << 31) | \
		(((unsigned int)(FACILITY_WIN32)) << 16) | ((x) & 0xFFFF)))
#endif

#if !defined(HRESULT_FROM_ERRNO)
#define HRESULT_FROM_ERRNO(x) \
		((HRESULT)((((unsigned int)(SEVERITY_ERROR)) << 31) | \
		(FACILITY_CUSTOMER_CRT) | ((x) & 0xFFFF)))
#endif

#else /* defined(_WIN32) */

#ifndef _Pal_Atomic
#define _Pal_Atomic
#endif

#endif /* !defined(_WIN32) */

/*
 * NOTE: These type defines are shared among all the supported platforms.
 */

#ifndef _TCL_UNICCHAR_DEFINED
#define _TCL_UNICCHAR_DEFINED
typedef const Tcl_UniChar Tcl_UniCChar;
#endif

#ifndef _TCL_UNISTR_DEFINED
#define _TCL_UNISTR_DEFINED
typedef Tcl_UniChar *Tcl_UniStr;
#endif

#ifndef _TCL_UNICSTR_DEFINED
#define _TCL_UNICSTR_DEFINED
typedef Tcl_UniCChar *Tcl_UniCStr;
#endif

/*
 * NOTE: These defines are shared among all the supported platforms.
 */

#ifndef PATH_MAX
#define PATH_MAX			(4096)
#endif

#ifndef RUNTIMECONFIG_SUFFIX
#define RUNTIMECONFIG_SUFFIX		".runtimeconfig.json"
#endif

#ifndef UNICODE_RUNTIMECONFIG_SUFFIX
#define UNICODE_RUNTIMECONFIG_SUFFIX	UNICODE_TEXT(RUNTIMECONFIG_SUFFIX)
#endif

/*
 * HACK: Stolen from file "runtime/src/native/corehost/error_codes.h"
 *       in the .NET runtime repository.
 */

#if defined(USE_CORE_CLR) && !defined(__ERROR_CODES_H__)
#define __ERROR_CODES_H__
enum StatusCode {
    Success = 0,
    Success_HostAlreadyInitialized = 0x00000001,
    Success_DifferentRuntimeProperties = 0x00000002
};
#endif

/*
 * HACK: By default, hide internal functions that are shared between
 *       files of this library.
 */

#ifndef PACKAGE_INTERN
#  if defined(__GNUC__) && __GNUC__ >= 4
#    define PACKAGE_INTERN  __attribute__((visibility("hidden")))
#  else
#    define PACKAGE_INTERN
#  endif
#endif

/*
 * NOTE: These are the non-Windows functions used internally by this
 *       library (i.e. they are shared by several files).  On Windows,
 *       they are not needed because their functionality is provided
 *       by the Windows SDK.
 */

#if !defined(_WIN32)
PACKAGE_INTERN HRESULT	build_runtimeconfig_file_name(char *fileName,
			    size_t size);
PACKAGE_INTERN size_t 	get_module_file_name(HMODULE hModule,
			    char *fileName, size_t size);
PACKAGE_INTERN HMODULE	get_tcl_module_handle(void);
#endif

/*
 * NOTE: Setup the wrappers for the Tcl mutex APIs that are used by
 *       this package.
 */

#if defined(USE_CORE_CLR) && !defined(_WIN32)
/*
 * NOTE: For non-Windows platforms, a special struct is required to keep track
 *       of Tcl mutex ownership in order to avoid (self) deadlocks, since Tcl
 *       8.x does not use recursive mutexes.
 */

typedef struct pthread_owner_t {
    size_t sizeOf;		    /* Size of this structure, in bytes. */
    pthread_t owner;		    /* Represents owner of an external mutex. */
    unsigned recursionDepth;	    /* How many recursive locks for owner? */
    Tcl_Mutex mutex;		    /* Mutex used to protect the owner field. */
} pthread_owner_t;

PACKAGE_INTERN int	Pal_MutexLock(Tcl_Mutex *mutexPtr,
			    pthread_owner_t *ownerPtr);
PACKAGE_INTERN int	Pal_MutexUnlock(Tcl_Mutex *mutexPtr,
			    pthread_owner_t *ownerPtr);

#  define Wrp_MutexLock(a)		Pal_MutexLock((a), &packageOwner)
#  define Wrp_MutexUnlock(a)		Pal_MutexUnlock((a), &packageOwner)
#else
#  define Wrp_MutexLock			Tcl_MutexLock
#  define Wrp_MutexUnlock		Tcl_MutexUnlock
#endif

/*
 * HACK: A POSIX-compatible PTHREAD_NULL macro is required -AND- we must be
 *       able to compare it against another pthread_t for equality.
 */

#if defined(PTHREAD_NULL)
#  define PTHREAD_IS_NULL(a)		pthread_equal((a), PTHREAD_NULL)
#else
#  define PTHREAD_NULL			((pthread_t)0)
#  define PTHREAD_IS_NULL(a)		((a) == PTHREAD_NULL)
#endif

#endif /* _GARUDA_PAL_H_ */
