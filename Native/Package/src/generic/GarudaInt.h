/*
 * GarudaInt.h -- Eagle Package for Tcl (Garuda)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#ifndef _GARUDA_INT_H_
#define _GARUDA_INT_H_

#define UNICODE_TEXT(x)			UNICODE_TEXT1(x)
#define UNICODE_TEXT1(x)		L##x

#define PACKAGE_UNICODE_NAME		UNICODE_TEXT(PACKAGE_NAME)
#define PACKAGE_UNICODE_PROTOCOL_V1R0	UNICODE_TEXT(PACKAGE_PROTOCOL_V1R0)
#define PACKAGE_UNICODE_PROTOCOL_V1R1	UNICODE_TEXT(PACKAGE_PROTOCOL_V1R1)
#define PACKAGE_UNICODE_PROTOCOL_V1R2	UNICODE_TEXT(PACKAGE_PROTOCOL_V1R2)

#define PACKAGE_HEX_FMT			"0x%X"
#define PACKAGE_UNICODE_HEX_FMT		UNICODE_TEXT(PACKAGE_HEX_FMT)

#define PACKAGE_PTR_FMT			"0x%p"
#define PACKAGE_UNICODE_PTR_FMT		UNICODE_TEXT(PACKAGE_PTR_FMT)

#define PACKAGE_ISTR_FMT		"%S"
#define PACKAGE_UNICODE_ISTR_FMT	UNICODE_TEXT(PACKAGE_ISTR_FMT)

#define PACKAGE_RESULT_SIZE		(1024)
#define PACKAGE_CAN_LOG(a,b)		(((a) != NULL) && ((b) != NULL))

/*
 * NOTE: As of Visual Studio 2008 (i.e. compiler version 15.00), the "swprintf"
 *       function was changed to conform to the ISO C standard.  Prior to that,
 *       the "_snwprintf" function must be used instead.  Also, a similar issue
 *       exists for the "vsnprintf" function.  Really, this check should target
 *       Visual Studio 2005 SP1; however, there does not appear to be an easy
 *       method of getting that level of precision via the preprocessor.
 */

#if !defined(_MSC_VER) || _MSC_VER >= 1500
  #define gwprintf				swprintf
  #define gsnprintf				vsnprintf
  #define GWPRINTF_LENGTH_HAS_NUL		(0)
#else
  #define gwprintf				_snwprintf
  #define gsnprintf				_vsnprintf
  #define GWPRINTF_LENGTH_HAS_NUL		(1)
#endif

/*
 * NOTE: The maximum size of the buffer to be used with OutputDebugString.
 */

#ifndef PACKAGE_TRACE_BUFFER_SIZE
  #define PACKAGE_TRACE_BUFFER_SIZE		((size_t)(4096-sizeof(DWORD)))
#endif

/*
 * NOTE: The PACKAGE_TRACE macro is used to report important diagnostics when
 *       other means are not available.  Currently, this macro is enabled by
 *       default; however, it may be overridden via the compiler command line.
 */

#ifndef PACKAGE_TRACE
  #ifdef _TRACE
    #define PACKAGE_TRACE(x)			TracePrintf x
  #else
    #define PACKAGE_TRACE(x)
  #endif
#endif

/*
 * NOTE: When the package is being compiled with the PACKAGE_DEBUG option
 *       enabled, we want to handle serious errors using either the Tcl_Panic
 *       function from the Tcl API or the printf function from the CRT.  Which
 *       of these functions gets used depends on the build configuration.  In
 *       the "Debug" build configuration, the Tcl_Panic function is used so
 *       that it can immediately abort the process.  In the "Release" build
 *       configuration, the printf function is used to report the error to the
 *       standard output channel, if available.  Currently, there are only two
 *       places where this macro is used and neither of them strictly require
 *       the process to be aborted; therefore, when the package is compiled
 *       without the PACKAGE_DEBUG option enabled, this macro does nothing.
 */

#ifdef PACKAGE_DEBUG
  #ifdef _DEBUG
    #define PACKAGE_PANIC(x)			Tcl_Panic x
  #else
    #define PACKAGE_PANIC(x)			printf x
  #endif
#else
  #define PACKAGE_PANIC(x)
#endif

/*
 * NOTE: These variable names are built using the base variable name strings
 *       defined in "Garuda.h".  The package name is prefixed to each variable
 *       name and the entire string is "converted" to Unicode.  These variables
 *       are always resolved relative to the global namespace.
 */

#define PACKAGE_UNICODE_ASSEMBLY_PATH_VAR_NAME \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(ASSEMBLY_PATH_VAR_NAME))

