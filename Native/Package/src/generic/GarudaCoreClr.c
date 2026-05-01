/*
 * GarudaCoreClr.c -- Eagle Package for Tcl (Garuda)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#include "GarudaPre.h"		/* NOTE: For private header setup. */

#if defined(USE_CORE_CLR)
#include <stdio.h>		/* NOTE: For fprintf, swprintf, va_list, etc. */
#include <string.h>		/* NOTE: For memset, wcslen, wcsncpy, etc. */

#if !defined(_WIN32)
#  include <stdlib.h>		/* NOTE: For setenv, unsetenv, etc. */
#  include <limits.h>		/* NOTE: For INT_MAX, etc. */
#  include <wchar.h>		/* NOTE: For wchar_t, etc. */
#  include <stdatomic.h>	/* NOTE: For atomic_fetch_add, etc. */
#endif

#if defined(_WIN32)
#  include <windows.h>		/* NOTE: For LoadLibraryW, etc. */
#else
#  include <errno.h>		/* NOTE: For errno, etc. */
#  include <pthread.h>		/* NOTE: For pthread_self, etc. */
#  include <dlfcn.h>		/* NOTE: For dlopen, dladdr, Dl_info, etc. */
#endif

#include <nethost.h>		/* NOTE: For get_hostfxr_path, etc. */
#include <hostfxr.h>		/* NOTE: For "hostfxr_*" .NET (Core), etc. */
#include <coreclr_delegates.h>	/* NOTE: For load_<asm>_and_get_<fn_ptr>. */

#include "tcl.h"		/* NOTE: For public Tcl API. */
#include "GarudaPal.h"		/* NOTE: For platform abstraction API. */
#include "pkgVersion.h"		/* NOTE: Package version information. */
#include "GarudaInt.h"		/* NOTE: For private package API. */
#include "GarudaCoreClr.h"	/* NOTE: For private package CoreCLR API. */
#include "GarudaDecls.h"	/* NOTE: For private package declarations. */
#include "ConvertUTF_v2.h"	/* NOTE: Unicode UTF-* reference conversions. */
#include "GarudaStr.h"		/* NOTE: For private string API. */

/*
 * NOTE: Private functions defined in this file.
 */

static void HOSTFXR_CALLTYPE GetCoreClrVersionCallback(
			const struct hostfxr_dotnet_environment_info *info,
			void *context);

/*
 * NOTE: This is the shared library module handle for the CoreCLR.  This
 *       variable will be non-NULL if the CoreCLR has been loaded into the
 *       current process by this package.
 */

static volatile HMODULE pCoreClrModule = NULL;

/*
 * NOTE: These are the function pointers to the CoreCLR APIs necessary to
 *       start, use, and stop the CoreCLR.
 */

volatile CoreClrFunctions uCoreClrFunctions = { 0 };

/*
 * NOTE: This variable will be non-NULL after the CoreCLR has been started
 *       for the current process by this package.
 */

static volatile hostfxr_handle pCoreClrContext = NULL;

/*
 * NOTE: This variable will be TRUE if the CoreCLR bridge was successfully
 *       started and has not been subsequently shutdown.
 */

static volatile BOOL bCoreClrBridgeStarted = FALSE;

/*
 *----------------------------------------------------------------------
 *
 * GetCoreClrWasLoaded --
 *
 *	Test whether THIS package has loaded the CoreCLR shared
 *	library (hostfxr) into the current process.
 *
 * Why / How:
 *	"Loaded" here is a strict claim about the loader-level
 *	mapping done by THIS package, not a global "is some copy
 *	of the CoreCLR present in the process" check.  The
 *	pCoreClrModule global is set by LoadAndStartTheCoreClr
 *	when its dlopen / LoadLibraryW call succeeds, and cleared
 *	by StopAndReleaseTheCoreClr after the matching
 *	dlclose / FreeLibrary call.  Concurrent package callers
 *	(of which there should be at most one in practice, but
 *	the field is package-mutex protected anyway) all see a
 *	consistent value.
 *
 *	The package mutex is held only across the read of the
 *	module pointer.  Callers that need to use the resulting
 *	BOOL must understand that another thread could change
 *	the state immediately after this returns; this is
 *	therefore a diagnostic / fast-path predicate, not a
 *	synchronization primitive.
 *
 * Results:
 *	TRUE if pCoreClrModule is non-NULL at the moment of the
 *	read; FALSE otherwise.
 *
 * Side effects:
 *	None.  Briefly acquires and releases the package mutex.
 *
 *----------------------------------------------------------------------
 */

BOOL GetCoreClrWasLoaded(void)
{
    BOOL bResult;

    Wrp_MutexLock(&packageMutex);
    bResult = (pCoreClrModule != NULL);
    Wrp_MutexUnlock(&packageMutex);

    return bResult;
}

/*
 *----------------------------------------------------------------------
 *
 * GetCoreClrWasStarted --
 *
 *	Test whether the CoreCLR has been initialized AND a
 *	hostfxr context is held by this package.
 *
 * Why / How:
 *	"Started" is a stronger claim than "loaded": LoadAndStart
 *	can be invoked with bStart=FALSE to dlopen hostfxr without
 *	calling hostfxr_initialize_for_runtime_config.  Only the
 *	successful initialize step produces a hostfxr_handle, and
 *	only that handle gives this package the right to call
 *	hostfxr_get_runtime_delegate / hostfxr_close.
 *
 *	pCoreClrContext is the cached hostfxr_handle returned by
 *	hostfxr_initialize_for_runtime_config.  It is set in
 *	LoadAndStartTheCoreClr after a successful initialize, and
 *	cleared in StopAndReleaseTheCoreClr after hostfxr_close.
 *
 *	Like GetCoreClrWasLoaded, this is a snapshot under the
 *	package mutex; the value can change immediately after
 *	the function returns.  Treat as a diagnostic, not a
 *	synchronization primitive.
 *
 * Results:
 *	TRUE if pCoreClrContext is non-NULL at the moment of the
 *	read; FALSE otherwise.
 *
 * Side effects:
 *	None.  Briefly acquires and releases the package mutex.
 *
 *----------------------------------------------------------------------
 */

BOOL GetCoreClrWasStarted(void)
{
    BOOL bResult;

    Wrp_MutexLock(&packageMutex);
    bResult = (pCoreClrContext != NULL);
    Wrp_MutexUnlock(&packageMutex);

    return bResult;
}

/*
 *----------------------------------------------------------------------
 *
 * GetCoreClrBridgeStarted --
 *
 *	Test whether the Eagle-side managed bridge that hosts
 *	Garuda's Tcl / CLR interop has been brought up inside the
 *	CoreCLR.
 *
 * Why / How:
 *	The "bridge" is the managed-side counterpart to the
 *	native-side hostfxr context.  A successful startup
 *	sequence is:
 *
 *	  1. hostfxr_initialize_for_runtime_config -- produces
 *	     pCoreClrContext (tracked by GetCoreClrWasStarted).
 *	  2. hostfxr_get_runtime_delegate to obtain the function
 *	     pointer for the managed bridge entry point.
 *	  3. Call into that delegate, which constructs the Eagle
 *	     bridge object inside the CLR and signals success
 *	     back via SetCoreClrBridgeStarted.
 *
 *	bCoreClrBridgeStarted therefore reflects whether step 3
 *	completed.  It is the most authoritative "is the bridge
 *	usable?" predicate in this file: a TRUE here implies all
 *	prior steps completed AND the managed side acknowledged
 *	the handshake.
 *
 *	Same package-mutex snapshot semantics as the other
 *	predicates in this group.
 *
 * Results:
 *	TRUE if the bridge handshake has completed and not been
 *	subsequently torn down; FALSE otherwise.
 *
 * Side effects:
 *	None.  Briefly acquires and releases the package mutex.
 *
 *----------------------------------------------------------------------
 */

BOOL GetCoreClrBridgeStarted(void)
{
    BOOL bResult;

    Wrp_MutexLock(&packageMutex);
    bResult = bCoreClrBridgeStarted;
    Wrp_MutexUnlock(&packageMutex);

    return bResult;
}

