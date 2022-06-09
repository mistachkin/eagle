/*
 * Garuda.c -- Eagle Package for Tcl (Garuda)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#include <stdio.h>	/* NOTE: For fprintf, swprintf, va_list, etc. */
#include <string.h>	/* NOTE: For memset, wcslen, wcsncpy, etc. */

#include "GarudaPre.h"	/* NOTE: For private header setup. */
#include "MSCorEE.h"	/* NOTE: For native CLR v2 API. */

#if defined(USE_CLR_40)
#include "MetaHost.h"	/* NOTE: For native CLR v4 API. */
#endif

#include "tcl.h"	/* NOTE: For public Tcl API. */
#include "tclInt.h"	/* HACK: For internal Tcl API. */
#include "stubs.h"	/* NOTE: #define and #pragma magic for stubs. */
#include "pkgVersion.h"	/* NOTE: Package version information. */
#include "GarudaInt.h"	/* NOTE: For private package API. */
#include "Garuda.h"	/* NOTE: For public package API. */

/*
 * NOTE: Private functions defined in this file that are only included when
 *       the private Tcl stubs mechanism is enabled at compile-time.
 */

#if defined(USE_TCL_PRIVATE_STUBS)
static const char *	initTclStubs(Tcl_Interp *interp, const char *version,
			    int exact);
#endif

/*
 * NOTE: Private functions defined in this file.
 */

static BOOL		GetPackageModuleFileName(HMODULE hModule,
			    LPWSTR *pFileName);
static BOOL		SetClrTclStubs(ClrTclStubs *pTclStubs, BOOL bTip285,
			    BOOL bTip335, BOOL bTip336);
static int		TracePrintf(LPCSTR format, ...);
static LPCWSTR		GetTclErrorMessage(LPCWSTR source, int code);
static LPCWSTR		GetClrErrorMessage(LPCWSTR source, HRESULT hResult);
static void		TclLog(Tcl_Interp *interp, LPCWSTR logCommand, ...);
static LPCWSTR		GetResultValue(Tcl_Interp *interp);
static LPWSTR		GetStringObjectValue(Tcl_Interp *interp,
			    Tcl_Obj *objPtr, int *lengthPtr);
static LPWSTR		GetStringVariableValue(Tcl_Interp *interp,
			    LPCWSTR varName, int *lengthPtr);
static BOOL		GetBooleanVariableValue(Tcl_Interp *interp,
			    LPCWSTR varName, BOOL defValue);
static int		GetIntegerVariableValue(Tcl_Interp *interp,
			    LPCWSTR varName, int defValue);
static int		CreateClrMethodInfo(Tcl_Interp *interp,
			    Tcl_Obj *assemblyPathPtr, Tcl_Obj *typeNamePtr,
			    Tcl_Obj *methodNamePtr, Tcl_Obj *argumentPtr,
			    ClrMethodInfo **ppMethodInfo);
static int		GetClrMethodInfo(Tcl_Interp *interp,
			    MethodFlags methodFlags,
			    ClrMethodInfo **ppMethodInfo);
static void		FreeClrMethodInfo(ClrMethodInfo **ppMethodInfo);
static int		GetClrConfigInfo(Tcl_Interp *interp, BOOL bForLogOnly,
			    BOOL bMethods, ClrConfigInfo **ppConfigInfo);
static void		FreeClrConfigInfo(ClrConfigInfo **ppConfigInfo);
static int		LoadAndStartTheClr(Tcl_Interp *interp,
			    LPCWSTR logCommand, BOOL bLoad,
			    BOOL bUseMinimumClr, BOOL bStart, BOOL bStrict);
static int		StopAndReleaseTheClr(Tcl_Interp *interp,
			    LPCWSTR logCommand, BOOL bRelease, BOOL bStrict);
static BOOL		CanExecuteClrCode(Tcl_Interp *interp);
static int		ExecuteClrMethod(HANDLE hModule,
			    ClrTclStubs *pTclStubs, Tcl_Interp *interp,
			    LPCWSTR logCommand, ClrMethodInfo *pMethodInfo,
			    LPCWSTR argument, MethodFlags methodFlags,
			    LPDWORD pReturnValue);
static void		MaybeCombineMethodFlags(ClrConfigInfo *pConfigInfo,
			    MethodFlags *pMethodFlags);
static int		GetAndExecuteClrMethod(HANDLE hModule,
			    ClrTclStubs *pTclStubs, ClrConfigInfo *pConfigInfo,
			    Tcl_Interp *interp, LPCWSTR argument,
			    MethodFlags methodFlags);
static int		DemandExecuteClrMethod(HANDLE hModule,
			    ClrTclStubs *pTclStubs, ClrConfigInfo *pConfigInfo,
			    Tcl_Interp *interp, Tcl_Obj *assemblyPathPtr,
			    Tcl_Obj *typeNamePtr, Tcl_Obj *methodNamePtr,
			    Tcl_Obj *argumentPtr, MethodFlags methodFlags,
			    LPDWORD pReturnValue);
static void		GarudaExitProc(ClientData clientData);
static int		GarudaObjCmd(ClientData clientData, Tcl_Interp *interp,
			    int objc, Tcl_Obj *CONST objv[]);
static void		GarudaObjCmdDeleteProc(ClientData clientData);

/*
 * NOTE: These are the private Tcl stubs pointers.  They are only included
 *       when the private Tcl stubs mechanism is enabled at compile-time.
 */

#if defined(USE_TCL_PRIVATE_STUBS)
static const TclStubs *tclStubsPtr = NULL;
static const TclPlatStubs *tclPlatStubsPtr = NULL;
static const TclIntStubs *tclIntStubsPtr = NULL;
static const TclIntPlatStubs *tclIntPlatStubsPtr = NULL;
#endif

/*
 * NOTE: This package is thread-safe and this mutex is used to protect access
 *       to the static state defined in this file.
 */

TCL_DECLARE_MUTEX(packageMutex);

/*
 * NOTE: The package module handle.  This is needed to obtain the full path to
 *       the package module file name.
 */

static HANDLE hPackageModule = NULL;

/*
 * NOTE: The package module file name.  The value stored here is obtained from
 *       the GetModuleFileName Win32 API.  This value is backed by dynamic
 *       storage obtained from the attemptckalloc Tcl API and will be freed
 *       prior this package being unloaded.
 */

static LPWSTR packageFileName;

/*
 * NOTE: Has the Tcl stubs mechanism been initialized properly?  If non-zero,
 *       the Tcl API is available; otherwise, it is not.  This is logically a
 *       boolean value; however, it is declared as LONG here so that the Win32
 *       interlocked API functions can be used with it.
 */

static LONG lTclStubs = 0;

/*
 * NOTE: The Tcl library module handle.  This is needed to pass to the bridge
 *       so that it can be used as the basis for looking up functions exported
 *       from the [already] loaded Tcl library.
 */

static HANDLE hTclModule = NULL;

/*
 * NOTE: The Tcl C API function pointers required by the Eagle native Tcl
 *       integration subsystem.  This is needed to pass to the bridge so that
 *       it can be used as the basis for calling the functions exported from
 *       the [already] loaded Tcl library.
 */

static ClrTclStubs uTclStubs = { 0 };

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

static BOOL bClrStarted = FALSE;

/*
 * NOTE: This variable will be TRUE if the bridge was successfully started and
 *       has not been shutdown yet.
 */

static BOOL bBridgeStarted = FALSE;

/*
 *----------------------------------------------------------------------
 *
 * SetPackageModule --
 *
 *	This function sets the saved package module handle to the
 *	specified value.
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
    HANDLE hModule)		/* The new package module handle. */
{
    /*
     * BUGBUG: This function is called via DllMain; therefore, we must
     *         not use any locking primitives here.  Also, the package
     *         mutex cannot be created and initialized until Tcl stubs
     *         are available.
     */

    hPackageModule = hModule;
}

#if defined(USE_TCL_PRIVATE_STUBS)
/*
 *----------------------------------------------------------------------
 *
 * initTclStubs --
 *
 *	This function initializes the Tcl stubs mechanism for the Tcl
 *	interpreter without needing to be linked to the real Tcl stubs
 *	library.  The code for this function was copied from the Fossil
 *	source code file "th_tcl.c" and was originally written by Jan
 *	Nijtmans and has been heavily modified for use by this project.
 *
 * Results:
 *	The actual version of Tcl satisfying the request -OR- NULL if
 *	the Tcl version is not acceptable, does not support stubs, or
 *	any other error condition occurred.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static const char *initTclStubs(
    Tcl_Interp *interp,			/* Current Tcl interpreter. */
    const char *version,		/* The Tcl version string. */
    int exact)				/* Non-zero, exact version only. */
{
    const char *actualVersion;

    tclStubsPtr = ((PrivateTclInterp *)interp)->stubTable;
    if ((tclStubsPtr == NULL) || (tclStubsPtr->magic != TCL_STUB_MAGIC)) {
	PACKAGE_TRACE((
	    "initTclStubs: could not initialize: incompatible mechanism\n"));

	return NULL;
    }

    /* NOTE: At this point, the Tcl API functions should be available. */
    actualVersion = Tcl_PkgRequireEx(interp, "Tcl", version, exact,
	(void *)&tclStubsPtr);

    if (actualVersion == NULL) {
	PACKAGE_TRACE((
	    "initTclStubs: could not initialize: incompatible version\n"));

	return NULL;
    }

    if (tclStubsPtr->hooks != NULL) {
	tclPlatStubsPtr = tclStubsPtr->hooks->tclPlatStubs;
	tclIntStubsPtr = tclStubsPtr->hooks->tclIntStubs;
	tclIntPlatStubsPtr = tclStubsPtr->hooks->tclIntPlatStubs;
    } else {
	tclPlatStubsPtr = NULL;
	tclIntStubsPtr = NULL;
	tclIntPlatStubsPtr = NULL;
    }

    return actualVersion;
}
#endif

/*
 *----------------------------------------------------------------------
 *
 * GetPackageModuleFileName --
 *
 *	This function attempts to query the package module file name
 *	from the operating system.  The resulting module file name is
 *	stored into a buffer allocated via the attemptckalloc Tcl API.
 *	Upon success, the pointer to module file name buffer is stored
 *	into the pointer provided by the caller.  This function uses no
 *	global state and assumes any required locks are already held by
 *	the caller.
 *
 * Results:
 *	None.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static BOOL GetPackageModuleFileName(
    HMODULE hModule,		/* The module handle. */
    LPWSTR *pFileName)		/* Pointer to the file name buffer. */
{
    DWORD size;
    LPWSTR result[2];

    /*
     * NOTE: The file name output pointer is required and must be valid.  It
     *       is set to the location of the file name buffer on success -OR-
     *       NULL upon any [other] failure.
     */

    if (pFileName == NULL)
	return FALSE;

    /*
     * HACK: The GetModuleFileName Win32 API has no clean way to report the
     *       exact size without guesswork.  Therefore, start off by using the
     *       maximum possible size for a WinNT file name.  If allocating this
     *       buffer fails, we cannot continue.
     */

    size = UNICODE_STRING_MAX_CHARS;
    result[0] = (LPWSTR) attemptckalloc((size + 1) * sizeof(WCHAR));

    if (result[0] == NULL) {
	*pFileName = NULL;
	return FALSE;
    }

    /*
     * NOTE: Zero the newly allocated file name buffer and then call into the
     *       GetModuleFileName Win32 API to obtain the module file name.  This
     *       call is almost be guaranteed to succeed because the file name
     *       string length cannot exceed 32767 characters when running on the
     *       WinNT kernel.  If this call fails, we cannot continue; however, we
     *       must free the allocated file name buffer before returning.
     */

    memset(result[0], 0, (size + 1) * sizeof(WCHAR));
    size = GetModuleFileNameW(hModule, result[0], size); /* NON-PORTABLE */

    if (size == 0) {
	ckfree((LPVOID) result[0]);
	*pFileName = NULL;
	return FALSE;
    }

    /*
     * NOTE: The module file name was obtained successfully.  Now, attempt to
     *       allocate a new buffer of exactly the needed size.  If this fails,
     *       free the previously allocated file name buffer and return failure
     *       to the caller.
     */

    result[1] = (LPWSTR) attemptckalloc((size + 1) * sizeof(WCHAR));

    if (result[1] == NULL) {
	ckfree((LPVOID) result[0]);
	*pFileName = NULL;
	return FALSE;
    }

    /*
     * NOTE: Success, copy exactly the number of bytes necessary to store the
     *       file name (including the terminating NUL character) from the
     *       originally allocated file name buffer.  Free the original file
     *       name buffer.  Finally, return success to the caller.
     */

    memcpy(result[1], result[0], (size + 1) * sizeof(WCHAR));

    ckfree((LPVOID) result[0]);
    *pFileName = result[1];
    return TRUE;
}

