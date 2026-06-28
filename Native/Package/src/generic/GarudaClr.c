/*
 * GarudaClr.c -- Eagle Package for Tcl (Garuda)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#include "GarudaPre.h"		/* NOTE: For private header setup. */

#if !defined(USE_CORE_CLR)
#include <stdio.h>		/* NOTE: For fprintf, swprintf, va_list, etc. */
#include <string.h>		/* NOTE: For memset, wcslen, wcsncpy, etc. */

#include "MSCorEE.h"		/* NOTE: For native CLR v2 API. */

#if defined(USE_CLR_40)
#  include "MetaHost.h"		/* NOTE: For native CLR v4 API. */
#endif

#include "tcl.h"		/* NOTE: For public Tcl API. */
#include "GarudaPal.h"		/* NOTE: For platform abstraction API. */
#include "pkgVersion.h"		/* NOTE: Package version information. */
#include "GarudaInt.h"		/* NOTE: For private package API. */
#include "GarudaClr.h"		/* NOTE: For private package CLR API. */
#include "GarudaDecls.h"	/* NOTE: For private package declarations. */

/*
 * NOTE: These are the CLR v4 metadata host and runtime introspection interface
 *       pointers.
 */

#if defined(USE_CLR_40)
static ICLRMetaHost *pClrMetaHost = NULL;
static ICLRRuntimeInfo *pClrRuntimeInfo = NULL;
#endif

/*
 * NOTE: This is the CLR v2+ runtime host interface pointer.  If NULL, the CLR
 *       has either not been loaded yet or the resources belonging to it have
 *       been freed via IUnknown::Release.  The CLR, once loaded, cannot be
 *       fully unloaded from a running process; however, in practice, this is
 *       not a big problem.
 */

static ICLRRuntimeHost *pClrRuntimeHost = NULL;

/*
 * NOTE: This variable will be TRUE if the ICLRRuntimeHost::Start method has
 *       been called successfully by this package.  When this package calls the
 *       ICLRRuntimeHost::Stop method successfully, the value of this variable
 *       will be reset to FALSE.
 */

static volatile BOOL bClrStarted = FALSE;

/*
 * NOTE: This variable will be TRUE if the bridge was successfully started and
 *       has not been shutdown yet.
 */

static volatile BOOL bClrBridgeStarted = FALSE;

/*
 *----------------------------------------------------------------------
 *
 * GetClrWasLoaded --
 *
 *	Test whether THIS package has obtained an
 *	ICLRRuntimeHost handle for the .NET Framework CLR (CLR
 *	2.x or 4.x) inside the current process.
 *
 * Why / How:
 *	This is the .NET Framework counterpart of
 *	GetCoreClrWasLoaded.  Unlike .NET 5+, the .NET Framework
 *	hosting model is COM-based: ICLRMetaHost is the entry-
 *	point factory, ICLRRuntimeInfo describes a candidate CLR
 *	build, and ICLRRuntimeHost (the v4 host) is the activated
 *	runtime handle this package retains across calls.  The
 *	pClrRuntimeHost global is set when LoadAndStartTheClr
 *	successfully obtains the host interface, and cleared by
 *	StopAndReleaseTheClr after Release().
 *
 *	"Loaded" here is therefore "we hold an ICLRRuntimeHost
 *	pointer", not "any CLR is present in the process."  The
 *	mscoree.dll loader stub is link-time present in any
 *	build of this package; what matters is whether a
 *	specific CLR build has been activated through it.
 *
 *	Snapshot semantics under the package mutex, same as
 *	GetCoreClrWasLoaded.
 *
 * Results:
 *	TRUE if pClrRuntimeHost is non-NULL at the moment of
 *	the read; FALSE otherwise.
 *
 * Side effects:
 *	None.  Briefly acquires and releases the package mutex.
 *
 *----------------------------------------------------------------------
 */

BOOL GetClrWasLoaded(void)
{
    BOOL bResult;

    Tcl_MutexLock(&packageMutex);
    bResult = (pClrRuntimeHost != NULL);
    Tcl_MutexUnlock(&packageMutex);

    return bResult;
}

/*
 *----------------------------------------------------------------------
 *
 * GetClrWasStarted --
 *
 *	Test whether the .NET Framework CLR has been transitioned
 *	from "loaded" (host interface obtained) to "started"
 *	(ICLRRuntimeHost::Start has been called successfully).
 *
 * Why / How:
 *	The .NET Framework hosting protocol distinguishes
 *	"activated" (host interface alive) from "started"
 *	(runtime is initialized and ready to execute managed
 *	code).  ICLRRuntimeHost::Start is the call that does the
 *	actual initialization; calling it twice on the same host
 *	is documented to be a no-op error.  bClrStarted records
 *	whether we have invoked Start AND received S_OK.
 *
 *	Cleared by StopAndReleaseTheClr after the matching
 *	ICLRRuntimeHost::Stop call.
 *
 *	Snapshot semantics under the package mutex.
 *
 * Results:
 *	TRUE if Start has succeeded and Stop has not been called
 *	since.  FALSE otherwise.
 *
 * Side effects:
 *	None.  Briefly acquires and releases the package mutex.
 *
 *----------------------------------------------------------------------
 */

BOOL GetClrWasStarted(void)
{
    BOOL bResult;

    Tcl_MutexLock(&packageMutex);
    bResult = bClrStarted;
    Tcl_MutexUnlock(&packageMutex);

    return bResult;
}

/*
 *----------------------------------------------------------------------
 *
 * GetClrBridgeStarted --
 *
 *	Test whether the Eagle-side managed bridge that hosts
 *	Garuda's Tcl / .NET Framework CLR interop has been
 *	brought up inside the activated CLR.
 *
 * Why / How:
 *	The "bridge" is the managed-side counterpart to the
 *	native ICLRRuntimeHost.  A successful startup sequence is:
 *
 *	  1. CLRCreateInstance / GetRuntime / GetInterface to
 *	     activate ICLRRuntimeHost (tracked by
 *	     GetClrWasLoaded).
 *	  2. ICLRRuntimeHost::Start to initialize the runtime
 *	     (tracked by GetClrWasStarted).
 *	  3. ICLRRuntimeHost::ExecuteInDefaultAppDomain to invoke
 *	     the Eagle bridge entry point, which constructs the
 *	     bridge object and signals success back via
 *	     SetClrBridgeStarted.
 *
 *	bClrBridgeStarted reflects step 3.  TRUE here implies
 *	all prior steps completed AND the managed side
 *	acknowledged the handshake -- the most authoritative
 *	"is the bridge usable?" predicate in this file.
 *
 *	Snapshot semantics under the package mutex.
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

BOOL GetClrBridgeStarted(void)
{
    BOOL bResult;

    Tcl_MutexLock(&packageMutex);
    bResult = bClrBridgeStarted;
    Tcl_MutexUnlock(&packageMutex);

    return bResult;
}

/*
 *----------------------------------------------------------------------
 *
 * SetClrBridgeStarted --
 *
 *	Record whether the managed-side .NET Framework bridge
 *	has been started.
 *
 * Why / How:
 *	Called by the managed bridge entry point (via the
 *	ICLRRuntimeHost::ExecuteInDefaultAppDomain callback) to
 *	signal completion of the handshake described in the
 *	GetClrBridgeStarted comment.  Also called by the
 *	teardown path with bStarted=FALSE before the matching
 *	ICLRRuntimeHost::Stop / Release sequence.
 *
 *	The package-mutex acquisition is what makes this safe to
 *	call from any thread the CLR happens to schedule on.
 *
 * Results:
 *	None.
 *
 * Side effects:
 *	Updates bClrBridgeStarted under the package mutex.
 *	Future GetClrBridgeStarted calls observe the new value.
 *
 *----------------------------------------------------------------------
 */