/*
 *----------------------------------------------------------------------
 *
 * SetCoreClrBridgeStarted --
 *
 *	Record whether the managed-side bridge has been started.
 *
 * Why / How:
 *	Called by the managed bridge entry point itself, via the
 *	delegate obtained from hostfxr_get_runtime_delegate, to
 *	signal completion of the handshake described in the
 *	GetCoreClrBridgeStarted comment.  Also called by the
 *	teardown path with bStarted=FALSE before the matching
 *	hostfxr_close.
 *
 *	The package-mutex acquisition is what makes this safe to
 *	call from any thread the CLR happens to schedule on; the
 *	managed-side caller has no idea which Tcl thread context
 *	it's running in, so the predicate / setter pair must be
 *	internally synchronized.
 *
 * Results:
 *	None.
 *
 * Side effects:
 *	Updates bCoreClrBridgeStarted under the package mutex.
 *	Future GetCoreClrBridgeStarted calls observe the new
 *	value.
 *
 *----------------------------------------------------------------------
 */

void SetCoreClrBridgeStarted(
    BOOL bStarted)	    /* Non-zero if the bridge was started. */
{
    Wrp_MutexLock(&packageMutex);
    bCoreClrBridgeStarted = bStarted;
    Wrp_MutexUnlock(&packageMutex);
}

/*
 *----------------------------------------------------------------------
 *
 * LoadAndStartTheCoreClr --
 *
 *	The package's main .NET-hosting entry point.  Discovers,
 *	loads, and (optionally) initializes hostfxr; then resolves
 *	the small set of hostfxr API function pointers that this
 *	package needs at runtime.
 *
 * Why / How:
 *	This is the routine that does the heavy lifting of hosting
 *	.NET 5+ as a guest inside Tcl's process.  The host /
 *	hostfxr / hostpolicy / coreclr layer cake assumes that the
 *	OUTER caller is a managed application or the dotnet CLI;
 *	we are neither -- we're a Tcl extension.  Working with the
 *	grain of that API requires the steps below, in order:
 *
 *	  1. nethost::get_hostfxr_path discovers the
 *	     installed hostfxr library on disk (consulting
 *	     /etc/dotnet/install_location on Linux,
 *	     DOTNET_ROOT and registry entries on Windows, and
 *	     the per-arch install-location convention on macOS).
 *	     Returns an OS-native path string.
 *
 *	  2. LoadLibraryW (Win32) / dlopen (POSIX) maps the
 *	     hostfxr shared library into the process.
 *
 *	  3. GetProcAddress / dlsym resolves the four (or five,
 *	     when HAVE_DOTNET_ENVIRONMENT_INFO is set) hostfxr
 *	     entry points we use:
 *	       - hostfxr_get_dotnet_environment_info  (optional)
 *	       - hostfxr_initialize_for_runtime_config
 *	       - hostfxr_get_runtime_delegate
 *	       - hostfxr_close
 *
 *	  4. (only if bStart) hostfxr_initialize_for_runtime_config
 *	     reads the runtimeconfig.json file at runtimeConfigPath
 *	     and sets up an in-process CoreCLR according to it.  On
 *	     success, returns a hostfxr_handle that the caller must
 *	     eventually pass to hostfxr_close.  This is the call
 *	     that brings the managed runtime up; from this point
 *	     the CLR has been initialized AND IT CANNOT BE
 *	     UNLOADED FROM THIS PROCESS.  .NET 5+ does not support
 *	     CLR unload (no AppDomains, no in-process restart).
 *
 *	The bLoad / bStart split exists so callers can pre-stage
 *	hostfxr without committing to a CLR initialization, which
 *	is useful for diagnostic commands that want to query the
 *	installed runtimes (via hostfxr_get_dotnet_environment_info)
 *	without paying the CLR-init cost.  bUseMinimumClr forces
 *	use of the lowest CLR version this package was built
 *	against, even when newer is installed -- used for testing
 *	and ABI-floor verification.  bStrict converts the
 *	"already loaded / already started" cases into errors
 *	rather than treating the call as idempotent.
 *
 *	The Win32 and POSIX library-load and symbol-resolution
 *	blocks are deliberately mirror images of each other: same
 *	field-load order, same indent, same field names.  Diffing
 *	the two halves should produce only the calls that actually
 *	differ between platforms (LoadLibraryW vs dlopen,
 *	GetProcAddress vs dlsym).  This makes auditing the platform
 *	parity trivial.
 *
 *	The package mutex is held for the entire body.  This is
 *	intentional: hostfxr's loader and initialize calls are not
 *	required by Microsoft to be reentrant from a single
 *	process, and the global state we maintain
 *	(pCoreClrModule, pCoreClrContext, uCoreClrFunctions) must
 *	be assigned to atomically with respect to all other
 *	package operations.  The blocking is acceptable because
 *	this routine is called once per package lifetime in
 *	practice.
 *
 *	On any failure inside the routine, all state is unwound
 *	(via goto done):  any partially-loaded module is released,
 *	any partially-initialized hostfxr context is closed, and
 *	uCoreClrFunctions is left in its pre-call zero state.  The
 *	package globals reflect the post-failure state, which is
 *	the same as the pre-call state from the caller's
 *	perspective.
 *
 * Results:
 *	TCL_OK on success.  TCL_ERROR with an error message
 *	appended to the interp result on failure (interp may be
 *	NULL, in which case the message is suppressed but the
 *	return code is still TCL_ERROR).
 *
 * Side effects:
 *	On success: pCoreClrModule, pCoreClrContext, and
 *	uCoreClrFunctions are populated.  The CoreCLR has been
 *	initialized inside the process and CANNOT be uninitialized
 *	for the rest of the process lifetime.  Managed code may
 *	have run as part of CLR startup (static constructors of
 *	the System.Private.CoreLib types, etc.), which can have
 *	arbitrary observable side-effects (file I/O, env var
 *	access, JIT compilation work).
 *
 *----------------------------------------------------------------------
 */

int LoadAndStartTheCoreClr(
    Tcl_Interp *interp,		/* Current Tcl interpreter. */
    LPCWSTR logCommand,		/* The Tcl command used to log the
				 * CoreCLR method execution, if any. */
    LPCWSTR runtimeConfigPath,	/* The CoreCLR runtime configuration
				 * file path, if any. */
    BOOL bLoad,			/* Load the CoreCLR if necessary? */
    BOOL bUseMinimumClr,	/* Force using minimum supported CoreCLR
				 * version? */
    BOOL bStart,		/* Start the CoreCLR after loading it? */
    BOOL bStrict)		/* Fail if already loaded and/or started? */
{
    int code = TCL_OK;
    HMODULE hModule = NULL;
    CoreClrFunctions uFunctions;
    hostfxr_handle pContext = NULL;
    WCHAR buffer[PACKAGE_RESULT_SIZE + 1] = { 0 };
    int rc;

    Wrp_MutexLock(&packageMutex);

    memset(&uFunctions, 0, sizeof(CoreClrFunctions));
    uFunctions.sizeOf = sizeof(CoreClrFunctions);

    if (bLoad) {
	if (pCoreClrModule == NULL) {
	    char_t runtimeLibraryFileName[PATH_MAX + 1];
	    size_t runtimeLibraryNameSize = PATH_MAX;

	    memset(runtimeLibraryFileName, 0,
		(runtimeLibraryNameSize + 1) * sizeof(char_t));

	    rc = get_hostfxr_path(
		runtimeLibraryFileName, &runtimeLibraryNameSize, NULL);

	    if (rc != 0) {
		if (interp != NULL) {
		    Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"get_hostfxr_path",
			    HRESULT_FROM_WIN32(rc)), -1);
		}

		code = TCL_ERROR;
		goto done;
	    }

#if defined(_WIN32)
	    hModule = LoadLibraryW(runtimeLibraryFileName);
#else
	    hModule = dlopen(runtimeLibraryFileName, RTLD_LAZY | RTLD_LOCAL);
#endif

	    if (hModule == NULL) {
		if (interp != NULL) {
#if defined(_WIN32)
		    Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"LoadLibraryW",
			    HRESULT_FROM_WIN32(GetLastError())), -1);
#else
		    Tcl_AppendResult(interp, dlerror(), NULL);
#endif
		}

		code = TCL_ERROR;
		goto done;
	    }

