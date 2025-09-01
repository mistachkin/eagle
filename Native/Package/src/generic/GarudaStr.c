/*
 * GarudaStr.c -- Eagle Package for Tcl (Garuda)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#include "GarudaPre.h"		    /* NOTE: For private header setup. */

#if defined(USE_GARUDA_STR)
#include <limits.h>		    /* NOTE: For INT_MAX, etc. */
#include <stddef.h>		    /* NOTE: For size_t, SIZE_MAX, etc. */
#include <stdlib.h>		    /* NOTE: For free, realpath, etc. */
#include <string.h>		    /* NOTE: For strlen, strrchr, strcat, etc. */
#include <wchar.h>		    /* NOTE: For wchar_t, etc. */

#if defined(_WIN32)
#  include <windows.h>		    /* NOTE: For LoadLibraryW, etc. */
#endif

#if defined(USE_CORE_CLR)
#  include <nethost.h>		    /* NOTE: For get_hostfxr_path, etc. */
#  include <hostfxr.h>		    /* NOTE: For "hostfxr_*" .NET (Core), etc. */
#  include <coreclr_delegates.h>    /* NOTE: For load_<asm>_and_get_<fn_ptr>. */
#endif

#include "tcl.h"		    /* NOTE: For public Tcl API. */
#include "GarudaPal.h"		    /* NOTE: For platform abstraction API. */
#include "GarudaInt.h"		    /* NOTE: For private package API. */
#include "GarudaCoreClr.h"	    /* NOTE: For private package CoreCLR API. */
#include "GarudaDecls.h"	    /* NOTE: For private package declarations. */
#include "ConvertUTF_v2.h"	    /* NOTE: Unicode UTF-* reference conversions. */
#include "GarudaStr.h"		    /* NOTE: For private string API. */

/*
 * HACK: Several CoreCLR function pointers are needed in this file.
 */

#if defined(USE_CORE_CLR)
extern volatile CoreClrFunctions uCoreClrFunctions;
#endif

/*
 *----------------------------------------------------------------------
 *
 * Cvt_NewUnicodeObj --
 *
 *	This function is a WCHAR string-conversion wrapper around the
 *	Tcl_NewUnicodeObj public Tcl runtime API.
 *
 * Results:
 *	Please see the Tcl_NewUnicodeObj function.
 *
 * Side effects:
 *	Please see the Tcl_NewUnicodeObj function.
 *
 *----------------------------------------------------------------------
 */

Tcl_Obj *Cvt_NewUnicodeObj(
    LPCWSTR unicode,		/* The unicode string used to initialize
				 * the new object. */
    int length)			/* Number of characters in the unicode
				 * string. */
{
    cvt_decls();
    cvt_u16_decls(1);

    if (length < 0)
	length = 0;

    cvt_u32_to_u16_body(cvtCtx1, unicode, length, 1);

    if (cvt_failed(cvtCtx1))
	goto cvt_exit;

    cvtObj0 = Tcl_NewUnicodeObj(
	cvtCtx1.pStart, (int)cvtCtx1.length5);

cvt_exit:

    cvt_ctx_cleanup(cvtCtx1);

    return cvtObj0;
}

/*
 *----------------------------------------------------------------------
 *
 * Cvt_AppendUnicodeToObj --
 *
 *	This function is a WCHAR string-conversion wrapper around the
 *	Tcl_AppendUnicodeToObj public Tcl runtime API.
 *
 * Results:
 *	Please see the Tcl_AppendUnicodeToObj function.
 *
 * Side effects:
 *	Please see the Tcl_AppendUnicodeToObj function.
 *
 *----------------------------------------------------------------------
 */

int Cvt_AppendUnicodeToObj(
    Tcl_Obj *objPtr,		/* Points to the object to append to. */
    LPCWSTR unicode,		/* The unicode string to append to the
				 * object. */
    int length)			/* Number of chars in "unicode". */
{
    int code = TCL_ERROR;

    cvt_decls();
    cvt_u16_decls(1);

    if (length < 0)
	length = 0;

    cvt_u32_to_u16_body(cvtCtx1, unicode, length, 1);

    if (cvt_failed(cvtCtx1))
	goto cvt_exit;

    Tcl_AppendUnicodeToObj(
	objPtr, cvtCtx1.pStart, (int)cvtCtx1.length5);

    code = TCL_OK;

cvt_exit:

    cvt_ctx_cleanup(cvtCtx1);

    return code;
}