/*
 *----------------------------------------------------------------------
 *
 * SetClrTclStubs --
 *
 *	This function sets the Tcl C API function pointers contained
 *	within the passed structure so they point to the functions
 *	contained in the Tcl module currently in use.
 *
 * Results:
 *	Non-zero for success; zero otherwise.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static BOOL SetClrTclStubs(
    ClrTclStubs *pTclStubs,	/* Upon success, the pointed to structure
				 * will contain the Tcl C API stub function
				 * pointers. */
    BOOL bTip285,		/* Set the TIP #285 functions pointers as
				 * well? */
    BOOL bTip335,		/* Set the TIP #335 functions pointers as
				 * well? */
    BOOL bTip336)		/* Set the TIP #336 functions pointers as
				 * well? */
{
    if (pTclStubs == NULL) {
	PACKAGE_TRACE(("SetClrTclStubs: pointer argument is invalid\n"));
	return FALSE;
    }

    if (tclStubsPtr == NULL) {
	PACKAGE_TRACE((
	    "SetClrTclStubs: Tcl public stubs pointer is invalid\n"));

	return FALSE;
    }

    if (bTip285 && (tclIntStubsPtr == NULL)) {
	PACKAGE_TRACE((
	    "SetClrTclStubs: Tcl internal stubs pointer is invalid\n"));

	return FALSE;
    }

    pTclStubs->tcl_GetVersion = tclStubsPtr->tcl_GetVersion;
    pTclStubs->tcl_FindExecutable = tclStubsPtr->tcl_FindExecutable;
    pTclStubs->tcl_CreateInterp = tclStubsPtr->tcl_CreateInterp;
    pTclStubs->tcl_Preserve = tclStubsPtr->tcl_Preserve;
    pTclStubs->tcl_Release = tclStubsPtr->tcl_Release;
    pTclStubs->tcl_ObjGetVar2 = tclStubsPtr->tcl_ObjGetVar2;
    pTclStubs->tcl_ObjSetVar2 = tclStubsPtr->tcl_ObjSetVar2;
    pTclStubs->tcl_UnsetVar2 = tclStubsPtr->tcl_UnsetVar2;
    pTclStubs->tcl_Init = tclStubsPtr->tcl_Init;
    pTclStubs->tcl_InitMemory = tclStubsPtr->tcl_InitMemory;
    pTclStubs->tcl_MakeSafe = tclStubsPtr->tcl_MakeSafe;
    pTclStubs->tcl_GetObjType = tclStubsPtr->tcl_GetObjType;
    pTclStubs->tcl_AppendAllObjTypes = tclStubsPtr->tcl_AppendAllObjTypes;
    pTclStubs->tcl_ConvertToType = tclStubsPtr->tcl_ConvertToType;
    pTclStubs->tcl_CreateObjCommand = tclStubsPtr->tcl_CreateObjCommand;
    pTclStubs->tcl_DeleteCommandFromToken = tclStubsPtr->tcl_DeleteCommandFromToken;
    pTclStubs->tcl_DeleteInterp = tclStubsPtr->tcl_DeleteInterp;
    pTclStubs->tcl_InterpDeleted = tclStubsPtr->tcl_InterpDeleted;

    if (bTip335) {
	pTclStubs->tcl_InterpActive = tclStubsPtr->tcl_InterpActive;
    }

    if (bTip336) {
	pTclStubs->tcl_GetErrorLine = tclStubsPtr->tcl_GetErrorLine;
	pTclStubs->tcl_SetErrorLine = tclStubsPtr->tcl_SetErrorLine;
    }

    pTclStubs->tcl_NewObj = tclStubsPtr->tcl_NewObj;
    pTclStubs->tcl_NewUnicodeObj = tclStubsPtr->tcl_NewUnicodeObj;
    pTclStubs->tcl_NewStringObj = tclStubsPtr->tcl_NewStringObj;
    pTclStubs->tcl_NewByteArrayObj = tclStubsPtr->tcl_NewByteArrayObj;
    pTclStubs->tcl_DbIncrRefCount = tclStubsPtr->tcl_DbIncrRefCount;
    pTclStubs->tcl_DbDecrRefCount = tclStubsPtr->tcl_DbDecrRefCount;
    pTclStubs->tcl_CommandComplete = tclStubsPtr->tcl_CommandComplete;
    pTclStubs->tcl_AllowExceptions = tclStubsPtr->tcl_AllowExceptions;
    pTclStubs->tcl_EvalObjEx = tclStubsPtr->tcl_EvalObjEx;
    pTclStubs->tcl_EvalFile = tclStubsPtr->tcl_EvalFile;
    pTclStubs->tcl_RecordAndEvalObj = tclStubsPtr->tcl_RecordAndEvalObj;
    pTclStubs->tcl_ExprObj = tclStubsPtr->tcl_ExprObj;
    pTclStubs->tcl_SubstObj = tclStubsPtr->tcl_SubstObj;

    if (bTip285) {
	pTclStubs->tcl_CancelEval = tclStubsPtr->tcl_CancelEval;
	pTclStubs->tcl_Canceled = tclStubsPtr->tcl_Canceled;
	pTclStubs->tclResetCancellation = tclIntStubsPtr->tclResetCancellation;
	pTclStubs->tclSetInterpCancelFlags = tclIntStubsPtr->tclSetSlaveCancelFlags;
    }

    pTclStubs->tcl_DoOneEvent = tclStubsPtr->tcl_DoOneEvent;
    pTclStubs->tcl_ResetResult = tclStubsPtr->tcl_ResetResult;
    pTclStubs->tcl_GetObjResult = tclStubsPtr->tcl_GetObjResult;
    pTclStubs->tcl_SetObjResult = tclStubsPtr->tcl_SetObjResult;
    pTclStubs->tcl_GetUnicodeFromObj = tclStubsPtr->tcl_GetUnicodeFromObj;
    pTclStubs->tcl_GetStringFromObj = tclStubsPtr->tcl_GetStringFromObj;
    pTclStubs->tcl_CreateExitHandler = tclStubsPtr->tcl_CreateExitHandler;
    pTclStubs->tcl_DeleteExitHandler = tclStubsPtr->tcl_DeleteExitHandler;
    pTclStubs->tcl_FinalizeThread = tclStubsPtr->tcl_FinalizeThread;
    pTclStubs->tcl_Finalize = tclStubsPtr->tcl_Finalize;

    return TRUE;
}

/*
 *----------------------------------------------------------------------
 *
 * TracePrintf --
 *
 *	This function sends a printf-style formatted trace message to
 *	the connected Win32 debugger, if any.
 *
 * Results:
 *	The number of characters written or -1 if the trace output was
 *	truncated.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static int TracePrintf(
    LPCSTR format,		/* The "printf-style" format string. */
    ...)			/* The extra arguments, if any. */
{
    va_list argList;
    char buffer[PACKAGE_TRACE_BUFFER_SIZE + 1] = {0};
    int result;

    va_start(argList, format);

    result = gsnprintf(buffer,
	PACKAGE_TRACE_BUFFER_SIZE, format, argList);

    va_end(argList);

#if defined(_WIN32)
    OutputDebugStringA(buffer); /* NON-PORTABLE */
#else
    fprintf(stderr, "%s", buffer);
#endif

    return result;
}

/*
 *----------------------------------------------------------------------
 *
 * GetTclErrorMessage --
 *
 *	This function accepts a Tcl return code (e.g. TCL_ERROR) and
 *	creates an appropriate error message as a Unicode string.
 *
 * Results:
 *	An error message string (Unicode) based on the specified Tcl
 *	return code.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static LPCWSTR GetTclErrorMessage(
    LPCWSTR source,	/* The original source of the failure,
			 * NULL if unknown or unavailable. */
    int code)		/* The Tcl return code. */
{
    static WCHAR message[PACKAGE_RESULT_SIZE + 1] = {0};
    LPCWSTR severity = (code == TCL_OK) ? L"success" : L"failure";

    if (source != NULL) {
	gwprintf(message, PACKAGE_RESULT_SIZE, L"%s: %s (code %d).\n",
	    source, severity, code);
    } else {
	gwprintf(message, PACKAGE_RESULT_SIZE, L"%s (code %d).\n",
	    severity, code);
    }

    return message;
}

/*
 *----------------------------------------------------------------------
 *
 * GetClrErrorMessage --
 *
 *	This function accepts a CLR error code (i.e. an HRESULT) and
 *	creates an appropriate error message as a Unicode string.
 *
 * Results:
 *	An error message string (Unicode) based on the specified CLR
 *	error code.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static LPCWSTR GetClrErrorMessage(
    LPCWSTR source,		/* The original source of the failure,
				 * NULL if unknown or unavailable. */
    HRESULT hResult)		/* The CLR error code. */
{
    static WCHAR message[PACKAGE_RESULT_SIZE + 1] = {0};
    LPCWSTR severity = SUCCEEDED(hResult) ? L"success" : L"failure";

    if (source != NULL) {
	gwprintf(message, PACKAGE_RESULT_SIZE, L"%s: %s (code 0x%lX).\n",
	    source, severity, hResult);
    } else {
	gwprintf(message, PACKAGE_RESULT_SIZE, L"%s (code 0x%lX).\n",
	    severity, hResult);
    }

    return message;
}

/*
 *----------------------------------------------------------------------
 *
 * TclLog --
 *
 *	This function uses the specified Tcl command (e.g. "tclLog") to
 *	log a warning, error, or informational message in a way that
 *	can be easily overridden by a Tcl script.  All failures are
 *	simply ignored.  If the supplied Tcl interpreter or log command
 *	is NULL, the function does nothing.
 *
 * Results:
 *	None.
 *
 * Side effects:
 *	Since a Tcl command will be evaluated, this function may have
 *	arbitrary side-effects; however, in practice it SHOULD simply
 *	log a message to a file or one of the standard channels.
 *
 *----------------------------------------------------------------------
 */

static void TclLog(
    Tcl_Interp *interp,	/* Current Tcl interpreter. */
    LPCWSTR logCommand,	/* Tcl log command to evaluate. */
    ...)		/* Strings to append to the message, if any. */
{
    va_list argList;
    Tcl_Obj *objv[2] = { NULL, NULL };
    Tcl_SavedResult savedResult;
    int code;

    if ((interp == NULL) || (logCommand == NULL))
	return;

    objv[0] = Tcl_NewUnicodeObj(logCommand, -1);

    if (objv[0] == NULL)
	goto done;

    Tcl_IncrRefCount(objv[0]);

    objv[1] = Tcl_NewObj();

    if (objv[1] == NULL)
	goto done;

    Tcl_IncrRefCount(objv[1]);

    va_start(argList, logCommand);

    while (1) {
	LPCWSTR arg = va_arg(argList, LPCWSTR);
	if (arg == NULL) {
	    break;
	}
	Tcl_AppendUnicodeToObj(objv[1], arg, -1);
    }

    va_end(argList);

    Tcl_SaveResult(interp, &savedResult);
    code = Tcl_EvalObjv(interp, 2, objv, TCL_EVAL_GLOBAL);
    Tcl_RestoreResult(interp, &savedResult);

    if (code == TCL_OK) {
	LPCWSTR args;

	Tcl_AppendUnicodeToObj(objv[1], L"\n", -1);
	args = Tcl_GetUnicode(objv[1]);

#if defined(_WIN32)
	OutputDebugStringW(args); /* NON-PORTABLE */
#else
	fwprintf(stderr, L"%s", args);
#endif
    }

done:
    if (objv[1] != NULL) {
	Tcl_DecrRefCount(objv[1]);
	objv[1] = NULL;
    }

    if (objv[0] != NULL) {
	Tcl_DecrRefCount(objv[0]);
	objv[0] = NULL;
    }
}