void SetClrBridgeStarted(
    BOOL bStarted)	    /* Non-zero if the bridge was started. */
{
    Tcl_MutexLock(&packageMutex);
    bClrBridgeStarted = bStarted;
    Tcl_MutexUnlock(&packageMutex);
}

/*
 *----------------------------------------------------------------------
 *
 * LoadAndStartTheClr --
 *
 *	This function loads and optionally starts the latest version of
 *	the CLR supported by this package.
 *
 * Why / How:
 *	This is the .NET Framework counterpart to LoadAndStartTheCoreClr
 *	(which targets .NET 5+ / CoreCLR).  The two functions arrive at
 *	the same end state -- a usable CLR instance referenced by the
 *	package -- but via dramatically different protocols, so they are
 *	intentionally kept as parallel implementations rather than fused
 *	behind a common abstraction.  The contrast is informative:
 *
 *	  CoreCLR (.NET 5+):
 *	    nethost.dll -> hostfxr -> hostpolicy -> coreclr (4-tier
 *	    LoadLibrary chain, runtimeconfig.json, side-by-side runtimes,
 *	    no AppDomains, cross-platform).
 *
 *	  .NET Framework (this function):
 *	    mscoree.dll (statically linked at build time) -> CLRCreateInstance
 *	    -> ICLRMetaHost -> GetRuntime -> ICLRRuntimeInfo -> IsLoadable
 *	    -> GetInterface(ICLRRuntimeHost) (COM tree, registry-based
 *	    runtime selection, real AppDomains, Windows-only).
 *
 *	Because mscoree.dll is part of the import table of this DLL, it
 *	is guaranteed to be in-process by the time this function runs;
 *	there is no LoadLibrary equivalent of nethost's bootstrap call.
 *	What we do need is a procedure address for CLRCreateInstance,
 *	which is obtained via GetModuleHandleW + GetProcAddress.  If the
 *	export is missing -- older OS, mscoree v2-only, or the v4 entry
 *	point was elided -- we fall through to the legacy path described
 *	below.
 *
 *	The v4 path (USE_CLR_40):
 *	  1. Resolve CLRCreateInstance (the v4 entry point).
 *	  2. Build an ICLRMetaHost (the registry-of-runtimes object).
 *	  3. Pick a version string -- "v4.0.30319" for the latest, or
 *	     "v2.0.50727" if bUseMinimumClr (controlled by the
 *	     UseMinimumClr environment variable or the package-namespace
 *	     useMinimumClr Tcl variable).  A NULL version string is NOT
 *	     accepted by ICLRMetaHost_GetRuntime; the version must be
 *	     hard-coded.
 *	  4. Ask the meta-host for an ICLRRuntimeInfo of that version.
 *	  5. Verify ICLRRuntimeInfo_IsLoadable -- this answers whether
 *	     binding policies + side-by-side rules permit loading this
 *	     runtime into THIS process.  An "installed but not loadable"
 *	     result is a hard error here; the caller asked for a specific
 *	     version and we will not silently substitute another.
 *	  6. Use ICLRRuntimeInfo::GetInterface to materialize the
 *	     ICLRRuntimeHost we will use for everything else.  This is
 *	     the COM object that exposes Start / Stop / ExecuteIn-
 *	     DefaultAppDomain / GetCurrentAppDomainId.
 *
 *	The fallback path (compiled in always; reached when v4 is not
 *	available OR when the E_NOTIMPL escape hatch fires):
 *	  CorBindToRuntimeEx -- the legacy CLR 2.0-era loader call.  This
 *	  ALSO returns an ICLRRuntimeHost, but with no version selection
 *	  and no IsLoadable check.  The hResult==E_NOTIMPL branch is
 *	  documented in Brad Wilson's [MSFT] 2010-04-19 blog post
 *	  "Selecting CLR Version From Unmanaged Host" but is curiously
 *	  absent from the MSDN reference for CLRCreateInstance -- the
 *	  blog is the only authoritative source for this contract, so
 *	  we cite it explicitly to defend the goto fallback edge.
 *
 *	The Start step (gated by bStart) is a separate decision because
 *	some embedders want to load-but-not-start (e.g. to inspect the
 *	CLR version string before committing to a runtime).  Once started
 *	the CLR cannot be re-started in the same process -- see
 *	StopAndReleaseTheClr's header for why.
 *
 *	Strictness (bStrict): when set, calling this on an already-loaded
 *	or already-started CLR is an error.  When clear, those become
 *	no-ops, supporting idempotent embedder startup sequences.
 *
 *	The package mutex wraps the entire operation.  This is necessary
 *	because the function reads-and-writes pClrRuntimeHost,
 *	pClrMetaHost, pClrRuntimeInfo, and bClrStarted, all of which are
 *	consulted by other entry points (Get*State / CanExecuteClrCode /
 *	ExecuteClrMethod / StopAndReleaseTheClr) that may be called from
 *	any thread the embedder chooses.  Holding the mutex across the
 *	COM calls is also fine: the .NET Framework hosting interface is
 *	free-threaded and calls do not block on managed code (managed
 *	code only runs once Start has succeeded AND a thread enters via
 *	ExecuteClrMethod).
 *
 *	If anything fails, the partial state is left in place rather
 *	than rolled back -- pClrMetaHost and pClrRuntimeInfo, if obtained,
 *	stay set so that StopAndReleaseTheClr (or a teardown call) can
 *	free them via ICLRMetaHost_Release / ICLRRuntimeInfo_Release.
 *	This keeps the cleanup contract uniform across success and error
 *	paths and avoids the "half-built and not-tracked" leak class.
 *
 * Results:
 *	A standard Tcl result.
 *
 * Side effects:
 *	Since the CLR may execute startup code, this function may
 *	have arbitrary side-effects.
 *
 *----------------------------------------------------------------------
 */

