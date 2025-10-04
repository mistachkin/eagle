/*
 * GarudaPal.c -- Eagle Package for Tcl (Garuda)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#include "GarudaPre.h"		/* NOTE: For private header setup. */

#if !defined(_WIN32)
#include <unistd.h>		/* NOTE: For readlink, etc. */
#include <assert.h>		/* NOTE: For assert macros, etc. */
#include <stdlib.h>		/* NOTE: For free, realpath, size_t, etc. */
#include <string.h>		/* NOTE: For strlen, strrchr, strcat, etc. */
#include <limits.h>		/* NOTE: For PATH_MAX (implicit?), etc. */
#include <errno.h>		/* NOTE: For errno, etc. */
#include <pthread.h>		/* NOTE: For pthread_self, etc. */
#include <dlfcn.h>		/* NOTE: For dlopen, dladdr, Dl_info, etc. */

#if defined(__APPLE__)
#  include <mach-o/dyld.h>	/* NOTE: For _NSGetExecutablePath, etc. */
#endif

#include "tcl.h"		/* NOTE: For public Tcl API. */
#include "GarudaPal.h"		/* NOTE: For platform abstraction API. */
#include "Garuda.h"		/* NOTE: For public package API. */
#include "GarudaInt.h"		/* NOTE: For private package API. */
#include "GarudaDecls.h"	/* NOTE: For private package declarations. */

#if defined(__linux__)
#  define LINUX_EXECUTABLE_LINK		"/proc/self/exe"
#endif

/*
 *----------------------------------------------------------------------
 *
 * linux_get_executable_file_name --
 *
 *	This function attempts to query the executable file name for
 *	the current process when running on Linux.
 *
 * Results:
 *	A standard COM result.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

#if defined(__linux__)
static HRESULT linux_get_executable_file_name(
    char *fileName,		/* Buffer that should contain the
				 * target file name upon success. */
    size_t size)		/* Size of the file name buffer. */
{
    ssize_t nRead;

    if (fileName == NULL) {
	return E_POINTER;
    }

    if (size == 0) {
	return E_INVALIDARG;
    }

    nRead = readlink(LINUX_EXECUTABLE_LINK, fileName, size - 1);

    if ((nRead < 0) || ((size_t)nRead >= size))
	return HRESULT_FROM_ERRNO(errno);

    fileName[nRead] = '\0';
    return S_OK;
}
#endif

/*
 *----------------------------------------------------------------------
 *
 * macos_get_executable_file_name --
 *
 *	This function attempts to query the executable file name for
 *	the current process when running on macOS.
 *
 * Results:
 *	A standard COM result.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

#if defined(__APPLE__)
static HRESULT  macos_get_executable_file_name(
    char *fileName,		/* Buffer that should contain the
				 * target file name upon success. */
    size_t size)		/* Size of the file name buffer. */
{
    char *realPath;
    uint32_t iSize;

    if (fileName == NULL) {
	return E_POINTER;
    }

    if (size == 0) {
	return E_INVALIDARG;
    }

    iSize = (uint32_t)size;

    if (_NSGetExecutablePath(fileName, &iSize) != 0)
	return E_FAIL;

    realPath = realpath(fileName, NULL);

    if (realPath == NULL)
	return HRESULT_FROM_ERRNO(errno);

    if (strlen(realPath) + 1 > size) {
	free(realPath);
	return CO_E_PATHTOOLONG;
    }

    strcpy(fileName, realPath);
    free(realPath);

    return S_OK;
}
#endif

/*
 *----------------------------------------------------------------------
 *
 * build_runtimeconfig_file_name --
 *
 *	This function attempts to build the (runtime) configuration
 *	file name for the current process, i.e. for use with the .NET
 *	runtime.
 *
 * Results:
 *	A standard COM result.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

HRESULT build_runtimeconfig_file_name(
    char *fileName,		/* Buffer that should contain the
				 * target file name upon success. */
    size_t size)		/* Size of the file name buffer. */
{
    HRESULT hResult;
    size_t newSize = 0;
    char *slash;
    char *dot;

#if defined(__linux__)
    hResult = linux_get_executable_file_name(fileName, size);
#elif defined(__APPLE__)
    hResult = macos_get_executable_file_name(fileName, size);
#else
    hResult = E_NOTIMPL;
#endif

    if (FAILED(hResult))
	return hResult;

    slash = strrchr(fileName, '/'); /* OPTIONAL */
    dot = strrchr(slash ? slash + 1 : fileName, '.'); /* OPTIONAL */

    if (dot != NULL)
	*dot = '\0';

    newSize += strlen(fileName) + 1;
    newSize += strlen(RUNTIMECONFIG_SUFFIX);

    if (newSize > size)
	return DISP_E_OVERFLOW;

    strcat(fileName, RUNTIMECONFIG_SUFFIX);
    return S_OK;
}
/*
 *----------------------------------------------------------------------
 *
 * get_module_file_name --
 *
 *	This function attempts to query the file name for the current
 *	shared library module handle within the current process.
 *
 * Results:
 *	The number of bytes actually written into the file name buffer.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

size_t get_module_file_name(
    HMODULE hModule,		/* The module handle to query the
				 * file name for. */
    char *fileName,		/* Buffer that should contain the
				 * target file name upon success. */
    size_t size)		/* Size of the file name buffer. */
{
    Dl_info info;
    size_t newSize = 0;

    memset(&info, 0, sizeof(Dl_info));

    if (dladdr((void *)hModule, &info) == 0)
	return 0;

    if (info.dli_fname == NULL)
	return 0;

    newSize += strlen(info.dli_fname) + 1;

    if (newSize > size)
	return 0;

    strcpy(fileName, info.dli_fname);
    return newSize;
}