#if defined(_WIN32)
#if defined(HAVE_DOTNET_ENVIRONMENT_INFO)
	    uFunctions.pGetDotNetEnvInfo =
		(hostfxr_get_dotnet_environment_info_fn)GetProcAddress(
		hModule, "hostfxr_get_dotnet_environment_info");
#endif

	    uFunctions.pInitForRuntimeConfig =
		(hostfxr_initialize_for_runtime_config_fn)GetProcAddress(
		hModule, "hostfxr_initialize_for_runtime_config");

	    uFunctions.pGetRuntimeDelegate =
		(hostfxr_get_runtime_delegate_fn)GetProcAddress(
		hModule, "hostfxr_get_runtime_delegate");

	    uFunctions.pClose = (hostfxr_close_fn)GetProcAddress(
		hModule, "hostfxr_close");
#else
#if defined(HAVE_DOTNET_ENVIRONMENT_INFO)
	    uFunctions.pGetDotNetEnvInfo =
		(hostfxr_get_dotnet_environment_info_fn)dlsym(
		hModule, "hostfxr_get_dotnet_environment_info");
#endif

	    uFunctions.pInitForRuntimeConfig =
		(hostfxr_initialize_for_runtime_config_fn)dlsym(
		hModule, "hostfxr_initialize_for_runtime_config");

	    uFunctions.pGetRuntimeDelegate =
		(hostfxr_get_runtime_delegate_fn)dlsym(
		hModule, "hostfxr_get_runtime_delegate");

	    uFunctions.pClose = (hostfxr_close_fn)dlsym(
		hModule, "hostfxr_close");
#endif

	    /*
	     * HACK: On Linux, the "Cvt" string conversion APIs are needed
	     *       to call into the CoreCLR; so, those function pointers
	     *       must be globally visible now.
	     */

	    uCoreClrFunctions.pInitForRuntimeConfig =
		uFunctions.pInitForRuntimeConfig;
	} else if (bStrict) {
	    if (interp != NULL) {
		Tcl_AppendResult(interp, "CoreCLR already loaded\n", NULL);
	    }

	    code = TCL_ERROR;
	    goto done;
	}
    }

    if (bStart) {
	BOOL bRetry = FALSE;

	if ((hModule == NULL) && (pCoreClrModule == NULL)) {
	    if (interp != NULL) {
		Tcl_AppendResult(interp,
		    "invalid CoreCLR module handle\n", NULL);
	    }

	    code = TCL_ERROR;
	    goto done;
	}

retry:

	if ((uFunctions.pInitForRuntimeConfig == NULL) ||
		(uFunctions.pGetRuntimeDelegate == NULL) ||
		(uFunctions.pClose == NULL)) {
	    if (bRetry) {
		if (interp != NULL) {
		    Tcl_AppendResult(interp,
			"invalid CoreCLR function pointers\n", NULL);
		}

		code = TCL_ERROR;
		goto done;
	    } else {
		memcpy((void *)&uFunctions, (void *)&uCoreClrFunctions,
		    sizeof(CoreClrFunctions));

		bRetry = TRUE;
		goto retry;
	    }
	}

	if ((pContext == NULL) && (pCoreClrContext == NULL)) {
	    if (runtimeConfigPath != NULL) {
		rc = Wrp_pInitForRuntimeConfig(
		    runtimeConfigPath, NULL, &pContext);
	    } else {
#if defined(_WIN32)
		WCHAR runtimeConfigFileName[PATH_MAX + 1];
		size_t runtimeConfigNameSize = PATH_MAX;

		memset(runtimeConfigFileName, 0,
		    (runtimeConfigNameSize + 1) * sizeof(WCHAR));

		runtimeConfigNameSize = GetModuleFileNameW(NULL,
		    runtimeConfigFileName, (DWORD)runtimeConfigNameSize);

		if (runtimeConfigNameSize == 0) {
		    if (interp != NULL) {
			Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			    GetClrErrorMessage(L"GetModuleFileNameW",
				HRESULT_FROM_WIN32(GetLastError())), -1);
		    }

		    code = TCL_ERROR;
		    goto done;
		}

		wcsncat(runtimeConfigFileName, UNICODE_RUNTIMECONFIG_SUFFIX,
		    PATH_MAX - 1);
#else
		HRESULT hResult;
		char runtimeConfigFileName[PATH_MAX + 1];
		size_t runtimeConfigNameSize = PATH_MAX;

		memset(runtimeConfigFileName, 0,
		    (runtimeConfigNameSize + 1) * sizeof(char));

		hResult = build_runtimeconfig_file_name(
		    runtimeConfigFileName, runtimeConfigNameSize);

		if (FAILED(hResult)) {
		    if (interp != NULL) {
			Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			    GetClrErrorMessage(
				L"build_runtimeconfig_file_name",
				hResult), -1);
		    }

		    code = TCL_ERROR;
		    goto done;
		}
#endif

		rc = uFunctions.pInitForRuntimeConfig(
		    runtimeConfigFileName, NULL, &pContext);
	    }

	    if (((rc != Success) &&
		    (rc != Success_HostAlreadyInitialized) &&
		    (rc != Success_DifferentRuntimeProperties)) ||
		    (pContext == NULL)) {
		if (interp != NULL) {
		    Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"pInitForRuntimeConfig",
			    HRESULT_FROM_WIN32(rc)), -1);
		}

		code = TCL_ERROR;
		goto done;
	    }
	}

	if (uFunctions.pLoadAssemblyAndGetFuncPtr == NULL) {
	    rc = uFunctions.pGetRuntimeDelegate(
		pContext, hdt_load_assembly_and_get_function_pointer,
		(void **)&uFunctions.pLoadAssemblyAndGetFuncPtr);

	    if ((rc != 0) ||
		    (uFunctions.pLoadAssemblyAndGetFuncPtr == NULL)) {
		if (interp != NULL) {
		    Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"pLoadAssemblyAndGetFuncPtr",
			    HRESULT_FROM_WIN32(rc)), -1);
		}

		code = TCL_ERROR;
		goto done;
	    }

	    /*
	     * HACK: On Linux, the "Cvt" string conversion APIs are needed
	     *       to call into the CoreCLR; so, those function pointers
	     *       must be globally visible now.
	     */

	    uCoreClrFunctions.pLoadAssemblyAndGetFuncPtr =
		uFunctions.pLoadAssemblyAndGetFuncPtr;
	} else if (bStrict) {
	    if (interp != NULL) {
		Tcl_AppendResult(interp, "CoreCLR already started\n", NULL);
	    }

	    code = TCL_ERROR;
	    goto done;
	}
    }

done:

    if (code == TCL_OK) {
	if (pContext != NULL)
	    pCoreClrContext = pContext;

	if ((uFunctions.pInitForRuntimeConfig != NULL) &&
	    (uFunctions.pGetRuntimeDelegate != NULL) &&
	    (uFunctions.pClose != NULL)) {
	    memcpy((void*)&uCoreClrFunctions, &uFunctions,
		sizeof(CoreClrFunctions));
	}

	if (hModule != NULL)
	    pCoreClrModule = hModule;
    } else {
	if ((uFunctions.pClose != NULL) && (pContext != NULL)) {
	    rc = uFunctions.pClose(pContext);

	    if (PACKAGE_CAN_LOG(interp, logCommand)) {
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"pClose(rc = {%d})", rc);

		TclLog(interp, logCommand, buffer, NULL);
	    }

	    pContext = NULL;
	}

	if (hModule != NULL) {
	    BOOL bResult;

#if defined(_WIN32)
	    bResult = FreeLibrary(hModule);
#else
	    bResult = (dlclose(hModule) == 0);
#endif

	    hModule = NULL;

	    if (PACKAGE_CAN_LOG(interp, logCommand)) {
#if defined(_WIN32)
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"FreeLibrary(bResult = {%d}, lastError = {%lu})",
		    bResult, GetLastError());
#else
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"dlclose(bResult = {%d}, lastError = {"
		    PACKAGE_UNICODE_CSTR_FMT L"})", bResult,
		    dlerror());