int LoadAndStartTheClr(
    Tcl_Interp *interp,		/* Current Tcl interpreter. */
    LPCWSTR logCommand,		/* The Tcl command used to log the CLR
				 * method execution, if any. */
    LPCWSTR runtimeConfigPath,	/* The CLR runtime configuration file
				 * path, if any. */
    BOOL bLoad,			/* Load the CLR if necessary? */
    BOOL bUseMinimumClr,	/* Force using minimum supported CLR
				 * version? */
    BOOL bStart,		/* Start the CLR after loading it? */
    BOOL bStrict)		/* Fail if already loaded and/or started? */
{
    int code = TCL_OK;
    WCHAR buffer[PACKAGE_RESULT_SIZE + 1] = {0};

    Tcl_MutexLock(&packageMutex);

    /*
     * NOTE: Has the CLR been loaded into this process [by this package] yet?
     *       If not, try to do it now.
     */

    if (bLoad) {
#if defined(USE_CLR_40)
	if (pClrRuntimeHost == NULL) {
	    HRESULT hResult;
	    LPCWSTR clrVersion;
	    BOOL bLoadable = FALSE;

	    /*
	     * NOTE: We link to the "MSCorEE" library; therefore, it should
	     *       already be loaded (i.e. no need to call LoadLibrary here).
	     *       However, we need the loaded module handle for our call to
	     *       GetProcAddress; therefore, get the existing module handle
	     *       using the GetModuleHandle function.
	     */

	    /* NON-PORTABLE */
	    CLRCreateInstanceFnPtr pClrCreateInstance =
		(CLRCreateInstanceFnPtr) GetProcAddress(GetModuleHandleW(
		    UNICODE_TEXT(CLR_MODULE_NAME)), CLR_PROC_NAME);

	    if (PACKAGE_CAN_LOG(interp, logCommand)) {
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"GetProcAddress(pClrCreateInstance = {"
		    PACKAGE_UNICODE_PTR_FMT L"})", pClrCreateInstance);

		TclLog(interp, logCommand, buffer, NULL);
	    }

	    if (pClrCreateInstance == NULL) {
		goto fallback;
	    }

	    hResult = pClrCreateInstance(&CLSID_CLRMetaHost, &IID_ICLRMetaHost,
		&pClrMetaHost);

	    if (PACKAGE_CAN_LOG(interp, logCommand)) {
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"pClrCreateInstance(hResult = {0x%lX}, "
		    L"pClrMetaHost = {" PACKAGE_UNICODE_PTR_FMT L"})",
		    (unsigned long)hResult, pClrMetaHost);

		TclLog(interp, logCommand, buffer, NULL);
	    }

	    if (FAILED(hResult)) {
		/*
		 * NOTE: According to Brad Wilson's [MSFT] blog post "Selecting
		 *       CLR Version From Unmanaged Host", dated 2010/04/19, if
		 *       E_NOTIMPL is returned from the CLRCreateInstance
		 *       function, we should simply try to load the CLR using
		 *       the "legacy" path (i.e. the CorBindToRuntimeEx
		 *       function).  However, this return value is not called
		 *       out in the MSDN documentation for this function.
		 */

		if (hResult == E_NOTIMPL) {
		    goto fallback;
		}

		if (interp != NULL) {
		    Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"pClrCreateInstance", hResult),
			    -1);
		}

		code = TCL_ERROR;
		goto done;
	    }

	    /*
	     * NOTE: By default, we want to load the latest supported version
	     *       of the CLR (e.g. "v4.0.30319").  However, this loading
	     *       behavior can now be overridden by setting the environment
	     *       variable named "UseMinimumClr" [to anything] -OR- by
	     *       setting the Tcl variable "useMinimumClr" (in the namespace
	     *       of the package) to non-zero.  In that case, the minimum
	     *       supported version of the CLR will be loaded instead (e.g.
	     *       "v2.0.50727").
	     */

	    if (bUseMinimumClr) {
		/*
		 * NOTE: Ok, the environment variable is set, use the minimum
		 *       supported version of the CLR instead of the latest.
		 */

		clrVersion = UNICODE_TEXT(CLR_VERSION_MINIMUM);
	    } else {
		/*
		 * NOTE: Ok, use the latest supported version of the CLR.
		 */

		clrVersion = UNICODE_TEXT(CLR_VERSION_LATEST);
	    }

	    if (PACKAGE_CAN_LOG(interp, logCommand)) {
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"BEFORE ICLRMetaHost_GetRuntime(pClrMetaHost = {"
		    PACKAGE_UNICODE_PTR_FMT L"}, clrVersion = {"
		    PACKAGE_UNICODE_STR_FMT L"})", pClrMetaHost,
		    clrVersion);

		TclLog(interp, logCommand, buffer, NULL);
	    }

	    /*
	     * NOTE: Apparently, a NULL version string cannot be used here to
	     *       mean "give me the latest version available" and that means
	     *       we have to hard-code it.
	     */

	    hResult = ICLRMetaHost_GetRuntime(pClrMetaHost, clrVersion,
		&IID_ICLRRuntimeInfo, &pClrRuntimeInfo);

	    if (PACKAGE_CAN_LOG(interp, logCommand)) {
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"AFTER ICLRMetaHost_GetRuntime(hResult = {0x%lX}, "
		    L"pClrRuntimeInfo = {" PACKAGE_UNICODE_PTR_FMT L"})",
		    (unsigned long)hResult, pClrRuntimeInfo);

		TclLog(interp, logCommand, buffer, NULL);
	    }

	    if (FAILED(hResult)) {
		if (interp != NULL) {
		    Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"ICLRMetaHost_GetRuntime",
			    hResult), -1);
		}

		code = TCL_ERROR;
		goto done;
	    }

	    /*
	     * NOTE: Check if the version of the CLR that we want to load can
	     *       actually be loaded into this process.
	     */

	    hResult = ICLRRuntimeInfo_IsLoadable(pClrRuntimeInfo, &bLoadable);

	    if (PACKAGE_CAN_LOG(interp, logCommand)) {
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"ICLRRuntimeInfo_IsLoadable(hResult = {0x%lX}, "
		    L"bLoadable = {%d})", (unsigned long)hResult,
		    bLoadable);

		TclLog(interp, logCommand, buffer, NULL);
	    }

	    if (FAILED(hResult)) {
		if (interp != NULL) {
		    Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"ICLRRuntimeInfo_IsLoadable",
			    hResult), -1);
		}

		code = TCL_ERROR;
		goto done;
	    }

	    if (!bLoadable) {
		if (interp != NULL) {
		    Tcl_AppendResult(interp, "CLR version \"", NULL);

		    Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			clrVersion, -1);

		    Tcl_AppendResult(interp, "\" not loadable\n", NULL);
		}

		code = TCL_ERROR;
		goto done;
	    }

	    hResult = ICLRRuntimeInfo_GetInterface(pClrRuntimeInfo,
		&CLSID_CLRRuntimeHost, &IID_ICLRRuntimeHost,
		&pClrRuntimeHost);

	    if (PACKAGE_CAN_LOG(interp, logCommand)) {
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"ICLRRuntimeInfo_GetInterface(hResult = {0x%lX}, "
		    L"pClrRuntimeHost = {" PACKAGE_UNICODE_PTR_FMT L"})",
		    (unsigned long)hResult, pClrRuntimeHost);

		TclLog(interp, logCommand, buffer, NULL);
	    }

	    if (SUCCEEDED(hResult)) {
		/*
		 * NOTE: Ok, the CLR should now be loaded into the process.
		 *       Vector to our normal CLR startup logic (below).
		 */

		goto start;
	    } else {
		if (interp != NULL) {
		    Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"ICLRRuntimeInfo_GetInterface",
			    hResult), -1);
		}

		code = TCL_ERROR;
		goto done;
	    }
	} else if (bStrict) {
	    if (interp != NULL) {
		Tcl_AppendResult(interp, "CLR already loaded\n", NULL);
	    }

	    code = TCL_ERROR;
	    goto done;
	}