/*
 *----------------------------------------------------------------------
 *
 * get_tcl_module_handle --
 *
 *	This function attempts to query the Tcl shared library module
 *	handle for the current process.  The caller should not call
 *	dlclose() on the returned shared library module handle as it
 *	is generally unwise to unload Tcl from the current process
 *	and the returned shared library module handle may not have an
 *	extra reference added to it by this function.
 *
 * Results:
 *	The Tcl shared library module handle -OR- NULL if it cannot be
 *	determined.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

HMODULE get_tcl_module_handle(void)
{
    Dl_info info;

    memset(&info, 0, sizeof(Dl_info));

    if (dladdr((void *)Tcl_CreateInterp, &info) == 0)
        return NULL;

    if (info.dli_fname == NULL)
        return NULL;

#if defined(RTLD_NOLOAD)
    {
	HMODULE hModule = dlopen(info.dli_fname,
	    RTLD_LAZY | RTLD_NOLOAD);

	if (hModule != NULL)
	    return hModule;
    }
#endif

    return dlopen(info.dli_fname, RTLD_LAZY);
}

/*
 *----------------------------------------------------------------------
 *
 * GetPackageModule --
 *
 *	This function returns a value that should be used in place of
 *	the package module handle on non-Windows operating systems.
 *
 * Results:
 *	The package module handle -OR- NULL if it is not available.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

HMODULE GetPackageModule(void)
{
    return (HMODULE)Garuda_Init;
}

/*
 *----------------------------------------------------------------------
 *
 * SetPackageModule --
 *
 *	This function sets the package module handle to the specified
 *	value.
 *
 * Results:
 *	None.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

void SetPackageModule(
    HMODULE hModule)		/* The new package module handle. */
{
    /*
     * HACK: This function does not actually do anything.  It is
     *       included for consistency, because it is declared in
     *       the "GarudaDecls.h" header file.
     */
}

/*
 *----------------------------------------------------------------------
 *
 * Pal_MutexLock --
 *
 *	This procedure is invoked to lock a mutex.  This is a self
 *	initializing mutex that is automatically finalized during
 *	Tcl_Finalize.
 *
 * Results:
 *	Non-zero upon success; otherwise, zero.
 *
 * Side effects:
 *	May block the current thread.  The mutex is either acquired
 *	or will have a reference added to it upon success.
 *
 *----------------------------------------------------------------------
 */

int Pal_MutexLock(
    Tcl_Mutex *mutexPtr,	/* The mutex to lock. */
    pthread_owner_t *ownerPtr)	/* Owner of mutex, if any. */
{
    pthread_t self;

    if ((mutexPtr == NULL) || (ownerPtr == NULL))
	return 0;

    self = pthread_self();

    Tcl_MutexLock(&ownerPtr->mutex);

    if (!PTHREAD_IS_NULL(ownerPtr->owner) &&
	    pthread_equal(ownerPtr->owner, self)) {
	assert(ownerPtr->recursionDepth > 0);
	ownerPtr->recursionDepth++;

	Tcl_MutexUnlock(&ownerPtr->mutex);
	return 1;
    }

    assert(ownerPtr->recursionDepth == 0);

    Tcl_MutexUnlock(&ownerPtr->mutex);
    Tcl_MutexLock(mutexPtr); /* BLOCKING */
    Tcl_MutexLock(&ownerPtr->mutex);

    ownerPtr->owner = self;
    ownerPtr->recursionDepth++;

    Tcl_MutexUnlock(&ownerPtr->mutex);
    return 1;
}

/*
 *----------------------------------------------------------------------
 *
 * Pal_MutexUnlock --
 *
 *	This procedure is invoked to unlock a mutex.  If the mutex is
 *	not owned by the current thread, results are undefined; it is
 *	possible that the call will simply be ignored.
 *
 * Results:
 *	Non-zero upon success; otherwise, zero.
 *
 * Side effects:
 *	The mutex reference is released upon success; if the final
 *	reference is released, the mutex will be unlocked. Upon any
 *	failure, the mutex state is unchanged.
 *
 *----------------------------------------------------------------------
 */

int Pal_MutexUnlock(
    Tcl_Mutex *mutexPtr,	/* The mutex to unlock. */
    pthread_owner_t *ownerPtr)	/* Owner of mutex, if any. */
{
    pthread_t self;
    unsigned depth;

    if ((mutexPtr == NULL) || (ownerPtr == NULL))
	return 0;

    self = pthread_self();

    Tcl_MutexLock(&ownerPtr->mutex);

    if (PTHREAD_IS_NULL(ownerPtr->owner)) {
	assert(ownerPtr->recursionDepth == 0);
	Tcl_MutexUnlock(&ownerPtr->mutex);
	return 0; /* BUGBUG: There is no owner. */
    }

    assert(ownerPtr->recursionDepth > 0);

    if (!pthread_equal(ownerPtr->owner, self) ||
	    (ownerPtr->recursionDepth == 0)) {
	Tcl_MutexUnlock(&ownerPtr->mutex);
	return 0; /* BUGBUG: Caller not owner. */
    }

    if ((depth = (--ownerPtr->recursionDepth)) == 0) {
	assert(ownerPtr->recursionDepth == 0);
	ownerPtr->owner = PTHREAD_NULL;
    }

    Tcl_MutexUnlock(&ownerPtr->mutex);

    if (depth == 0)
	Tcl_MutexUnlock(mutexPtr);

    return 1;
}
#endif /* !defined(_WIN32) */
