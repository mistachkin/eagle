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

static volatile _Pal_Atomic LONG lTclStubs = 0;

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
    PACKAGE_NAME_0, PACKAGE_NAME_1, PACKAGE_NAME_2, PACKAGE_NAME_3, NULL
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
 * Why / How:
 *	This is the "Fossil trick": Tcl stubs are normally bound by
 *	linking the loader against tclstub.lib / libtclstub.so, which
 *	provides the magic Tcl_InitStubs symbol that pokes the stubs
 *	pointer into the interpreter and validates ABI compat.  But
 *	building Garuda.dll with that link dependency means a copy of
 *	tclstub goes into every Garuda binary, AND Garuda has to know
 *	at link-time which Tcl version it's targeting.  Fossil hit the
 *	same problem (it embeds Tcl as a script engine for skin
 *	scripting) and Jan Nijtmans's solution there was to skip the
 *	import library entirely: read the stubs pointer directly out
 *	of the Tcl_Interp's private layout (cast through PrivateTclInterp
 *	defined in our private headers), validate the magic number, then
 *	hand it to Tcl_PkgRequireEx which is itself a stubs-table call.
 *
 *	The cost: we depend on the layout of TclInterp's stubTable
 *	field being stable across Tcl point releases.  In practice it
 *	is -- the field has been at offset 16 (32-bit) / 32 (64-bit)
 *	since Tcl 8.4.  If that ever changes we will get TCL_STUB_MAGIC
 *	mismatches at startup, which is the right failure mode (loud,
 *	at first call, with a clear log message).
 *
 *	The hooks dance at the bottom unpacks the platform / internal /
 *	internal-platform sub-tables.  These exist because Tcl's stubs
 *	mechanism splits its API surface across four tables -- the public
 *	one we got from PrivateTclInterp's stubTable, plus three reached
 *	via tclStubsPtr->hooks.  Older Tcl builds NULL-out the hooks
 *	field if they were configured without internal-stub support;
 *	we tolerate that by leaving the sub-table pointers NULL and
 *	letting later use-sites detect the absence (the TIP #285
 *	cancellation path is the main consumer that cares).
 *
 *	Only compiled when USE_TCL_PRIVATE_STUBS is defined; otherwise
 *	a normal Tcl_InitStubs link is expected.
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
 * Why / How:
 *	The package needs its own .dll/.so/.dylib path on disk for two
 *	reasons: (1) the lib/ subdirectory of the package is computed
 *	relative to it (where helper.tcl, Eagle.dll, and the runtime
 *	configuration files live), and (2) [object dump] surfaces it
 *	for diagnostics.  Tcl gives us hModule via the Init entry
 *	point, but resolving hModule -> file name is a Win32 API call
 *	(GetModuleFileName) and not portable per se -- on POSIX the
 *	equivalent comes from dladdr.  GarudaPal abstracts the
 *	difference into Wrp_get_module_file_name; this function just
 *	wraps the buffer protocol around it.
 *
 *	The two-buffer dance is deliberate.  Win32's GetModuleFileName
 *	has the well-known "you can't ask for the size first" wart:
 *	you must pass a buffer, and if it was too small, the API
 *	returns the buffer size you supplied (NOT the size you needed).
 *	The only way to be certain you got the whole path is to pass
 *	a buffer at least UNICODE_STRING_MAX_CHARS long (32767, the
 *	NT max path length) and then truncate down to the actual
 *	length the API reports.  We do exactly that: oversize first
 *	allocation, copy into a right-sized second allocation, free
 *	the oversize one.  The cost of the oversize alloc is one
 *	~64 KB transient -- paid once at package load, never again.
 *
 *	Caller-owns-the-result: ckfree-able via the standard Tcl
 *	allocator, freed during Garuda_Unload.  The static
 *	packageFileName variable is the consumer of the success
 *	output; nothing else owns a copy.
 *
 *	"Uses no global state and assumes any required locks are
 *	already held" -- a defensive note because GetPackageModule-
 *	FileName is called from Garuda_Init, which holds packageMutex
 *	for the entire setup sequence.  Hot path concurrency is not
 *	a concern (load is single-threaded by Tcl convention) but
 *	the comment makes the contract explicit.
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
 * Why / How:
 *	This is the bridge between Tcl's stubs world and Eagle's
 *	managed-side native-Tcl integration subsystem.  When Eagle
 *	wants to call back into Tcl from C# (to set a variable in
 *	the calling interpreter, fire an event, etc.), it can't go
 *	through Tcl's stubs table directly -- the stubs table is in
 *	this DLL's address space, and the marshaling code on the
 *	managed side wants typed function pointers it can pin into
 *	delegates.  The ClrTclStubs structure is that -- a flat list
 *	of function pointers, populated by this function, passed
 *	across the bridge protocol so the managed side can invoke
 *	any of them as a P/Invoke target.
 *
 *	The TIP-flag arguments switch on conditional fields:
 *
 *	  bTip285  TIP #285 = "Script-level interrupt of Tcl evaluation"
 *	           (Tcl_CancelEval, Tcl_Canceled, TclResetCancellation,
 *	           TclSetSlaveCancelFlags).  Required for Eagle's
 *	           cooperative-cancel design -- without these, Eagle
 *	           cannot cancel an in-flight Tcl_Eval from a managed
 *	           thread.  Falls back to no-cancel if the running
 *	           Tcl is too old.
 *
 *	  bTip335  TIP #335 = "Detect interp-active state"
 *	           (Tcl_InterpActive).  Used by Eagle's interp-state
 *	           inspector; absence is non-fatal.
 *
 *	  bTip336  TIP #336 = "Public API for Tcl_GetErrorLine"
 *	           Two function pointers (get + set).  Older Tcl
 *	           versions have ErrorLine as a struct field accessed
 *	           directly; newer ones go through the public API.
 *	           Eagle's stack-trace formatter prefers the API.
 *
 *	The internal-stubs requirement on bTip285 is explicit: the
 *	cancel-flag setters live in tclIntStubsPtr (Tcl considers
 *	them internal-but-stubbed), so if internal stubs failed to
 *	resolve, we cannot honor a TIP #285 request -- return FALSE
 *	rather than silently dropping cancel support.
 *
 *	Note: the field assignments below are in a fixed order that
 *	mirrors the layout of the ClrTclStubs struct in GarudaInt.h.
 *	If you add a field, append both here and in the struct
 *	definition.  The managed side's marshalling code is
 *	field-order-sensitive (positional, not nominal).
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
 * Why / How:
 *	The package's lowest-level diagnostic primitive.  TclLog goes
 *	through the script layer (a configurable Tcl command that may
 *	or may not exist depending on package configuration) and is
 *	therefore unsuitable for very-early failures (before the Tcl
 *	stubs are wired up) and very-late failures (after the package
 *	is being unloaded and the log command is gone).
 *
 *	This bypasses both worlds: on Win32 it routes to
 *	OutputDebugStringA, which any attached debugger (or DebugView,
 *	or DbgPrint capture tool) will pick up.  On non-Win32 it
 *	falls through to fprintf(stderr) as a stand-in.  The intent is
 *	"there is no scenario in which calling this is unsafe" -- it
 *	does not allocate, does not call into Tcl, does not depend on
 *	package state being initialized.
 *
 *	The PACKAGE_TRACE_BUFFER_SIZE truncation is silent.  Long
 *	traces get cut at the buffer limit; the return value
 *	reflects how many characters made it out (or -1 if gsnprintf
 *	itself signaled truncation, but it is rare in practice).
 *	Callers who care about diagnostic completeness chunk their
 *	output across multiple calls rather than depending on one
 *	huge trace surviving.
 *
 *	This is invoked through the PACKAGE_TRACE macro, which is
 *	compiled to nothing in production builds -- see GarudaInt.h.
 *	A debug build of Garuda.dll is therefore the only one that
 *	produces trace output; the release path is zero-cost.
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
 * Why / How:
 *	Sister of GetClrErrorMessage; the same shape but for Tcl
 *	return codes (TCL_OK / TCL_ERROR / TCL_RETURN / TCL_BREAK /
 *	TCL_CONTINUE) instead of HRESULTs.  Used by the bridge code
 *	to format diagnostic strings for log output.
 *
 *	The static-buffer return is intentional and ABI-stable: the
 *	caller copies (or appends) the returned text immediately,
 *	never holds the pointer across another GetTclErrorMessage
 *	call.  We do not malloc here because the only call sites are
 *	logging paths that should NEVER fail-because-of-OOM during
 *	a diagnostic.  This trade -- caller must not retain the
 *	pointer -- is consistent across the package's other
 *	error-formatting helpers (GetClrErrorMessage shares the
 *	pattern, even has its own static buffer).
 *
 *	Concurrency: the static buffer is NOT thread-safe.  Two
 *	threads calling this concurrently can scramble each other's
 *	output.  All current callers hold packageMutex when they
 *	call this, which serializes them.  If a future caller wants
 *	to use this from outside the mutex, switch to caller-allocated
 *	buffer convention rather than adding internal locking -- the
 *	contract is simple and the call frequency is low.
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
 * Why / How:
 *	Used by EVERY error-return path in GarudaClr.c and
 *	GarudaCoreClr.c that wants a human-friendly representation
 *	of an HRESULT for the Tcl interp result.  Format is
 *
 *	    "<source>: <severity> (code 0x<hex>).\n"
 *	or
 *	    "<severity> (code 0x<hex>).\n"
 *
 *	when source is NULL, where <severity> is "success" if the
 *	HRESULT's S-bit is clear, "failure" otherwise.  We use
 *	SUCCEEDED() rather than checking == S_OK because COM HRESULTs
 *	have legitimate non-S_OK success codes (S_FALSE, all the
 *	S_OK_FOO variants used by hosting interfaces).
 *
 *	NOTE: this deliberately does NOT call FormatMessage to
 *	stringify the HRESULT.  FormatMessage gives locale-dependent
 *	text and resolves to system error messages even when the
 *	HRESULT came from the CLR (whose error space overlaps Win32
 *	but is not equivalent).  Hex-only output is unambiguous,
 *	greppable, and locale-stable -- which matters for diagnostics
 *	the user pastes into a bug report.
 *
 *	Same static-buffer / not-thread-safe / called-under-package-
 *	mutex contract as GetTclErrorMessage.
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
	    L": " PACKAGE_UNICODE_STR_FMT L" (code 0x%lX).\n", source,
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
 * Why / How:
 *	The package's middle-tier diagnostic primitive (between
 *	TracePrintf at the bottom and a full [object dump] at the
 *	top).  Routes through a configurable Tcl command -- by default
 *	[tclLog], which Tcl pre-defines, but the embedder can
 *	override per-call by passing a different command name.  This
 *	indirection is what lets a host application redirect Garuda's
 *	chatter into its own log stream without touching this code.
 *
 *	The variadic API takes one or more LPCWSTR strings terminated
 *	by a NULL sentinel.  Internally they are concatenated into
 *	a single Tcl_Obj which becomes the second argument to the
 *	log command:
 *
 *	    {logCommand} "msg arg1 arg2 ..."
 *
 *	The result is restored via Tcl_SaveResult / Tcl_RestoreResult
 *	so a log call inside an error path doesn't clobber the error
 *	message the caller is about to surface.  This is the entire
 *	reason for the SaveResult dance -- without it, a TCL_ERROR-
 *	bearing interp would have its result replaced by the log
 *	command's empty success result.
 *
 *	If the log command succeeds, we ALSO mirror the message to
 *	the platform's debug stream: OutputDebugStringW on Win32,
 *	fwprintf(stderr) elsewhere.  This dual-write is intentional:
 *	when debugging a load failure, you typically want to see the
 *	message regardless of whether the embedder's log command
 *	wrote anywhere visible.  The debug-stream mirror is
 *	conditioned on TCL_OK from the log command -- failures are
 *	silent (per the contract -- "All failures are simply ignored")
 *	to avoid recursive noise from a broken log command.
 *
 *	Reentrancy: the called Tcl command runs at TCL_EVAL_GLOBAL,
 *	so it does not see the calling proc's locals.  If the log
 *	command itself calls back into this DLL, the package mutex
 *	is recursive on Win32 (Tcl_Mutex is) but on POSIX it is
 *	emulated via packageOwner -- see GarudaPal.c.  Don't put
 *	expensive work in a custom log command.
 *
 *	Memory: objv[1] is built incrementally with AppendUnicodeToObj.
 *	On the POSIX-stderr path we additionally ckfree the unicode
 *	buffer because Wrp_GetUnicode allocates there; on Win32 the
 *	buffer is owned by the Tcl_Obj and freed when its refcount
 *	hits zero at the bottom of this function.
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
 * Why / How:
 *	Garuda's Win32-DNA dictates Unicode (WCHAR / UTF-16) for the
 *	managed-side bridge surface -- the CLR is UTF-16-native -- but
 *	Tcl on POSIX uses UTF-8 internally and exposes UTF-16 only
 *	through conversion helpers in Wrp_*.  This function papers
 *	over that platform difference with a single contract:
 *	"give me a fresh, ckfree-owned LPWSTR".
 *
 *	Win32 path:
 *	    Wrp_GetUnicodeFromObj returns a pointer into Tcl's
 *	    internal storage.  We must copy it into a fresh
 *	    attemptckalloc'd buffer because Tcl_Obj backing memory
 *	    can be invalidated by ANY subsequent Tcl call.
 *
 *	POSIX path:
 *	    Wrp_GetUnicodeFromObj allocates a fresh buffer (see
 *	    GarudaStr.c).  We can return that pointer directly --
 *	    the caller-owns-and-ckfrees contract is already
 *	    satisfied without an extra copy.  The cleanup branch
 *	    at `done:` skips ckfree only when result aliased the
 *	    objValue allocation; otherwise it cleans up.
 *
 *	The branching is asymmetric on purpose: the POSIX path
 *	avoids an unnecessary copy when the string is already a
 *	standalone heap allocation.  The Win32 path's invariant
 *	(don't trust Tcl_Obj-backed pointers) is non-negotiable.
 *
 *	Length semantics: the int *lengthPtr argument is the
 *	UCS-2 / UTF-16 code-unit count, NOT byte count or
 *	codepoint count.  Surrogate pairs count as two units.
 *	This matches WCHAR-counting conventions throughout the
 *	package and the managed bridge.
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
 * Why / How:
 *	Lookups one of the package's configuration variables by
 *	name (e.g. ::Garuda::library, ::Garuda::useMinimumClr,
 *	::Garuda::runtimeConfigPath) and returns the value as
 *	an LPWSTR.  Used by Garuda_Init and the [object load]
 *	command to read embedder-supplied settings.
 *
 *	Lookup is unconditionally TCL_GLOBAL_ONLY: the package's
 *	configuration variables live in the package's namespace
 *	(::Garuda::*), and resolving against the calling proc's
 *	scope would be wrong -- embedders may invoke [object]
 *	from inside any proc.  The varName argument is therefore
 *	expected to be fully-qualified (the package's setup code
 *	prepends "::Garuda::" before each lookup).
 *
 *	On variable-not-found, the interp result gets a
 *	"variable not found: <name>" message.  Some callers
 *	suppress this via Tcl_SaveResult / Tcl_RestoreResult
 *	when the variable is optional (e.g. logging-related
 *	settings); see Garuda_Init for the dance.
 *
 *	Same Win32-copy / POSIX-direct-return ownership pattern
 *	as GetStringObjectValue.
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
 * Why / How:
 *	Same lookup convention as GetStringVariableValue (TCL_GLOBAL_
 *	ONLY, fully-qualified name) but the value is parsed via
 *	Tcl_GetBooleanFromObj -- meaning Tcl's standard boolean
 *	encoding accepts true/false/yes/no/0/1/on/off.  This is
 *	user-friendly: an embedder can set ::Garuda::verbose to
 *	"yes" or "1" and both work.
 *
 *	Failure modes:
 *	  - varName == NULL  -> defValue, "invalid variable name" in
 *	    interp result.
 *	  - variable not set -> defValue, "variable not found:" in
 *	    interp result.
 *	  - non-boolean value -> defValue, "variable value is invalid:"
 *	    in interp result.
 *
 *	The default-on-error pattern is a deliberate design choice for
 *	configuration variables: most callers do not want to fail the
 *	entire Init/load over a malformed verbose flag.  The error
 *	message in interp result lets a curious embedder still see
 *	what happened if they choose to inspect.  Callers that DO
 *	require strictness handle the result themselves before
 *	calling this and skip the lookup if the variable is malformed.
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
 * Why / How:
 *	Mirror of GetBooleanVariableValue for int-valued config
 *	variables (e.g. method-flag bitmasks, timeout milliseconds,
 *	the protocol-revision selector).  Parsed via
 *	Tcl_GetIntFromObj which accepts decimal, "0x..." hex, "0..."
 *	octal, and "0b..." binary on modern Tcl -- same flexibility as
 *	[expr].  Uses int (not long, not Tcl_WideInt) because every
 *	current consumer fits in 32 bits and using a wider type would
 *	require parallel changes throughout the package.
 *
 *	Same default-on-error pattern as the boolean variant -- the
 *	defValue is what the caller wants if the embedder didn't set
 *	the variable or set it to garbage.  See the boolean variant's
 *	Why/How for the rationale.
 *
 *	No floating-point sister exists because no current
 *	configuration value is fractional; if one is added in the
 *	future, follow the same pattern with Tcl_GetDoubleFromObj.
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
 * Why / How:
 *	The "demand-dispatch" path's method-info builder.  When a
 *	caller invokes [object demand assemblyPath typeName methodName
 *	arg], they have explicitly told us which managed method to
 *	call -- there is no lookup against package configuration
 *	variables (that's GetClrMethodInfo's job, the
 *	configuration-driven sister of this function).
 *
 *	Each of the four Tcl_Obj inputs is converted to a fresh
 *	LPWSTR via GetStringObjectValue.  All four are required --
 *	a NULL or empty string aborts with a specific error message
 *	naming which field was bad.  The argument string is the only
 *	one that may be empty (length == 0) but not NULL; the others
 *	must be non-empty (length > 0).  Asymmetry is intentional:
 *	an empty managed argument is a valid call shape; an empty
 *	method name is not.
 *
 *	The sizeOf field is set as the first thing after zeroing the
 *	struct.  This is part of the package's general "every public
 *	struct carries its own size" pattern (so future revisions can
 *	add fields without breaking the bridge protocol -- the managed
 *	side reads sizeOf to know how many bytes are valid).
 *
 *	Caller MUST ckfree via FreeClrMethodInfo (not raw ckfree); the
 *	four LPWSTR fields each have their own ownership and must be
 *	freed individually.  Do not call ckfree on the struct directly
 *	or you leak the four embedded strings.
 *
 *	On any failure mid-build, the partially-populated struct is
 *	left in *ppMethodInfo for the caller to clean up via
 *	FreeClrMethodInfo.  This keeps the cleanup path uniform --
 *	the caller does not need to track WHERE in setup we failed.
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
 * Why / How:
 *	The "configuration-driven" sister of CreateClrMethodInfo.
 *	Where CreateClrMethodInfo takes Tcl_Obj arguments
 *	(supplied by [object demand]), this one builds the
 *	ClrMethodInfo entirely from package-namespace
 *	configuration variables, indexed by methodFlags.
 *
 *	Method-type fan-out (METHOD_TYPE_MASK in methodFlags):
 *	  STARTUP    -> ::Garuda::startupMethod   (called from Init)
 *	  CONTROL    -> ::Garuda::controlMethod   (mid-life ops)
 *	  DETACH     -> ::Garuda::detachMethod    (soft unload)
 *	  SHUTDOWN   -> ::Garuda::shutdownMethod  (called from Unload)
 *	  DEMAND     -> N/A (use CreateClrMethodInfo instead)
 *
 *	The assemblyPath, typeName, and argument fields are
 *	always read from the ASSEMBLY_PATH / TYPE_NAME / METHOD_
 *	ARGUMENTS package variables -- a method invocation thus
 *	spans one ClrMethodInfo with three constants and one
 *	method-type-specific name.  This matches the embedder's
 *	mental model: "all my managed lifecycle hooks live in
 *	the same type, with different methods for different
 *	events."
 *
 *	Returning TCL_ERROR with "invalid method type" if METHOD_
 *	TYPE_DEMAND or anything else falls through.  The DEMAND
 *	case is deliberately rejected here because there is no
 *	demand-method package variable to read -- the caller must
 *	invoke CreateClrMethodInfo with explicit args instead.
 *
 *	Same partial-struct-on-failure / FreeClrMethodInfo cleanup
 *	contract as CreateClrMethodInfo.
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
 * Why / How:
 *	Reverse-order cleanup of CreateClrMethodInfo /
 *	GetClrMethodInfo: argument, methodName, typeName,
 *	assemblyPath, then the struct itself.  The reverse order
 *	is not load-bearing here (these are independent
 *	allocations) but follows the convention used elsewhere
 *	in the package, which makes interleaved leak diagnoses
 *	easier to read in trace output.
 *
 *	Tolerates a NULL ppMethodInfo OR a NULL *ppMethodInfo --
 *	useful because failure paths in the builder leave the
 *	struct partially populated; this function copes with
 *	whatever it finds.  Each LPWSTR field is checked
 *	independently before ckfree.  After freeing the struct,
 *	*ppMethodInfo is set to NULL so the caller can safely
 *	double-free or test-after-free without UB.
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
 * Why / How:
 *	The "snapshot the package's configuration into one struct"
 *	helper.  Garuda's runtime behavior is configured through
 *	a substantial set of namespace variables (see lib/helper.tcl
 *	for the full enumeration) and ClrConfigInfo is the C-side
 *	denormalized snapshot of them.  This function is what
 *	converts the loose Tcl-namespace state into a single
 *	ckalloc'd struct that downstream code can reason about.
 *
 *	Two boolean parameters tune the depth of the snapshot:
 *
 *	  bForLogOnly  Read ONLY ::Garuda::logCommand, skip
 *	                 everything else.  Used by paths that only
 *	                 need to issue diagnostic output and don't
 *	                 want to evaluate the rest of the config
 *	                 (which may itself fail and produce noise).
 *	                 Most importantly, used during error paths
 *	                 in Init/Unload where partially-set config
 *	                 must not derail the error message.
 *
 *	  bMethods     Read the four method-info structs (startup /
 *	                 control / detach / shutdown).  Skipped during
 *	                 fast-path config queries that don't need to
 *	                 dispatch.  Each method-info is itself an
 *	                 attemptckalloc; bMethods=FALSE saves four
 *	                 small allocations and four name lookups.
 *
 *	The runtimeConfigPath field is REQUIRED on USE_CORE_CLR
 *	(the .NET 5+ path needs the runtimeconfig.json to load
 *	the runtime), but optional on .NET Framework -- Framework
 *	uses its own version-binding logic, no JSON needed.  The
 *	#if-guarded error-out reflects that asymmetry.
 *
 *	Variables backing the boolean flags (bLoadClr, bStartClr,
 *	bStartBridge, bStopClr, bUseIsolation, bUseSafeInterp)
 *	all default to FALSE if not set.  This matches embedder
 *	expectations: opt-in for everything; nothing happens
 *	implicitly.  Default-load + default-start would be
 *	surprising for a library that wants to expose [object]
 *	without committing to a runtime selection.
 *
 *	logCommand is REQUIRED unconditionally -- every path through
 *	this function expects to be able to log.  Helper.tcl ensures
 *	::Garuda::logCommand is always set (defaults to "tclLog").
 *	If the embedder unsets it, we fail loudly here rather than
 *	silently skipping logs later.
 *
 *	Caller MUST FreeClrConfigInfo, not raw ckfree, for the same
 *	reason as FreeClrMethodInfo -- embedded LPWSTR / sub-struct
 *	pointers are independently owned.
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
 * Why / How:
 *	Cleanup mirror of GetClrConfigInfo, in reverse construction
 *	order: scalar fields first (logCommand, runtimeConfigPath),
 *	then the four method-info sub-structs delegated to
 *	FreeClrMethodInfo, then the parent struct.
 *
 *	The four method-info pointers are freed in reverse order
 *	of build (shutdown -> detach -> control -> startup) -- same
 *	purely-cosmetic LIFO convention as FreeClrMethodInfo.
 *	Independent allocations, so order is not load-bearing.
 *
 *	NULL-tolerant in both arguments and contained pointers,
 *	just like FreeClrMethodInfo, so partial-build cleanup
 *	from GetClrConfigInfo's failure paths works correctly.
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
 * Why / How:
 *	The package supports two sources of method-flag bits: the
 *	caller's immediate request (e.g. METHOD_TYPE_STARTUP from the
 *	dispatch path) and the embedder-set ::Garuda::methodFlags
 *	configuration variable (held in pConfigInfo->methodFlags).
 *	This function OR-merges the two so the dispatched managed
 *	method sees both -- caller-required type bits AND embedder-
 *	configured behavior bits, with neither side overriding the
 *	other.
 *
 *	On top of the OR-merge, this function also folds in two
 *	derived bits:
 *	  bUseIsolation  -> METHOD_USE_ISOLATION
 *	  bUseSafeInterp -> METHOD_USE_SAFE_INTERP
 *
 *	These are kept as separate booleans in ClrConfigInfo (so
 *	that helper.tcl can configure them via dedicated variable
 *	names rather than requiring users to compute bit values),
 *	but get folded into the flags word here for transmission
 *	across the bridge protocol.  The managed side only reads
 *	flag bits, not booleans.
 *
 *	Defensive against NULL inputs -- used in dispatch paths
 *	where pConfigInfo or pMethodFlags might be skipped under
 *	error conditions.  No-op rather than crash.
 *
 *	Note the read-modify-write through `methodFlags` local:
 *	pMethodFlags is read once, modified, then written back
 *	once.  This avoids torn reads if pMethodFlags happens to
 *	point at concurrently-mutated storage (the package mutex
 *	covers this, but the local-copy pattern is robust to
 *	future caller refactoring).
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
 * Why / How:
 *	The "configured-dispatch" entry point.  Used for the four
 *	package-lifecycle methods (startup / control / detach /
 *	shutdown) -- which managed method to call is read from the
 *	configuration variables, not supplied by the caller.
 *	DemandExecuteClrMethod is the sister for caller-supplied
 *	dispatch.
 *
 *	Sequence:
 *	  1. Validate pConfigInfo (must have been built via
 *	     GetClrConfigInfo before this call).
 *	  2. MaybeCombineMethodFlags -- fold caller flags with
 *	     configured flags.
 *	  3. CanExecuteClrCode / CanExecuteCoreClrCode predicate;
 *	     if FALSE, return early.  METHOD_STRICT_CLR controls
 *	     whether the early return is success or error -- strict
 *	     callers (Init's startup-method call) want an error
 *	     because they cannot proceed; relaxed callers (Unload's
 *	     shutdown-method call) want success because "CLR
 *	     already gone" is fine for shutdown.
 *	  4. GetClrMethodInfo to resolve the method-type to a
 *	     concrete method-info struct using the configuration.
 *	  5. ExecuteClrMethod / ExecuteCoreClrMethod via the
 *	     #ifdef-selected branch.
 *	  6. METHOD_STRICT_RETURN check on the managed method's
 *	     returnValue -- this is the cross-language ABI contract
 *	     "managed lifecycle methods return TCL_OK on success".
 *	     Strict callers convert non-TCL_OK to TCL_ERROR with a
 *	     formatted error message naming the method type, type,
 *	     method, and assembly.  Relaxed callers tolerate any
 *	     return code.
 *	  7. Cleanup pMethodInfo unconditionally.
 *
 *	The two STRICT_* flag bits (CLR and RETURN) are separable
 *	because some lifecycle states want one kind of strictness
 *	but not the other.  E.g. detach wants STRICT_RETURN
 *	(non-TCL_OK from detach is a real bug) but not STRICT_CLR
 *	(detaching a CLR that's already gone is fine).
 *
 *	Note: this function does NOT acquire packageMutex.
 *	Callers (Init, Unload, [object dispatch], etc.) hold it
 *	already; nesting it here would be redundant and on POSIX
 *	would burn a recursive-lock cycle in packageOwner.
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
 * Why / How:
 *	The "demand-dispatch" sister of GetAndExecuteClrMethod.
 *	Where GetAndExecuteClrMethod resolves the method to call
 *	by reading package configuration variables, this function
 *	takes assembly / type / method / argument as Tcl_Obj*
 *	arguments -- supplied directly by the caller, typically
 *	from the [object demand assemblyPath typeName methodName
 *	arg] script-level command.
 *
 *	Sequence is parallel to GetAndExecuteClrMethod minus the
 *	configured-method lookup:
 *	  1. Validate pConfigInfo (only logCommand / methodFlags
 *	     are read from it; the method coords come from the
 *	     Tcl_Obj args).
 *	  2. MaybeCombineMethodFlags.
 *	  3. CreateClrMethodInfo (NOT GetClrMethodInfo) to build
 *	     the method-info from the Tcl_Obj arguments.
 *	  4. ExecuteCoreClrMethod / ExecuteClrMethod dispatch.
 *	  5. Cleanup pMethodInfo.
 *
 *	Notable absences relative to GetAndExecuteClrMethod:
 *	  - No CanExecute*ClrCode predicate guard -- the script-
 *	    level [object demand] command does its own gating.
 *	  - No STRICT_RETURN treatment -- demand-dispatched methods
 *	    pass their return value back through pReturnValue
 *	    instead of being interpreted as TCL_OK/TCL_ERROR.  The
 *	    caller decides what to do with the DWORD.
 *
 *	The "argument" parameter to ExecuteCoreClrMethod /
 *	ExecuteClrMethod is passed as NULL because the Tcl_Obj
 *	argumentPtr was already merged into the method-info via
 *	CreateClrMethodInfo's argument capture.  Passing it again
 *	would double-include it in the protocol payload.
 *
 *	pReturnValue is forwarded all the way to the dispatch
 *	function, which writes the managed method's DWORD return
 *	there before returning.  Caller MAY pass NULL if they
 *	don't care about the return value.
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
 * Why / How:
 *	The Tcl-mandated package entry point -- invoked once when an
 *	embedder calls [load /path/to/Garuda.dll] (or auto-loads via
 *	pkgIndex.tcl).  This is the function whose name is matched
 *	by Tcl's [load] command after capitalizing the first letter
 *	of the package name and appending "_Init".  Its presence in
 *	the export table is non-negotiable.
 *
 *	The startup sequence is the longest single piece of logic in
 *	the package, ordered as follows.  Each step has a specific
 *	failure mode that determines whether [load] reports success
 *	or error to the embedder:
 *
 *	  1. Tcl stubs init (Tcl_InitStubs or initTclStubs).
 *	     If this fails, no Tcl API is callable; we must return
 *	     TCL_ERROR via PACKAGE_TRACE-only diagnostic (we do not
 *	     have an interp to write a result on yet -- well, we have
 *	     `interp`, but if Tcl_InitStubs failed, calling
 *	     Tcl_AppendResult would itself crash).  This is a
 *	     trace-and-bail.
 *
 *	  2. lTclStubs interlocked-increment.  Marks "Tcl API is
 *	     available" so that downstream paths (including unload)
 *	     can detect we got past stubs init.  Atomicity matters
 *	     because Garuda_Unload reads this flag from a potentially
 *	     different thread.
 *
 *	  3. Package mutex acquired for the rest of the function.
 *	     This serializes against any other Garuda_Init/_Unload
 *	     racing through the same DLL instance (rare but not
 *	     impossible: an embedder loading two interps that each
 *	     load Garuda).  Recursive on POSIX via packageOwner.
 *
 *	  4. GetPackageModuleFileName -- discover our own
 *	     .dll/.so/.dylib path.  Required because helper.tcl
 *	     computes ::Garuda::library relative to it.
 *
 *	  5. Tcl version / TIP-feature detection (Tcl_GetVersion,
 *	     Tcl_PkgPresent for tcl::tip-285/335/336).  Result is
 *	     bTcl86 and the per-TIP flags fed to SetClrTclStubs.
 *
 *	  6. SetClrTclStubs -- populate uTclStubs (the static
 *	     ClrTclStubs struct) with function pointers that the
 *	     bridge will hand to Eagle.
 *
 *	  7. Source helper.tcl from <package-dir>/lib/helper.tcl
 *	     (computed relative to packageFileName).  This is a
 *	     SUBSTANTIAL amount of script that does runtime
 *	     selection (CoreCLR vs .NET Framework), assembly path
 *	     resolution, runtime config writing, namespace setup.
 *	     A failure here is the most common embedder-visible
 *	     load failure; the helper.tcl error message bubbles
 *	     up through the interp result.
 *
 *	  8. GetClrConfigInfo -- snapshot helper.tcl's namespace
 *	     variables into a ClrConfigInfo struct.
 *
 *	  9. logCommand pulled out of pConfigInfo.  From this
 *	     point on, errors get logged through TclLog as well
 *	     as written to interp result.
 *
 *	 10. Conditional CLR load + start (bLoadClr, bStartClr).
 *	     LoadAndStartTheClr or LoadAndStartTheCoreClr depending
 *	     on the build.  Skip if already loaded by a sibling
 *	     interp earlier (bClrWasLoaded / bClrWasStarted are
 *	     tracked because we should NOT teardown a CLR we did
 *	     not bring up, even if Init eventually fails).
 *
 *	 11. Conditional bridge startup (bStartBridge).  Calls
 *	     into the managed startup method via
 *	     GetAndExecuteClrMethod with METHOD_TYPE_STARTUP.
 *	     This is the call that crosses into Eagle and runs
 *	     the C# Garuda bootstrap.
 *
 *	 12. Tcl_CreateObjCommand to register [object].  Done
 *	     LAST so that the command is only available if every
 *	     prior step succeeded -- partial init must not leave
 *	     a half-functional [object] in the interp.
 *
 *	 13. Tcl_PkgProvideEx for each name in packageNames[]
 *	     (currently four aliases: garuda, eagle, dotnet, clr --
 *	     see PACKAGE_NAME_0..3 in pkgVersion.h).
 *
 *	On any error mid-sequence, the function jumps to `done:`
 *	which:
 *	  - On TCL_ERROR with bClrWasLoaded/bClrWasStarted set
 *	    by THIS Init (not by an earlier sibling Init), tear
 *	    down what we built.
 *	  - Free pConfigInfo via FreeClrConfigInfo.
 *	  - Release packageMutex.
 *	  - Return code.
 *
 *	The teardown asymmetry -- only undo what THIS Init did --
 *	is critical.  An embedder loading Garuda into two interps
 *	in sequence wants the second [load] to be safely skippable
 *	if the first one already brought up the CLR; a failure on
 *	the second Init must NOT tear down the first interp's CLR.
 *
 *	Concurrency caveat: although packageMutex serializes Init
 *	calls, the global state (pClrRuntimeHost, bClrStarted,
 *	bClrBridgeStarted) means TWO concurrent [load Garuda] in
 *	different interps share the same CLR.  This is intended
 *	(.NET single-runtime-per-process is mandatory) but worth
 *	knowing when reasoning about "why did my second interp see
 *	a managed object created by the first?"  Eagle's isolation
 *	bit (METHOD_USE_ISOLATION) addresses that at the AppDomain
 *	level on .NET Framework.
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
 * Why / How:
 *	The Tcl-mandated entry point for safe interpreter init.
 *	Tcl looks up <Package>_SafeInit when [load] is called from
 *	a safe interp; if absent, [load] fails.  Garuda's design
 *	puts the safe-interp gating inside GarudaObjCmd
 *	(per-sub-command) rather than refusing the load entirely,
 *	so this function legitimately delegates to Garuda_Init.
 *
 *	The reason GarudaObjCmd's per-sub-command gating works
 *	for safe interps: the introspection sub-commands (with the
 *	dumpstate exception) reveal nothing the embedder could not
 *	already learn through other means; the lifecycle and
 *	bridge sub-commands fail with "permission denied".  Net
 *	effect from a safe-interp viewpoint: [object dumpstate],
 *	[object clrload], [object startup], etc. all return errors;
 *	[object clrrunning], [object clrversion], etc. work fine.
 *
 *	If the gating model ever changes -- for example, if a future
 *	sub-command needs to be safe-only or unsafe-only with
 *	different semantics in each -- this function would acquire
 *	per-mode behavior and stop being a one-line delegate.  For
 *	now, the delegation is the right answer.
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
 * Why / How:
 *	The mirror entry point of Garuda_Init.  Registered with Tcl
 *	via standard package conventions and invoked from one of
 *	three contexts identified by the `flags` argument:
 *
 *	  TCL_UNLOAD_DETACH_FROM_PROCESS:
 *	    Full unload -- the [load Garuda] is being undone for
 *	    real, the .dll/.so will be FreeLibrary'd / dlclose'd
 *	    after we return.  This is the destructive path: stop
 *	    bridge, stop CLR, free packageFileName, release the
 *	    static storage.  Goes through GarudaExitProc to
 *	    coordinate with Tcl's exit handlers.
 *
 *	  TCL_UNLOAD_FROM_INIT (bFromInit):
 *	    Garuda_Init failed mid-sequence; this is the cleanup
 *	    pass.  We must NOT do anything that assumes a fully-
 *	    successful init -- for example, do not try to call the
 *	    managed shutdown method if the bridge never started.
 *	    The flag tells us to be conservative.
 *
 *	  TCL_UNLOAD_FROM_CMD_DELETE (bFromCmdDelete):
 *	    Tcl is destroying the [object] command (interp delete,
 *	    or rename to ""); Tcl will follow up with another
 *	    Garuda_Unload call if the package's full unload is
 *	    intended.  This call is just the per-interp cleanup,
 *	    NOT the process-level teardown.
 *
 *	The bShutdown / bFromInit / bFromCmdDelete trio gets used
 *	throughout the function to gate which steps run.  The
 *	default decision tree:
 *
 *	  bFromInit                -> minimal cleanup; no managed
 *	                              calls; release packageMutex
 *	                              and return.
 *	  bShutdown                -> full teardown via
 *	                              GarudaExitProc; this is the
 *	                              "really exit" path.
 *	  bFromCmdDelete           -> per-interp cleanup; leave
 *	                              the CLR alone for other
 *	                              interps.
 *	  (none of the above)      -> dispatch the [Garuda] managed
 *	                              detach method but leave the
 *	                              CLR up for re-attach.
 *
 *	The lTclStubs interlocked-CAS at the top is the symmetric
 *	pair of Garuda_Init's interlocked-increment.  It's
 *	idempotent -- calling Garuda_Unload after a successful
 *	Garuda_Unload is a no-op (PACKAGE_TRACE + return).  This
 *	lets Tcl's unload machinery be sloppy without breaking us:
 *	if Tcl decides to call Unload twice (it usually doesn't,
 *	but defensive code is cheap here), the second call is
 *	silent.
 *
 *	One thing this function deliberately does NOT do: it does
 *	not call Tcl_DeleteCommandFromToken to remove [object].
 *	That happens automatically through the GarudaObjCmdDelete-
 *	Proc which Tcl invokes when the command's interp is being
 *	destroyed.  Doing it from here would race with Tcl's own
 *	command-deletion machinery.
 *
 *	The "TODO: Good default?" comment on bStopClr reflects an
 *	open design question -- should an unload that's not
 *	bShutdown still stop the CLR?  Currently TRUE on the
 *	theory "if you're unloading the package, you don't need
 *	the CLR running anymore", but a sibling-interp scenario
 *	might want the opposite.  Left as a known issue for now.
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
 * Why / How:
 *	The Tcl-mandated unload entry point for safe interpreters,
 *	the symmetric pair of Garuda_SafeInit.  Tcl looks up
 *	<Package>_SafeUnload when a safe interp's [unload] runs;
 *	if absent, the unload fails.  Same delegation rationale as
 *	Garuda_SafeInit: the per-sub-command gating in GarudaObjCmd
 *	already enforces safe-interp restrictions, so Unload
 *	itself has nothing extra to do.
 *
 *	Note: Tcl's unload mechanism passes the same `flags`
 *	through to both the regular and safe variants.  The
 *	bShutdown / bFromInit / bFromCmdDelete branches inside
 *	Garuda_Unload handle every case identically regardless of
 *	whether the interp was safe.
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
 * Why / How:
 *	Registered with Tcl_CreateExitHandler from Garuda_Init.
 *	Tcl invokes this when the process is terminating cleanly
 *	(Tcl_Exit, Tcl_Finalize, or main() returning).  The
 *	purpose is to coordinate Garuda's teardown with Tcl's
 *	finalization order -- by the time Tcl calls our handler,
 *	all interpreters are gone but the .dll is still mapped,
 *	so we can safely unwind CLR state.
 *
 *	The implementation is a single delegated call into
 *	Garuda_Unload(NULL, TCL_UNLOAD_DETACH_FROM_PROCESS).
 *	The NULL interp is intentional -- there is no live interp
 *	at exit time; we are doing process-wide teardown.  Code
 *	that tries to use the interp inside Unload checks for
 *	NULL first and falls back to log-only or no-op as
 *	appropriate.
 *
 *	If Unload fails -- exceedingly rare; this would mean a
 *	managed shutdown method threw -- we cannot return an error
 *	to Tcl (the signature is void).  Instead we PACKAGE_PANIC
 *	which on debug builds aborts with a diagnostic and on
 *	release builds is a no-op.  The asymmetry is deliberate:
 *	in production, a failed exit-time cleanup should not
 *	prevent the process from exiting.
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
 * Why / How:
 *	The implementation of [object], the package's only script-
 *	level command.  The dispatcher is a Tcl_GetIndexFromObj +
 *	switch over an enum, the standard Ousterhout pattern for
 *	multi-subcommand commands.  The cmdOptions / options enum
 *	pair must stay in sync -- Tcl_GetIndexFromObj returns the
 *	index of the matched name, which we cast to the enum.
 *
 *	Sub-commands fall into four categories:
 *
 *	  Introspection (always safe; allowed in safe interps):
 *	    clrappdomainid    -> ICLRRuntimeHost::GetCurrentAppDomainId
 *	    clrbridgerunning  -> bClrBridgeStarted
 *	    clrrunning        -> bClrStarted
 *	    clrversion        -> ICLRRuntimeInfo::GetVersionString
 *	    packageid         -> compile-time package identity
 *
 *	  Forbidden in safe interps (denied even though they look
 *	  introspective):
 *	    dumpstate         -> exposes raw pointers; pointer
 *	                         disclosure is a sandbox-escape
 *	                         vector for Tcl scripts that have
 *	                         FFI access (memcpy, etc).
 *
 *	  CLR lifecycle (forbidden in safe interps):
 *	    clrload           -> LoadAndStart*TheClr (load only)
 *	    clrstart          -> LoadAndStart*TheClr (load + start)
 *	    clrstop           -> StopAndRelease*TheClr
 *	    clrexecute        -> DemandExecuteClrMethod
 *
 *	  Bridge lifecycle (forbidden in safe interps):
 *	    startup           -> METHOD_TYPE_STARTUP
 *	    control           -> METHOD_TYPE_CONTROL
 *	    detach            -> METHOD_TYPE_DETACH
 *	    shutdown          -> METHOD_TYPE_SHUTDOWN
 *
 *	Per-subcommand argv check uses Tcl_WrongNumArgs to produce
 *	the canonical "wrong # args:" error, with the prefix already
 *	formatted to include the subcommand name (because
 *	Tcl_WrongNumArgs is given objc=2 to skip past "object
 *	<sub>" before formatting the rest).
 *
 *	Concurrency: packageMutex is acquired at entry and released
 *	at every exit path via the `done:` label.  This serializes
 *	the entire sub-command implementation, including the
 *	bridge-method dispatches, against any other thread that
 *	might call Garuda_Init / Garuda_Unload / a parallel [object]
 *	call.  The cost is reduced concurrency for [object]
 *	parallelism, but the savings are real: every sub-command
 *	either reads or modifies global package state, and a
 *	finer-grained scheme would require per-piece locks that
 *	don't exist.
 *
 *	Safe-interp gating uses Tcl_IsSafe(interp) and is checked
 *	at the top of each non-introspection case.  Failure case
 *	produces the canonical "permission denied" error message
 *	and bypasses any further work.  The pattern is tedious
 *	but explicit; an alternative top-of-function check was
 *	rejected because some safe sub-commands have nuanced
 *	gating (the "dumpstate" exception above).
 *
 *	Sub-commands that dispatch managed methods build the
 *	pConfigInfo via GetClrConfigInfo at the top of the case
 *	body and free it at `done:` via FreeClrConfigInfo.  This
 *	keeps the cost of building config info paid only by the
 *	sub-commands that actually need it.  Introspection paths
 *	skip the cost.
 *
 *	When the bridge is involved (startup/control/detach/
 *	shutdown), MaybeCombineMethodFlags is invoked through
 *	GetAndExecuteClrMethod to fold in embedder-set flags.
 *	The clrexecute sub-command goes through Demand-
 *	ExecuteClrMethod instead, which takes explicit Tcl_Obj
 *	arguments rather than reading from the configuration.
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
 * Why / How:
 *	The deleteProc passed to Tcl_CreateObjCommand when [object]
 *	was registered in Garuda_Init.  Tcl invokes this in two
 *	scenarios:
 *
 *	  1. Explicit [rename object {}] -- script-level command
 *	     deletion.  The interp is still alive; we want
 *	     per-interp cleanup but the CLR / bridge / package
 *	     state should remain because OTHER interps may still
 *	     have [object] registered.
 *
 *	  2. Tcl_DeleteInterp on the [object]-owning interp --
 *	     the interp is being torn down; same per-interp
 *	     cleanup; the interp pointer is still valid until
 *	     this callback returns.
 *
 *	In both cases the right move is Garuda_Unload(interp,
 *	TCL_UNLOAD_FROM_CMD_DELETE | TCL_UNLOAD_DETACH_FROM_
 *	INTERPRETER).  The two flags together tell Garuda_Unload
 *	"this is a per-interp cleanup, not a process-wide
 *	teardown", which causes it to invoke the managed detach
 *	method (if the bridge is up) but leave the CLR running.
 *
 *	The PACKAGE_PANIC on failure is mostly defensive -- the
 *	failure modes here are exotic (managed detach throws,
 *	already-torn-down state, etc.) and we have no return
 *	channel to surface the error.  In production this is a
 *	no-op; in debug builds it aborts with a diagnostic
 *	message identifying the failure point.
 *
 *	Note: this is REGISTERED for [object] in Garuda_Init via
 *	Tcl_CreateObjCommand's deleteProc parameter.  Tcl
 *	guarantees this fires before the interp is fully torn
 *	down -- that's why we can still call Tcl_AppendResult and
 *	friends from inside Garuda_Unload's per-interp branches.
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