fallback:
#endif

	if (pClrRuntimeHost == NULL) {
	    HRESULT hResult = CorBindToRuntimeEx(NULL, NULL, 0,
		&CLSID_CLRRuntimeHost, &IID_ICLRRuntimeHost,
		&pClrRuntimeHost);

	    if (PACKAGE_CAN_LOG(interp, logCommand)) {
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"CorBindToRuntimeEx(hResult = {0x%lX}, "
		    L"pClrRuntimeHost = {" PACKAGE_UNICODE_PTR_FMT L"})",
		    (unsigned long)hResult, pClrRuntimeHost);

		TclLog(interp, logCommand, buffer, NULL);
	    }

	    if (FAILED(hResult)) {
		if (interp != NULL) {
		    Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"CorBindToRuntimeEx", hResult),
			    -1);
		}

		code = TCL_ERROR;
		goto done;
	    }
	} else if (bStrict) {
	    if (interp != NULL) {
		Tcl_AppendResult(interp, "CLR already loaded\n", NULL);
	    }

	    code = TCL_ERROR;
	    goto done;
	}
    }

    /*
     * NOTE: This label is only referenced if CLR v4 support is enabled at
     *       compile-time; therefore, to avoid a compiler warning, we #ifdef
     *       it out when that option is not enabled.
     */

#if defined(USE_CLR_40)
start:
#endif

    /*
     * NOTE: Has the CLR been started in this process [by this package] yet?
     *       If not, try to do it now.
     */

    if (bStart) {
	if (pClrRuntimeHost == NULL) {
	    if (interp != NULL) {
		Tcl_AppendResult(interp, "CLR not loaded\n", NULL);
	    }

	    code = TCL_ERROR;
	    goto done;
	}

	if (!bClrStarted) {
	    HRESULT hResult = ICLRRuntimeHost_Start(pClrRuntimeHost);

	    if (PACKAGE_CAN_LOG(interp, logCommand)) {
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"ICLRRuntimeHost_Start(hResult = {0x%lX})",
		    (unsigned long)hResult);

		TclLog(interp, logCommand, buffer, NULL);
	    }

	    if (SUCCEEDED(hResult)) {
		bClrStarted = TRUE;
	    } else {
		Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp),
		    GetClrErrorMessage(L"ICLRRuntimeHost_Start", hResult),
			-1);

		code = TCL_ERROR;
		goto done;
	    }
	} else if (bStrict) {
	    if (interp != NULL) {
		Tcl_AppendResult(interp, "CLR already started\n", NULL);
	    }

	    code = TCL_ERROR;
	    goto done;
	}
    }

done:

    Tcl_MutexUnlock(&packageMutex);
    return code;
}

/*
 *----------------------------------------------------------------------
 *
 * StopAndReleaseTheClr --
 *
 *	This function stops and releases the CLR.
 *
 * Why / How:
 *	The .NET Framework counterpart to ShutdownTheCoreClr.  Reverses
 *	the work of LoadAndStartTheClr in the canonical COM order: stop
 *	first, then release the interface pointers, last to first.
 *
 *	Important: even though this function exists and the COM contract
 *	formally permits ICLRRuntimeHost::Stop followed by another
 *	ICLRRuntimeHost::Start, the .NET Framework CLR is in practice a
 *	process-lifetime resource.  Once Stop has been called, attempting
 *	to re-Start the same runtime in the same process produces
 *	undefined behavior in our experience (managed threads remain in
 *	limbo, finalizers may or may not have run, type system state is
 *	half-torn-down).  This function is therefore intended primarily
 *	for the package-unload path and for embedder shutdown -- not as
 *	a "rebuild the CLR" primitive.  The CoreCLR side has the same
 *	property despite shipping a documented coreclr_shutdown_2 entry
 *	point; the underlying assumption "one CLR per process, ever" is
 *	a .NET-wide invariant, not a hosting-layer detail.
 *
 *	The CLR_STOPPING environment variable bracket
 *	(SetEnvironmentVariableW around the ICLRRuntimeHost_Stop call)
 *	is a signal to the managed bridge: "the unmanaged side is
 *	tearing down the CLR, do not attempt any callbacks during
 *	finalization".  Without this, finalizers running for managed
 *	objects that hold callbacks back into native could re-enter the
 *	package after its state has already been freed.  The variable
 *	is set just before Stop and cleared immediately after, holding
 *	the package mutex across both writes.
 *
 *	Release ordering, when bRelease is set:
 *	  1. ICLRRuntimeHost_Release (the live-runtime interface).
 *	  2. ICLRRuntimeInfo_Release (the version-handle interface;
 *	     v4 path only).
 *	  3. ICLRMetaHost_Release    (the registry-of-runtimes; v4
 *	     path only).
 *	Each Release returns a refcount which is logged but not acted
 *	on -- non-zero refcounts here are usually a sign of a managed
 *	object holding a back-reference, NOT a bug in this code.
 *
 *	The bridge-flag clearing at the bottom (under the `done` label)
 *	is a defensive rule: if the CLR has been stopped but the bridge
 *	flag is still set, that is logically inconsistent because the
 *	bridge cannot run without a live CLR.  We forcibly clear it and
 *	emit a WARNING -- this lets future predicate checks (especially
 *	GetClrBridgeStarted / CanExecuteClrCode) report a coherent
 *	answer.  The only way this branch fires legitimately is the
 *	embedder calling StopAndReleaseTheClr without first calling
 *	the bridge teardown -- usually a programming error worth
 *	flagging in the log.
 *
 *	Strictness (bStrict): same shape as LoadAndStartTheClr.  Strict
 *	mode treats already-stopped or not-loaded as errors; relaxed
 *	mode treats them as no-ops, supporting idempotent shutdown.
 *
 * Results:
 *	A standard Tcl result.
 *
 * Side effects:
 *	Since the CLR may execute cleanup code, this function may have
 *	arbitrary side-effects.
 *
 *----------------------------------------------------------------------
 */

