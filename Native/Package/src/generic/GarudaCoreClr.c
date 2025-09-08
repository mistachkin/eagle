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
 *	This function returns a boolean value that indicates whether
 *	or not the CoreCLR has been loaded.
 *
 * Results:
 *	Non-zero if the CoreCLR has been loaded; otherwise, zero.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

BOOL GetCoreClrWasLoaded(void)
{
    BOOL bResult;

#if defined(_WIN32)
    Wrp_MutexLock(&packageMutex);
#endif

    bResult = (pCoreClrModule != NULL);

#if defined(_WIN32)
    Wrp_MutexUnlock(&packageMutex);
#endif

    return bResult;
}

/*
 *----------------------------------------------------------------------
 *
 * GetCoreClrWasStarted --
 *
 *	This function returns a boolean value that indicates whether
 *	or not the CoreCLR has been started.
 *
 * Results:
 *	Non-zero if the CoreCLR has been started; otherwise, zero.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

BOOL GetCoreClrWasStarted(void)
{
    BOOL bResult;

#if defined(_WIN32)
    Wrp_MutexLock(&packageMutex);
#endif

    bResult = (pCoreClrContext != NULL);

#if defined(_WIN32)
    Wrp_MutexUnlock(&packageMutex);
#endif

    return bResult;
}

/*
 *----------------------------------------------------------------------
 *
 * GetCoreClrBridgeStarted --
 *
 *	This function returns a boolean value that indicates whether
 *	or not the bridge between Tcl and the CoreCLR has been started.
 *
 * Results:
 *	Non-zero if the bridge has been started; otherwise, zero.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

BOOL GetCoreClrBridgeStarted(void)
{
    BOOL bResult;

#if defined(_WIN32)
    Wrp_MutexLock(&packageMutex);
#endif

    bResult = bCoreClrBridgeStarted;

#if defined(_WIN32)
    Wrp_MutexUnlock(&packageMutex);
#endif

    return bResult;
}

/*
 *----------------------------------------------------------------------
 *
 * SetCoreClrBridgeStarted --
 *
 *	This function sets a boolean value that indicates whether or
 *	not the bridge between Tcl and the CoreCLR has been started.
 *
 * Results:
 *	None.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

void SetCoreClrBridgeStarted(
    BOOL bStarted)	    /* Non-zero if the bridge was started. */
{
#if defined(_WIN32)
    Wrp_MutexLock(&packageMutex);
#endif

    bCoreClrBridgeStarted = bStarted;

#if defined(_WIN32)
    Wrp_MutexUnlock(&packageMutex);
#endif
}

/*
 *----------------------------------------------------------------------
 *
 * LoadAndStartTheCoreClr --
 *
 *	This function loads and optionally starts the latest version of
 *	the CoreCLR supported by this package.
 *
 * Results:
 *	A standard Tcl result.
 *
 * Side effects:
 *	Since the CoreCLR may execute startup code, this function may
 *	have arbitrary side-effects.
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
	    uFunctions.pGetDotNetEnvInfo =
		(hostfxr_get_dotnet_environment_info_fn)GetProcAddress(
		hModule, "hostfxr_get_dotnet_environment_info");

	    uFunctions.pInitForRuntimeConfig =
		(hostfxr_initialize_for_runtime_config_fn)GetProcAddress(
		hModule, "hostfxr_initialize_for_runtime_config");

	    uFunctions.pGetRuntimeDelegate =
		(hostfxr_get_runtime_delegate_fn)GetProcAddress(
		hModule, "hostfxr_get_runtime_delegate");

	    uFunctions.pClose = (hostfxr_close_fn)GetProcAddress(
		hModule, "hostfxr_close");
#else
	    uFunctions.pGetDotNetEnvInfo =
		(hostfxr_get_dotnet_environment_info_fn)dlsym(
		    hModule, "hostfxr_get_dotnet_environment_info");

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

	    if ((rc != 0) || (pContext == NULL)) {
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
 *	This function stops and releases the CoreCLR.
 *
 * Results:
 *	A standard Tcl result.
 *
 * Side effects:
 *	Since the CoreCLR may execute cleanup code, this function may have
 *	arbitrary side-effects.
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
 *	This function checks if CoreCLR code can safely be executed by this
 *	package.
 *
 * Results:
 *	Non-zero if CoreCLR code can be safely executed by this package,
 *	zero otherwise.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

BOOL CanExecuteCoreClrCode(
    Tcl_Interp *interp)			/* Current Tcl interpreter. */
{
    BOOL bResult = FALSE;

#if defined(_WIN32)
    Wrp_MutexLock(&packageMutex);
#endif

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

#if defined(_WIN32)
    Wrp_MutexUnlock(&packageMutex);
#endif

    return bResult;
}

/*
 *----------------------------------------------------------------------
 *
 * ExecuteCoreClrMethod --
 *
 *	This function executes the specified CoreCLR method.
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
 *	This function attempts to query the integer identifier for the
 *	current application domain of the CoreCLR.
 *
 * Results:
 *	A standard COM result.
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

/*
 *----------------------------------------------------------------------
 *
 * GetCoreClrVersionCallback --
 *
 *	This function handles the version information callbacks from
 *	the currently loaded CoreCLR.  The resulting information for
 *	the user command is stored in the context structure.
 *
 * Results:
 *	None.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static void HOSTFXR_CALLTYPE GetCoreClrVersionCallback(
    const struct hostfxr_dotnet_environment_info *info,
    void *context)
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

/*
 *----------------------------------------------------------------------
 *
 * GetCoreClrVersion --
 *
 *	This function attempts to query version information for the
 *	currently loaded CoreCLR.
 *
 * Results:
 *	A standard COM result.
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
}

/*
 *----------------------------------------------------------------------
 *
 * DumpCoreClrState --
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