#define PACKAGE_UNICODE_TYPE_NAME_VAR_NAME \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(TYPE_NAME_VAR_NAME))

#define PACKAGE_UNICODE_STARTUP_METHOD_VAR_NAME \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(STARTUP_METHOD_VAR_NAME))

#define PACKAGE_UNICODE_CONTROL_METHOD_VAR_NAME \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(CONTROL_METHOD_VAR_NAME))

#define PACKAGE_UNICODE_DETACH_METHOD_VAR_NAME \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(DETACH_METHOD_VAR_NAME))

#define PACKAGE_UNICODE_SHUTDOWN_METHOD_VAR_NAME \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(SHUTDOWN_METHOD_VAR_NAME))

#define PACKAGE_UNICODE_METHOD_ARGUMENTS_VAR_NAME \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(METHOD_ARGUMENTS_VAR_NAME))

#define PACKAGE_UNICODE_METHOD_FLAGS_VAR_NAME \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(METHOD_FLAGS_VAR_NAME))

#define PACKAGE_UNICODE_VERBOSE_VAR_NAME \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(VERBOSE_VAR_NAME))

#define PACKAGE_UNICODE_LOAD_CLR_VAR_NAME \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(LOAD_CLR_VAR_NAME))

#define PACKAGE_UNICODE_START_CLR_VAR_NAME \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(START_CLR_VAR_NAME))

#define PACKAGE_UNICODE_START_BRIDGE_VAR_NAME \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(START_BRIDGE_VAR_NAME))

#define PACKAGE_UNICODE_STOP_CLR_VAR_NAME \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(STOP_CLR_VAR_NAME))

#define PACKAGE_UNICODE_LOG_COMMAND_VAR_NAME \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(LOG_COMMAND_VAR_NAME))

#define PACKAGE_UNICODE_NO_NORMALIZE_VAR_NAME \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(NO_NORMALIZE_VAR_NAME))

#define PACKAGE_UNICODE_USE_MINIMUM_CLR_VAR_NAME \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(USE_MINIMUM_CLR_VAR_NAME))

#define PACKAGE_UNICODE_USE_ISOLATION \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(USE_ISOLATION))

#define PACKAGE_UNICODE_USE_SAFE_INTERP \
    JOIN(PACKAGE_UNICODE_NAME, UNICODE_TEXT(USE_SAFE_INTERP))

/*
 * NOTE: This is the latest version of the CLR that we know about.  This is
 *       the value that will be passed to the GetRuntime method of the
 *       ICLRMetaHost interface (which will only be used when the USE_CLR_40
 *       compile-time option is enabled).  For now, only configure use of the
 *       latest version of the CLR if we are compiling with the MSVC compiler
 *       that shipped with Visual Studio 2010 or higher.
 */

#if defined(USE_CLR_40)
  #define CLR_MODULE_NAME			"MSCorEE"
  #define CLR_PROC_NAME				"CLRCreateInstance"
  #define CLR_VERSION_ENVVAR_NAME		"UseMinimumClr"
  #define CLR_VERSION_MINIMUM			CLR_VERSION_V2
  #define CLR_VERSION_LATEST			CLR_VERSION_V4
#endif

/*
 * NOTE: The environment variable used to indicate that the CLR is being
 *       stopped.
 */

#define CLR_STOPPING_ENVVAR_NAME		L"EAGLE_CLR_STOPPING"

/*
 * NOTE: Flag values for the <pkg>_Unload callback function (Tcl 8.5+).
 */

#if !defined(TCL_UNLOAD_DETACH_FROM_INTERPRETER)
  #define TCL_UNLOAD_DETACH_FROM_INTERPRETER	(1<<0)
#endif

#if !defined(TCL_UNLOAD_DETACH_FROM_PROCESS)
  #define TCL_UNLOAD_DETACH_FROM_PROCESS	(1<<1)
#endif

/*
 * HACK: This flag means that the <pkg>_Unload callback function is being
 *       called from the <pkg>_Init function to cleanup due to a package load
 *       failure.  This flag is NOT defined by Tcl and MUST NOT conflict with
 *       the unloading related flags defined by Tcl in "tcl.h".
 */

#if !defined(TCL_UNLOAD_FROM_INIT)
  #define TCL_UNLOAD_FROM_INIT			(1<<2)
#endif