/*
 *----------------------------------------------------------------------
 *
 * Cvt_GetUnicode --
 *
 *	This function is a WCHAR string-conversion wrapper around the
 *	Tcl_GetUnicode public Tcl runtime API.
 *
 * Results:
 *	Please see the Tcl_GetUnicode function.
 *
 * Side effects:
 *	Please see the Tcl_GetUnicode function.
 *
 *----------------------------------------------------------------------
 */

LPWSTR Cvt_GetUnicode(
    Tcl_Obj *objPtr)	/* The object to find the unicode string for. */
{
    /*
     * HACK: Caller receives ownership of UTF-32 string "unicode" -AND-
     *       must call ckfree() on it when done in order to free its
     */

    LPWSTR unicode = NULL;

    cvt_decls();
    cvt_u32_decls(1);

    cvt_u16_to_u32_body(cvtCtx1, Tcl_GetUnicode(objPtr), 0, 1);

    if (cvt_failed(cvtCtx1))
	goto cvt_exit;

    unicode = cvtCtx1.pStart;
    cvtCtx1.pStart = NULL;

cvt_exit:

    cvt_ctx_cleanup(cvtCtx1);

    return unicode;
}

/*
 *----------------------------------------------------------------------
 *
 * Cvt_GetUnicodeFromObj --
 *
 *	This function is a WCHAR string-conversion wrapper around the
 *	Tcl_GetUnicodeFromObj public Tcl runtime API.
 *
 * Results:
 *	Please see the Tcl_GetUnicodeFromObj function.
 *
 * Side effects:
 *	Please see the Tcl_GetUnicodeFromObj function.
 *
 *----------------------------------------------------------------------
 */

LPWSTR Cvt_GetUnicodeFromObj(
    Tcl_Obj *objPtr,	/* The object to find the unicode string for. */
    int *lengthPtr)	/* If non-NULL, the location where the
			 * string rep's unichar length should be
			 * stored. If NULL, no length is stored. */
{
    /*
     * HACK: Caller receives ownership of UTF-32 string "unicode" -AND-
     *       must call ckfree() on it when done in order to free its
     */

    LPWSTR unicode = NULL;

    cvt_decls();
    cvt_u32_decls(1);

    cvt_u16_to_u32_body(cvtCtx1, Tcl_GetUnicode(objPtr), 0, 1);

    if (cvt_failed(cvtCtx1))
	goto cvt_exit;

    unicode = cvtCtx1.pStart;
    cvtCtx1.pStart = NULL;

    if (lengthPtr != NULL)
	*lengthPtr = (int)cvtCtx1.length5;

cvt_exit:

    cvt_ctx_cleanup(cvtCtx1);

    return unicode;
}

#if defined(USE_CORE_CLR)
/*
 *----------------------------------------------------------------------
 *
 * Cvt_pInitForRuntimeConfig --
 *
 *	This function is a WCHAR string-conversion wrapper around the
 *	hostfxr_initialize_for_runtime_config_fn .NET delegate type.
 *
 * Results:
 *	Please see the hostfxr_initialize_for_runtime_config_fn type.
 *
 * Side effects:
 *	Please see the hostfxr_initialize_for_runtime_config_fn type.
 *
 *----------------------------------------------------------------------
 */

int32_t Cvt_pInitForRuntimeConfig(
    LPCWSTR runtimeConfigPath,		/* Fully qualified path to the
					 * configuration file for the
					 * runtime. */
    dnh_init_params *parameters,	/* The extra initialization
					 * parameters, if any.  This is
					 * optional and may be NULL. */
    hostfxr_handle *hostContextHandle)	/* Upon success, this will be
					 * modified to contain the
					 * created context handle. */
{
    int32_t result = E_FAIL;

    cvt_decls();
    cvt_u8_decls(1);

    cvt_u32_to_u8_body(cvtCtx1, runtimeConfigPath, 0, 1);

    if (cvt_failed(cvtCtx1))
	goto cvt_exit;

    if (uCoreClrFunctions.pInitForRuntimeConfig == NULL) {
	result = E_POINTER;
	goto cvt_exit;
    }

    result = uCoreClrFunctions.pInitForRuntimeConfig(
	(const char *)cvtCtx1.pStart, parameters, hostContextHandle);

cvt_exit:

    cvt_ctx_cleanup(cvtCtx1);

    return result;
}

/*
 *----------------------------------------------------------------------
 *
 * Cvt_pLoadAssemblyAndGetFuncPtr --
 *
 *	This function is a WCHAR string-conversion wrapper around the
 *	load_assembly_and_get_function_pointer_fn .NET delegate type.
 *
 * Results:
 *	Please see the load_assembly_and_get_function_pointer_fn type.
 *
 * Side effects:
 *	Please see the load_assembly_and_get_function_pointer_fn type.
 *
 *----------------------------------------------------------------------
 */