/*
 *----------------------------------------------------------------------
 *
 * GetResultValue --
 *
 *	This function returns the value of the Tcl interpreter result
 *	as a Unicode string.  The result must not be freed because the
 *	underlying storage belongs to the Tcl object manager.
 *
 * Results:
 *	The value of the Tcl interpreter result as a Unicode string or
 *	NULL if the Tcl interpreter is NULL.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static LPCWSTR GetResultValue(
    Tcl_Interp *interp)	/* Current Tcl interpreter. */
{
    if (interp == NULL)
	return NULL;

    return Tcl_GetUnicode(Tcl_GetObjResult(interp));
}

/*
 *----------------------------------------------------------------------
 *
 * GetStringObjectValue --
 *
 *	This function returns the value of the specified Tcl object
 *	as a Unicode string.
 *
 * Results:
 *	The value of the Tcl object as a Unicode string or NULL if
 *	any of the arguments are NULL or if the Tcl object cannot be
 *	converted to a string.  The return value, if not NULL, must be
 *	freed by the caller via the ckfree Tcl API.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static LPWSTR GetStringObjectValue(
    Tcl_Interp *interp,	/* Current Tcl interpreter. */
    Tcl_Obj *objPtr,	/* The Tcl object to extract the string from. */
    int *lengthPtr)	/* If non-NULL, the location where the
			 * string rep's unichar length should be
			 * stored.  If NULL, no length is stored. */
{
    int length = 0;
    LPCWSTR objValue;
    LPWSTR result = NULL;

    if (interp == NULL)
	return NULL;

    objValue = Tcl_GetUnicodeFromObj(objPtr, &length);

    if ((objValue == NULL) || (length < 0)) {
	Tcl_AppendResult(interp, "object value is invalid\n", NULL);
	goto done;
    }

    result = (LPWSTR) attemptckalloc((length + 1) * sizeof(WCHAR));

    if (result == NULL) {
	Tcl_AppendResult(interp, "out of memory: objValue\n", NULL);
	goto done;
    }

    memset(result, 0, (length + 1) * sizeof(WCHAR));
    wcsncpy(result, objValue, length + 1);

    if (lengthPtr != NULL)
	*lengthPtr = length;

done:
    return result;
}

/*
 *----------------------------------------------------------------------
 *
 * GetStringVariableValue --
 *
 *	This function returns the value of the specified Tcl variable
 *	as a Unicode string.
 *
 * Results:
 *	The value of the Tcl variable as a Unicode string or NULL if
 *	any of the arguments are NULL or if the Tcl variable is not
 *	found.  The return value, if not NULL, must be freed by the
 *	caller via the ckfree Tcl API.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static LPWSTR GetStringVariableValue(
    Tcl_Interp *interp,	/* Current Tcl interpreter. */
    LPCWSTR varName,	/* The name of the Tcl variable. */
    int *lengthPtr)	/* If non-NULL, the location where the
			 * string rep's unichar length should be
			 * stored.  If NULL, no length is stored. */
{
    Tcl_Obj *part1Ptr = NULL;
    Tcl_Obj *objPtr = NULL;
    int length = 0;
    LPCWSTR varValue;
    LPWSTR result = NULL;

    if (interp == NULL)
	return NULL;

    if (varName == NULL) {
	Tcl_AppendResult(interp, "invalid variable name\n", NULL);
	return NULL;
    }

    part1Ptr = Tcl_NewUnicodeObj(varName, -1);

    if (part1Ptr != NULL) {
	Tcl_IncrRefCount(part1Ptr);
    } else {
	Tcl_AppendResult(interp, "out of memory: part1Ptr\n", NULL);
	goto done;
    }

    objPtr = Tcl_ObjGetVar2(interp, part1Ptr, NULL, TCL_GLOBAL_ONLY);

    if (objPtr != NULL) {
	Tcl_IncrRefCount(objPtr);
    } else {
	Tcl_AppendResult(interp, "variable not found: ", NULL);
	Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp), varName, -1);
	Tcl_AppendResult(interp, "\n", NULL);
	goto done;
    }

    varValue = Tcl_GetUnicodeFromObj(objPtr, &length);

    if ((varValue == NULL) || (length < 0)) {
	Tcl_AppendResult(interp, "variable value is invalid: ", NULL);
	Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp), varName, -1);
	Tcl_AppendResult(interp, "\n", NULL);
	goto done;
    }

    result = (LPWSTR) attemptckalloc((length + 1) * sizeof(WCHAR));

    if (result == NULL) {
	Tcl_AppendResult(interp, "out of memory: varValue\n", NULL);
	goto done;
    }

    memset(result, 0, (length + 1) * sizeof(WCHAR));
    wcsncpy(result, varValue, length + 1);

    if (lengthPtr != NULL)
	*lengthPtr = length;

done:
    if (objPtr != NULL) {
	Tcl_DecrRefCount(objPtr);
	objPtr = NULL;
    }

    if (part1Ptr != NULL) {
	Tcl_DecrRefCount(part1Ptr);
	part1Ptr = NULL;
    }

    return result;
}

/*
 *----------------------------------------------------------------------
 *
 * GetBooleanVariableValue --
 *
 *	This function returns the value of the specified Tcl variable
 *	as a boolean value.
 *
 * Results:
 *	The value of the Tcl variable as a boolean value.  If any of
 *	the arguments are NULL or if the variable is not found, the
 *	supplied default value will be returned.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static BOOL GetBooleanVariableValue(
    Tcl_Interp *interp,	/* Current Tcl interpreter. */
    LPCWSTR varName,	/* The name of the Tcl variable. */
    BOOL defValue)	/* The value to return in case of error. */
{
    int code;
    Tcl_Obj *part1Ptr = NULL;
    Tcl_Obj *objPtr = NULL;
    BOOL result = defValue;

    if (interp == NULL)
	return result;

    if (varName == NULL) {
	Tcl_AppendResult(interp, "invalid variable name\n", NULL);
	return result;
    }

    part1Ptr = Tcl_NewUnicodeObj(varName, -1);

    if (part1Ptr != NULL) {
	Tcl_IncrRefCount(part1Ptr);
    } else {
	Tcl_AppendResult(interp, "out of memory: part1Ptr\n", NULL);
	goto done;
    }

    objPtr = Tcl_ObjGetVar2(interp, part1Ptr, NULL, TCL_GLOBAL_ONLY);

    if (objPtr != NULL) {
	Tcl_IncrRefCount(objPtr);
    } else {
	Tcl_AppendResult(interp, "variable not found: ", NULL);
	Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp), varName, -1);
	Tcl_AppendResult(interp, "\n", NULL);
	goto done;
    }

    code = Tcl_GetBooleanFromObj(interp, objPtr, &result);

    if (code != TCL_OK) {
	Tcl_AppendResult(interp, "variable value is invalid: ", NULL);
	Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp), varName, -1);
	Tcl_AppendResult(interp, "\n", NULL);
	goto done;
    }

done:
    if (objPtr != NULL) {
	Tcl_DecrRefCount(objPtr);
	objPtr = NULL;
    }

    if (part1Ptr != NULL) {
	Tcl_DecrRefCount(part1Ptr);
	part1Ptr = NULL;
    }

    return result;
}

/*
 *----------------------------------------------------------------------
 *
 * GetIntegerVariableValue --
 *
 *	This function returns the value of the specified Tcl variable
 *	as an integer value.
 *
 * Results:
 *	The value of the Tcl variable as an integer value.  If any of
 *	the arguments are NULL or if the variable is not found, the
 *	supplied default value will be returned.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static int GetIntegerVariableValue(
    Tcl_Interp *interp,	/* Current Tcl interpreter. */
    LPCWSTR varName,	/* The name of the Tcl variable. */
    int defValue)	/* The value to return in case of error. */
{
    int code;
    Tcl_Obj *part1Ptr = NULL;
    Tcl_Obj *objPtr = NULL;
    int result = defValue;

    if (interp == NULL)
	return result;

    if (varName == NULL) {
	Tcl_AppendResult(interp, "invalid variable name\n", NULL);
	return result;
    }

    part1Ptr = Tcl_NewUnicodeObj(varName, -1);

    if (part1Ptr != NULL) {
	Tcl_IncrRefCount(part1Ptr);
    } else {
	Tcl_AppendResult(interp, "out of memory: part1Ptr\n", NULL);
	goto done;
    }

    objPtr = Tcl_ObjGetVar2(interp, part1Ptr, NULL, TCL_GLOBAL_ONLY);

    if (objPtr != NULL) {
	Tcl_IncrRefCount(objPtr);
    } else {
	Tcl_AppendResult(interp, "variable not found: ", NULL);
	Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp), varName, -1);
	Tcl_AppendResult(interp, "\n", NULL);
	goto done;
    }

    code = Tcl_GetIntFromObj(interp, objPtr, &result);

    if (code != TCL_OK) {
	Tcl_AppendResult(interp, "variable value is invalid: ", NULL);
	Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp), varName, -1);
	Tcl_AppendResult(interp, "\n", NULL);
	goto done;
    }

done:
    if (objPtr != NULL) {
	Tcl_DecrRefCount(objPtr);
	objPtr = NULL;
    }

    if (part1Ptr != NULL) {
	Tcl_DecrRefCount(part1Ptr);
	part1Ptr = NULL;
    }

    return result;
}

/*
 *----------------------------------------------------------------------
 *
 * CreateClrMethodInfo --
 *
 *	This function allocates space for, and returns the necessary
 *	information for this package to execute a CLR method. The
 *	allocated resources must be freed by the caller via the
 *	FreeClrMethodInfo function.
 *
 * Results:
 *	A standard Tcl result.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static int CreateClrMethodInfo(
    Tcl_Interp *interp,		    /* Current Tcl interpreter. */
    Tcl_Obj *assemblyPathPtr,	    /* The path of the assembly containing the
				     * method. */
    Tcl_Obj *typeNamePtr,	    /* The managed type containing the method. */
    Tcl_Obj *methodNamePtr,	    /* The name of the method. */
    Tcl_Obj *argumentPtr,	    /* The argument string for the method. */
    ClrMethodInfo **ppMethodInfo)   /* Upon success, the pointed to structure
				     * will contain the CLR method information. */
{
    int length = 0;

    if (interp == NULL) {
	return TCL_ERROR;
    }

    if (ppMethodInfo == NULL) {
	Tcl_AppendResult(interp, "invalid argument: ppMethodInfo\n", NULL);
	return TCL_ERROR;
    }

    *ppMethodInfo = (ClrMethodInfo *) attemptckalloc(sizeof(ClrMethodInfo));

    if (*ppMethodInfo == NULL) {
	Tcl_AppendResult(interp, "out of memory: ClrMethodInfo\n", NULL);
	return TCL_ERROR;
    }

    memset(*ppMethodInfo, 0, sizeof(ClrMethodInfo));
    (*ppMethodInfo)->sizeOf = sizeof(ClrMethodInfo);

    (*ppMethodInfo)->assemblyPath = GetStringObjectValue(interp,
	assemblyPathPtr, &length);

    if (((*ppMethodInfo)->assemblyPath == NULL) || (length <= 0)) {
	Tcl_AppendResult(interp, "invalid assembly path\n", NULL);
	return TCL_ERROR;
    }

    (*ppMethodInfo)->typeName = GetStringObjectValue(interp, typeNamePtr,
	&length);

    if (((*ppMethodInfo)->typeName == NULL) || (length <= 0)) {
	Tcl_AppendResult(interp, "invalid type name\n", NULL);
	return TCL_ERROR;
    }

    (*ppMethodInfo)->methodName = GetStringObjectValue(interp, methodNamePtr,
	&length);

    if (((*ppMethodInfo)->methodName == NULL) || (length <= 0)) {
	Tcl_AppendResult(interp, "invalid method name\n", NULL);
	return TCL_ERROR;
    }

    (*ppMethodInfo)->argument = GetStringObjectValue(interp, argumentPtr,
	&length);

    if (((*ppMethodInfo)->argument == NULL) || (length < 0)) {
	Tcl_AppendResult(interp, "invalid method argument\n", NULL);
	return TCL_ERROR;
    }

    return TCL_OK;
}