int StopAndReleaseTheClr(
    Tcl_Interp *interp,	    /* Current Tcl interpreter. */
    LPCWSTR logCommand,	    /* The Tcl command used to log the CLR method
			     * execution, if any. */
    BOOL bRelease,	    /* Release the CLR after stopping it? */
    BOOL bStrict)	    /* Fail if already stopped and/or released? */
{
    int code = TCL_OK;
    BOOL bResult;
    WCHAR buffer[PACKAGE_RESULT_SIZE + 1] = {0};

    Tcl_MutexLock(&packageMutex);

    if (pClrRuntimeHost != NULL) {
	/*
	 * NOTE: If we were previously able to start the CLR, stop it now.
	 */

	if (bClrStarted) {
	    HRESULT hResult = S_OK;

	    /* NON-PORTABLE */
	    bResult = SetEnvironmentVariableW(
		UNICODE_CLR_STOPPING_ENVVAR_NAME, L"1");

	    if (!bResult) {
		if (interp != NULL) {
		    Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"SetEnvironmentVariableW",
			    HRESULT_FROM_WIN32(GetLastError())), -1);
		}

		code = TCL_ERROR;
		goto done;
	    }

	    hResult = ICLRRuntimeHost_Stop(pClrRuntimeHost);

	    /* NON-PORTABLE */
	    bResult = SetEnvironmentVariableW(
		UNICODE_CLR_STOPPING_ENVVAR_NAME, NULL);

	    if (!bResult) {
		if (interp != NULL) {
		    Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"UnsetEnvironmentVariableW",
			    HRESULT_FROM_WIN32(GetLastError())), -1);
		}

		code = TCL_ERROR;
		goto done;
	    }

	    if (PACKAGE_CAN_LOG(interp, logCommand)) {
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"ICLRRuntimeHost_Stop(hResult = {0x%lX})",
		    (unsigned long)hResult);

		TclLog(interp, logCommand, buffer, NULL);
	    }

	    if (SUCCEEDED(hResult)) {
		bClrStarted = FALSE;
	    } else {
		if (interp != NULL) {
		    Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp),
			GetClrErrorMessage(L"ICLRRuntimeHost_Stop", hResult),
			    -1);
		}

		code = TCL_ERROR;
		goto done;
	    }
	} else if (bStrict) {
	    if (interp != NULL) {
		Tcl_AppendResult(interp, "CLR not started\n", NULL);
	    }

	    code = TCL_ERROR;
	    goto done;
	}

	/*
	 * NOTE: Should we also release the COM reference to the CLR runtime
	 *       host?
	 */

	if (bRelease) {
	    ULONG result = ICLRRuntimeHost_Release(pClrRuntimeHost);

	    pClrRuntimeHost = NULL;

	    if (PACKAGE_CAN_LOG(interp, logCommand)) {
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"ICLRRuntimeHost_Release(result = {%lu})", result);

		TclLog(interp, logCommand, buffer, NULL);
	    }
	}
    } else if (bStrict) {
	if (interp != NULL) {
	    Tcl_AppendResult(interp, "CLR not loaded\n", NULL);
	}

	code = TCL_ERROR;
	goto done;
    }

#if defined(USE_CLR_40)
    /*
     * BUGFIX: Only release these interface pointers if we released the CLR
     *         runtime host (above).
     */

    if (bRelease) {
	if (pClrRuntimeInfo != NULL) {
	    ICLRRuntimeInfo_Release(pClrRuntimeInfo);
	    pClrRuntimeInfo = NULL;
	}

	if (pClrMetaHost != NULL) {
	    ICLRMetaHost_Release(pClrMetaHost);
	    pClrMetaHost = NULL;
	}
    }
#endif

done:

    /*
     * BUGFIX: If the CLR has been stopped, then the bridge cannot be
     *         running either.
     */

    if ((code == TCL_OK) && bClrBridgeStarted) {
	bClrBridgeStarted = FALSE;

	if (PACKAGE_CAN_LOG(interp, logCommand)) {
	    TclLog(interp, logCommand,
		L"WARNING: CLR was stopped with bridge running.", NULL);
	}
    }

    Tcl_MutexUnlock(&packageMutex);
    return code;
}

/*
 *----------------------------------------------------------------------
 *
 * CanExecuteClrCode --
 *
 *	This function checks if CLR code can safely be executed by this
 *	package.
 *
 * Why / How:
 *	Two-state predicate: a usable CLR requires (a) a loaded
 *	ICLRRuntimeHost interface pointer (pClrRuntimeHost != NULL),
 *	AND (b) ICLRRuntimeHost_Start to have already been called
 *	successfully (bClrStarted == TRUE).  This function packages
 *	that check into a single call so ExecuteClrMethod, the [object]
 *	command, and the bridge wiring code do not duplicate it.
 *
 *	Note: this is the .NET Framework predicate.  CoreCLR has a
 *	three-state version because CoreCLR has no separate "started"
 *	step (the runtime is implicitly started by the first managed
 *	dispatch) -- the two functions are intentionally not unified.
 *
 *	If `interp` is non-NULL, a failure leaves a human-readable
 *	diagnostic ("CLR not loaded" or "CLR not started") in the
 *	interp result so callers can surface it without composing
 *	their own error text.  Passing NULL is supported for callers
 *	that only need the boolean answer (e.g. health checks).
 *
 *	Snapshot semantics under the package mutex.  The bridge-started
 *	flag is intentionally NOT consulted here -- bridge readiness is
 *	checked separately by GetClrBridgeStarted because some embedder
 *	paths legitimately need to call into managed code BEFORE the
 *	bridge handshake completes (the bridge's own entry point being
 *	the prime example).
 *
 * Results:
 *	Non-zero if CLR code can be safely executed by this package,
 *	zero otherwise.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

BOOL CanExecuteClrCode(
    Tcl_Interp *interp)			/* Current Tcl interpreter. */
{
    BOOL bResult = FALSE;

    Tcl_MutexLock(&packageMutex);

    if (pClrRuntimeHost == NULL) {
	if (interp != NULL) {
	    Tcl_AppendResult(interp, "CLR not loaded\n", NULL);
	}

	goto done;
    }

    if (!bClrStarted) {
	if (interp != NULL) {
	    Tcl_AppendResult(interp, "CLR not started\n", NULL);
	}

	goto done;
    }

    bResult = TRUE;

done:

    Tcl_MutexUnlock(&packageMutex);
    return bResult;
}