int Cvt_pLoadAssemblyAndGetFuncPtr(
    LPCWSTR assemblyPath,	/* Fully qualified path to assembly. */
    LPCWSTR typeName,		/* Assembly qualified type name. */
    LPCWSTR methodName,		/* Compatible public static method
				 * name. */
    LPCWSTR delegateTypeName,	/* Assembly qualified type name -OR-
				 * null. */
    void *pReserved,		/* Reserved, must be zero. */
    void **ppDelegate)		/* Store function pointer here. */
{
    int result = E_FAIL;

    cvt_decls();
    cvt_u8_decls(1);
    cvt_u8_decls(2);
    cvt_u8_decls(3);
    cvt_u8_decls(4);

    cvt_u32_to_u8_body(cvtCtx1, assemblyPath, 0, 1);

    if (cvt_failed(cvtCtx1))
	goto cvt_exit;

    cvt_u32_to_u8_body(cvtCtx2, typeName, 0, 1);

    if (cvt_failed(cvtCtx2))
	goto cvt_exit;

    cvt_u32_to_u8_body(cvtCtx3, methodName, 0, 1);

    if (cvt_failed(cvtCtx3))
	goto cvt_exit;

    if (delegateTypeName != NULL) {
	cvt_u32_to_u8_body(cvtCtx4, delegateTypeName, 0, 1);

	if (cvt_failed(cvtCtx4))
	    goto cvt_exit;
    }

    if (uCoreClrFunctions.pLoadAssemblyAndGetFuncPtr == NULL) {
	result = E_POINTER;
	goto cvt_exit;
    }

    result = uCoreClrFunctions.pLoadAssemblyAndGetFuncPtr(
	(const char *)cvtCtx1.pStart, (const char *)cvtCtx2.pStart,
	(const char *)cvtCtx3.pStart, (const char *)cvtCtx4.pStart,
	pReserved, ppDelegate);

cvt_exit:

    cvt_ctx_cleanup(cvtCtx4);
    cvt_ctx_cleanup(cvtCtx3);
    cvt_ctx_cleanup(cvtCtx2);
    cvt_ctx_cleanup(cvtCtx1);

    return result;
}
#endif /* defined(USE_CORE_CLR) */

#if !defined(_WIN32)
/*
 *----------------------------------------------------------------------
 *
 * Cvt_get_module_file_name --
 *
 *	This function is a WCHAR string-conversion wrapper around the
 *	get_module_file_name private platform abstraction API.
 *
 * Results:
 *	Please see the get_module_file_name function.
 *
 * Side effects:
 *	Please see the get_module_file_name function.
 *
 *----------------------------------------------------------------------
 */

size_t Cvt_get_module_file_name(
    HMODULE hModule,		/* The module handle to query the
				 * file name for. */
    LPWSTR fileName,		/* Buffer that should contain the
				 * target file name upon success. */
    size_t size)		/* Size of the file name buffer. */
{
    size_t result = 0;
    size_t capacity = 0;

    cvt_decls();
    cvt_u32_decls(1);
    cvt_ctx_initialize(cvtCtx1);

    if (size > (SIZE_MAX / UNI_UTF8_MAX_BYTES))
	goto cvt_exit;

    capacity = size * UNI_UTF8_MAX_BYTES;
    cvtBuf0 = (UTF8 *)attemptckalloc((capacity + 1) * sizeof(UTF8));

    if (cvtBuf0 == NULL)
	goto cvt_exit;

    memset(cvtBuf0, 0, (capacity + 1) * sizeof(UTF8));

    result = get_module_file_name(hModule, (char *)cvtBuf0, capacity);

    if (result == 0)
	goto cvt_exit;

    cvt_u8_to_u32_body(cvtCtx1, cvtBuf0, 0, 1);

    if (cvt_failed(cvtCtx1)) {
	result = 0;
	goto cvt_exit;
    }

    cvt_ctx_copy_clamped_with_nul(result, cvtCtx1, fileName, size);

cvt_exit:

    if (cvtBuf0 != NULL) {
	ckfree((LPVOID)cvtBuf0);
	cvtBuf0 = NULL;
    }

    cvt_ctx_cleanup(cvtCtx1);

    return result;
}
#endif /* !defined(_WIN32) */
#endif /* defined(USE_GARUDA_STR) */