/*
 *----------------------------------------------------------------------
 *
 * GetClrMethodInfo --
 *
 *	This function queries, allocates space for, and returns the
 *	necessary information for this package to execute a CLR method.
 *	The allocated resources must be freed by the caller via the
 *	FreeClrMethodInfo function.
 *
 * Results:
 *	A standard Tcl result.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static int GetClrMethodInfo(
    Tcl_Interp *interp,		    /* Current Tcl interpreter. */
    MethodFlags methodFlags,	    /* The type of CLR method we need the
				     * information for (e.g. startup, control,
				     * detach, or shutdown). */
    ClrMethodInfo **ppMethodInfo)   /* Upon success, the pointed to structure
				     * will contain the CLR method information. */
{
    LPCWSTR varName;
    int length = 0;

    if (interp == NULL) {
	return TCL_ERROR;
    }

    if (ppMethodInfo == NULL) {
	Tcl_AppendResult(interp, "invalid argument: ppMethodInfo\n", NULL);
	return TCL_ERROR;
    }

    *ppMethodInfo = (ClrMethodInfo *) attemptckalloc(sizeof(ClrMethodInfo));

    if (*ppMethodInfo == NULL) {
	Tcl_AppendResult(interp, "out of memory: ClrMethodInfo\n", NULL);
	return TCL_ERROR;
    }

    memset(*ppMethodInfo, 0, sizeof(ClrMethodInfo));
    (*ppMethodInfo)->sizeOf = sizeof(ClrMethodInfo);

    (*ppMethodInfo)->assemblyPath = GetStringVariableValue(interp,
	PACKAGE_UNICODE_ASSEMBLY_PATH_VAR_NAME, &length);

    if (((*ppMethodInfo)->assemblyPath == NULL) || (length <= 0)) {
	Tcl_AppendResult(interp, "invalid assembly path\n", NULL);
	return TCL_ERROR;
    }

    (*ppMethodInfo)->typeName = GetStringVariableValue(interp,
	PACKAGE_UNICODE_TYPE_NAME_VAR_NAME, &length);

    if (((*ppMethodInfo)->typeName == NULL) || (length <= 0)) {
	Tcl_AppendResult(interp, "invalid type name\n", NULL);
	return TCL_ERROR;
    }

    switch (methodFlags & METHOD_TYPE_MASK) {
	case METHOD_TYPE_DEMAND: {
	    varName = NULL; /* NOTE: Not supported. */
	    break;
	}
	case METHOD_TYPE_STARTUP: {
	    varName = PACKAGE_UNICODE_STARTUP_METHOD_VAR_NAME;
	    break;
	}
	case METHOD_TYPE_CONTROL: {
	    varName = PACKAGE_UNICODE_CONTROL_METHOD_VAR_NAME;
	    break;
	}
	case METHOD_TYPE_DETACH: {
	    varName = PACKAGE_UNICODE_DETACH_METHOD_VAR_NAME;
	    break;
	}
	case METHOD_TYPE_SHUTDOWN: {
	    varName = PACKAGE_UNICODE_SHUTDOWN_METHOD_VAR_NAME;
	    break;
	}
	default: {
	    varName = NULL;
	    break;
	}
    }

    if (varName == NULL) {
	Tcl_AppendResult(interp, "invalid method type\n", NULL);
	return TCL_ERROR;
    }

    (*ppMethodInfo)->methodName = GetStringVariableValue(interp,
	varName, &length);

    if (((*ppMethodInfo)->methodName == NULL) || (length <= 0)) {
	Tcl_AppendResult(interp, "invalid method name\n", NULL);
	return TCL_ERROR;
    }

    (*ppMethodInfo)->argument = GetStringVariableValue(interp,
	PACKAGE_UNICODE_METHOD_ARGUMENTS_VAR_NAME, &length);

    if (((*ppMethodInfo)->argument == NULL) || (length < 0)) {
	Tcl_AppendResult(interp, "invalid method argument\n", NULL);
	return TCL_ERROR;
    }

    return TCL_OK;
}

/*
 *----------------------------------------------------------------------
 *
 * FreeClrMethodInfo --
 *
 *	This function frees all the resources allocated by the
 *	GetClrMethodInfo function.
 *
 * Results:
 *	None.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static void FreeClrMethodInfo(
    ClrMethodInfo **ppMethodInfo)	/* Pointer to structure to free. */
{
    if ((ppMethodInfo == NULL) || (*ppMethodInfo == NULL))
	return;

    if ((*ppMethodInfo)->argument != NULL) {
	ckfree((LPVOID) (*ppMethodInfo)->argument);
	(*ppMethodInfo)->argument = NULL;
    }

    if ((*ppMethodInfo)->methodName != NULL) {
	ckfree((LPVOID) (*ppMethodInfo)->methodName);
	(*ppMethodInfo)->methodName = NULL;
    }

    if ((*ppMethodInfo)->typeName != NULL) {
	ckfree((LPVOID) (*ppMethodInfo)->typeName);
	(*ppMethodInfo)->typeName = NULL;
    }

    if ((*ppMethodInfo)->assemblyPath != NULL) {
	ckfree((LPVOID) (*ppMethodInfo)->assemblyPath);
	(*ppMethodInfo)->assemblyPath = NULL;
    }

    ckfree((LPVOID) *ppMethodInfo);
    *ppMethodInfo = NULL;
}

/*
 *----------------------------------------------------------------------
 *
 * GetClrConfigInfo --
 *
 *	This function queries, allocates space for, and returns the
 *	requested configuration information for this package.  The
 *	allocated resources must be freed by the caller via the
 *	FreeClrConfigInfo function.
 *
 * Results:
 *	A standard Tcl result.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static int GetClrConfigInfo(
    Tcl_Interp *interp,		    /* Current Tcl interpreter. */
    BOOL bForLogOnly,		    /* Non-zero to include -ONLY- logging
				     * subsystem related information. */
    BOOL bMethods,		    /* Non-zero to include information about
				     * the configured CLR methods, zero
				     * otherwise. */
    ClrConfigInfo **ppConfigInfo)   /* Upon success, the pointed to structure
				     * will contain the configuration
				     * information. */
{
    int length = 0;

    if (interp == NULL) {
	return TCL_ERROR;
    }

    if (ppConfigInfo == NULL) {
	Tcl_AppendResult(interp, "invalid argument: ppConfigInfo\n", NULL);
	return TCL_ERROR;
    }

    *ppConfigInfo = (ClrConfigInfo *) attemptckalloc(sizeof(ClrConfigInfo));

    if (*ppConfigInfo == NULL) {
	Tcl_AppendResult(interp, "out of memory: ClrConfigInfo\n", NULL);
	return TCL_ERROR;
    }

    memset(*ppConfigInfo, 0, sizeof(ClrConfigInfo));
    (*ppConfigInfo)->sizeOf = sizeof(ClrConfigInfo);

    if (!bForLogOnly && bMethods) {
	if (GetClrMethodInfo(interp, METHOD_TYPE_STARTUP,
		&(*ppConfigInfo)->pStartupMethod) != TCL_OK) {
	    return TCL_ERROR;
	}

	if (GetClrMethodInfo(interp, METHOD_TYPE_CONTROL,
		&(*ppConfigInfo)->pControlMethod) != TCL_OK) {
	    return TCL_ERROR;
	}

	if (GetClrMethodInfo(interp, METHOD_TYPE_DETACH,
		&(*ppConfigInfo)->pDetachMethod) != TCL_OK) {
	    return TCL_ERROR;
	}

	if (GetClrMethodInfo(interp, METHOD_TYPE_SHUTDOWN,
		&(*ppConfigInfo)->pShutdownMethod) != TCL_OK) {
	    return TCL_ERROR;
	}
    }

    (*ppConfigInfo)->logCommand = GetStringVariableValue(interp,
	PACKAGE_UNICODE_LOG_COMMAND_VAR_NAME, &length);

    if (((*ppConfigInfo)->logCommand == NULL) || (length <= 0)) {
	Tcl_AppendResult(interp, "invalid log command\n", NULL);
	return TCL_ERROR;
    }

    if (!bForLogOnly) {
	(*ppConfigInfo)->methodFlags = GetIntegerVariableValue(interp,
	    PACKAGE_UNICODE_METHOD_FLAGS_VAR_NAME, METHOD_NONE);

	(*ppConfigInfo)->bVerbose = GetBooleanVariableValue(interp,
	    PACKAGE_UNICODE_VERBOSE_VAR_NAME, FALSE);

	(*ppConfigInfo)->bNoNormalize = GetBooleanVariableValue(interp,
	    PACKAGE_UNICODE_NO_NORMALIZE_VAR_NAME, FALSE);

	(*ppConfigInfo)->bLoadClr = GetBooleanVariableValue(interp,
	    PACKAGE_UNICODE_LOAD_CLR_VAR_NAME, FALSE);

	(*ppConfigInfo)->bStartClr = GetBooleanVariableValue(interp,
	    PACKAGE_UNICODE_START_CLR_VAR_NAME, FALSE);

	(*ppConfigInfo)->bStartBridge = GetBooleanVariableValue(interp,
	    PACKAGE_UNICODE_START_BRIDGE_VAR_NAME, FALSE);

	(*ppConfigInfo)->bStopClr = GetBooleanVariableValue(interp,
	    PACKAGE_UNICODE_STOP_CLR_VAR_NAME, FALSE);

	(*ppConfigInfo)->bUseIsolation = GetBooleanVariableValue(interp,
	    PACKAGE_UNICODE_USE_ISOLATION, FALSE);

	(*ppConfigInfo)->bUseSafeInterp = GetBooleanVariableValue(interp,
	    PACKAGE_UNICODE_USE_SAFE_INTERP, FALSE);
    }

    return TCL_OK;
}

/*
 *----------------------------------------------------------------------
 *
 * FreeClrConfigInfo --
 *
 *	This function frees all the resources allocated by the
 *	GetClrConfigInfo function.
 *
 * Results:
 *	None.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static void FreeClrConfigInfo(
    ClrConfigInfo **ppConfigInfo)
{
    if ((ppConfigInfo == NULL) || (*ppConfigInfo == NULL))
	return;

    if ((*ppConfigInfo)->logCommand != NULL) {
	ckfree((LPVOID) (*ppConfigInfo)->logCommand);
	(*ppConfigInfo)->logCommand = NULL;
    }

    FreeClrMethodInfo(&(*ppConfigInfo)->pShutdownMethod);
    FreeClrMethodInfo(&(*ppConfigInfo)->pDetachMethod);
    FreeClrMethodInfo(&(*ppConfigInfo)->pControlMethod);
    FreeClrMethodInfo(&(*ppConfigInfo)->pStartupMethod);

    ckfree((LPVOID) *ppConfigInfo);
    *ppConfigInfo = NULL;
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

static int LoadAndStartTheClr(
    Tcl_Interp *interp,	    /* Current Tcl interpreter.*/
    LPCWSTR logCommand,	    /* The Tcl command used to log the CLR method
			     * execution, if any. */
    BOOL bLoad,		    /* Load the CLR if necessary? */
    BOOL bUseMinimumClr,    /* Force using minimum supported CLR
			     * version? */
    BOOL bStart,	    /* Start the CLR after loading it? */
    BOOL bStrict)	    /* Fail if already loaded and/or started? */
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
		    hResult, pClrMetaHost);

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
		    PACKAGE_UNICODE_PTR_FMT L"}, clrVersion = {%s})",
		    pClrMetaHost, clrVersion);

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
		    hResult, pClrRuntimeInfo);

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
		    L"bLoadable = {%d})", hResult, bLoadable);

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
		    hResult, pClrRuntimeHost);

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
		    hResult, pClrRuntimeHost);

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
		    L"ICLRRuntimeHost_Start(hResult = {0x%lX})", hResult);

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