/*
 *----------------------------------------------------------------------
 *
 * ExecuteClrMethod --
 *
 *	This function executes the specified CLR method.
 *
 * Why / How:
 *	The single channel by which all native-to-managed dispatch in
 *	the .NET Framework path flows.  The CoreCLR counterpart exposes
 *	a typed function-pointer call (coreclr_create_delegate produces
 *	a real C-callable address); the .NET Framework hosting interface
 *	does not, so we are constrained to use ICLRRuntimeHost_Execute-
 *	InDefaultAppDomain -- a method with the world's most cramped
 *	signature for a hosting API:
 *
 *	    HRESULT ExecuteInDefaultAppDomain(
 *	        LPCWSTR pwzAssemblyPath,
 *	        LPCWSTR pwzTypeName,
 *	        LPCWSTR pwzMethodName,
 *	        LPCWSTR pwzArgument,    // ONE string.  That's it.
 *	        DWORD  *pReturnValue);
 *
 *	The managed callee MUST have signature
 *	    public static int Method(string arg)
 *	-- no overloads, no extra arguments, no marshalled structs, no
 *	delegate hand-off.  Everything we want to pass through this
 *	gate has to fit inside that single LPCWSTR.
 *
 *	The "Garuda protocol" is the workaround.  When the protocol
 *	flag bits are set in methodFlags, this function builds a
 *	wide-string argument with this shape:
 *
 *	    "Garuda_v1.0_r2.0 0xHMOD 0xPSTUBS 0xINTERP 1 0 1 USERARGS"
 *	     <----name----->  <hMod>  <stubs>  <interp> <use> <safe>
 *	                                                <iso>
 *
 *	Where HMOD is the Tcl library module handle, PSTUBS is the
 *	Tcl C API stubs table address, INTERP is the Tcl_Interp
 *	pointer, and the trailing flags carry "use isolation" /
 *	"is safe interp" / "use isolation".  The managed bridge
 *	parses this string back into the components it needs.  The
 *	leading "Garuda_v1.0_rN.0" tag is the protocol revision --
 *	older (R0/legacy) methods do not include the stubs pointer,
 *	for example, so the managed side must know the revision
 *	before parsing.
 *
 *	The protocol revision flags fan out as:
 *	    bUseProtocolR1 + bUseProtocolR2 -> V1R2 (Tcl_Interp,
 *	      stubs, isolation, safe-interp)
 *	    bUseProtocolR1 + bLegacyProtocol -> V1R0 (legacy)
 *	    bUseProtocolR1 alone -> V1R1 (no stubs)
 *	    neither -> no Garuda prefix; raw user-supplied argument
 *
 *	Two more design notes worth recording:
 *
 *	1. The BUFFER LENGTH MATH at the top is precise, not slack.
 *	   It must add up to exactly the size of the gwprintf output
 *	   plus NUL.  The contributors are commented inline.  Each
 *	   formatted token contributes (sizeof(T) * 2) + 3 = 2 hex
 *	   chars per byte plus "0x" prefix plus one trailing space.
 *	   If you change a format string here you MUST adjust the
 *	   length math; gwprintf does NOT auto-grow.
 *
 *	2. The `newArgument` lifecycle is: ckalloc when the protocol
 *	   prefix is needed, point at pMethodInfo->argument otherwise.
 *	   The cleanup at `done:` checks both pointers being unequal
 *	   before freeing -- exactly because the no-prefix branch
 *	   aliases the caller's buffer rather than allocating.
 *	   Misusing the same conditional has caused at least one
 *	   double-free bug in our history; preserve it.
 *
 *	Concurrency: the entire body is under packageMutex.  Even
 *	though ICLRRuntimeHost::ExecuteInDefaultAppDomain is itself
 *	free-threaded, holding the package mutex serializes the
 *	predicate check (CanExecuteClrCode) with the dispatch -- we
 *	don't want a teardown thread to reach Stop+Release between
 *	the predicate succeeding and the actual ExecuteInDefault-
 *	AppDomain call.  The CLR side may release its own internal
 *	locks before our HRESULT returns; that's fine, our mutex
 *	guarantees only the package-side state coherence.
 *
 *	Logging is gated by both METHOD_LOG_EXECUTE in methodFlags
 *	AND PACKAGE_CAN_LOG.  This is intentionally two-layered:
 *	the flag controls whether this CALL site logs, while
 *	PACKAGE_CAN_LOG controls whether the package as a whole has
 *	logging configured.  A caller that wants to log must opt in
 *	via the flag; a caller that has no logCommand or no interp
 *	will safely no-op.
 *
 *	Note on the deprecated-but-only-API problem: ICLRRuntime-
 *	Host::ExecuteInDefaultAppDomain has been formally deprecated
 *	by Microsoft in favor of CLR Hosting v4 / ICLRStrongName +
 *	custom AppDomain managers + delegate-based dispatch.  We do
 *	not migrate to those because (a) they are even more
 *	convoluted than the protocol above, and (b) ExecuteIn-
 *	DefaultAppDomain still works on every shipping .NET Framework
 *	version and there is no signal Microsoft will remove it.
 *	The CoreCLR side has no such constraint and uses
 *	coreclr_create_delegate directly.
 *
 * Results:
 *	A standard Tcl result.
 *
 * Side effects:
 *	Since third-party code is executed during this function, there
 *	may be arbitrary side-effects.
 *
 *----------------------------------------------------------------------
 */

