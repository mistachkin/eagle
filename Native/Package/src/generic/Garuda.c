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

#include "GarudaPre.h"		    /* NOTE: For private header setup. */
#include <stdio.h>		    /* NOTE: For fprintf, swprintf, va_list, etc. */
#include <string.h>		    /* NOTE: For memset, wcslen, wcsncpy, etc. */

#if !defined(_WIN32)
#  include <limits.h>		    /* NOTE: For INT_MAX, etc. */
#  include <wchar.h>		    /* NOTE: For wchar_t, etc. */
#  include <pthread.h>		    /* NOTE: For pthread_self, etc. */
#  include <stdatomic.h>	    /* NOTE: For atomic_fetch_add, etc. */
#  include <stdbool.h>		    /* NOTE: For true, false, etc. */
#endif

#if defined(USE_CORE_CLR)
#  include <nethost.h>		    /* NOTE: For get_hostfxr_path, etc. */
#  include <hostfxr.h>		    /* NOTE: For "hostfxr_*" .NET (Core), etc. */
#  include <coreclr_delegates.h>    /* NOTE: For load_<asm>_and_get_<fn_ptr>. */
#endif

#include "tcl.h"		    /* NOTE: For public Tcl API. */
#include "tclInt.h"		    /* HACK: For internal Tcl API. */
#include "stubs.h"		    /* NOTE: #define and #pragma magic for stubs. */
#include "GarudaPal.h"		    /* HACK: For portability API. */
#include "pkgVersion.h"		    /* NOTE: Package version information. */
#include "Garuda.h"		    /* NOTE: For public package API. */
#include "GarudaInt.h"		    /* NOTE: For private package API. */
#include "GarudaDecls.h"	    /* NOTE: For private package declarations. */
#include "ConvertUTF_v2.h"	    /* NOTE: Unicode UTF-* reference conversions. */
#include "GarudaStr.h"		    /* NOTE: For private string API. */

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
static LPCWSTR		GetTclErrorMessage(LPCWSTR source, int code);
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
static void		MaybeCombineMethodFlags(ClrConfigInfo *pConfigInfo,
			    MethodFlags *pMethodFlags);
static int		GetAndExecuteClrMethod(HMODULE hModule,
			    ClrTclStubs *pTclStubs, ClrConfigInfo *pConfigInfo,
			    Tcl_Interp *interp, LPCWSTR argument,
			    MethodFlags methodFlags);