static int StopAndReleaseTheClr(
    Tcl_Interp *interp,	    /* Current Tcl interpreter. */
    LPCWSTR logCommand,	    /* The Tcl command used to log the CLR method
			     * execution, if any. */
    BOOL bRelease,	    /* Release the CLR after stopping it? */
    BOOL bStrict)	    /* Fail if already stopped and/or released? */
{
    int code = TCL_OK;
    WCHAR buffer[PACKAGE_RESULT_SIZE + 1] = {0};

    Tcl_MutexLock(&packageMutex);

    if (pClrRuntimeHost != NULL) {
	/*
	 * NOTE: If we were previously able to start the CLR, stop it now.
	 */

	if (bClrStarted) {
	    HRESULT hResult = S_OK;

	    /* NON-PORTABLE */
	    SetEnvironmentVariableW(CLR_STOPPING_ENVVAR_NAME, L"1");

	    hResult = ICLRRuntimeHost_Stop(pClrRuntimeHost);

	    /* NON-PORTABLE */
	    SetEnvironmentVariableW(CLR_STOPPING_ENVVAR_NAME, NULL);

	    if (PACKAGE_CAN_LOG(interp, logCommand)) {
		gwprintf(buffer, PACKAGE_RESULT_SIZE,
		    L"ICLRRuntimeHost_Stop(hResult = {0x%lX})", hResult);

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

    if ((code == TCL_OK) && bBridgeStarted) {
	bBridgeStarted = FALSE;

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

static BOOL CanExecuteClrCode(
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

static int ExecuteClrMethod(
    HANDLE hModule,		/* Tcl library module handle. */
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
	    length += (sizeof(HANDLE) * 2) + 3; /* "0x" + handleAsStr + " " */
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
		    L"%s_%s " PACKAGE_UNICODE_PTR_FMT L" "
		    PACKAGE_UNICODE_PTR_FMT L" " PACKAGE_UNICODE_PTR_FMT
		    L" %s %s %s %s\0", PACKAGE_UNICODE_NAME,
		    protocolRevision, hModule, pTclStubs, interp,
		    bUseIsolation ? L"1 " : L"0 ",
		    bUseSafeInterp ? L"1 " : L"0 ",
		    (pMethodInfo->argument != NULL) ? pMethodInfo->argument :
		    L"", (argument != NULL) ? argument : L"");
	    } else {
		gwprintf(newArgument, length - GWPRINTF_LENGTH_HAS_NUL,
		    L"%s_%s " PACKAGE_UNICODE_PTR_FMT L" "
		    PACKAGE_UNICODE_PTR_FMT L" %s %s %s\0",
		    PACKAGE_UNICODE_NAME, protocolRevision, hModule, interp,
		    bUseSafeInterp ? L"1 " : L"0 ",
		    (pMethodInfo->argument != NULL) ? pMethodInfo->argument :
		    L"", (argument != NULL) ? argument : L"");
	    }
	} else {
	    gwprintf(newArgument, length - GWPRINTF_LENGTH_HAS_NUL, L"%s %s\0",
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
	    L"ICLRRuntimeHost_ExecuteInDefaultAppDomain(hResult = {0x%lX}, "
	    L"returnValue = {%d})", hResult, returnValue);

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
 * MaybeCombineMethodFlags --
 *
 *	This function combines the specified method flags with those
 *	from the configuration information for this package.
 *
 * Results:
 *	None.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */
static void MaybeCombineMethodFlags(
    ClrConfigInfo *pConfigInfo, /* The configuration information. */
    MethodFlags *pMethodFlags)	/* Type [and flags] of the CLR method to
				 * execute. */
{
    MethodFlags methodFlags;

    /*
     * NOTE: This function does nothing if either of the required parameters
     *       is invalid.
     */

    if ((pConfigInfo == NULL) || (pMethodFlags == NULL))
	return;

    /*
     * NOTE: Start out with the method flags specified by the immediate chain
     *       of callers.
     */

    methodFlags = *pMethodFlags;

    /*
     * NOTE: Add the configured method flags to the ones passed to us by the
     *       caller.
     */

    methodFlags |= pConfigInfo->methodFlags;

    /*
     * NOTE: Add the method flags associated with the "use isolation" and the
     *       "use safe interp" configuration settings, if necessary.
     */

    if (pConfigInfo->bUseIsolation)
	methodFlags |= METHOD_USE_ISOLATION;

    if (pConfigInfo->bUseSafeInterp)
	methodFlags |= METHOD_USE_SAFE_INTERP;

    /*
     * NOTE: Update the original method flags specified by the caller to the
     *       newly combined value.
     */

    *pMethodFlags = methodFlags;
}

/*
 *----------------------------------------------------------------------
 *
 * GetAndExecuteClrMethod --
 *
 *	This function queries the CLR method information from the Tcl
 *	interpreter and executes the CLR method responsible for setting
 *	up, controlling, detaching from, or shutting down the bridge
 *	between Eagle and Tcl.
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

static int GetAndExecuteClrMethod(
    HANDLE hModule,		/* Tcl library module handle. */
    ClrTclStubs *pTclStubs,	/* Tcl C API stub function pointer table. */
    ClrConfigInfo *pConfigInfo, /* The configuration information. */
    Tcl_Interp *interp,		/* Current Tcl interpreter. */
    LPCWSTR argument,		/* Extra argument to the method, if any. */
    MethodFlags methodFlags)	/* Type [and flags] of the CLR method to
				 * execute. */
{
    int code = TCL_OK;
    ClrMethodInfo *pMethodInfo = NULL;
    DWORD returnValue = TCL_OK;

    /*
     * NOTE: Check the Tcl interpreter for the configuration settings.  The Tcl
     *       variables being queried should have been set by code in our custom
     *       "helper.tcl" file.  If not, this function call should fail.
     */

    if (pConfigInfo == NULL) {
	Tcl_AppendResult(interp, "invalid argument: pConfigInfo\n", NULL);
	code = TCL_ERROR;
	goto done;
    }

    MaybeCombineMethodFlags(pConfigInfo, &methodFlags);

    /*
     * NOTE: If the CLR is either not loaded -OR- not started, then we cannot
     *	     use it to execute any code.
     */

    if (!CanExecuteClrCode(interp)) {
	if (methodFlags & METHOD_STRICT_CLR)
	    code = TCL_ERROR;

	goto done;
    }

    /*
     * NOTE: Query the Tcl interpreter for the information required to execute
     *       the CLR method responsible for starting up, controlling, detaching
     *       from, or shutting down the bridge.  The Tcl variables being
     *       queried should have been set by code in our custom "helper.tcl"
     *       file.  If not, this function call should fail.
     */

    code = GetClrMethodInfo(interp, methodFlags, &pMethodInfo);

    if (code != TCL_OK)
	goto done;

    /*
     * NOTE: Attempt to execute the CLR method.
     */

    code = ExecuteClrMethod(hModule, pTclStubs, interp,
	pConfigInfo->logCommand, pMethodInfo, argument, methodFlags,
	&returnValue);

    if (code != TCL_OK)
	goto done;

    /*
     * NOTE: By "convention", the pre-defined CLR methods used by this package
     *       must return the value TCL_OK upon success; therefore, check the
     *       return value against that and fail if this is not the case;
     *       however, we allow this handling to be bypassed by the caller if
     *       necessary.
     */

    if ((methodFlags & METHOD_STRICT_RETURN) && (returnValue != TCL_OK)) {
	if (interp != NULL) {
	    LPCWSTR methodTypeName; /* NOTE: For display only. */
	    WCHAR buffer[PACKAGE_RESULT_SIZE + 1] = {0};

	    switch (methodFlags & METHOD_TYPE_MASK) {
		case METHOD_TYPE_DEMAND: {
		    methodTypeName = L"demand";
		    break;
		}
		case METHOD_TYPE_STARTUP: {
		    methodTypeName = L"startup";
		    break;
		}
		case METHOD_TYPE_CONTROL: {
		    methodTypeName = L"control";
		    break;
		}
		case METHOD_TYPE_DETACH: {
		    methodTypeName = L"detach";
		    break;
		}
		case METHOD_TYPE_SHUTDOWN: {
		    methodTypeName = L"shutdown";
		    break;
		}
		default: {
		    methodTypeName = L"unknown";
		    break;
		}
	    }

	    /*
	     * NOTE: Build an informative error message about this specific
	     *       CLR method execution failure.
	     */

	    gwprintf(buffer, PACKAGE_RESULT_SIZE,
		L"%s return value not TCL_OK, method: \"%s.%s\", assembly: "
		L"\"%s\"\0", methodTypeName, pMethodInfo->typeName,
		pMethodInfo->methodName, pMethodInfo->assemblyPath);

	    Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp),
		GetTclErrorMessage(buffer, returnValue), -1);
	}

	code = TCL_ERROR;
	goto done;
    }

done:
    FreeClrMethodInfo(&pMethodInfo);

    return code;
}

/*
 *----------------------------------------------------------------------
 *
 * DemandExecuteClrMethod --
 *
 *	This function executes the specified CLR method using the
 *	specified assembly, type, and method information.
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

static int DemandExecuteClrMethod(
    HANDLE hModule,		/* Tcl library module handle. */
    ClrTclStubs *pTclStubs,	/* Tcl C API stub function pointer table. */
    ClrConfigInfo *pConfigInfo, /* The configuration information. */
    Tcl_Interp *interp,		/* Current Tcl interpreter. */
    Tcl_Obj *assemblyPathPtr,	/* The path of the assembly containing the
				 * method. */
    Tcl_Obj *typeNamePtr,	/* The managed type containing the method. */
    Tcl_Obj *methodNamePtr,	/* The name of the method. */
    Tcl_Obj *argumentPtr,	/* The argument string for the method. */
    MethodFlags methodFlags,	/* Flags that control logging, arguments, etc.
				 * See the MethodFlags enum for details. */
    LPDWORD pReturnValue)	/* Location where the return value should be
				 * stored or NULL if the return value is not
				 * required. */
{
    int code = TCL_OK;
    ClrMethodInfo *pMethodInfo = NULL;

    /*
     * NOTE: Check the Tcl interpreter for the configuration settings.  The Tcl
     *       variables being queried should have been set by code in our custom
     *       "helper.tcl" file.  If not, this function call should fail.
     */

    if (pConfigInfo == NULL) {
	Tcl_AppendResult(interp, "invalid argument: pConfigInfo\n", NULL);
	code = TCL_ERROR;
	goto done;
    }

    MaybeCombineMethodFlags(pConfigInfo, &methodFlags);

    /*
     * NOTE: Copy the information required to execute the CLR method.
     */

    code = CreateClrMethodInfo(interp, assemblyPathPtr, typeNamePtr,
	methodNamePtr, argumentPtr, &pMethodInfo);

    if (code != TCL_OK)
	goto done;

    /*
     * NOTE: Attempt to execute the CLR method.
     */

    code = ExecuteClrMethod(hModule, pTclStubs, interp,
	pConfigInfo->logCommand, pMethodInfo, NULL, methodFlags,
	pReturnValue);

    if (code != TCL_OK)
	goto done;

done:
    FreeClrMethodInfo(&pMethodInfo);

    return code;
}

/*
 *----------------------------------------------------------------------
 *
 * Garuda_Init --
 *
 *	This function initializes the package for the specified Tcl
 *	interpreter.
 *
 * Results:
 *	A standard Tcl result.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

int Garuda_Init(
    Tcl_Interp *interp)			/* Current Tcl interpreter. */
{
    int code = TCL_OK;
    ClrConfigInfo *pConfigInfo = NULL;
    LPCWSTR logCommand = NULL;
    int tclVersion[4] = {0, 0, 0, 0};
    BOOL bTcl86 = FALSE;
    BOOL bClrWasLoaded = FALSE;
    BOOL bClrWasStarted = FALSE;
    Tcl_Command command;

    /*
     * NOTE: Make sure the Tcl interpreter is valid and then try to initialize
     *       the Tcl stubs table.  We cannot call any Tcl API unless this call
     *       succeeds.
     */

#if defined(USE_TCL_PRIVATE_STUBS)
    if ((interp == NULL) || !initTclStubs(interp, PACKAGE_TCL_VERSION, 0)) {
	PACKAGE_TRACE(("Garuda_Init: Tcl private stubs not initialized\n"));
	return TCL_ERROR;
    }
#else
    if ((interp == NULL) || !Tcl_InitStubs(interp, PACKAGE_TCL_VERSION, 0)) {
	PACKAGE_TRACE(("Garuda_Init: Tcl stubs not initialized\n"));
	return TCL_ERROR;
    }
#endif

    /*
     * NOTE: Mark the Tcl stubs mechanism as being fully initialized now and
     *       then grab the package lock for the entire time we are loading and
     *       setting up the package.
     */

    InterlockedIncrement(&lTclStubs);
    Tcl_MutexLock(&packageMutex);

    /*
     * NOTE: Query the package module file name, before proceeding further.
     */

    if (!GetPackageModuleFileName(hPackageModule, &packageFileName)) {
	Tcl_AppendResult(interp, "failed to get package module file name\n",
	    NULL);

	code = TCL_ERROR;
	goto done;
    }

    /*
     * NOTE: Query the version of the loaded Tcl library.  This is needed to
     *       determine if TIPs #285, #335, and #336 are available.
     */

    Tcl_GetVersion(&tclVersion[0], &tclVersion[1], &tclVersion[2],
	&tclVersion[3]);

    /*
     * NOTE: Tcl 8.6 or higher is required for TIPs #285, #335, and #336.
     */

    bTcl86 = (tclVersion[0] > 8) ||
	((tclVersion[0] == 8) && (tclVersion[1] >= 6));

    /*
     * NOTE: Initialize the Tcl C API function pointer table to be passed to
     *       the bridge.
     */

    memset(&uTclStubs, 0, sizeof(ClrTclStubs));
    uTclStubs.sizeOf = sizeof(ClrTclStubs);

    if (!SetClrTclStubs(&uTclStubs, bTcl86, bTcl86, bTcl86)) {
	Tcl_AppendResult(interp, "failed to set Tcl function pointers\n",
	    NULL);

	code = TCL_ERROR;
	goto done;
    }

    /*
     * NOTE: Query the Tcl interpreter for the configuration settings.  The Tcl
     *       variables being queried should have been set by code in our custom
     *       "helper.tcl" file.  If not, this function call should fail.
     */

    code = GetClrConfigInfo(interp, FALSE, FALSE, &pConfigInfo);

    if (code != TCL_OK)
	goto done;

    /*
     * NOTE: Grab the configured log command.  This will be used several times
     *       in this function.
     */

    logCommand = pConfigInfo->logCommand;

    /*
     * NOTE: Grab the Tcl library module handle.  This is needed by the Eagle
     *       CLR method that looks up the exported Tcl API functions it needs
     *       from the Tcl library.
     */

    if (hTclModule == NULL) {
	/* NON-PORTABLE */
	hTclModule = TclWinGetTclInstance(); /* HACK: Requires "tclInt.h". */
    }

    if (hTclModule == NULL) {
	Tcl_AppendResult(interp, "invalid Tcl library module\n", NULL);
	code = TCL_ERROR;
	goto done;
    }

    /*
     * NOTE: Add our exit handler prior to performing any actions that need to
     *       be undone by it.  However, first delete it in case it has already
     *       been added.  If it has never been added, trying to delete it will
     *       be a harmless no-op.  This appears to be necessary to ensure that
     *       our exit handler has been added exactly once after this point.
     */

    Tcl_DeleteExitHandler(GarudaExitProc, NULL);
    Tcl_CreateExitHandler(GarudaExitProc, NULL);

    /*
     * NOTE: Has the CLR already been loaded and started previously by this
     *       package (i.e. in another Tcl interpreter)?
     */

    bClrWasLoaded = (pClrRuntimeHost != NULL);
    bClrWasStarted = bClrStarted;

    /*
     * NOTE: Load [and possibly start] the CLR now.
     */

    code = LoadAndStartTheClr(interp, logCommand, pConfigInfo->bLoadClr,
	pConfigInfo->bUseMinimumClr, pConfigInfo->bStartClr, FALSE);

    if (code != TCL_OK)
	goto done;

    /*
     * NOTE: Do we want to execute the CLR method to startup the bridge between
     *       Eagle and Tcl now?  The CLR must be loaded and started for this to
     *       work.
     */

    if ((bClrWasLoaded || pConfigInfo->bLoadClr) &&
	(bClrWasStarted || pConfigInfo->bStartClr) &&
	    pConfigInfo->bStartBridge) {
	code = GetAndExecuteClrMethod(hTclModule, &uTclStubs, pConfigInfo,
	    interp, NULL, METHOD_TYPE_STARTUP | METHOD_VIA_LOAD);

	if (code != TCL_OK)
	    goto done;

	bBridgeStarted = TRUE;
    }

    /*
     * NOTE: Create our command in the Tcl interpreter.  This command is not
     *       used for evaluation of Eagle scripts; rather, it is used to query
     *       and/or modify the state of this package.
     */

    command = Tcl_CreateObjCommand(interp, COMMAND_NAME, GarudaObjCmd, interp,
	GarudaObjCmdDeleteProc);

    if (command == NULL) {
	Tcl_AppendResult(interp, "command creation failed\n", NULL);
	code = TCL_ERROR;
	goto done;
    }

    /*
     * NOTE: Store the token for the command created by this package.  This
     *       way, we can properly delete it when the package is being unloaded.
     */

    Tcl_SetAssocData(interp, PACKAGE_NAME, NULL, command);

    /*
     * NOTE: Attempt to provide the primary package to the Tcl interpreter.
     */

    code = Tcl_PkgProvide(interp, PACKAGE_NAME, PACKAGE_VERSION);

    if (code != TCL_OK)
	goto done;

    /*
     * NOTE: Attempt to provide the secondary package to the Tcl interpreter.
     */

    code = Tcl_PkgProvide(interp, PACKAGE_ALTERNATE_NAME, PACKAGE_VERSION);

done:
    /*
     * NOTE: If possible, log this attempt to initialize the package, including
     *       the saved package module handle, the associated module file name,
     *       the current Tcl interpreter, and the return code.
     */

    if (PACKAGE_CAN_LOG(interp, logCommand)) {
	WCHAR buffer[PACKAGE_RESULT_SIZE + 1] = {0};

	gwprintf(buffer, PACKAGE_RESULT_SIZE, L"Garuda_Init(hPackageModule = {"
	    PACKAGE_UNICODE_PTR_FMT L"}, packageFileName = {%s}, hTclModule = {"
	    PACKAGE_UNICODE_PTR_FMT L"}, pTclStubs = {" PACKAGE_UNICODE_PTR_FMT
	    L"}, interp = {" PACKAGE_UNICODE_PTR_FMT L"}, code = {%d})",
	    hPackageModule, (packageFileName != NULL) ? packageFileName : L"",
	    hTclModule, &uTclStubs, interp, code);

	TclLog(interp, logCommand, buffer, NULL);
    }

    /*
     * NOTE: If the configuration settings were successfully queried, free them
     *       now as they are no longer needed.
     */

    FreeClrConfigInfo(&pConfigInfo);

    /*
     * BUGFIX: Release the package mutex prior to calling the unload procedure
     *         for this package (i.e. in the event of a package load failure);
     *         previously, an attempt was made to unlock an already finalized
     *         mutex [in the event of a package load failure], thereby causing
     *         an access violation.
     */

    Tcl_MutexUnlock(&packageMutex);

    /*
     * NOTE: If some step of loading the package failed, attempt to cleanup now
     *       by unloading the package, either from just this Tcl interpreter or
     *       from the entire process.
     */

    if (code != TCL_OK) {
	/*
	 * BUGBUG: Perhaps it may be too harsh to stop and release the CLR
	 *         runtime host in the event of a failure in this function?
	 */

	if (Garuda_Unload(interp, TCL_UNLOAD_FROM_INIT | (bClrWasLoaded ?
		TCL_UNLOAD_DETACH_FROM_INTERPRETER :
		TCL_UNLOAD_DETACH_FROM_PROCESS)) != TCL_OK) {
	    /*
	     * NOTE: We failed to undo something and we have no nice way of
	     *       reporting this failure; therefore, complain about it.
	     */

	    PACKAGE_PANIC(("Garuda_Unload: failed via Garuda_Init\n"));
	}
    }

    return code;
}

/*
 *----------------------------------------------------------------------
 *
 * Garuda_SafeInit --
 *
 *	This function initializes the package for the specified "safe"
 *	Tcl interpreter.  Since all functionality provided by this
 *	package is aware of "safe" Tcl interpreters, no extra handling
 *	is needed here.
 *
 * Results:
 *	A standard Tcl result.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

int Garuda_SafeInit(
    Tcl_Interp *interp)			/* Current Tcl interpreter. */
{
    return Garuda_Init(interp);
}

/*
 *----------------------------------------------------------------------
 *
 * Garuda_Unload --
 *
 *	This function unloads the package from the specified Tcl
 *	interpreter -OR- from the entire process.
 *
 * Results:
 *	A standard Tcl result.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

int Garuda_Unload(
    Tcl_Interp *interp,			/* Current Tcl interpreter. */
    int flags)				/* Unload behavior flags. */
{
    int code = TCL_OK;
    ClrConfigInfo *pConfigInfo = NULL;
    BOOL bStopClr = TRUE; /* TODO: Good default? */
    BOOL bShutdown = (flags & TCL_UNLOAD_DETACH_FROM_PROCESS);
    BOOL bFromInit = (flags & TCL_UNLOAD_FROM_INIT);
    BOOL bFromCmdDelete = (flags & TCL_UNLOAD_FROM_CMD_DELETE);

    /*
     * NOTE: If the Tcl stubs mechanism has not been initialized, nothing can
     *       be done in this function; therefore, bail out early in that case.
     */

    if (InterlockedCompareExchange(&lTclStubs, 0, 0) == 0) { /* NON-PORTABLE */
	PACKAGE_TRACE(("Garuda_Unload: Tcl stubs are not initialized\n"));
	return TCL_ERROR;
    }

    /*
     * NOTE: Grab the package lock and hold onto it for the entire time we are
     *       cleaning up and unloading the package.
     */

    Tcl_MutexLock(&packageMutex);

    /*
     * NOTE: If we are unloading this package from the process, determine if we
     *       need to stop the CLR as well.  Normally, stopping the CLR is the
     *       right thing to do here; however, there are certain circumstances
     *       where this default behavior is undesirable.
     */

    if (bShutdown) {
	/*
	 * NOTE: Query the Tcl interpreter for the information required to
	 *       unload this package from the process.  The Tcl variables
	 *       being queried should have been set by code in our custom
	 *       "helper.tcl" file.  If not, this function call should fail.
	 */

        if (GetClrConfigInfo(interp, FALSE, FALSE, &pConfigInfo) == TCL_OK) {
	    bStopClr = pConfigInfo->bStopClr;

	    if (bStopClr) {
		PACKAGE_TRACE(("Garuda_Unload: configured to stop CLR\n"));
	    } else {
		PACKAGE_TRACE(("Garuda_Unload: configured not to stop CLR\n"));
	    }
	} else {
	    /*
	     * NOTE: Not much we can do at this point.  We are unable to obtain
	     *       the configuration variables; however, they are not [yet]
	     *       vital to our overall success.  Therefore, just log this
	     *       issue and carry on using the default (legacy) behavior.
	     */

	    if (bStopClr) {
		PACKAGE_TRACE(("Garuda_Unload: will stop CLR\n"));
	    } else {
		PACKAGE_TRACE(("Garuda_Unload: will not stop CLR\n"));
	    }
	}
    }

    /*
     * NOTE: If we have a valid Tcl interpreter, try to get the token for the
     *       command added to it when the package was being loaded.  We need to
     *       delete the command now because the whole library may be unloading.
     */

    if (interp != NULL) {
	if (!bFromCmdDelete) {
	    Tcl_Command command = Tcl_GetAssocData(interp, PACKAGE_NAME, NULL);

	    if (command != NULL) {
		if (Tcl_DeleteCommandFromToken(interp, command) != 0) {
		    Tcl_AppendResult(interp, "command deletion failed\n", NULL);
		    code = TCL_ERROR;
		    goto done;
		}
	    }
	}

	/*
	 * NOTE: Always delete our saved association data from the Tcl
	 *       interpreter because the Tcl_GetAssocData function does not
	 *       reserve any return value to indicate "failure" or "not found"
	 *       and calling the Tcl_DeleteAssocData function for association
	 *       data that does not exist is a harmless no-op.
	 */

	Tcl_DeleteAssocData(interp, PACKAGE_NAME);

	/*
	 * NOTE: If the bridge between Eagle and Tcl has never is not marked
	 *       as started (and may never have been started), there is not
	 *       much point in calling the detach or shutdown methods, even
	 *       though we could technically do so (assuming the CLR itself
	 *       is loaded and started).
	 */

	if (bBridgeStarted) {
	    /*
	     * NOTE: Try to execute the CLR method to shutdown the bridge
	     *       between Eagle and Tcl now.  If the CLR is not started
	     *       or not loaded, this does nothing and returns success.
	     *       This requires the Tcl interpreter to access the method
	     *       configuration information.
	     */

	    MethodFlags methodFlags = METHOD_VIA_UNLOAD |
		(bShutdown ? METHOD_TYPE_SHUTDOWN : METHOD_TYPE_DETACH);

	    /*
	     * NOTE: If needed, query the Tcl interpreter for the information
	     *       required to unload this package from the process.  The
	     *       Tcl variables being queried should have been set by code
	     *       in our custom "helper.tcl" file.  If not, this function
	     *       call should fail.
	     */

	    if (pConfigInfo == NULL) {
		code = GetClrConfigInfo(interp, FALSE, FALSE, &pConfigInfo);

		if (code != TCL_OK)
		    goto done;
	    }

	    /*
	     * NOTE: Take care to avoid resetting the Tcl interpreter result
	     *       if we are being called from Garuda_Init.
	     */

	    if (bFromInit)
		methodFlags &= ~METHOD_LOG_EXECUTE;

	    code = GetAndExecuteClrMethod(hTclModule, &uTclStubs, pConfigInfo,
		interp, NULL, methodFlags);

	    if (code != TCL_OK)
		goto done;

	    if (bShutdown)
		bBridgeStarted = FALSE;

	    /*
	     * NOTE: Remove any stray Tcl interpreter result that may have
	     *       been set by the above call to GetAndExecuteClrMethod
	     *       above from the Tcl interpreter result unless we are
	     *       being called due to a package load failure within the
	     *       Garuda_Init function.
	     */

	    if (!bFromInit)
		Tcl_ResetResult(interp);
	}
    }

    /*
     * NOTE: If we are unloading this package from the process, stop the CLR
     *       and release our reference to it now.  This operation cannot be
     *       undone because the CLR cannot be restarted in the process once
     *       it has been stopped or unloaded.  This will be skipped if the
     *       package has been configured to avoid automatically stopping the
     *       CLR.
     */

    if (bShutdown && bStopClr) {
	code = StopAndReleaseTheClr(interp, NULL, TRUE, FALSE);

	if (code != TCL_OK)
	    goto done;
    }

    /*
     * NOTE: Reset memory holding the Tcl C API stub function pointer table
     *       (only if we are being shutdown, because this is shared state
     *       between all Tcl interpreters).
     */

    if (bShutdown) {
	memset(&uTclStubs, 0, sizeof(ClrTclStubs));
    }

    /*
     * NOTE: Free the memory holding the package module file name now (only
     *       if we are being shutdown, because this is shared state between
     *       all Tcl interpreters).
     */

    if (bShutdown && (packageFileName != NULL)) {
	ckfree((LPVOID) packageFileName);
	packageFileName = NULL;
    }

    /*
     * NOTE: Delete our exit handler after performing the actions that needed
     *       to be undone.  However, this should only be done if the package
     *       is actually being unloaded from the process; otherwise, none of
     *       the process-wide cleanup was done and it must be done later.  If
     *       this function is actually being called from our exit handler now,
     *       trying to delete our exit handler will be a harmless no-op.
     */

    if (bShutdown)
	Tcl_DeleteExitHandler(GarudaExitProc, NULL);

done:
    /*
     * NOTE: If possible, log this attempt to unload the package, including
     *       the saved package module handle, the associated module file name,
     *       the current Tcl interpreter, the flags, and the return code.
     */

    PACKAGE_TRACE(("Garuda_Unload(hPackageModule = {" PACKAGE_PTR_FMT
	"}, packageFileName = {" PACKAGE_ISTR_FMT "}, hTclModule = {"
	PACKAGE_PTR_FMT "}, pTclStubs = {" PACKAGE_PTR_FMT "}, interp = {"
	PACKAGE_PTR_FMT "}, flags = {" PACKAGE_HEX_FMT "}, code = {%d})\n",
	hPackageModule, (packageFileName != NULL) ? packageFileName : L"",
	hTclModule, &uTclStubs, interp, flags, code));

    /*
     * NOTE: If the configuration settings were successfully queried, free them
     *       now as they are no longer needed.
     */

    FreeClrConfigInfo(&pConfigInfo);

    /*
     * NOTE: Unlock the package mutex now as we might be finalizing it just
     *       below (i.e. as the final step of unloading this package from
     *       the entire process).
     */

    Tcl_MutexUnlock(&packageMutex);

    /*
     * NOTE: If we are unloading this package from the process, finalize our
     *       mutex now.  Otherwise the Tcl finalization process may throw an
     *       access violation exception later (i.e. via Tcl_Finalize).
     */

    if ((code == TCL_OK) && bShutdown)
	Tcl_MutexFinalize(&packageMutex);

    return code;
}

