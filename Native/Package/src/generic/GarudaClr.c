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
 *	This function returns a boolean value that indicates whether
 *	or not the CLR has been loaded.
 *
 * Results:
 *	Non-zero if the CLR has been loaded; otherwise, zero.
 *
 * Side effects:
 *	None.
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
 *	This function returns a boolean value that indicates whether
 *	or not the CLR has been started.
 *
 * Results:
 *	Non-zero if the CLR has been started; otherwise, zero.
 *
 * Side effects:
 *	None.
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
 *	This function returns a boolean value that indicates whether
 *	or not the bridge between Tcl and the CLR has been started.
 *
 * Results:
 *	Non-zero if the bridge has been started; otherwise, zero.
 *
 * Side effects:
 *	None.
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
 *	This function sets a boolean value that indicates whether or
 *	not the bridge between Tcl and the CLR has been started.
 *
 * Results:
 *	None.
 *
 * Side effects:
 *	None.
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