/*
 * HACK: This flag means that the <pkg>_Unload callback function is being
 *       called from the <pkg>ObjCmdDeleteProc function to cleanup due to the
 *       command or its containing interpreter being deleted.  This flag is NOT
 *       defined by Tcl and MUST NOT conflict with the unloading related flags
 *       defined by Tcl in "tcl.h".
 */

#if !defined(TCL_UNLOAD_FROM_CMD_DELETE)
  #define TCL_UNLOAD_FROM_CMD_DELETE		(1<<3)
#endif

/*
 * NOTE: This enumeration contains the "types" of [and flags for] the CLR
 *       methods this package knows how to deal with.
 */

typedef enum {
    METHOD_NONE = 0x0,		     /* Unknown method type with no flags. */
    METHOD_TYPE_DEMAND = 0x1,	     /* Used for any method executed on-demand
				      * via the Tcl command interface. */
    METHOD_TYPE_STARTUP = 0x2,	     /* Startup the bridge between Eagle and
				      * Tcl. */
    METHOD_TYPE_CONTROL = 0x4,	     /* Issue a control directive to the bridge
				      * between Eagle and Tcl. */
    METHOD_TYPE_DETACH = 0x8,	     /* Detach a Tcl interpreter from the
				      * bridge between Eagle and Tcl (i.e. make
				      * the bridge "forget" about it because it
				      * was deleted, the package was unloaded
				      * from it, etc). */
    METHOD_TYPE_SHUTDOWN = 0x10,     /* Shutdown the bridge between Eagle and
				      * Tcl. */
    METHOD_TYPE_MASK = 0x1F,	     /* Used for limiting the method flags to
				      * [one or more] of the type values. */
    METHOD_PROTOCOL_V1R1 = 0x20,     /* If set, the protocol version indicator,
				      * the Tcl library module handle, the Tcl
				      * interpreter pointer, and Tcl
				      * interpreter safety indicator will be
				      * converted to strings and prepended to
				      * the configured and specified method
				      * arguments, if any. */
    METHOD_PROTOCOL_V1R2 = 0x40,     /* Superset of METHOD_PROTOCOL_V1R1.  If
				      * set, the Tcl C API function pointers
				      * will be passed via a pointer to a
				      * ClrTclStubs structure.  In addition,
				      * the Eagle interpreter isolation
				      * indicator will be passed. */
    METHOD_LOG_EXECUTE = 0x80,	     /* If set, the method execution will be
				      * logged using the configured Tcl command
				      * name. */
    METHOD_STRICT_CLR = 0x100,	     /* If set, method execution will be
				      * considered a failure if the CLR has not
				      * been started by this package. */
    METHOD_STRICT_RETURN = 0x200,    /* If set, the return value from the CLR
				      * method must be TCL_OK to be considered
				      * successful. */
    METHOD_PROTOCOL_LEGACY = 0x400,  /* If set, use the legacy protocol version
				      * indicator instead of the more explicit
				      * one for V1R1.  Normally, this should be
				      * enabled by default. */
    METHOD_USE_ISOLATION = 0x800,    /* Should an isolated Eagle interpreter be
				      * created?  Automatically set to non-zero
				      * when the associated configuration
				      * setting is enabled. */
    METHOD_USE_SAFE_INTERP = 0x1000, /* Should a "safe" Eagle interpreter be
				      * created?  Automatically set to non-zero
				      * when the associated configuration
				      * setting is enabled. */

    /*
     * NOTE: These are the "standard" flag combinations used by this package to
     *       execute CLR methods at specific stages in the package lifecycle.
     */

    METHOD_VIA_DEMAND = METHOD_LOG_EXECUTE | METHOD_STRICT_CLR,

    METHOD_VIA_LOAD = METHOD_PROTOCOL_V1R1 | METHOD_LOG_EXECUTE |
		      METHOD_STRICT_CLR | METHOD_STRICT_RETURN |
		      METHOD_PROTOCOL_LEGACY,

    METHOD_VIA_COMMAND = METHOD_PROTOCOL_V1R1 | METHOD_LOG_EXECUTE |
			 METHOD_STRICT_CLR | METHOD_STRICT_RETURN |
			 METHOD_PROTOCOL_LEGACY,

    METHOD_VIA_UNLOAD = METHOD_PROTOCOL_V1R1 | METHOD_LOG_EXECUTE |
			METHOD_STRICT_RETURN | METHOD_PROTOCOL_LEGACY,
} MethodFlags;

/*
 * NOTE: Are private Tcl stubs being used?  If so, it removes the need
 *       to link against a compiled Tcl stubs library.
 */