/*
 *----------------------------------------------------------------------
 *
 * Garuda_SafeUnload --
 *
 *	This function unloads the package from the specified "safe"
 *	Tcl interpreter -OR- from the entire process.  Since all
 *	functionality provided by this package is aware of "safe"
 *	Tcl interpreters, no extra handling is needed here.
 *
 * Results:
 *	A standard Tcl result.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

int Garuda_SafeUnload(
    Tcl_Interp *interp,			/* Current Tcl interpreter. */
    int flags)				/* Unload behavior flags. */
{
    return Garuda_Unload(interp, flags);
}

/*
 *----------------------------------------------------------------------
 *
 * GarudaExitProc --
 *
 *	Cleanup all the resources allocated by this package.
 *
 * Results:
 *	None.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static void GarudaExitProc(
    ClientData clientData)		/* Not used. */
{
    if (Garuda_Unload(NULL,
	    TCL_UNLOAD_DETACH_FROM_PROCESS) != TCL_OK) {
	/*
	 * NOTE: We failed to undo something and we have no nice way of
	 *       reporting this failure; therefore, complain about it.
	 */

	PACKAGE_PANIC(("Garuda_Unload: failed via GarudaExitProc\n"));
    }
}

/*
 *----------------------------------------------------------------------
 *
 * GarudaObjCmd --
 *
 *	Handles the command(s) added by this package.  This command is
 *	aware of "safe" Tcl interpreters.  For "safe" Tcl interpreters,
 *	all introspection sub-commands are allowed with the exception
 *	of "dumpstate", which is forbidden; all other sub-commands are
 *	also forbidden.
 *
 * Results:
 *	A standard Tcl result.
 *
 * Side effects:
 *	The CLR may be started or stopped in this process, potentially
 *	causing arbitrary side-effects due to execution of third-party
 *	CLR code, Tcl scripts, or Eagle scripts.
 *
 *----------------------------------------------------------------------
 */