static int		DemandExecuteClrMethod(HMODULE hModule,
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
 * NOTE: This mutex is used to protect access to all static state.  This
 *       should be using the TCL_DECLARE_MUTEX macro; however, this mutex
 *       cannot be static as it is needed by multiple source code files.
 */

#if defined(TCL_THREADS)
Tcl_Mutex packageMutex = { 0 };

/*
 * NOTE: On non-Windows, this structure is used to "simulate" mutexes that
 *       are capable of being used recursively.
 */

#if defined(USE_CORE_CLR) && !defined(_WIN32)
pthread_owner_t packageOwner = {
    sizeof(pthread_owner_t), PTHREAD_NULL, 0
};
#endif
#endif

/*
 * NOTE: This define (and its associated "constant") is needed to abstract
 *       away platform differences for some interlocked operations, e.g.
 *       (interlocked-)compare-and-swap.
 */

#if defined(USE_CORE_CLR) && !defined(_WIN32)
static LONG atomicLongZero = 0;

#  define ATOMIC_LONG_ZERO			(&atomicLongZero)
#  define ATOMIC_TRUE				(true)
#else
#  define ATOMIC_LONG_ZERO			(0)
#  define ATOMIC_TRUE				(0)
#endif

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

static volatile LONG lTclStubs = 0;

/*
 * NOTE: The Tcl library module handle.  This is needed to pass to the bridge
 *       so that it can be used as the basis for looking up functions exported
 *       from the [already] loaded Tcl library.
 */

static volatile HMODULE hTclModule = NULL;

/*
 * NOTE: The Tcl C API function pointers required by the Eagle native Tcl
 *       integration subsystem.  This is needed to pass to the bridge so that
 *       it can be used as the basis for calling the functions exported from
 *       the [already] loaded Tcl library.
 */

static ClrTclStubs uTclStubs = { 0 };

/*
 * NOTE: The logical list of package names that will be provided to the Tcl
 *       interpreter from within the Garuda_Init function.
 */

static const char *packageNames[] = {
    PACKAGE_NAME, PACKAGE_NAME_1, PACKAGE_NAME_2, PACKAGE_NAME_3, NULL
};

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
    BOOL bResult = FALSE;
    LPWSTR result[2] = { NULL, NULL };
    DWORD size;

    /*
     * NOTE: The file name output pointer is required and must be valid.  It
     *       is set to the location of the file name buffer on success -OR-
     *       NULL upon any [other] failure.
     */

    if (pFileName == NULL)
	goto done;

    /*
     * HACK: The GetModuleFileName Win32 API has no clean way to report the
     *       exact size without guesswork.  Therefore, start off by using the
     *       maximum possible size for a WinNT file name.  If allocating this
     *       buffer fails, we cannot continue.
     */

    size = UNICODE_STRING_MAX_CHARS;
    result[0] = (LPWSTR)attemptckalloc((size + 1) * sizeof(WCHAR));

    if (result[0] == NULL) {
	*pFileName = NULL;
	goto done;
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

    size = Wrp_get_module_file_name(hModule, result[0], size);

    if (size == 0) {
	*pFileName = NULL;
	goto done;
    }

    /*
     * NOTE: The module file name was obtained successfully.  Now, attempt to
     *       allocate a new buffer of exactly the needed size.  If this fails,
     *       free the previously allocated file name buffer and return failure
     *       to the caller.
     */

    result[1] = (LPWSTR)attemptckalloc((size + 1) * sizeof(WCHAR));

    if (result[1] == NULL) {
	*pFileName = NULL;
	goto done;
    }

    /*
     * NOTE: Success, copy exactly the number of bytes necessary to store the
     *       file name (including the terminating NUL character) from the
     *       originally allocated file name buffer.  Free the original file
     *       name buffer.  Finally, return success to the caller.
     */

    memcpy(result[1], result[0], (size + 1) * sizeof(WCHAR));

    *pFileName = result[1];
    bResult = TRUE;

done:

    if (!bResult && (result[1] != NULL)) {
	ckfree((LPVOID)result[1]);
	result[1] = NULL;
    }

    if (result[0] != NULL) {
	ckfree((LPVOID)result[0]);
	result[0] = NULL;
    }

    return bResult;
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

int TracePrintf(
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
    fprintf(stderr, PACKAGE_CSTR_FMT, buffer);
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
	gwprintf(message, PACKAGE_RESULT_SIZE, PACKAGE_UNICODE_STR_FMT
	    L": " PACKAGE_UNICODE_STR_FMT L" (code %d).\n", source,
	    severity, code);
    } else {
	gwprintf(message, PACKAGE_RESULT_SIZE, PACKAGE_UNICODE_STR_FMT
	    L" (code %d).\n", severity, code);
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

LPCWSTR GetClrErrorMessage(
    LPCWSTR source,		/* The original source of the failure,
				 * NULL if unknown or unavailable. */
    HRESULT hResult)		/* The CLR error code. */
{
    static WCHAR message[PACKAGE_RESULT_SIZE + 1] = {0};
    LPCWSTR severity = SUCCEEDED(hResult) ? L"success" : L"failure";

    if (source != NULL) {
	gwprintf(message, PACKAGE_RESULT_SIZE, PACKAGE_UNICODE_STR_FMT
	    L": " PACKAGE_UNICODE_STR_FMT " (code 0x%lX).\n", source,
	    severity, (unsigned long)hResult);
    } else {
	gwprintf(message, PACKAGE_RESULT_SIZE, PACKAGE_UNICODE_STR_FMT
	    L" (code 0x%lX).\n", severity, (unsigned long)hResult);
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

void TclLog(
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

    objv[0] = Wrp_NewUnicodeObj(logCommand, -1);

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

	Wrp_AppendUnicodeToObj(objv[1], arg, -1);
    }

    va_end(argList);

    Tcl_SaveResult(interp, &savedResult);
    code = Tcl_EvalObjv(interp, 2, objv, TCL_EVAL_GLOBAL);
    Tcl_RestoreResult(interp, &savedResult);

    if (code == TCL_OK) {
	LPCWSTR args;

	Wrp_AppendUnicodeToObj(objv[1], L"\n", -1);
	args = Wrp_GetUnicode(objv[1]);

	if (args != NULL) {
#if defined(_WIN32)
	    OutputDebugStringW(args); /* NON-PORTABLE */
#else
	    fwprintf(stderr, PACKAGE_UNICODE_STR_FMT, args);

	    ckfree((LPVOID)args);
	    args = NULL;
#endif
	}
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
    LPCWSTR objValue = NULL;
    LPWSTR result = NULL;

    if (interp == NULL)
	return NULL;

    objValue = Wrp_GetUnicodeFromObj(objPtr, &length);

    if ((objValue == NULL) || (length < 0)) {
	Tcl_AppendResult(interp, "object value is invalid\n", NULL);
	goto done;
    }

#if defined(_WIN32)
    result = (LPWSTR)attemptckalloc((length + 1) * sizeof(WCHAR));

    if (result == NULL) {
	Tcl_AppendResult(interp, "out of memory: objValue\n", NULL);
	goto done;
    }

    memset(result, 0, (length + 1) * sizeof(WCHAR));
    wcsncpy(result, objValue, length + 1);
#else
    result = (LPWSTR)objValue;
#endif

    if (lengthPtr != NULL)
	*lengthPtr = length;

done:

#if !defined(_WIN32)
    if ((objValue != NULL) && (objValue != result)) {
	ckfree((LPVOID)objValue);
	objValue = NULL;
    }
#endif

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
    LPCWSTR varValue = NULL;
    LPWSTR result = NULL;

    if (interp == NULL)
	return NULL;

    if (varName == NULL) {
	Tcl_AppendResult(interp, "invalid variable name\n", NULL);
	return NULL;
    }

    part1Ptr = Wrp_NewUnicodeObj(varName, -1);

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
	Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp), varName, -1);
	Tcl_AppendResult(interp, "\n", NULL);
	goto done;
    }

    varValue = Wrp_GetUnicodeFromObj(objPtr, &length);

    if ((varValue == NULL) || (length < 0)) {
	Tcl_AppendResult(interp, "variable value is invalid: ", NULL);
	Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp), varName, -1);
	Tcl_AppendResult(interp, "\n", NULL);
	goto done;
    }

#if defined(_WIN32)
    result = (LPWSTR)attemptckalloc((length + 1) * sizeof(WCHAR));

    if (result == NULL) {
	Tcl_AppendResult(interp, "out of memory: varValue\n", NULL);
	goto done;
    }

    memset(result, 0, (length + 1) * sizeof(WCHAR));
    wcsncpy(result, varValue, length + 1);
#else
    result = (LPWSTR)varValue;
#endif

    if (lengthPtr != NULL)
	*lengthPtr = length;

done:

#if !defined(_WIN32)
    if ((varValue != NULL) && (varValue != result)) {
	ckfree((LPVOID)varValue);
	varValue = NULL;
    }
#endif

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

    part1Ptr = Wrp_NewUnicodeObj(varName, -1);

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
	Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp), varName, -1);
	Tcl_AppendResult(interp, "\n", NULL);
	goto done;
    }

    code = Tcl_GetBooleanFromObj(interp, objPtr, &result);

    if (code != TCL_OK) {
	Tcl_AppendResult(interp, "variable value is invalid: ", NULL);
	Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp), varName, -1);
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

    part1Ptr = Wrp_NewUnicodeObj(varName, -1);

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
	Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp), varName, -1);
	Tcl_AppendResult(interp, "\n", NULL);
	goto done;
    }

    code = Tcl_GetIntFromObj(interp, objPtr, &result);

    if (code != TCL_OK) {
	Tcl_AppendResult(interp, "variable value is invalid: ", NULL);
	Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp), varName, -1);
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

    *ppMethodInfo = (ClrMethodInfo *)attemptckalloc(sizeof(ClrMethodInfo));

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

    *ppMethodInfo = (ClrMethodInfo *)attemptckalloc(sizeof(ClrMethodInfo));

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
	ckfree((LPVOID)(*ppMethodInfo)->argument);
	(*ppMethodInfo)->argument = NULL;
    }

    if ((*ppMethodInfo)->methodName != NULL) {
	ckfree((LPVOID)(*ppMethodInfo)->methodName);
	(*ppMethodInfo)->methodName = NULL;
    }

    if ((*ppMethodInfo)->typeName != NULL) {
	ckfree((LPVOID)(*ppMethodInfo)->typeName);
	(*ppMethodInfo)->typeName = NULL;
    }

    if ((*ppMethodInfo)->assemblyPath != NULL) {
	ckfree((LPVOID)(*ppMethodInfo)->assemblyPath);
	(*ppMethodInfo)->assemblyPath = NULL;
    }

    ckfree((LPVOID)*ppMethodInfo);
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

    *ppConfigInfo = (ClrConfigInfo *)attemptckalloc(sizeof(ClrConfigInfo));

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

	(*ppConfigInfo)->runtimeConfigPath = GetStringVariableValue(interp,
	    PACKAGE_UNICODE_RUNTIME_CONFIG_PATH_VAR_NAME, &length);