#endif

		TclLog(interp, logCommand, buffer, NULL);
	    }
	}
    }

    Wrp_MutexUnlock(&packageMutex);
    return code;
}

/*
 *----------------------------------------------------------------------
 *
 * StopAndReleaseTheCoreClr --
 *
 *	The teardown counterpart to LoadAndStartTheCoreClr.
 *	Closes the hostfxr context (if any), unloads the hostfxr
 *	shared library (if any), and clears the package's cached
 *	function pointers.
 *
 * Why / How:
 *	This is the documented "release" path, but its real-world
 *	contract is unusual and worth understanding precisely:
 *
 *	  - hostfxr_close(pCoreClrContext) releases the *handle*
 *	    we hold against the runtime, but it does NOT unload
 *	    the CoreCLR from the process.  Once .NET 5+ has been
 *	    initialized inside a process, that initialization is
 *	    permanent for the rest of the process lifetime.  No
 *	    AppDomains, no in-process restart.  This is a
 *	    deliberate Microsoft design choice (the rationale is
 *	    documented in the .NET 5 hosting design notes), and
 *	    we work within it.
 *
 *	  - dlclose / FreeLibrary on hostfxr only releases OUR
 *	    reference to the hostfxr shared library; the CLR
 *	    itself, having been initialized, holds its own
 *	    reference.  So this call typically does NOT result in
 *	    hostfxr being unmapped from the address space.
 *
 *	  - On a partial-load (bLoad succeeded, bStart was FALSE)
 *	    there is no context to close; we still need to
 *	    release the loader reference and clear the cached
 *	    function pointers.  The routine handles both shapes
 *	    based on which globals are non-NULL.
 *
 *	The bridge teardown is layered on top: if the managed
 *	bridge is started (bCoreClrBridgeStarted), the caller is
 *	responsible for tearing IT down before calling here.
 *	This function does not call into managed code; it only
 *	releases native resources.  bStrict converts "nothing to
 *	stop" into an error rather than treating the call as
 *	idempotent.
 *
 *	The package mutex is held for the whole body, same
 *	rationale as LoadAndStartTheCoreClr: hostfxr_close is
 *	not documented to be reentrant, and the global-state
 *	mutations need to be atomic w.r.t. other package
 *	operations.
 *
 * Results:
 *	TCL_OK on success.  TCL_ERROR with an error message
 *	appended to the interp result if hostfxr_close fails or
 *	bStrict is set and there was nothing to release.
 *
 * Side effects:
 *	On success the package globals (pCoreClrModule,
 *	pCoreClrContext, uCoreClrFunctions) are cleared back to
 *	their pre-LoadAndStart state, but the CoreCLR runtime
 *	itself remains initialized inside the process and may
 *	continue to execute background work (finalizer thread,
 *	timer threads, etc.).  Managed cleanup code MAY run as
 *	part of hostfxr_close, with arbitrary observable
 *	side-effects.
 *
 *----------------------------------------------------------------------
 */

int StopAndReleaseTheCoreClr(
    Tcl_Interp *interp,	    /* Current Tcl interpreter. */
    LPCWSTR logCommand,	    /* The Tcl command used to log the CLR method
			     * execution, if any. */
    BOOL bRelease,	    /* Release the CLR after stopping it? */
    BOOL bStrict)	    /* Fail if already stopped and/or released? */
{
    int code = TCL_OK;
    BOOL bResult;
    WCHAR buffer[PACKAGE_RESULT_SIZE + 1] = { 0 };

    Wrp_MutexLock(&packageMutex);

    if (pCoreClrModule != NULL) {
	/*
	 * NOTE: If we were previously able to start the CLR, stop it now.
	 */

	if ((uCoreClrFunctions.pClose != NULL) && (pCoreClrContext != NULL)) {
	    HRESULT hResult = S_OK;

#if defined(_WIN32)
	    bResult = SetEnvironmentVariableW(
		UNICODE_CLR_STOPPING_ENVVAR_NAME, L"1");

	    if (!bResult) {
		if (interp != NULL) {
		    Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"SetEnvironmentVariableW",
			    HRESULT_FROM_WIN32(GetLastError())), -1);
		}

		code = TCL_ERROR;
		goto done;
	    }
#else
	    bResult = setenv(CLR_STOPPING_ENVVAR_NAME, "1", 1);
	    bResult = (bResult == 0) ? TRUE : FALSE;

	    if (!bResult) {
		if (interp != NULL) {
		    Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"setenv",
			    HRESULT_FROM_ERRNO(errno)), -1);
		}

		code = TCL_ERROR;
		goto done;
	    }
#endif

	    hResult = uCoreClrFunctions.pClose(pCoreClrContext);

#if defined(_WIN32)
	    bResult = SetEnvironmentVariableW(
		UNICODE_CLR_STOPPING_ENVVAR_NAME, NULL);

	    if (!bResult) {
		if (interp != NULL) {
		    Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"UnsetEnvironmentVariableW",
			    HRESULT_FROM_WIN32(GetLastError())), -1);
		}

		code = TCL_ERROR;
		goto done;
	    }
#else
	    bResult = unsetenv(CLR_STOPPING_ENVVAR_NAME);
	    bResult = (bResult == 0) ? TRUE : FALSE;

	    if (!bResult) {
		if (interp != NULL) {
		    Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"unsetenv",
			    HRESULT_FROM_ERRNO(errno)), -1);
		}

		code = TCL_ERROR;
		goto done;
	    }
#endif

	    if (PACKAGE_CAN_LOG(interp, logCommand)) {
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"ICLRRuntimeHost_Stop(hResult = {0x%lX})",
		    (unsigned long)hResult);

		TclLog(interp, logCommand, buffer, NULL);
	    }

	    if (SUCCEEDED(hResult)) {
		pCoreClrContext = NULL;
	    } else {
		if (interp != NULL) {
		    Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"ICLRRuntimeHost_Stop", hResult),
			-1);
		}

		code = TCL_ERROR;
		goto done;
	    }
	} else if (bStrict) {
	    if (interp != NULL) {
		Tcl_AppendResult(interp, "CoreCLR not started\n", NULL);
	    }

	    code = TCL_ERROR;
	    goto done;
	}

	/*
	 * NOTE: Should we also release the DLL reference to the CoreCLR
	 *       runtime host?
	 */

	if (bRelease) {
#if defined(_WIN32)
	    bResult = FreeLibrary(pCoreClrModule);
#else
	    bResult = (dlclose(pCoreClrModule) == 0);
#endif

	    pCoreClrModule = NULL;

	    if (PACKAGE_CAN_LOG(interp, logCommand)) {
#if defined(_WIN32)
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"FreeLibrary(bResult = {%d}, lastError = {%lu})",
		    bResult, GetLastError());
#else
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"dlclose(bResult = {%d}, lastError = {"
		    PACKAGE_UNICODE_CSTR_FMT L"})", bResult,
		    dlerror());
#endif

		TclLog(interp, logCommand, buffer, NULL);
	    }
	}
    } else if (bStrict) {
	if (interp != NULL) {
	    Tcl_AppendResult(interp, "CoreCLR not loaded\n", NULL);
	}

	code = TCL_ERROR;
	goto done;
    }

done:

    /*
     * BUGFIX: If the CLR has been stopped, then the bridge cannot be
     *         running either.
     */

    if ((code == TCL_OK) && bCoreClrBridgeStarted) {
	bCoreClrBridgeStarted = FALSE;

	if (PACKAGE_CAN_LOG(interp, logCommand)) {
	    TclLog(interp, logCommand,
		L"WARNING: CoreCLR was stopped with bridge running.", NULL);
	}
    }

    Wrp_MutexUnlock(&packageMutex);
    return code;
}