static int GarudaObjCmd(
    ClientData clientData,	/* Not used. */
    Tcl_Interp *interp,		/* Current Tcl interpreter. */
    int objc,			/* Number of arguments. */
    Tcl_Obj *CONST objv[])	/* The arguments. */
{
    int code = TCL_OK;
    int option;
    ClrConfigInfo *pConfigInfo = NULL;
    Tcl_Obj *listPtr = NULL;

    static CONST char *cmdOptions[] = {
	"bridgerunning", "clrappdomainid", "clrexecute", "clrload",
	"clrrunning", "clrstart", "clrstop", "clrversion", "control",
	"detach", "dumpstate", "packageid", "shutdown", "startup",
	(char *) NULL
    };

    enum options {
	OPT_BRIDGERUNNING, OPT_CLRAPPDOMAINID, OPT_CLREXECUTE, OPT_CLRLOAD,
	OPT_CLRRUNNING, OPT_CLRSTART, OPT_CLRSTOP, OPT_CLRVERSION, OPT_CONTROL,
	OPT_DETACH, OPT_DUMPSTATE, OPT_PACKAGEID, OPT_SHUTDOWN, OPT_STARTUP
    };

    if (interp == NULL) {
	return TCL_ERROR;
    }

    if (objc < 2) {
	Tcl_WrongNumArgs(interp, 1, objv, "option ?arg ...?");
	return TCL_ERROR;
    }

    if (Tcl_GetIndexFromObj(interp, objv[1], cmdOptions, "option", 0,
	    &option) != TCL_OK) {
	return TCL_ERROR;
    }

    Tcl_MutexLock(&packageMutex);

    switch ((enum options)option) {
	case OPT_BRIDGERUNNING: { /* SAFE */
	    Tcl_Obj *objPtr;

	    if (objc != 2) {
		Tcl_WrongNumArgs(interp, 2, objv, NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    objPtr = Tcl_NewIntObj(bBridgeStarted);

	    if (objPtr == NULL) {
		Tcl_AppendResult(interp, "out of memory: objPtr\n", NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    Tcl_IncrRefCount(objPtr);
	    Tcl_SetObjResult(interp, objPtr);
	    Tcl_DecrRefCount(objPtr);
	    break;
	}
	case OPT_CLRAPPDOMAINID: { /* SAFE */
	    HRESULT hResult;
	    DWORD appDomainId = 0;

	    if (objc != 2) {
		Tcl_WrongNumArgs(interp, 2, objv, NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    if (pClrRuntimeHost == NULL) {
		Tcl_AppendResult(interp, "CLR not loaded\n", NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    if (!bClrStarted) {
		Tcl_AppendResult(interp, "CLR not started\n", NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    hResult = ICLRRuntimeHost_GetCurrentAppDomainId(pClrRuntimeHost,
		&appDomainId);

	    if (SUCCEEDED(hResult)) {
		Tcl_Obj *objPtr = Tcl_NewLongObj(appDomainId);

		if (objPtr == NULL) {
		    Tcl_AppendResult(interp, "out of memory: objPtr\n", NULL);
		    code = TCL_ERROR;
		    goto done;
		}

		Tcl_IncrRefCount(objPtr);
		Tcl_SetObjResult(interp, objPtr);
		Tcl_DecrRefCount(objPtr);
	    } else {
		Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp),
		    GetClrErrorMessage(
			L"ICLRRuntimeHost_GetCurrentAppDomainId",
			hResult), -1);

		code = TCL_ERROR;
		goto done;
	    }
	    break;
	}
	case OPT_CLREXECUTE: {
	    DWORD returnValue = TCL_OK;

	    if (objc != 6) {
		Tcl_WrongNumArgs(interp, 2, objv,
		    "assemblyPath typeName methodName argument");

		code = TCL_ERROR;
		goto done;
	    }

	    if (Tcl_IsSafe(interp)) {
		Tcl_AppendResult(interp, "permission denied: safe interp\n",
		    NULL);

		code = TCL_ERROR;
		goto done;
	    }

	    code = GetClrConfigInfo(interp, FALSE, FALSE, &pConfigInfo);

	    if (code != TCL_OK)
		goto done;

	    code = DemandExecuteClrMethod(hTclModule, &uTclStubs, pConfigInfo,
		interp, objv[2], objv[3], objv[4], objv[5], METHOD_TYPE_DEMAND |
		METHOD_VIA_DEMAND, &returnValue);

	    if (code == TCL_OK) {
		Tcl_Obj *objPtr = Tcl_NewLongObj(returnValue);

		if (objPtr == NULL) {
		    Tcl_AppendResult(interp, "out of memory: objPtr\n", NULL);
		    code = TCL_ERROR;
		    goto done;
		}

		Tcl_IncrRefCount(objPtr);
		Tcl_SetObjResult(interp, objPtr);
		Tcl_DecrRefCount(objPtr);
	    }
	    break;
	}
	case OPT_CLRRUNNING: { /* SAFE */
	    Tcl_Obj *objPtr;

	    if (objc != 2) {
		Tcl_WrongNumArgs(interp, 2, objv, NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    objPtr = Tcl_NewIntObj(bClrStarted);

	    if (objPtr == NULL) {
		Tcl_AppendResult(interp, "out of memory: objPtr\n", NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    Tcl_IncrRefCount(objPtr);
	    Tcl_SetObjResult(interp, objPtr);
	    Tcl_DecrRefCount(objPtr);
	    break;
	}
	case OPT_CLRLOAD: {
	    if (objc != 2) {
		Tcl_WrongNumArgs(interp, 2, objv, NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    if (Tcl_IsSafe(interp)) {
		Tcl_AppendResult(interp, "permission denied: safe interp\n",
		    NULL);

		code = TCL_ERROR;
		goto done;
	    }

	    code = GetClrConfigInfo(interp, TRUE, FALSE, &pConfigInfo);

	    if (code != TCL_OK)
		goto done;

	    code = LoadAndStartTheClr(interp, pConfigInfo->logCommand, TRUE,
		FALSE, FALSE, TRUE);

	    break;
	}
	case OPT_CLRSTART: {
	    if (objc != 2) {
		Tcl_WrongNumArgs(interp, 2, objv, NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    if (Tcl_IsSafe(interp)) {
		Tcl_AppendResult(interp, "permission denied: safe interp\n",
		    NULL);

		code = TCL_ERROR;
		goto done;
	    }

	    code = GetClrConfigInfo(interp, TRUE, FALSE, &pConfigInfo);

	    if (code != TCL_OK)
		goto done;

	    code = LoadAndStartTheClr(interp, pConfigInfo->logCommand, FALSE,
		FALSE, TRUE, TRUE);

	    break;
	}
	case OPT_CLRSTOP: {
	    if (objc != 2) {
		Tcl_WrongNumArgs(interp, 2, objv, NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    if (Tcl_IsSafe(interp)) {
		Tcl_AppendResult(interp, "permission denied: safe interp\n",
		    NULL);

		code = TCL_ERROR;
		goto done;
	    }

	    code = GetClrConfigInfo(interp, TRUE, FALSE, &pConfigInfo);

	    if (code != TCL_OK)
		goto done;

	    /*
	     * NOTE: Stop the CLR within this process now.  This operation
	     *       cannot be undone because the CLR cannot be restarted
	     *       in the process once it has been stopped or unloaded.
	     */

	    code = StopAndReleaseTheClr(interp, pConfigInfo->logCommand,
		FALSE, TRUE);

	    break;
	}
	case OPT_CLRVERSION: { /* SAFE */
	    HRESULT hResult;
	    WCHAR buffer[PACKAGE_RESULT_SIZE + 1] = {0};
	    DWORD length;

	    if (objc != 2) {
		Tcl_WrongNumArgs(interp, 2, objv, NULL);
		code = TCL_ERROR;
		goto done;
	    }

#if defined(USE_CLR_40)
	    if (pClrRuntimeInfo == NULL) {
		Tcl_AppendResult(interp, "CLR not loaded\n", NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    length = PACKAGE_RESULT_SIZE;

	    hResult = ICLRRuntimeInfo_GetVersionString(pClrRuntimeInfo, buffer,
		&length);
#else
	    hResult = GetCORVersion(buffer, PACKAGE_RESULT_SIZE, &length);
#endif

	    if (SUCCEEDED(hResult)) {
		Tcl_Obj *objPtr = Tcl_NewUnicodeObj(buffer, -1);

		if (objPtr == NULL) {
		    Tcl_AppendResult(interp, "out of memory: objPtr\n", NULL);
		    code = TCL_ERROR;
		    goto done;
		}

		Tcl_IncrRefCount(objPtr);
		Tcl_SetObjResult(interp, objPtr);
		Tcl_DecrRefCount(objPtr);
	    } else {
		Tcl_AppendUnicodeToObj(Tcl_GetObjResult(interp),
#if defined(USE_CLR_40)
		    GetClrErrorMessage(L"ICLRRuntimeInfo_GetVersionString",
			hResult), -1);
#else
		    GetClrErrorMessage(L"GetCORVersion", hResult), -1);
#endif

		code = TCL_ERROR;
		goto done;
	    }
	    break;
	}
	case OPT_CONTROL: {
	    if (Tcl_IsSafe(interp)) {
		Tcl_AppendResult(interp, "permission denied: safe interp\n",
		    NULL);

		code = TCL_ERROR;
		goto done;
	    }

	    if (objc > 2) {
		listPtr = Tcl_NewListObj(objc - 2, objv + 2);

		if (listPtr == NULL) {
		    Tcl_AppendResult(interp, "out of memory: listPtr\n", NULL);
		    goto done;
		}

		Tcl_IncrRefCount(listPtr);
	    }

	    code = GetClrConfigInfo(interp, FALSE, FALSE, &pConfigInfo);

	    if (code != TCL_OK)
		goto done;

	    code = GetAndExecuteClrMethod(hTclModule, &uTclStubs, pConfigInfo,
		interp, (listPtr != NULL) ? Tcl_GetUnicode(listPtr) : NULL,
		METHOD_TYPE_CONTROL | METHOD_VIA_COMMAND);

	    break;
	}
	case OPT_DETACH: {
	    if (objc != 2) {
		Tcl_WrongNumArgs(interp, 2, objv, NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    if (Tcl_IsSafe(interp)) {
		Tcl_AppendResult(interp, "permission denied: safe interp\n",
		    NULL);

		code = TCL_ERROR;
		goto done;
	    }

	    code = GetClrConfigInfo(interp, FALSE, FALSE, &pConfigInfo);

	    if (code != TCL_OK)
		goto done;

	    code = GetAndExecuteClrMethod(hTclModule, &uTclStubs, pConfigInfo,
		interp, NULL, METHOD_TYPE_DETACH | METHOD_VIA_COMMAND);

	    break;
	}
	case OPT_DUMPSTATE: {
	    Tcl_Obj *objPtr;
	    WCHAR buffer[PACKAGE_RESULT_SIZE + 1] = {0};

	    if (objc != 2) {
		Tcl_WrongNumArgs(interp, 2, objv, NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    if (Tcl_IsSafe(interp)) {
		Tcl_AppendResult(interp, "permission denied: safe interp\n",
		    NULL);

		code = TCL_ERROR;
		goto done;
	    }

	    gwprintf(buffer, PACKAGE_RESULT_SIZE, L"packageMutex "
		PACKAGE_UNICODE_PTR_FMT L" hPackageModule "
		PACKAGE_UNICODE_PTR_FMT
		L" packageFileName {%s} lTclStubs %ld hTclModule "
		PACKAGE_UNICODE_PTR_FMT L" pTclStubs "
		PACKAGE_UNICODE_PTR_FMT
#if defined(USE_CLR_40)
		L" pClrMetaHost " PACKAGE_UNICODE_PTR_FMT
		L" pClrRuntimeInfo " PACKAGE_UNICODE_PTR_FMT
#endif
		L" pClrRuntimeHost " PACKAGE_UNICODE_PTR_FMT
		L" bClrStarted %d bBridgeStarted %d", packageMutex,
		hPackageModule, packageFileName, lTclStubs, hTclModule,
		&uTclStubs,
#if defined(USE_CLR_40)
		pClrMetaHost, pClrRuntimeInfo,
#endif
		pClrRuntimeHost, bClrStarted, bBridgeStarted);

	    objPtr = Tcl_NewUnicodeObj(buffer, -1);

	    if (objPtr == NULL) {
		Tcl_AppendResult(interp, "out of memory: objPtr\n", NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    Tcl_IncrRefCount(objPtr);
	    Tcl_SetObjResult(interp, objPtr);
	    Tcl_DecrRefCount(objPtr);
	    break;
	}
	case OPT_PACKAGEID: { /* SAFE */
	    if (objc != 2) {
		Tcl_WrongNumArgs(interp, 2, objv, NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    Tcl_AppendResult(interp, PACKAGE_NAME, " ", PACKAGE_VERSION,
		" ", SOURCE_ID " {" SOURCE_TIMESTAMP "}", NULL);

	    break;
	}
	case OPT_SHUTDOWN: {
	    if (objc != 2) {
		Tcl_WrongNumArgs(interp, 2, objv, NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    if (Tcl_IsSafe(interp)) {
		Tcl_AppendResult(interp, "permission denied: safe interp\n",
		    NULL);

		code = TCL_ERROR;
		goto done;
	    }

	    code = GetClrConfigInfo(interp, FALSE, FALSE, &pConfigInfo);

	    if (code != TCL_OK)
		goto done;

	    code = GetAndExecuteClrMethod(hTclModule, &uTclStubs, pConfigInfo,
		interp, NULL, METHOD_TYPE_SHUTDOWN | METHOD_VIA_COMMAND);

	    if (code == TCL_OK)
		bBridgeStarted = FALSE;

	    break;
	}
	case OPT_STARTUP: {
	    if (objc != 2) {
		Tcl_WrongNumArgs(interp, 2, objv, NULL);
		code = TCL_ERROR;
		goto done;
	    }

	    if (Tcl_IsSafe(interp)) {
		Tcl_AppendResult(interp, "permission denied: safe interp\n",
		    NULL);

		code = TCL_ERROR;
		goto done;
	    }

	    code = GetClrConfigInfo(interp, FALSE, FALSE, &pConfigInfo);

	    if (code != TCL_OK)
		goto done;

	    code = GetAndExecuteClrMethod(hTclModule, &uTclStubs, pConfigInfo,
		interp, NULL, METHOD_TYPE_STARTUP | METHOD_VIA_COMMAND);

	    if (code == TCL_OK)
		bBridgeStarted = TRUE;

	    break;
	}
	default: {
	    Tcl_AppendResult(interp, "bad option index\n", NULL);
	    code = TCL_ERROR;
	    goto done;
	}
    }

done:
    if (listPtr != NULL) {
	Tcl_DecrRefCount(listPtr);
	listPtr = NULL;
    }

    FreeClrConfigInfo(&pConfigInfo);

    Tcl_MutexUnlock(&packageMutex);
    return code;
}

/*
 *----------------------------------------------------------------------
 *
 * GarudaObjCmdDeleteProc --
 *
 *	Handles deletion of the command(s) added by this package.
 *	This will cause the saved package data associated with the
 *	Tcl interpreter to be deleted, if it has not been already.
 *
 * Results:
 *	None.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static void GarudaObjCmdDeleteProc(
    ClientData clientData)	/* Current Tcl interpreter. */
{
    /*
     * NOTE: The client data for this callback function should be the
     *       pointer to the Tcl interpreter.  It must be valid.
     */

    Tcl_Interp *interp = (Tcl_Interp *) clientData;

    if (interp == NULL) {
	PACKAGE_TRACE(("GarudaObjCmdDeleteProc: no Tcl interpreter\n"));
	return;
    }

    /*
     * BUGFIX: The command (or the entire Tcl interpreter) is being deleted;
     *         make sure to call the configured detach method on the managed
     *         side and then cleanup our associated native state, if any.
     */

    if (Garuda_Unload(interp, TCL_UNLOAD_FROM_CMD_DELETE |
	    TCL_UNLOAD_DETACH_FROM_INTERPRETER) != TCL_OK) {
	/*
	 * NOTE: We failed to undo something and we have no nice way of
	 *       reporting this failure; therefore, complain about it.
	 */

	PACKAGE_PANIC((
	    "Garuda_Unload: failed via GarudaObjCmdDeleteProc\n"));
    }
}
