/*
 * GarudaCoreClr.h -- Eagle Package for Tcl (Garuda)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#ifndef _GARUDA_CORE_CLR_H_
#define _GARUDA_CORE_CLR_H_

/*
 * NOTE: These are the primary (major) versions of the CLR and CoreCLR that
 *       this package knows about.
 */

#ifndef CORE_CLR_VERSION_V3
#  define CORE_CLR_VERSION_V3			"v3.0+"
#endif

/*
 * NOTE: This is the latest version of the CoreCLR that we know about.
 */

#if defined(USE_CORE_CLR)
#  define CLR_MODULE_NAME			"nethost"
#  define CLR_CORE_ENVVAR_NAME			"UseCoreClr"
#  define CLR_VERSION_MINIMUM			CORE_CLR_VERSION_V3
#  define CLR_VERSION_LATEST			CORE_CLR_VERSION_V3
#endif

/*
 * NOTE: This structure contains the various CoreCLR function pointers used
 *       by this package.  This is applicable -ONLY- when compiling for the
 *       .NET runtime, i.e. not the "legacy" .NET Framework.
 */

#if defined(USE_CORE_CLR)
typedef struct CoreClrFunctions {
    size_t sizeOf;			/* Size of this structure, in bytes. */
#if defined(HAVE_DOTNET_ENVIRONMENT_INFO)
    hostfxr_get_dotnet_environment_info_fn
	pGetDotNetEnvInfo;		/* Used to obtain version information
					 * from the CoreCLR. */
#endif
    hostfxr_initialize_for_runtime_config_fn
	pInitForRuntimeConfig;		/* Used to initialize the CoreCLR via
					 * specified runtime configuration. */
    hostfxr_get_runtime_delegate_fn
	pGetRuntimeDelegate;		/* Used to lookup a necessary CoreCLR
					 * runtime functions, e.g. the last
					 * member in this structure. */
    hostfxr_close_fn pClose;		/* Used to close the CoreCLR runtime
					 * context. */
    load_assembly_and_get_function_pointer_fn
	pLoadAssemblyAndGetFuncPtr;	/* Used to obtain a native function
					 * pointer from a managed assembly,
					 * after loading it first. */
} CoreClrFunctions;

/*
 * NOTE: This structure is used for bi-directional communication with the
 *       CoreCLR version subsystem via the callback function(s).
 */

typedef struct CoreClrVersionInfo {
    size_t sizeOf;	    /* Size of this structure, in bytes. */
    Tcl_Interp *interp;	    /* Current Tcl interpreter.  This may be
			     * NULL. */
    Tcl_Obj *result;	    /* Staging area for all results from the
			     * version query.  This may be NULL and
			     * will be created as needed. */
    int count;		    /* How many times has the callback been
			     * invoked? */
} CoreClrVersionInfo;
#endif

#endif /* _GARUDA_INT_H_ */