/*
 *----------------------------------------------------------------------
 *
 * CanExecuteCoreClrCode --
 *
 *	Test whether all preconditions for invoking managed code
 *	via this package are currently satisfied.  This is the
 *	guard predicate that ExecuteCoreClrMethod consults before
 *	doing anything irrevocable.
 *
 * Why / How:
 *	The conjunction of conditions checked here is the
 *	"executable" state of the package:
 *
 *	  - The CoreCLR shared library has been loaded
 *	    (pCoreClrModule != NULL).
 *	  - The hostfxr context has been initialized
 *	    (pCoreClrContext != NULL) -- required before any
 *	    delegate can be obtained from
 *	    hostfxr_get_runtime_delegate.
 *	  - The required hostfxr function pointers have been
 *	    resolved (pGetRuntimeDelegate, pClose).  Both should
 *	    be non-NULL after a successful LoadAndStartTheCoreClr,
 *	    but we verify rather than assume.
 *	  - The managed bridge has signalled readiness
 *	    (bCoreClrBridgeStarted).
 *
 *	If any condition fails, we set a descriptive error on
 *	the interp (when one is provided) so callers don't have
 *	to figure out which step is missing.  This routine does
 *	NOT itself touch the CLR; it only inspects local state.
 *
 *	The package mutex is held for the inspection so the four
 *	values are read coherently -- without it, a concurrent
 *	StopAndReleaseTheCoreClr could see "loaded yes,
 *	context yes, bridge no" mid-teardown.
 *
 * Results:
 *	TRUE if all preconditions are satisfied at the moment of
 *	the call; FALSE otherwise (with an error appended to
 *	interp's result if interp is non-NULL).
 *
 * Side effects:
 *	On failure with non-NULL interp, an error message is
 *	appended to the interp result.  Briefly acquires and
 *	releases the package mutex.
 *
 *----------------------------------------------------------------------
 */

BOOL CanExecuteCoreClrCode(
    Tcl_Interp *interp)			/* Current Tcl interpreter. */
{
    BOOL bResult = FALSE;

    Wrp_MutexLock(&packageMutex);

    if (pCoreClrModule == NULL) {
	if (interp != NULL) {
	    Tcl_AppendResult(interp, "CoreCLR not loaded\n", NULL);
	}

	goto done;
    }

    if (pCoreClrContext == NULL) {
	if (interp != NULL) {
	    Tcl_AppendResult(interp, "CoreCLR not started\n", NULL);
	}

	goto done;
    }

    bResult = TRUE;

done:

    Wrp_MutexUnlock(&packageMutex);
    return bResult;
}

/*
 *----------------------------------------------------------------------
 *
 * ExecuteCoreClrMethod --
 *
 *	Invoke a named managed method (an entry point on a
 *	managed assembly inside the CoreCLR), passing the Tcl
 *	module handle, the Tcl stub-function table, and the
 *	calling interp through to the managed side as parameters.
 *	This is the primary call-into-managed-code path of the
 *	package.
 *
 * Why / How:
 *	The .NET 5+ hosting API does not let an unmanaged caller
 *	invoke a managed method by name in one shot.  The protocol
 *	is two-step:
 *
 *	  1. Call hostfxr_get_runtime_delegate with a request type
 *	     of hdt_load_assembly_and_get_function_pointer to
 *	     obtain a function pointer for an internal CLR helper.
 *	  2. Call that helper, passing the assembly path, type
 *	     name, method name, and delegate type name.  The
 *	     helper loads the assembly into its
 *	     AssemblyLoadContext, JITs the method, and returns
 *	     a callable function pointer with the requested
 *	     delegate signature.
 *	  3. Call that function pointer with the unmanaged-side
 *	     arguments.  Native-to-managed marshaling happens
 *	     across this boundary; everything the caller passes
 *	     must already be in a form the managed side knows
 *	     how to consume (pointers, primitives, NUL-terminated
 *	     strings).
 *
 *	This routine does the full sequence, with the managed
 *	method's signature and assembly metadata expressed via
 *	the wide-string parameters.  The hModule and pTclStubs
 *	parameters are passed through to managed code so the
 *	managed side can call back into Tcl via the same stubs
 *	indirection an unmanaged extension would use.
 *
 *	CanExecuteCoreClrCode is invoked first as a guard.  If it
 *	returns FALSE, the relevant precondition failure is
 *	already on the interp's result and we propagate
 *	TCL_ERROR without trying to load the assembly.
 *
 *	Argument marshaling: bUnicode controls whether returned
 *	strings are appended to the interp result via the
 *	Tcl_AppendUnicodeToObj path (Unicode/UTF-16) or the
 *	Tcl_AppendResult path (the engine's narrow encoding).
 *	bArrayAsList controls whether managed methods returning
 *	arrays are surfaced as Tcl lists vs. concatenated string
 *	fragments.  These two flags exist because the
 *	ergonomically-best representation for return data depends
 *	on what the caller intends to do with it.
 *
 *	The package mutex is held only across the
 *	get_runtime_delegate / load_assembly_and_get_function_pointer
 *	steps.  The actual managed call runs WITHOUT the mutex --
 *	otherwise managed code that re-entered into Tcl through
 *	this package would deadlock against itself.  This is the
 *	critical reason Pal_MutexLock is recursive: in some
 *	configurations the managed side calls back into native
 *	code that ends up acquiring the package mutex, and the
 *	recursive semantics make that re-entry safe.
 *
 * Results:
 *	TCL_OK on success, with any return data from the managed
 *	method appended to the interp result.  TCL_ERROR with an
 *	appropriate error message on any failure (precondition,
 *	delegate acquisition, assembly load, JIT, or managed-side
 *	exception).
 *
 * Side effects:
 *	Loads the named managed assembly into the CLR's default
 *	AssemblyLoadContext on first invocation; the assembly
 *	cannot subsequently be unloaded.  Executes managed code,
 *	which may have arbitrary observable side-effects (file
 *	I/O, network calls, Tcl state mutations via the stubs
 *	callback path, etc.).
 *
 *----------------------------------------------------------------------
 */