#if defined(USE_CORE_CLR)
	if (((*ppConfigInfo)->runtimeConfigPath == NULL) || (length <= 0)) {
	    Tcl_AppendResult(interp,
		"invalid runtime configuration path\n", NULL);

	    return TCL_ERROR;
	}
#endif

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
	ckfree((LPVOID)(*ppConfigInfo)->logCommand);
	(*ppConfigInfo)->logCommand = NULL;
    }

    if ((*ppConfigInfo)->runtimeConfigPath != NULL) {
	ckfree((LPVOID)(*ppConfigInfo)->runtimeConfigPath);
	(*ppConfigInfo)->runtimeConfigPath = NULL;
    }

    FreeClrMethodInfo(&(*ppConfigInfo)->pShutdownMethod);
    FreeClrMethodInfo(&(*ppConfigInfo)->pDetachMethod);
    FreeClrMethodInfo(&(*ppConfigInfo)->pControlMethod);
    FreeClrMethodInfo(&(*ppConfigInfo)->pStartupMethod);

    ckfree((LPVOID)*ppConfigInfo);
    *ppConfigInfo = NULL;
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
    HMODULE hModule,		/* Tcl library module handle. */
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

#if defined(USE_CORE_CLR)
    if (!CanExecuteCoreClrCode(interp)) {
#else
    if (!CanExecuteClrCode(interp)) {
#endif
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

#if defined(USE_CORE_CLR)
    code = ExecuteCoreClrMethod(hModule, pTclStubs, interp,
	pConfigInfo->logCommand, pMethodInfo, argument, methodFlags,
	&returnValue);
#else
    code = ExecuteClrMethod(hModule, pTclStubs, interp,
	pConfigInfo->logCommand, pMethodInfo, argument, methodFlags,
	&returnValue);
#endif

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

	    gwprintf(buffer, PACKAGE_RESULT_SIZE, PACKAGE_UNICODE_STR_FMT
		L" return value not TCL_OK, method: \""
		PACKAGE_UNICODE_STR_FMT L"." PACKAGE_UNICODE_STR_FMT
		L"\", assembly: \"" PACKAGE_UNICODE_STR_FMT L"\"\0",
		methodTypeName, pMethodInfo->typeName,
		pMethodInfo->methodName, pMethodInfo->assemblyPath);

	    Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp),
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
    HMODULE hModule,		/* Tcl library module handle. */
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

#if defined(USE_CORE_CLR)
    code = ExecuteCoreClrMethod(hModule, pTclStubs, interp,
	pConfigInfo->logCommand, pMethodInfo, NULL, methodFlags,
	pReturnValue);
#else
    code = ExecuteClrMethod(hModule, pTclStubs, interp,
	pConfigInfo->logCommand, pMethodInfo, NULL, methodFlags,
	pReturnValue);
#endif

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
    int index;

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
    Wrp_MutexLock(&packageMutex);

    /*
     * NOTE: Query the package module file name, before proceeding further.
     */

    if (!GetPackageModuleFileName(GetPackageModule(), &packageFileName)) {
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
#if defined(_WIN32)
	/* NON-PORTABLE */
	hTclModule = TclWinGetTclInstance(); /* HACK: Requires "tclInt.h". */
#else
	hTclModule = get_tcl_module_handle(); /* HACK: Requires "dladdr". */
#endif
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

#if defined(USE_CORE_CLR)
    bClrWasLoaded = GetCoreClrWasLoaded();
    bClrWasStarted = GetCoreClrWasStarted();
#else
    bClrWasLoaded = GetClrWasLoaded();
    bClrWasStarted = GetClrWasStarted();
#endif

    /*
     * NOTE: Load [and possibly start] the CLR now.
     */

#if defined(USE_CORE_CLR)
    code = LoadAndStartTheCoreClr(interp, logCommand,
	pConfigInfo->runtimeConfigPath, pConfigInfo->bLoadClr,
	pConfigInfo->bUseMinimumClr, pConfigInfo->bStartClr, FALSE);
#else
    code = LoadAndStartTheClr(interp, logCommand,
	pConfigInfo->runtimeConfigPath, pConfigInfo->bLoadClr,
	pConfigInfo->bUseMinimumClr, pConfigInfo->bStartClr, FALSE);
#endif

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

#if defined(USE_CORE_CLR)
	SetCoreClrBridgeStarted(TRUE);
#else
	SetClrBridgeStarted(TRUE);
#endif
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
     * NOTE: Attempt to provide various package names to the Tcl interpreter.
     */

    for (index = 0; packageNames[index] != NULL; index++) {
	code = Tcl_PkgProvide(interp, packageNames[index], PACKAGE_VERSION);

	if (code != TCL_OK)
	    goto done;
    }

done:

    /*
     * NOTE: If possible, log this attempt to initialize the package, including
     *       the saved package module handle, the associated module file name,
     *       the current Tcl interpreter, and the return code.
     */

    if (PACKAGE_CAN_LOG(interp, logCommand)) {
	WCHAR buffer[PACKAGE_RESULT_SIZE + 1] = {0};

	gwprintf(buffer, PACKAGE_RESULT_SIZE, L"Garuda_Init(hPackageModule = {"
	    PACKAGE_UNICODE_PTR_FMT L"}, packageFileName = {"
	    PACKAGE_UNICODE_STR_FMT L"}, hTclModule = {"
	    PACKAGE_UNICODE_PTR_FMT L"}, pTclStubs = {" PACKAGE_UNICODE_PTR_FMT
	    L"}, interp = {" PACKAGE_UNICODE_PTR_FMT L"}, code = {%d})",
	    GetPackageModule(), (packageFileName != NULL) ?
	    packageFileName : L"", hTclModule, &uTclStubs, interp, code);

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

    Wrp_MutexUnlock(&packageMutex);

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

    if (InterlockedCompareExchange(
	    &lTclStubs, 0, ATOMIC_LONG_ZERO) == ATOMIC_TRUE) {
	PACKAGE_TRACE(("Garuda_Unload: Tcl stubs are not initialized\n"));
	return TCL_ERROR;
    }

    /*
     * NOTE: Grab the package lock and hold onto it for the entire time we are
     *       cleaning up and unloading the package.
     */

    Wrp_MutexLock(&packageMutex);

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

#if defined(USE_CORE_CLR)
	if (GetCoreClrBridgeStarted()) {
#else
	if (GetClrBridgeStarted()) {
#endif
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

	    if (bShutdown) {
#if defined(USE_CORE_CLR)
		SetCoreClrBridgeStarted(FALSE);
#else
		SetClrBridgeStarted(FALSE);
#endif
	    }

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
#if defined(USE_CORE_CLR)
	code = StopAndReleaseTheCoreClr(interp, NULL, TRUE, FALSE);
#else
	code = StopAndReleaseTheClr(interp, NULL, TRUE, FALSE);
#endif

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
	ckfree((LPVOID)packageFileName);
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
	GetPackageModule(), (packageFileName != NULL) ? packageFileName : L"",
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

    Wrp_MutexUnlock(&packageMutex);

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
	"clrappdomainid", "clrbridgerunning", "clrexecute", "clrload",
	"clrrunning", "clrstart", "clrstop", "clrversion", "control",
	"detach", "dumpstate", "packageid", "shutdown", "startup",
	(char *) NULL
    };

    enum options {
	OPT_CLRAPPDOMAINID, OPT_CLRBRIDGERUNNING, OPT_CLREXECUTE, OPT_CLRLOAD,
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

    Wrp_MutexLock(&packageMutex);

    switch ((enum options)option) {
	case OPT_CLRAPPDOMAINID: { /* SAFE */
	    HRESULT hResult;
	    DWORD appDomainId = 0;

	    if (objc != 2) {
		Tcl_WrongNumArgs(interp, 2, objv, NULL);
		code = TCL_ERROR;
		goto done;
	    }

#if defined(USE_CORE_CLR)
	    hResult = GetCurrentCoreClrAppDomainId(&appDomainId);
#else
	    hResult = GetCurrentClrAppDomainId(&appDomainId);
#endif

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
		Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp),
		    GetClrErrorMessage(
			L"ICLRRuntimeHost_GetCurrentAppDomainId",
			hResult), -1);

		code = TCL_ERROR;
		goto done;
	    }
	    break;
	}
	case OPT_CLRBRIDGERUNNING: { /* SAFE */
	    Tcl_Obj *objPtr;

	    if (objc != 2) {
		Tcl_WrongNumArgs(interp, 2, objv, NULL);
		code = TCL_ERROR;
		goto done;
	    }

#if defined(USE_CORE_CLR)
	    objPtr = Tcl_NewIntObj(GetCoreClrBridgeStarted());
#else
	    objPtr = Tcl_NewIntObj(GetClrBridgeStarted());
#endif

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

#if defined(USE_CORE_CLR)
	    objPtr = Tcl_NewIntObj(GetCoreClrWasStarted());
#else
	    objPtr = Tcl_NewIntObj(GetClrWasStarted());
#endif

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

#if defined(USE_CORE_CLR)
	    code = LoadAndStartTheCoreClr(interp, pConfigInfo->logCommand,
		pConfigInfo->runtimeConfigPath, TRUE, FALSE, FALSE, TRUE);
#else
	    code = LoadAndStartTheClr(interp, pConfigInfo->logCommand,
		pConfigInfo->runtimeConfigPath, TRUE, FALSE, FALSE, TRUE);
#endif

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

#if defined(USE_CORE_CLR)
	    code = LoadAndStartTheCoreClr(interp, pConfigInfo->logCommand,
		pConfigInfo->runtimeConfigPath, FALSE, FALSE, TRUE, TRUE);
#else
	    code = LoadAndStartTheClr(interp, pConfigInfo->logCommand,
		pConfigInfo->runtimeConfigPath, FALSE, FALSE, TRUE, TRUE);
#endif

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

#if defined(USE_CORE_CLR)
	    code = StopAndReleaseTheCoreClr(interp, pConfigInfo->logCommand,
		FALSE, TRUE);
#else
	    code = StopAndReleaseTheClr(interp, pConfigInfo->logCommand,
		FALSE, TRUE);
#endif

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

	    length = PACKAGE_RESULT_SIZE;

#if defined(USE_CORE_CLR)
	    hResult = GetCoreClrVersion(buffer, &length);
#else
	    hResult = GetClrVersion(buffer, &length);
#endif

	    if (SUCCEEDED(hResult)) {
		Tcl_Obj *objPtr;

		objPtr = Wrp_NewUnicodeObj(buffer, -1);

		if (objPtr == NULL) {
		    Tcl_AppendResult(interp, "out of memory: objPtr\n", NULL);
		    code = TCL_ERROR;
		    goto done;
		}

		Tcl_IncrRefCount(objPtr);
		Tcl_SetObjResult(interp, objPtr);
		Tcl_DecrRefCount(objPtr);
	    } else {
		Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp),
#if defined(USE_CORE_CLR)
		    GetClrErrorMessage(L"hostfxr_get_dotnet_environment_info",
			hResult), -1);
#elif defined(USE_CLR_40)
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
	    LPCWSTR argument = NULL;

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

	    if (listPtr != NULL)
		argument = Wrp_GetUnicode(listPtr);

	    code = GetAndExecuteClrMethod(hTclModule, &uTclStubs, pConfigInfo,
		interp, argument, METHOD_TYPE_CONTROL | METHOD_VIA_COMMAND);

#if !defined(_WIN32)
	    if (argument != NULL) {
		ckfree((LPVOID)argument);
		argument = NULL;
	    }
#endif

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
	    HRESULT hResult;
	    Tcl_Obj *objPtr;
	    WCHAR buffer[PACKAGE_RESULT_SIZE + 1] = {0};
	    DWORD length;

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

	    length = PACKAGE_RESULT_SIZE;

#if defined(USE_CORE_CLR)
	    hResult = DumpCoreClrState(
		packageFileName, lTclStubs, hTclModule, &uTclStubs, buffer,
		&length);
#else
	    hResult = DumpClrState(
		packageFileName, lTclStubs, hTclModule, &uTclStubs, buffer,
		&length);
#endif

	    if (SUCCEEDED(hResult)) {
		objPtr = Wrp_NewUnicodeObj(buffer, -1);

		if (objPtr == NULL) {
		    Tcl_AppendResult(interp, "out of memory: objPtr\n", NULL);
		    code = TCL_ERROR;
		    goto done;
		}

		Tcl_IncrRefCount(objPtr);
		Tcl_SetObjResult(interp, objPtr);
		Tcl_DecrRefCount(objPtr);
	    } else {
		Wrp_AppendUnicodeToObj(Tcl_GetObjResult(interp),
		    GetClrErrorMessage(L"DumpClrState", hResult), -1);

		code = TCL_ERROR;
		goto done;
	    }
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

	    if (code == TCL_OK) {
#if defined(USE_CORE_CLR)
		SetCoreClrBridgeStarted(FALSE);
#else
		SetClrBridgeStarted(FALSE);
#endif
	    }

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

	    if (code == TCL_OK) {
#if defined(USE_CORE_CLR)
		SetCoreClrBridgeStarted(TRUE);
#else
		SetClrBridgeStarted(TRUE);
#endif
	    }

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

    Wrp_MutexUnlock(&packageMutex);
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

    Tcl_Interp *interp = (Tcl_Interp *)clientData;

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