int ExecuteClrMethod(
    HMODULE hModule,		/* Tcl library module handle. */
    ClrTclStubs *pTclStubs,	/* Tcl C API stub function pointer table. */
    Tcl_Interp *interp,		/* Current Tcl interpreter. */
    LPCWSTR logCommand,		/* The Tcl command used to log the CLR method
				 * execution, if any. */
    ClrMethodInfo *pMethodInfo, /* Contains the information necessary for this
				 * function to execute the CLR method. */
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

    if (pMethodInfo == NULL) {
	if (interp != NULL) {
	    Tcl_AppendResult(interp, "invalid method information\n", NULL);
	}

	return TCL_ERROR;
    }

    Tcl_MutexLock(&packageMutex);

    /*
     * NOTE: If the CLR is either not loaded -OR- not started, then we cannot
     *	     use it to execute any code.
     */

    if (!CanExecuteClrCode(interp)) {
	code = TCL_ERROR;
	goto done;
    }

    bUseProtocolR1 = (methodFlags & METHOD_PROTOCOL_V1R1);
    bUseProtocolR2 = (methodFlags & METHOD_PROTOCOL_V1R2);
    bLegacyProtocol = (methodFlags & METHOD_PROTOCOL_LEGACY);
    bUseIsolation = (methodFlags & METHOD_USE_ISOLATION);
    bUseSafeInterp = (methodFlags & METHOD_USE_SAFE_INTERP);

    if ((argument != NULL) || bUseProtocolR1) {
	size_t length = 0;

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
	newArgument = (LPWSTR) attemptckalloc(length * sizeof(WCHAR));

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
		    PACKAGE_UNICODE_STR_FMT L"_" PACKAGE_UNICODE_STR_FMT L" "
		    PACKAGE_UNICODE_PTR_FMT L" " PACKAGE_UNICODE_PTR_FMT L" "
		    PACKAGE_UNICODE_PTR_FMT L" " PACKAGE_UNICODE_STR_FMT L" "
		    PACKAGE_UNICODE_STR_FMT L" " PACKAGE_UNICODE_STR_FMT L" "
		    PACKAGE_UNICODE_STR_FMT L"\0", PACKAGE_UNICODE_NAME,
		    protocolRevision, hModule, pTclStubs, interp,
		    bUseIsolation ? L"1 " : L"0 ",
		    bUseSafeInterp ? L"1 " : L"0 ",
		    (pMethodInfo->argument != NULL) ? pMethodInfo->argument :
		    L"", (argument != NULL) ? argument : L"");
	    } else {
		gwprintf(newArgument, length - GWPRINTF_LENGTH_HAS_NUL,
		    PACKAGE_UNICODE_STR_FMT L"_" PACKAGE_UNICODE_STR_FMT L" "
		    PACKAGE_UNICODE_PTR_FMT L" " PACKAGE_UNICODE_PTR_FMT L" "
		    PACKAGE_UNICODE_STR_FMT L" " PACKAGE_UNICODE_STR_FMT L" "
		    PACKAGE_UNICODE_STR_FMT L"\0", PACKAGE_UNICODE_NAME,
		    protocolRevision, hModule, interp,
		    bUseSafeInterp ? L"1 " : L"0 ",
		    (pMethodInfo->argument != NULL) ? pMethodInfo->argument :
		    L"", (argument != NULL) ? argument : L"");
	    }
	} else {
	    gwprintf(newArgument, length - GWPRINTF_LENGTH_HAS_NUL,
		PACKAGE_UNICODE_STR_FMT L" " PACKAGE_UNICODE_STR_FMT L"\0",
		(pMethodInfo->argument != NULL) ? pMethodInfo->argument : L"",
		(argument != NULL) ? argument : L"");
	}
    } else {
	newArgument = (LPWSTR) pMethodInfo->argument;
    }

    bLogExecute = (methodFlags & METHOD_LOG_EXECUTE);

    if (bLogExecute && PACKAGE_CAN_LOG(interp, logCommand)) {
	/*
	 * NOTE: Verbose mode is enabled; show all the information about the
	 *       CLR method we are about to execute.
	 */

	TclLog(interp, logCommand, L"BEFORE ",
	    L"ICLRRuntimeHost_ExecuteInDefaultAppDomain(assemblyPath = {",
	    pMethodInfo->assemblyPath, L"}, typeName = {",
	    pMethodInfo->typeName, L"}, methodName = {",
	    pMethodInfo->methodName, L"}, argument = {",
	    newArgument, L"})", NULL);
    }

    hResult = ICLRRuntimeHost_ExecuteInDefaultAppDomain(pClrRuntimeHost,
	pMethodInfo->assemblyPath, pMethodInfo->typeName,
	pMethodInfo->methodName, newArgument, &returnValue);

    if (bLogExecute && PACKAGE_CAN_LOG(interp, logCommand)) {
	WCHAR buffer[PACKAGE_RESULT_SIZE + 1] = {0};

	gwprintf(buffer, PACKAGE_RESULT_SIZE, L"AFTER "
	    L"ICLRRuntimeHost_ExecuteInDefaultAppDomain("
	    L"hResult = {0x%lX}, returnValue = {%lu})",
	    (unsigned long)hResult, (unsigned long)returnValue);

	TclLog(interp, logCommand, buffer, NULL);
    }

    if (SUCCEEDED(hResult)) {
	if (pReturnValue != NULL)
	    *pReturnValue = returnValue;
    } else {
	if (interp != NULL) {
	    Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp),
		GetClrErrorMessage(
		    L"ICLRRuntimeHost_ExecuteInDefaultAppDomain",
		    hResult), -1);
	}

	code = TCL_ERROR;
	goto done;
    }

done:

    if ((newArgument != NULL) && (newArgument != pMethodInfo->argument)) {
	ckfree((LPVOID) newArgument);
	newArgument = NULL;
    }

    Tcl_MutexUnlock(&packageMutex);
    return code;
}

/*
 *----------------------------------------------------------------------
 *
 * GetCurrentClrAppDomainId --
 *
 *	This function attempts to query the integer identifier for the
 *	current application domain of the CLR.
 *
 * Why / How:
 *	One of the few hosting-API calls with a meaningful answer
 *	on .NET Framework but no equivalent on CoreCLR.  Real
 *	AppDomains are a .NET Framework feature; CoreCLR collapsed
 *	the design into a single "default" load context with no
 *	domain numbering, so the CoreCLR side of this package has
 *	no GetCurrentCoreClrAppDomainId -- its callers either skip
 *	the question or hardcode 0.  Keeping this here makes
 *	embedder code that runs on both runtimes write
 *
 *	    #if defined(USE_CLR)
 *	        GetCurrentClrAppDomainId(&id);
 *	    #else
 *	        id = 0;
 *	    #endif
 *
 *	The return shape is HRESULT (not Tcl_OK / Tcl_ERROR) because
 *	this is the COM-layer accessor, not a Tcl-command-layer one.
 *	Callers translate it themselves.  Specific HRESULT codes:
 *	  E_POINTER  pAppDomainId == NULL.
 *	  E_NOINTERFACE  CLR not loaded yet.
 *	  HRESULT_FROM_WIN32(ERROR_SERVICE_NEVER_STARTED)
 *	    CLR loaded but Start has not yet been called.
 *	  S_OK + valid *pAppDomainId on success.
 *
 *	Snapshot semantics under the package mutex; the AppDomain ID
 *	itself is stable for the lifetime of the runtime once Started.
 *
 * Results:
 *	A standard COM result.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

HRESULT GetCurrentClrAppDomainId(
    LPDWORD pAppDomainId)	/* Upon success, will contain an integer
				 * identifier for the current application
				 * domain. */
{
    HRESULT hResult;

    Tcl_MutexLock(&packageMutex);

    if (pAppDomainId == NULL) {
	hResult = E_POINTER;
	goto done;
    }

    if (pClrRuntimeHost == NULL) {
	hResult = E_NOINTERFACE;
	goto done;
    }

    if (!bClrStarted) {
	hResult = HRESULT_FROM_WIN32(ERROR_SERVICE_NEVER_STARTED);
	goto done;
    }

    hResult = ICLRRuntimeHost_GetCurrentAppDomainId(pClrRuntimeHost,
	pAppDomainId);

done:

    Tcl_MutexUnlock(&packageMutex);
    return hResult;
}