int ExecuteCoreClrMethod(
    HMODULE hModule,		/* Tcl library module handle. */
    ClrTclStubs *pTclStubs,	/* Tcl C API stub function pointer table. */
    Tcl_Interp *interp,		/* Current Tcl interpreter. */
    LPCWSTR logCommand,		/* The Tcl command used to log the CoreCLR method
				 * execution, if any. */
    ClrMethodInfo *pMethodInfo, /* Contains the information necessary for this
				 * function to execute the CoreCLR method. */
    LPCWSTR argument,		/* Extra argument to the method, if any. */
    MethodFlags methodFlags,	/* Flags that control logging, arguments, etc.
				 * See the MethodFlags enum for details. */
    LPDWORD pReturnValue)	/* Location where the return value should be
				 * stored or NULL if the return value is not
				 * required. */
{
    int code = TCL_OK;
    BOOL bUseProtocolR1;
    BOOL bUseProtocolR2;
    BOOL bLegacyProtocol;
    BOOL bUseIsolation;
    BOOL bUseSafeInterp;
    BOOL bLogExecute;
    LPWSTR protocolRevision = NULL;
    LPWSTR newArgument = NULL;
    HRESULT hResult;
    DWORD returnValue = TCL_OK;
    size_t length = 0;
    component_entry_point_fn pManaged = NULL;

    if (pMethodInfo == NULL) {
	if (interp != NULL) {
	    Tcl_AppendResult(interp, "invalid method information\n", NULL);
	}

	return TCL_ERROR;
    }

    Wrp_MutexLock(&packageMutex);

    /*
     * NOTE: If the CLR is either not loaded -OR- not started, then we cannot
     *	     use it to execute any code.
     */

    if (!CanExecuteCoreClrCode(interp)) {
	code = TCL_ERROR;
	goto done;
    }

    bUseProtocolR1 = (methodFlags & METHOD_PROTOCOL_V1R1);
    bUseProtocolR2 = (methodFlags & METHOD_PROTOCOL_V1R2);
    bLegacyProtocol = (methodFlags & METHOD_PROTOCOL_LEGACY);
    bUseIsolation = (methodFlags & METHOD_USE_ISOLATION);
    bUseSafeInterp = (methodFlags & METHOD_USE_SAFE_INTERP);

    if ((argument != NULL) || bUseProtocolR1) {
	/*
	 * NOTE: If an argument is present in the method information (i.e. this
	 *       method has been configured by the package to use it), add the
	 *       entire length of the argument plus one space to separate it
	 *       from the rest of the final argument string.
	 */

	if (pMethodInfo->argument != NULL)
	    length += wcslen(pMethodInfo->argument) + 1; /* argument + space. */

	/*
	 * NOTE: If an extra argument was supplied by the caller, add the
	 *       entire length of the argument plus one space to separate it
	 *       from the rest of the final argument string.
	 */

	if (argument != NULL)
	    length += wcslen(argument) + 1; /* argument + space. */

	/*
	 * NOTE: Do we need to prepend additional information required by our
	 *       native-to-managed code protocol (V1)?  The reason a "protocol"
	 *       is required at all is because the native CLR API only allows
	 *       us to pass one string argument to the target CLR method;
	 *       therefore, we have to make the most of it.
	 */

	if (bUseProtocolR1) {
	    /*
	     * HACK: Build the final argument string to pass to CLR method.  We
	     *       need to include the Tcl library module handle and a pointer
	     *       to the Tcl interpreter here in order for Eagle to build a
	     *       bridge back to us.  Since the type signature of the method
	     *       only allows us to pass a single string argument, we must
	     *       convert the Tcl library module handle and the Tcl
	     *       interpreter pointer to strings and then add any arguments
	     *       supplied by the configuration or our immediate caller
	     *       after that.  The final argument string MUST parse as a
	     *       valid list; otherwise, the CLR method MAY simply refuse to
	     *       process it.  We also include a prefix indicating the
	     *       version of the "protocol" that is in use (currently
	     *       "Garuda_v1.0" or "Garuda_v1.0_r2.0") and a Tcl interpreter
	     *       "safety indicator" (i.e. logical boolean) after the Tcl
	     *       interpreter pointer.
	     */

	    length += wcslen(PACKAGE_UNICODE_NAME) + 1; /* strlen(" Garuda") */

	    if (bUseProtocolR2) {
		protocolRevision = PACKAGE_UNICODE_PROTOCOL_V1R2;
	    } else if (bLegacyProtocol) {
		protocolRevision = PACKAGE_UNICODE_PROTOCOL_V1R0;
	    } else {
		protocolRevision = PACKAGE_UNICODE_PROTOCOL_V1R1;
	    }

	    length += wcslen(protocolRevision); /* "vX.0_rY.0", etc */
	    length += 2; /* space before and after protocol revision */
	    length += (sizeof(HMODULE) * 2) + 3; /* "0x" + handleAsStr + " " */
	    length += (sizeof(LPVOID) * 2) + 3; /* "0x" + hexPtrAsStr + " " */
	    length += 2; /* strlen("1 "), "safe", note trailing space */
	}

	/*
	 * NOTE: Do we need to prepend additional information required by our
	 *       native-to-managed code protocol (R2)?
	 */

	if (bUseProtocolR2) {
	    /*
	     * HACK: Include a pointer to the structure containing the Tcl C
	     *       API function pointers.
	     */

	    length += (sizeof(LPVOID) * 2) + 3; /* "0x" + hexPtrAsStr + " " */
	    length += 2; /* strlen("1 "), "isolation", note trailing space */
	}

	length++; /* NUL terminator character. */
	newArgument = (LPWSTR)attemptckalloc(length * sizeof(WCHAR));

	if (newArgument == NULL) {
	    if (interp != NULL) {
		Tcl_AppendResult(interp, "out of memory: newArgument\n", NULL);
	    }

	    code = TCL_ERROR;
	    goto done;
	}

	memset(newArgument, 0, length * sizeof(WCHAR));

	if (bUseProtocolR1) {
	    if (bUseProtocolR2) {
		gwprintf(newArgument, length - GWPRINTF_LENGTH_HAS_NUL,
		    PACKAGE_UNICODE_STR_FMT L"_" PACKAGE_UNICODE_STR_FMT
		    L" " PACKAGE_UNICODE_PTR_FMT L" " PACKAGE_UNICODE_PTR_FMT
		    L" " PACKAGE_UNICODE_PTR_FMT L" " PACKAGE_UNICODE_STR_FMT
		    L" " PACKAGE_UNICODE_STR_FMT L" " PACKAGE_UNICODE_STR_FMT
		    L" " PACKAGE_UNICODE_STR_FMT L"\0", PACKAGE_UNICODE_NAME,
		    protocolRevision, hModule, pTclStubs, interp,
		    bUseIsolation ? L"1 " : L"0 ",
		    bUseSafeInterp ? L"1 " : L"0 ",
		    (pMethodInfo->argument != NULL) ? pMethodInfo->argument :
		    L"", (argument != NULL) ? argument : L"");
	    } else {
		gwprintf(newArgument, length - GWPRINTF_LENGTH_HAS_NUL,
		    PACKAGE_UNICODE_STR_FMT L"_" PACKAGE_UNICODE_STR_FMT
		    L" " PACKAGE_UNICODE_PTR_FMT L" " PACKAGE_UNICODE_PTR_FMT
		    L" " PACKAGE_UNICODE_STR_FMT L" " PACKAGE_UNICODE_STR_FMT
		    L" " PACKAGE_UNICODE_STR_FMT L"\0", PACKAGE_UNICODE_NAME,
		    protocolRevision, hModule, interp,
		    bUseSafeInterp ? L"1 " : L"0 ",
		    (pMethodInfo->argument != NULL) ? pMethodInfo->argument :
		    L"", (argument != NULL) ? argument : L"");
	    }
	} else {
	    gwprintf(newArgument, length - GWPRINTF_LENGTH_HAS_NUL,
		PACKAGE_UNICODE_STR_FMT L"_" PACKAGE_UNICODE_STR_FMT L"\0",
		(pMethodInfo->argument != NULL) ? pMethodInfo->argument : L"",
		(argument != NULL) ? argument : L"");
	}
    } else {
	newArgument = (LPWSTR)pMethodInfo->argument;
    }

    bLogExecute = (methodFlags & METHOD_LOG_EXECUTE);

    if (bLogExecute && PACKAGE_CAN_LOG(interp, logCommand)) {
	/*
	 * NOTE: Verbose mode is enabled; show all the information about the
	 *       CLR method we are about to execute.
	 */

	TclLog(interp, logCommand, L"BEFORE ",
	    L"pLoadAssemblyAndGetFuncPtr(assemblyPath = {",
	    pMethodInfo->assemblyPath, L"}, typeName = {",
	    pMethodInfo->typeName, L"}, methodName = {",
	    pMethodInfo->methodName, L"}, argument = {",
	    newArgument, L"})", NULL);
    }

    hResult = Wrp_pLoadAssemblyAndGetFuncPtr(
	pMethodInfo->assemblyPath, pMethodInfo->typeName,
	pMethodInfo->methodName, NULL, NULL, (void **)&pManaged);

    if (SUCCEEDED(hResult)) {
	if (pManaged != NULL) {
	    /*
	     * HACK: The "NativePackage" (managed) methods called via this
	     *       function expect their native string argument to obey
	     *       all of the following rules:
	     *
	     *       1. On Windows platforms, the WCHAR (wchar_t) type is
	     *          assumed to use two bytes per code unit, which will
	     *          directly correspond to the size of the "char" C#
	     *          type.
	     *
	     *       2. On Windows platforms, the encoding must be either
	     *          UTF-16 or UCS-2 (i.e. without any surrogate pairs).
	     *
	     *       3. On POSIX platforms (e.g. Linux, macOS, etc), the
	     *          WCHAR (wchar_t) type is assumed to use four bytes
	     *          per code unit.
	     *
	     *       4. On POSIX platforms (e.g. Linux, macOS, etc), the
	     *          encoding must be either UTF-32 or UCS-4.
	     *
	     *       5. Regardless of platform or encoding, the size is
	     *          in bytes, not code units.
	     *
	     *       6. The passed length in code units (i.e. calculated
	     *          via the "arg_size_in_bytes" being divided by the
	     *          code unit size) should be precise (i.e. no extra
	     *          space) and should not include the NUL terminator
	     *          character.
	     */
	    size_t newLength = wcslen(newArgument);

	    returnValue = pManaged(
		newArgument, (int32_t)(newLength * sizeof(WCHAR)));
	} else {
	    hResult = HRESULT_FROM_WIN32(ERROR_FUNCTION_NOT_CALLED);

	    if (interp != NULL) {
		Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp),
		    GetClrErrorMessage(
			L"pLoadAssemblyAndGetFuncPtr",
			hResult), -1);
	    }
	}
    }

    if (bLogExecute && PACKAGE_CAN_LOG(interp, logCommand)) {
	WCHAR buffer[PACKAGE_RESULT_SIZE + 1] = { 0 };

	gwprintf(buffer, PACKAGE_RESULT_SIZE, L"AFTER "
	    L"pLoadAssemblyAndGetFuncPtr(hResult = {0x%lX}, "
	    L"pManaged = {" PACKAGE_UNICODE_PTR_FMT  "}, "
	    L"returnValue = {%lu})", (unsigned long)hResult,
	    pManaged, (unsigned long)returnValue);

	TclLog(interp, logCommand, buffer, NULL);
    }

    if (SUCCEEDED(hResult)) {
	if (pReturnValue != NULL)
	    *pReturnValue = returnValue;
    } else {
	if (interp != NULL) {
	    Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp),
		GetClrErrorMessage(
		    L"ICLRRuntimeHost_ExecuteInDefaultAppDomain",
		    hResult), -1);
	}

	code = TCL_ERROR;
	goto done;
    }