#if defined(USE_TCL_PRIVATE_STUBS)
/*
 * HACK: Using some preprocessor magic and private static variables,
 *       redirect the Tcl API calls [found within this file] to the
 *       function pointers that will be contained in our private Tcl
 *       stubs table.  This takes advantage of the fact that the Tcl
 *       headers always define the Tcl API functions in terms of the
 *       "tcl*StubsPtr" variables when the define USE_TCL_STUBS is
 *       present during compilation.
 */

#define tclStubsPtr privateTclStubsPtr
#define tclPlatStubsPtr privateTclPlatStubsPtr
#define tclIntStubsPtr privateTclIntStubsPtr
#define tclIntPlatStubsPtr privateTclIntPlatStubsPtr

/*
 * NOTE: Create a Tcl interpreter structure that mirrors just enough
 *       fields to get it up and running successfully with our private
 *       implementation of the Tcl stubs mechanism.
 */

typedef struct PrivateTclInterp {
    char *result;
    Tcl_FreeProc *freeProc;
    int errorLine;
    const struct TclStubs *stubTable;
} PrivateTclInterp;
#endif

/*
 * NOTE: This structure contains Tcl C API function pointers required by
 *       the Eagle native Tcl integration subsystem (i.e. TclApi).
 *
 * WARNING: The size and layout of this structure MUST match the managed
 *          "NativeStubs" structure defined in the Eagle source code file
 *          "Eagle\Library\Components\Private\TclApi.cs" exactly.
 */

typedef struct ClrTclStubs {
    size_t sizeOf;		    /* The size of this structure, in bytes. */
    void (*tcl_GetVersion) (int *, int *, int *, int *);
    void (*tcl_FindExecutable) (const char *);
    Tcl_Interp *(*tcl_CreateInterp) (void);
    void (*tcl_Preserve) (ClientData);
    void (*tcl_Release) (ClientData);
    Tcl_Obj *(*tcl_ObjGetVar2) (Tcl_Interp *, Tcl_Obj *, Tcl_Obj *, int);
    Tcl_Obj *(*tcl_ObjSetVar2) (Tcl_Interp *, Tcl_Obj *, Tcl_Obj *, Tcl_Obj *,
				int);
    int (*tcl_UnsetVar2) (Tcl_Interp *, const char *, const char *, int);
    int (*tcl_Init) (Tcl_Interp *);
    void (*tcl_InitMemory) (Tcl_Interp *);
    int (*tcl_MakeSafe) (Tcl_Interp *);
    CONST86 Tcl_ObjType *(*tcl_GetObjType) (const char *);
    int (*tcl_AppendAllObjTypes) (Tcl_Interp *, Tcl_Obj *);
    int (*tcl_ConvertToType) (Tcl_Interp *, Tcl_Obj *, const Tcl_ObjType *);
    Tcl_Command (*tcl_CreateObjCommand) (Tcl_Interp *, const char *,
				Tcl_ObjCmdProc *, ClientData,
				Tcl_CmdDeleteProc *);
    int (*tcl_DeleteCommandFromToken) (Tcl_Interp *, Tcl_Command);
    void (*tcl_DeleteInterp) (Tcl_Interp *);
    int (*tcl_InterpDeleted) (Tcl_Interp *);
    int (*tcl_InterpActive) (Tcl_Interp *);
    int (*tcl_GetErrorLine) (Tcl_Interp *);
    void (*tcl_SetErrorLine) (Tcl_Interp *, int);
    Tcl_Obj *(*tcl_NewObj) (void);
    Tcl_Obj *(*tcl_NewUnicodeObj) (const Tcl_UniChar *, int);
    Tcl_Obj *(*tcl_NewStringObj) (const char *, int);
    Tcl_Obj *(*tcl_NewByteArrayObj) (const unsigned char *, int);
    void (*tcl_DbIncrRefCount) (Tcl_Obj *, const char *, int);
    void (*tcl_DbDecrRefCount) (Tcl_Obj *, const char *, int);
    int (*tcl_CommandComplete) (const char *);
    void (*tcl_AllowExceptions) (Tcl_Interp *);
    int (*tcl_EvalObjEx) (Tcl_Interp *, Tcl_Obj *, int);
    int (*tcl_EvalFile) (Tcl_Interp *, const char *);
    int (*tcl_RecordAndEvalObj) (Tcl_Interp *, Tcl_Obj *, int);
    int (*tcl_ExprObj) (Tcl_Interp *, Tcl_Obj *, Tcl_Obj **);
    Tcl_Obj *(*tcl_SubstObj) (Tcl_Interp *, Tcl_Obj *, int);
    int (*tcl_CancelEval) (Tcl_Interp *, Tcl_Obj *, ClientData, int);
    int (*tcl_Canceled) (Tcl_Interp *, int);
    int (*tclResetCancellation) (Tcl_Interp *, int);
    void (*tclSetInterpCancelFlags) (Tcl_Interp *, int, int);
    int (*tcl_DoOneEvent) (int);
    void (*tcl_ResetResult) (Tcl_Interp *);
    Tcl_Obj *(*tcl_GetObjResult) (Tcl_Interp *);
    void (*tcl_SetObjResult) (Tcl_Interp *, Tcl_Obj *);
    Tcl_UniChar *(*tcl_GetUnicodeFromObj) (Tcl_Obj *, int *);
    char *(*tcl_GetStringFromObj) (Tcl_Obj *objPtr, int *);
    void (*tcl_CreateExitHandler) (Tcl_ExitProc *, ClientData);
    void (*tcl_DeleteExitHandler) (Tcl_ExitProc *, ClientData);
    void (*tcl_FinalizeThread) (void);
    void (*tcl_Finalize) (void);
} ClrTclStubs;

