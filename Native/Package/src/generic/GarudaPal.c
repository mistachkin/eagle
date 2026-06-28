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
 *	Query the absolute file name of the currently-running
 *	executable on Linux.
 *
 * Why / How:
 *	Reads the /proc/self/exe symlink, which the Linux kernel
 *	maintains as a pointer to the executable file backing the
 *	current process.  This is the canonical way to discover the
 *	exe path on Linux: argv[0] is unreliable (the parent process
 *	can pass an arbitrary string), and there is no portable
 *	equivalent of macOS's _NSGetExecutablePath.  readlink does
 *	NOT NUL-terminate its output buffer, so we explicitly add
 *	the terminator after the successful read.  We pass size-1
 *	to readlink so a successful call that fills the buffer to
 *	the requested size still leaves room for the NUL.
 *
 *	Truncation is reported as failure rather than silently
 *	accepted: if readlink wrote exactly size-1 bytes the result
 *	may have been truncated, and we have no reliable way to
 *	distinguish "fits exactly" from "truncated to fit", so we
 *	treat any return >= size as an error.
 *
 * Results:
 *	S_OK on success with fileName populated and NUL-terminated.
 *	E_POINTER if fileName is NULL.  E_INVALIDARG if size is 0.
 *	HRESULT_FROM_ERRNO(errno) if readlink fails or the result
 *	would not fit in the caller's buffer.
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
 *	Query the absolute, canonical file name of the currently-
 *	running executable on macOS.
 *
 * Why / How:
 *	Two-step process.  _NSGetExecutablePath (from
 *	<mach-o/dyld.h>) returns the path the kernel knows for the
 *	exe, BUT that path can contain symlinks and "." or ".."
 *	segments -- Apple's API does not canonicalize.  The .NET
 *	hosting layer's runtimeconfig discovery expects a fully-
 *	resolved path, so we run the result through realpath() to
 *	produce the absolute, symlink-followed form.
 *
 *	realpath() with a NULL second argument allocates the
 *	resolved path on the heap; we copy it into the caller's
 *	buffer and free the heap allocation before returning.  We
 *	do NOT hand the heap pointer back to the caller because
 *	the API contract here is "fill the caller-owned buffer".
 *
 *	_NSGetExecutablePath takes a uint32_t* for size; we copy
 *	size_t into a local uint32_t.  On 64-bit macOS this is
 *	never a problem in practice -- paths are bounded by
 *	PATH_MAX -- but the cast is explicit for clarity and in
 *	case the buffer convention ever changes.
 *
 * Results:
 *	S_OK on success.  E_POINTER if fileName is NULL.
 *	E_INVALIDARG if size is 0.  E_FAIL if _NSGetExecutablePath
 *	rejects the buffer (it returns non-zero per its own size
 *	protocol).  HRESULT_FROM_ERRNO(errno) if realpath fails.
 *	CO_E_PATHTOOLONG if the canonical path would not fit in
 *	the caller's buffer.
 *
 * Side effects:
 *	None caller-visible.  Internally allocates and frees a
 *	transient heap buffer via realpath/free.
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
 *	Construct the path to the .NET runtime-configuration JSON
 *	file (.runtimeconfig.json) that hostfxr expects to find
 *	co-located with the running host executable.
 *
 * Why / How:
 *	.NET 5+'s native hosting API requires a runtimeconfig.json
 *	file alongside the running executable, named after the
 *	executable with the RUNTIMECONFIG_SUFFIX appended.  Garuda
 *	is unusual in that the HOST is a third-party binary (tclsh
 *	or wish), not a .NET application that ships its own config:
 *	we have to compute the config path FOR that third-party
 *	binary at runtime.
 *
 *	Steps:
 *	  1. Get the running executable's path via the OS-specific
 *	     helper (Linux uses /proc/self/exe; macOS uses
 *	     _NSGetExecutablePath + realpath; other POSIX platforms
 *	     return E_NOTIMPL until they get added.  The Windows
 *	     path lives in another file in this package).
 *	  2. Find the last extension dot AFTER the final slash, if
 *	     any, and truncate there.  The "after the final slash"
 *	     part is load-bearing because path components like
 *	     "/usr/local.bin/foo" have a dot in "local." but no
 *	     extension on the file itself.
 *	  3. Verify the resulting truncated name plus the suffix
 *	     fits in the caller's buffer.
 *	  4. strcat the suffix.
 *
 * Results:
 *	S_OK on success with fileName populated.
 *	E_NOTIMPL on platforms where exe-path discovery is not
 *	implemented in this file.
 *	DISP_E_OVERFLOW if the constructed name would not fit in
 *	the caller's buffer.
 *	Any failure HRESULT propagated unchanged from the
 *	underlying OS-specific exe-path helper.
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
 *	POSIX equivalent of Win32 GetModuleFileName: given a shared
 *	library "module handle", return the absolute file name of
 *	the loaded library.
 *
 * Why / How:
 *	Uses dladdr() with the module handle reinterpreted as a
 *	generic pointer.  dladdr looks up which shared object
 *	contains the address and returns metadata about it via
 *	Dl_info, including dli_fname -- a pointer to the resolved
 *	path the loader used to map the library.
 *
 *	dli_fname points into loader-owned memory that is valid
 *	for the lifetime of the loaded library, so we strcpy it
 *	out into the caller's buffer rather than handing the
 *	loader's pointer back.  This frees the caller from having
 *	to know who owns the result string.
 *
 *	NB: on POSIX a "module handle" is whatever the caller
 *	last received from dlopen(), OR a pointer to a function
 *	known to live in the module -- see GetPackageModule() and
 *	get_tcl_module_handle() for both shapes.  This function
 *	accepts both because dladdr handles both transparently.
 *
 * Results:
 *	The number of bytes written to fileName INCLUDING the
 *	terminating NUL, on success.  Zero on any failure
 *	(dladdr returns 0, dli_fname is NULL, or the result
 *	would not fit in the caller's buffer).
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
 *	Locate the shared-library handle for the Tcl library that
 *	hosts the current process.
 *
 * Why / How:
 *	Garuda is a Tcl extension: when it loads, it does NOT
 *	receive a handle to the Tcl shared library -- it receives
 *	only the Tcl_Interp * that the [load] command was invoked
 *	from.  But the .NET hosting layer (and several Garuda
 *	diagnostics) want a module handle to Tcl itself.  We
 *	reverse-engineer one:
 *
 *	  1. Take a function pointer known to live inside Tcl --
 *	     specifically Tcl_CreateInterp.  When the package was
 *	     built USE_TCL_STUBS, Tcl_CreateInterp is a macro that
 *	     expands to a stubs-table indirection; we deref through
 *	     tclStubsPtr->tcl_CreateInterp explicitly to get the
 *	     real function address.  Without this explicit deref,
 *	     Clang on macOS will optimize the stubs-table chain in
 *	     a way that confuses dladdr() -- it ends up resolving
 *	     to a thunk inside our own package rather than a symbol
 *	     in the Tcl library.  This was learned the hard way.
 *	  2. dladdr() that pointer to find the path of the .so /
 *	     .dylib that contains it.
 *	  3. dlopen() that path with RTLD_LAZY | RTLD_NOLOAD if
 *	     the platform supports it.  RTLD_NOLOAD returns a
 *	     handle to the *already-loaded* library without
 *	     forcing a fresh load -- which would risk introducing
 *	     a second copy of Tcl into the process.
 *	  4. If RTLD_NOLOAD is not supported by the platform,
 *	     fall back to a plain RTLD_LAZY dlopen.  In practice
 *	     the library is already loaded so the second dlopen
 *	     just adds a reference to the existing image.
 *
 *	The caller MUST NOT call dlclose() on the returned handle.
 *	Two reasons: (a) unloading Tcl mid-process is essentially
 *	never what you want, and (b) on the RTLD_NOLOAD path the
 *	returned handle may not carry an additional reference of
 *	its own -- a dlclose would over-release.
 *
 * Results:
 *	A non-NULL handle to the Tcl shared library on success;
 *	NULL on any failure (dladdr fails, dli_fname is NULL, or
 *	dlopen returns NULL).
 *
 * Side effects:
 *	May add a reference to the Tcl shared library on platforms
 *	without RTLD_NOLOAD.  Caller MUST NOT release that
 *	reference (see above).
 *
 *----------------------------------------------------------------------
 */

HMODULE get_tcl_module_handle(void)
{
    Dl_info info;

    memset(&info, 0, sizeof(Dl_info));

#if defined(USE_TCL_STUBS)
    /*
     * HACK: For some reason, Clang on macOS wants to be difficult
     *       here.  It cannot deduce that "Tcl_CreateInterp" is a
     *       macro defined via the Tcl stubs mechanism; therefore,
     *       force the issue.
     */
    if (dladdr((void *)tclStubsPtr->tcl_CreateInterp, &info) == 0)
        return NULL;
#else
    if (dladdr((void *)Tcl_CreateInterp, &info) == 0)
        return NULL;
#endif

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
 *	Return a synthetic "module handle" for the Garuda package
 *	itself, suitable for use with get_module_file_name() to
 *	discover our own shared library's path on disk.
 *
 * Why / How:
 *	On Windows, an extension knows its own HMODULE because the
 *	loader passes it to DllMain.  POSIX has no equivalent: a
 *	shared library is not told its own handle at load time.
 *	We work around this by handing back the address of
 *	Garuda_Init -- a function we know lives inside our own
 *	package -- cast to HMODULE.  Subsequent calls into
 *	get_module_file_name() can dladdr() this pointer to
 *	resolve the file name of OUR shared library.
 *
 *	The returned value is not a real OS handle; it is an
 *	address that just happens to satisfy the dladdr-based
 *	queries this PAL uses.  This idiom is portable to any
 *	POSIX with dladdr (Linux, macOS, FreeBSD, OpenBSD,
 *	Solaris, etc.).
 *
 * Results:
 *	The address of Garuda_Init reinterpreted as HMODULE.
 *	Always non-NULL.
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
 *	No-op on POSIX; included for API symmetry with the Win32
 *	side of this package, where SetPackageModule stores the
 *	HMODULE that DllMain received from the loader for later
 *	use by GetPackageModule.
 *
 * Why / How:
 *	POSIX has no "loader hands the library its own handle"
 *	event analogous to Windows' DllMain DLL_PROCESS_ATTACH,
 *	so there is nothing to store.  The function is declared
 *	in GarudaDecls.h and called from cross-platform code
 *	paths that don't know which OS they're running on; the
 *	no-op behavior here keeps those call sites portable
 *	without #ifdef'ing them.
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
 *	Acquire a recursive mutex.  Garuda needs recursive
 *	semantics because nested calls into the CLR can re-enter
 *	the same lock from the same thread; Tcl_Mutex itself is
 *	non-recursive (a second lock by the same thread would
 *	deadlock).  This routine builds the recursive layer on
 *	top of two non-recursive Tcl_Mutex objects.
 *
 * Why / How:
 *	State per recursive mutex is a (Tcl_Mutex, owner record)
 *	pair, where the owner record (pthread_owner_t) carries
 *	the current owner thread id, the recursion depth, and its
 *	OWN Tcl_Mutex used to protect reads and writes of the
 *	owner fields atomically.  The protocol:
 *
 *	  1. Lock the owner record's inner mutex.
 *	  2. If the owner record names the current thread, bump
 *	     the recursion depth and unlock the inner mutex.  We
 *	     are already the owner, so no need to touch the user
 *	     mutex at all.
 *	  3. Otherwise, unlock the inner mutex, acquire the USER
 *	     mutex (this BLOCKS if another thread holds it), then
 *	     re-acquire the inner mutex, set the owner field to
 *	     ourselves, bump the depth from zero to one, and
 *	     unlock the inner mutex.
 *
 *	The reason for releasing the inner mutex BEFORE blocking
 *	on the user mutex is the obvious one: holding two locks
 *	while blocking on either invites deadlock.  The reason
 *	for re-acquiring the inner mutex AFTER the user mutex is
 *	that the owner record write must happen while we hold
 *	the user mutex (so the record is consistent with the
 *	mutex's locked state) AND while we hold the inner mutex
 *	(so the record write is atomic with respect to concurrent
 *	inspectors).
 *
 *	The asserts encode the load-bearing invariants:
 *	  - if the owner field is non-null, depth > 0
 *	  - if the owner field is null, depth == 0
 *	An assert violation is a sign that an unrelated code path
 *	corrupted the owner record outside this routine.
 *
 *	Self-initialization: the underlying Tcl_Mutex objects use
 *	Tcl's standard "first lock initializes" pattern, so no
 *	explicit init step is required.  Tcl_Finalize() reaps
 *	both mutex objects at process shutdown.
 *
 * Results:
 *	1 on success; 0 if either pointer argument was NULL.
 *
 * Side effects:
 *	Either bumps the recursion depth on the calling thread --
 *	no actual lock acquisition -- or BLOCKS until the user
 *	mutex can be acquired and then sets the calling thread
 *	as owner with depth 1.
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
 *	Release one level of a recursive mutex acquired via
 *	Pal_MutexLock.  Each successful Pal_MutexLock must be
 *	paired with exactly one Pal_MutexUnlock; only the call
 *	that decrements depth to zero actually releases the
 *	underlying Tcl_Mutex.
 *
 * Why / How:
 *	Inverse of Pal_MutexLock.  Acquire the owner record's
 *	inner mutex, validate that the calling thread is the
 *	current owner, decrement the recursion depth.  If the
 *	depth reached zero, clear the owner field and release
 *	the user mutex (after releasing the inner mutex, same
 *	anti-deadlock discipline as Pal_MutexLock).
 *
 *	Two failure paths are flagged BUGBUG because the API
 *	does not specify behavior for them; this implementation
 *	treats both as no-ops returning zero, which is the
 *	safest choice but is a deliberate decision rather than a
 *	derivation:
 *
 *	  - The mutex has no owner (depth == 0, owner field is
 *	    null) at the time of the call.  This means the caller
 *	    is unbalanced -- more unlocks than locks -- and there
 *	    is nothing meaningful to do.  Refusing prevents a
 *	    spurious release on whichever thread next acquires.
 *	  - The caller is not the current owner.  Honoring an
 *	    unlock from a non-owner would corrupt the owner
 *	    record AND release a Tcl_Mutex that the caller did
 *	    not acquire, so we refuse.
 *
 *	The asserts encode the same invariants as Pal_MutexLock
 *	(owner is non-null iff depth > 0).
 *
 * Results:
 *	1 on a successful balanced unlock (depth decremented;
 *	user mutex released if depth reached zero).  0 if either
 *	pointer argument was NULL, the mutex had no owner, or the
 *	calling thread is not the current owner.
 *
 * Side effects:
 *	On a successful unlock that takes depth to zero, releases
 *	the user mutex (other threads may now acquire it).  On a
 *	successful unlock that leaves depth > 0, the calling
 *	thread still owns the mutex.  On any failure, all state
 *	is unchanged.
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