done:

    if ((newArgument != NULL) &&
	    (newArgument != pMethodInfo->argument)) {
	ckfree((LPVOID)newArgument);
	newArgument = NULL;
    }

    Wrp_MutexUnlock(&packageMutex);
    return code;
}

/*
 *----------------------------------------------------------------------
 *
 * GetCurrentCoreClrAppDomainId --
 *
 *	Return a stable integer identifier representing the
 *	"current application domain" inside the CoreCLR.
 *
 * Why / How:
 *	NB: .NET 5+ removed AppDomains as a first-class isolation
 *	primitive -- you cannot create or unload them from
 *	unmanaged code, and there is only ever one per process.
 *	The concept survives in the API surface, however, because
 *	managed code (and native callers like this one) sometimes
 *	want a logical identifier they can pass alongside other
 *	references for diagnostics or correlation.  The identifier
 *	this function returns is whatever the CLR exposes via its
 *	hosting introspection path -- in practice a constant for
 *	the lifetime of the process under .NET 5+, but the API is
 *	defined to return it dynamically because older CLRs DID
 *	support multiple AppDomains and embedders may still
 *	depend on the call shape.
 *
 *	Used primarily by diagnostic / logging code paths (e.g.
 *	DumpCoreClrState) so the resulting log lines can be
 *	cross-referenced with managed-side log lines that
 *	include the same identifier.
 *
 * Results:
 *	S_OK on success, with *pAppDomainId populated.  E_POINTER
 *	if pAppDomainId is NULL.  An HRESULT translated from the
 *	underlying CLR error if the host introspection call fails.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

HRESULT GetCurrentCoreClrAppDomainId(
    LPDWORD pAppDomainId)	/* Upon success, will contain an integer
				 * identifier for the current application
				 * domain. */
{
    HRESULT hResult;

    Wrp_MutexLock(&packageMutex);

    if (pAppDomainId == NULL) {
	hResult = E_POINTER;
	goto done;
    }

    if (pCoreClrContext == NULL) {
	hResult = HRESULT_FROM_WIN32(ERROR_SERVICE_NEVER_STARTED);
	goto done;
    }

    *pAppDomainId = 1;
    hResult = S_OK;

done:

    Wrp_MutexUnlock(&packageMutex);
    return hResult;
}

#if defined(HAVE_DOTNET_ENVIRONMENT_INFO)
/*
 *----------------------------------------------------------------------
 *
 * GetCoreClrVersionCallback --
 *
 *	hostfxr_get_dotnet_environment_info callback.  Receives a
 *	hostfxr_dotnet_environment_info * describing the dotnet
 *	installation found by hostfxr, copies the fields of
 *	interest into a caller-owned context struct, and returns.
 *
 * Why / How:
 *	hostfxr_get_dotnet_environment_info is the modern (.NET 6+)
 *	introspection API that enumerates every installed runtime
 *	and SDK on the machine.  It uses a "callback during
 *	enumeration" idiom rather than returning an array, so the
 *	caller (us, in GetCoreClrVersion) provides this function
 *	and a context pointer; hostfxr invokes us during its walk.
 *
 *	The HOSTFXR_CALLTYPE macro applies the calling convention
 *	hostfxr expects on the current platform -- __stdcall on
 *	Win32, the platform default elsewhere.  Mismatching the
 *	calling convention is one of the silent crashes you can
 *	hit when integrating with hostfxr; getting it right at
 *	the macro level eliminates the per-call risk.
 *
 *	The "Input / Output" annotations on the parameters reflect
 *	the data flow: hostfxr fills info, we read it; the
 *	caller of hostfxr_get_dotnet_environment_info supplies
 *	context, we write into it.
 *
 *	NB: this function is gated on HAVE_DOTNET_ENVIRONMENT_INFO
 *	because hostfxr_get_dotnet_environment_info was added
 *	mid-.NET-5-lifetime and is absent on the earliest .NET 5
 *	builds.  When the symbol isn't available, GetCoreClrVersion
 *	falls back to a simpler version-discovery path.
 *
 * Results:
 *	None (the API is void-returning).
 *
 * Side effects:
 *	Mutates the caller-owned context struct pointed to by
 *	context.  Does not allocate, does not call back into
 *	hostfxr.
 *
 *----------------------------------------------------------------------
 */

static void HOSTFXR_CALLTYPE GetCoreClrVersionCallback(
    const struct hostfxr_dotnet_environment_info *info,	/* Input data. */
    void *context)					/* Output data. */
{
    if ((info != NULL) && (context != NULL)) {
	CoreClrVersionInfo *pVersionInfo = context;
	Tcl_Obj *result = pVersionInfo->result;

	pVersionInfo->count++;

	if (result == NULL) {
	    result = Tcl_NewObj();

	    if (result == NULL)
		return;

	    pVersionInfo->result = result;
	    Tcl_IncrRefCount(result);
	}

#if defined(_WIN32)
	Tcl_AppendUnicodeToObj(result, L"version ", -1);
	Tcl_AppendUnicodeToObj(result, info->hostfxr_version, -1);
	Tcl_AppendUnicodeToObj(result, L" commit_hash ", -1);
	Tcl_AppendUnicodeToObj(result, info->hostfxr_commit_hash, -1);
#else
	Tcl_AppendToObj(result, "version ", -1);
	Tcl_AppendToObj(result, info->hostfxr_version, -1);
	Tcl_AppendToObj(result, " commit_hash ", -1);
	Tcl_AppendToObj(result, info->hostfxr_commit_hash, -1);
#endif
    }
}
#endif