/*
 * NOTE: This structure contains the information used by this package to execute
 *       a CLR method.
 */

typedef struct ClrMethodInfo {
    size_t sizeOf;	    /* The size of this structure, in bytes. */
    LPCWSTR assemblyPath;   /* The fully qualified path and file name of the
			     * assembly containing the method to execute. */
    LPCWSTR typeName;	    /* The fully qualified type name of the type
			     * containing the method to execute. */
    LPCWSTR methodName;	    /* The method name to execute. */
    LPCWSTR argument;	    /* The argument string to be passed to the method.
			     * This SHOULD be a well-formed Tcl list. */
} ClrMethodInfo;

/*
 * NOTE: This structure contains the various configuration settings used by this
 *       package.  Currently, all of these settings are read from Tcl variables;
 *       however, this may not always be the case.
 */

typedef struct ClrConfigInfo {
    size_t sizeOf;		    /* The size of this structure, in bytes. */
    ClrMethodInfo *pStartupMethod;  /* The [cached] CLR method used to startup
				     * the bridge between Eagle and Tcl, if
				     * any. */
    ClrMethodInfo *pControlMethod;  /* The [cached] CLR method used to issue
				     * control directives to the bridge between
				     * Eagle and Tcl, if any. */
    ClrMethodInfo *pDetachMethod;   /* The [cached] CLR method used to detach a
				     * Tcl interpreter from the bridge between
				     * Eagle and Tcl, if any. */
    ClrMethodInfo *pShutdownMethod; /* The [cached] CLR method used to shutdown
				     * the bridge between Eagle and Tcl, if
				     * any. */
    LPCWSTR logCommand;		    /* The name of the Tcl command to use for
				     * logging purposes (e.g. tclLog). */
    MethodFlags methodFlags;	    /* The method flags to use when executing
				     * any of the CLR methods. */
    BOOL bVerbose;		    /* Enable extra diagnostic output from the
				     * managed assembly loading process? */
    BOOL bNoNormalize;		    /* Disable use of [file normalize] on the
				     * managed assembly path?  This is needed
				     * in some special environments due to a
				     * bug in Tcl where it resolves junctions
				     * as part of the path normalization
				     * process.*/
    BOOL bUseMinimumClr;	    /* Force using the minimum supported CLR
				     * version? */
    BOOL bLoadClr;		    /* Load the CLR immediately upon loading
				     * the package? */
    BOOL bStartClr;		    /* Start the CLR immediately upon loading
				     * the package? */
    BOOL bStartBridge;		    /* Start the bridge between Eagle and Tcl
				     * immediately upon loading the package? */
    BOOL bStopClr;		    /* Try to stop (and release) the CLR when
				     * unloading the package? */
    BOOL bUseIsolation;		    /* Should an isolated Eagle interpreter
				     * be created?  When non-zero, will cause
				     * the associated MethodFlags to be set. */
    BOOL bUseSafeInterp;	    /* Should a "safe" Eagle interpreter be
				     * created?  When non-zero, will cause the
				     * associated MethodFlags to be set. */
} ClrConfigInfo;

/*
 * NOTE: These are the functions used internally by this library (i.e. they are
 *       shared by several files).
 */

#ifndef PACKAGE_INTERN
#define PACKAGE_INTERN
#endif

PACKAGE_INTERN void	SetPackageModule(HANDLE hModule);

#endif /* _GARUDA_INT_H_ */