/*
 *----------------------------------------------------------------------
 *
 * GetClrVersion --
 *
 *	This function attempts to query version information for the
 *	currently loaded CLR.
 *
 * Why / How:
 *	Returns the .NET Framework version string ("v4.0.30319",
 *	"v2.0.50727", etc).  Two implementations live behind the same
 *	signature:
 *
 *	  USE_CLR_40 (v4 path):
 *	    ICLRRuntimeInfo_GetVersionString -- the modern, version-aware
 *	    accessor.  Requires pClrRuntimeInfo to have been obtained
 *	    via the meta-host route in LoadAndStartTheClr.  Returns
 *	    E_NOINTERFACE here if the meta-host path was not used (e.g.
 *	    the CorBindToRuntimeEx fallback fired) -- there is no v2-era
 *	    way to recover the version after binding via that path.
 *
 *	  legacy (v2 path):
 *	    GetCORVersion -- the pre-CLR-4 entry point in mscoree.dll.
 *	    Same buffer protocol as the v4 version: caller passes a
 *	    pre-sized buffer and a length-in/length-out DWORD pointer.
 *
 *	Buffer protocol convention (both paths):
 *	  - On entry, *pLength is the buffer size in WCHARs.
 *	  - On success, *pLength is updated to the string length
 *	    (excluding NUL).
 *	  - If the buffer is too small, the call returns
 *	    HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER) and
 *	    *pLength is the required size -- caller can re-allocate
 *	    and retry.  This is the standard "two-call probe" pattern
 *	    common across the COM-era hosting APIs.
 *
 *	Caller must NULL-check both arguments -- E_POINTER is returned
 *	if either is missing rather than crashing.  Snapshot semantics
 *	under the package mutex.
 *
 * Results:
 *	A standard COM result.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

HRESULT GetClrVersion(
    LPWSTR pVersion,		/* Upon success, will contain a string
				 * with CLR version information. */
    LPDWORD pLength)		/* Upon entry, the length of the state
				 * buffer.  Upon success, will contain
				 * the length of the resulting string. */
{
    HRESULT hResult;

    Tcl_MutexLock(&packageMutex);

    if ((pVersion == NULL) || (pLength == NULL)) {
	hResult = E_POINTER;
	goto done;
    }

#if defined(USE_CLR_40)
    if (pClrRuntimeInfo == NULL) {
	hResult = E_NOINTERFACE;
	goto done;
    }

    hResult = ICLRRuntimeInfo_GetVersionString(pClrRuntimeInfo,
	pVersion, pLength);
#else
    hResult = GetCORVersion(pVersion, *pLength, pLength);
#endif

done:

    Tcl_MutexUnlock(&packageMutex);
    return hResult;
}

/*
 *----------------------------------------------------------------------
 *
 * DumpClrState --
 *
 *	This function attempts to debugging information for this
 *	package.  Generally, this information is only useful for
 *	advanced troubleshooting.
 *
 * Why / How:
 *	Returns a single wide-string snapshot of every package-level
 *	piece of state that has ever been useful when debugging a
 *	Garuda load failure.  The format is space-separated key/value
 *	pairs (parseable by Tcl's [list] machinery on the script side):
 *
 *	    packageMutex 0xPTR
 *	    hPackageModule 0xPTR
 *	    packageFileName {...}
 *	    lTclStubs N
 *	    hTclModule 0xPTR
 *	    pTclStubs 0xPTR
 *	    pClrMetaHost 0xPTR        (only USE_CLR_40)
 *	    pClrRuntimeInfo 0xPTR     (only USE_CLR_40)
 *	    pClrRuntimeHost 0xPTR
 *	    bClrStarted 0|1
 *	    bClrBridgeStarted 0|1
 *
 *	The order is fixed and reflects the rough lifecycle order in
 *	which the values become valid: mutex (always), package module
 *	(always), Tcl stubs handle (after Tcl_InitStubs), CLR meta-host
 *	and runtime-info (after the v4 meta-host path), runtime host
 *	(after either v4 or fallback path), then the started/bridge-
 *	started booleans.  Reading the dump top-to-bottom gives you a
 *	trace of how far through startup the package made it before
 *	whatever you're debugging happened.
 *
 *	Pointers are formatted via PACKAGE_UNICODE_PTR_FMT ("0x%p" with
 *	the right width prefix on 32 vs 64-bit builds) so the output
 *	is unambiguously addressable.  This is intentionally raw --
 *	the cooked, human-readable version lives at the script layer
 *	in lib/helper.tcl.
 *
 *	Buffer protocol same as GetClrVersion: caller passes a
 *	pre-sized buffer plus length-in/length-out, gets back a
 *	formatted string.  Unlike GetClrVersion, however, this
 *	function does NOT distinguish "buffer too small" from "ran
 *	out of state to dump" -- gwprintf truncates silently.  In
 *	practice the buffer is sized to PACKAGE_RESULT_SIZE which
 *	easily holds the dump on every supported platform.  If you
 *	add new state fields, audit the call sites' buffer sizes.
 *
 *	Snapshot semantics under the package mutex.  Note that all
 *	the pointers and booleans this dumps are themselves protected
 *	by packageMutex, so the dump is internally consistent -- you
 *	will not see, for example, bClrStarted=1 with pClrRuntimeHost=
 *	NULL even if a teardown is racing this call.
 *
 * Results:
 *	A standard COM result.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

HRESULT DumpClrState(
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

    Tcl_MutexLock(&packageMutex);

    if ((pState == NULL) || (pLength == NULL)) {
	hResult = E_POINTER;
	goto done;
    }

    gwprintf(pState, *pLength, L"packageMutex "
	PACKAGE_UNICODE_PTR_FMT L" hPackageModule "
	PACKAGE_UNICODE_PTR_FMT L" packageFileName {"
	PACKAGE_UNICODE_STR_FMT L"} lTclStubs %ld hTclModule "
	PACKAGE_UNICODE_PTR_FMT L" pTclStubs "
	PACKAGE_UNICODE_PTR_FMT
#if defined(USE_CLR_40)
	L" pClrMetaHost " PACKAGE_UNICODE_PTR_FMT
	L" pClrRuntimeInfo " PACKAGE_UNICODE_PTR_FMT
#endif
	L" pClrRuntimeHost " PACKAGE_UNICODE_PTR_FMT
	L" bClrStarted %d bClrBridgeStarted %d", packageMutex,
	GetPackageModule(), fileName, lTclStubs, hTclModule,
	pTclStubs,
#if defined(USE_CLR_40)
	pClrMetaHost, pClrRuntimeInfo,
#endif
	pClrRuntimeHost,
	bClrStarted, bClrBridgeStarted);

    hResult = S_OK;

done:

    Tcl_MutexUnlock(&packageMutex);
    return hResult;
}
#endif /* !defined(USE_CORE_CLR) */