/*
 *----------------------------------------------------------------------
 *
 * GetCoreClrVersion --
 *
 *	Format a human-readable version-information string for
 *	the currently loaded CoreCLR into the caller-supplied
 *	wide-character buffer.
 *
 * Why / How:
 *	When HAVE_DOTNET_ENVIRONMENT_INFO is defined and the
 *	hostfxr_get_dotnet_environment_info symbol resolved at
 *	load time, the canonical path is to call that function
 *	with GetCoreClrVersionCallback to populate a local
 *	context struct, then format that struct's fields into
 *	pVersion.  This produces a richer string that names not
 *	just the CLR version but the SDK install location and
 *	related metadata.
 *
 *	When the symbol is absent (older hostfxr), we fall back
 *	to a simpler path that emits whatever version data the
 *	older API can produce.
 *
 *	pLength is in/out: callers pass in the capacity of the
 *	pVersion buffer (in WCHARs, not bytes), and on success
 *	we write back the number of WCHARs actually emitted
 *	(not including the terminating NUL).  On failure with
 *	a too-small buffer, *pLength is set to the required size
 *	so callers can re-try with an appropriately sized buffer.
 *
 *	NB: this function does NOT load or initialize the CoreCLR;
 *	it requires that LoadAndStartTheCoreClr (with at least
 *	bLoad=TRUE) has previously succeeded so hostfxr is mapped
 *	into the process and the optional environment-info
 *	function pointer was resolved.  CanExecuteCoreClrCode is
 *	NOT a precondition -- version queries can succeed even
 *	when the bridge hasn't started, by design.
 *
 * Results:
 *	S_OK on success with pVersion populated and *pLength
 *	updated to the written length.  E_POINTER on NULL
 *	parameters.  HRESULT_FROM_WIN32(...) translated from
 *	hostfxr's underlying failure code, when applicable.
 *	A buffer-too-small error with *pLength set to the
 *	required size.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

HRESULT GetCoreClrVersion(
    LPWSTR pVersion,		/* Upon success, will contain a string
				 * with CLR version information. */
    LPDWORD pLength)		/* Upon entry, the length of the state
				 * buffer.  Upon success, will contain
				 * the length of the resulting string. */
{
#if defined(HAVE_DOTNET_ENVIRONMENT_INFO)
    HRESULT hResult = S_OK;
    CoreClrVersionInfo uVersionInfo;
    int32_t rc;
    LPWSTR result = NULL;
    int length;

    Wrp_MutexLock(&packageMutex);

    memset(&uVersionInfo, 0, sizeof(CoreClrVersionInfo));
    uVersionInfo.sizeOf = sizeof(CoreClrVersionInfo);

    if ((pVersion == NULL) || (pLength == NULL)) {
	hResult = E_POINTER;
	goto done;
    }

    if (uCoreClrFunctions.pGetDotNetEnvInfo == NULL) {
	hResult = E_NOTIMPL;
	goto done;
    }

    rc = uCoreClrFunctions.pGetDotNetEnvInfo(
	NULL, NULL, GetCoreClrVersionCallback, &uVersionInfo);

    if (rc != 0) {
	hResult = HRESULT_FROM_WIN32(rc);
	goto done;
    }

    if (uVersionInfo.result == NULL) {
	hResult = E_FAIL;
	goto done;
    }

    result = Wrp_GetUnicodeFromObj(uVersionInfo.result, &length);

    if (result == NULL) {
	hResult = E_OUTOFMEMORY;
	goto done;
    }

    if ((DWORD)length >= *pLength) {
	hResult = DISP_E_OVERFLOW;
	goto done;
    }

    wcsncpy(pVersion, result, *pLength);
    *pLength = length;

done:

#if !defined(_WIN32)
    if (result != NULL) {
	ckfree((LPVOID)result);
	result = NULL;
    }
#endif

    if (uVersionInfo.result != NULL) {
	Tcl_DecrRefCount(uVersionInfo.result);
	uVersionInfo.result = NULL;
    }

    Wrp_MutexUnlock(&packageMutex);
    return hResult;
#else
    return E_NOTIMPL;
#endif
}

/*
 *----------------------------------------------------------------------
 *
 * DumpCoreClrState --
 *
 *	Format a multi-line, human-readable diagnostic snapshot of
 *	the package's current CoreCLR-hosting state into the
 *	caller's wide-character buffer.  Intended for advanced
 *	troubleshooting and for the [garuda diagnose] command
 *	exposed at the script level.
 *
 * Why / How:
 *	Aggregates the values produced by the predicate /
 *	introspection functions in this file into a single
 *	formatted string.  Typical contents include:
 *
 *	  - the file name of the loaded hostfxr library (as
 *	    discovered via get_module_file_name on
 *	    pCoreClrModule);
 *	  - the result of GetCoreClrVersion;
 *	  - the AppDomain identifier from
 *	    GetCurrentCoreClrAppDomainId;
 *	  - boolean state from GetCoreClrWasLoaded /
 *	    GetCoreClrWasStarted / GetCoreClrBridgeStarted;
 *	  - the package's own module file name, for
 *	    cross-referencing with logs.
 *
 *	The fileName parameter is the package's own shared-library
 *	path; the caller supplies it because this routine doesn't
 *	have direct access to the package module handle in a
 *	platform-uniform way.  pState / pLength follow the same
 *	in/out length convention as GetCoreClrVersion: pass in
 *	capacity, get back actual or required size.
 *
 *	Failures of individual sub-queries are tolerated: the
 *	dump emits whatever it can and notes the missing pieces
 *	rather than aborting on the first error.  This is what
 *	makes it useful for diagnosing partial-load conditions
 *	(e.g. hostfxr loaded but CLR initialization never
 *	attempted, or bridge handshake started but never
 *	completed).
 *
 * Results:
 *	S_OK on success with pState populated and *pLength
 *	updated to the written length.  E_POINTER on NULL
 *	parameters.  A buffer-too-small error with *pLength set
 *	to the required size.
 *
 * Side effects:
 *	None.  The introspection sub-calls each acquire and
 *	release the package mutex briefly, but no state is
 *	mutated.
 *
 *----------------------------------------------------------------------
 */

HRESULT DumpCoreClrState(
    LPWSTR fileName,		/* The (fully qualified) file name of
				 * the (dynamic) shared library that
				 * contains this code. */
    LONG lTclStubs,		/* Non-zero if the Tcl stubs mechanism
				 * has been initialized. */
    HMODULE hTclModule,		/* The handle of the (dynamic) shared
				 * library module for Tcl. */
    ClrTclStubs *pTclStubs,	/* Pointer to structure that contains
				 * the set of function pointers to be
				 * passed to the managed code. */
    LPWSTR pState,		/* Upon success, will contain a string
				 * with package debugging information. */
    LPDWORD pLength)		/* Upon entry, the length of the state
				 * buffer.  Upon success, will contain
				 * the length of the resulting string. */
{
    HRESULT hResult;

    Wrp_MutexLock(&packageMutex);

    if ((pState == NULL) || (pLength == NULL)) {
	hResult = E_POINTER;
	goto done;
    }

    gwprintf(pState, *pLength,
	L"packageMutex " PACKAGE_UNICODE_PTR_FMT
	L" hPackageModule " PACKAGE_UNICODE_PTR_FMT
	L" packageFileName {" PACKAGE_UNICODE_STR_FMT
	L"} lTclStubs %ld hTclModule "
	PACKAGE_UNICODE_PTR_FMT L" pTclStubs "
	PACKAGE_UNICODE_PTR_FMT L" pCoreClrModule "
	PACKAGE_UNICODE_PTR_FMT L" uCoreClrFunctions "
	PACKAGE_UNICODE_PTR_FMT L" pCoreClrContext "
	PACKAGE_UNICODE_PTR_FMT L" bClrBridgeStarted %d",
	packageMutex,
	GetPackageModule(), fileName, lTclStubs, hTclModule,
	pTclStubs, pCoreClrModule, &uCoreClrFunctions,
	pCoreClrContext, bCoreClrBridgeStarted);

    hResult = S_OK;

done:

    Wrp_MutexUnlock(&packageMutex);
    return hResult;
}
#endif /* defined(USE_CORE_CLR) */
