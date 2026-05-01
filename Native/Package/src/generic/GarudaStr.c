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
#else
#  include <pthread.h>		    /* NOTE: For pthread_self, etc. */
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
 * Why / How:
 *	Tcl_NewUnicodeObj takes a Tcl_UniChar* (16-bit, UTF-16) on
 *	every platform.  Garuda's caller-side WCHAR is also 16-bit
 *	on Win32 and macOS-mingw, but on POSIX (Linux, macOS Clang)
 *	it is 32-bit UTF-32.  Passing a UTF-32 buffer where Tcl
 *	expects UTF-16 would index into half-words and produce
 *	scrambled output.  This function is the conversion bridge:
 *	on a "wrong size" platform, convert UTF-32 -> UTF-16 in a
 *	transient ckalloc'd buffer, hand the UTF-16 pointer to
 *	Tcl_NewUnicodeObj, free the buffer.
 *
 *	On platforms where WCHAR is already 16-bit, this entire
 *	function is bypassed via the Wrp_NewUnicodeObj macro in
 *	GarudaStr.h, which #defines straight to Tcl_NewUnicodeObj.
 *	The compile-time selection means the conversion path
 *	exists ONLY where it's actually needed.
 *
 *	The conversion uses the cvt_u32_to_u16_body macro, which
 *	requires a cvt_exit: label in this function (it does
 *	`goto cvt_exit` on any error).  See GarudaStr.h for the
 *	full conversion-macro contract.
 *
 *	Negative `length` is normalized to 0 (rather than passed
 *	through to Tcl_NewUnicodeObj which does its own NUL-search)
 *	because the conversion macro reads `length` to decide how
 *	much to convert; -1 sentinel handling is the macro's job
 *	via wcslen on `length0 == 0`.
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
 * Why / How:
 *	Same UTF-32-on-POSIX conversion problem as Cvt_NewUnicodeObj,
 *	but for the append-to-existing-object case.  Tcl_Append-
 *	UnicodeToObj wants UTF-16; on POSIX our WCHAR is UTF-32; we
 *	convert in a transient buffer and forward.
 *
 *	Returns TCL_OK / TCL_ERROR (rather than the void return of
 *	the underlying Tcl_AppendUnicodeToObj) so the caller can
 *	detect conversion failures separately from append-time
 *	errors.  In practice the conversion can fail on
 *	out-of-memory or on input that isn't valid UTF-32 (the
 *	package uses strictConversion in ConvertUTF32toUTF16, which
 *	rejects malformed sequences instead of silently substituting
 *	U+FFFD).
 *
 *	Same cvt_exit: label contract as the rest of the file.
 *	Normalizes negative length to 0 for the same reason as
 *	Cvt_NewUnicodeObj.
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
 * Why / How:
 *	Inverse of Cvt_NewUnicodeObj -- pulls a UTF-16 string out of
 *	a Tcl_Obj and converts it to UTF-32 for the POSIX caller.
 *	The conversion result is a fresh ckalloc'd buffer; the
 *	caller becomes its owner and MUST ckfree it when done.
 *
 *	The "transfer ownership" bookkeeping happens in the cvt_u16
 *	_to_u32_body macro: it sets cvtCtx1.owned=TRUE during the
 *	allocate.  We then steal the pointer (`unicode = pStart;
 *	pStart = NULL;`) so cvt_ctx_cleanup at cvt_exit: skips the
 *	ckfree -- the buffer's lifetime is now the caller's
 *	responsibility.
 *
 *	On Win32, this entire function is bypassed (Wrp_GetUnicode
 *	macro #defines straight to Tcl_GetUnicode); the result on
 *	Win32 is a non-owned Tcl_Obj-internal pointer that the
 *	caller must NOT free.  Callers therefore branch their
 *	cleanup logic by platform -- see the cleanup pattern at
 *	the bottom of GetStringObjectValue / GetStringVariableValue
 *	in Garuda.c.
 *
 *	Asymmetric error reporting: on conversion failure, returns
 *	NULL.  Compared to Cvt_AppendUnicodeToObj's TCL_OK /
 *	TCL_ERROR, this is just because the function naturally
 *	returns a pointer; NULL is the unambiguous failure marker
 *	in that ABI.
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
 * Why / How:
 *	Same conversion as Cvt_GetUnicode but additionally returns
 *	the length (in WCHAR units) via lengthPtr.  The length
 *	written is the UTF-32 unit count after conversion -- NOT
 *	the source UTF-16 unit count, which differs whenever the
 *	source contains supplementary-plane codepoints (UTF-16
 *	encodes them as surrogate pairs, two units; UTF-32
 *	encodes them as one unit).  Callers that care about
 *	codepoint-vs-unit precision should reason about lengths
 *	in the WCHAR domain that matches their own platform.
 *
 *	Same caller-owns-buffer transfer trick as Cvt_GetUnicode
 *	(steal pStart, NULL it out before cvt_exit:).  Same NULL-
 *	on-failure ABI.  Same Win32-bypass via the Wrp_*
 *	macro.
 *
 *	Note that Tcl_GetUnicode is called internally rather than
 *	Tcl_GetUnicodeFromObj -- this is intentional, the macro
 *	name reflects the public API surface, but the underlying
 *	Tcl call we wrap is the same in both cases (the length-
 *	returning variant of Tcl_GetUnicode is what we'd want, but
 *	it doesn't exist as a public stubs entry; the FromObj
 *	variant takes a length-out parameter that we couldn't
 *	bridge through the conversion macro).  The macro's
 *	post-conversion length5 field gives us the UTF-32 unit
 *	count we hand back to the caller via lengthPtr.
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
 * Why / How:
 *	A different shape from the Tcl wrappers above.  hostfxr's
 *	*_initialize_for_runtime_config* takes its config-path
 *	argument as `const char*` UTF-8 (not UTF-16) on POSIX.
 *	On Win32 the same function pointer takes UTF-16 wchar_t*.
 *	Garuda's caller-side convention is always WCHAR (UTF-32 on
 *	POSIX, UTF-16 on Win32) so we have a UTF-32-to-UTF-8
 *	conversion to do on POSIX, and a UTF-16-to-UTF-8 conversion
 *	we'd do on Win32 -- except on Win32 this entire file is
 *	compiled out (USE_GARUDA_STR is undefined) and the calls go
 *	directly to uCoreClrFunctions.pInitForRuntimeConfig with
 *	the WCHAR string.
 *
 *	On POSIX we therefore convert UTF-32 -> UTF-8, route to the
 *	cached function pointer in uCoreClrFunctions (populated
 *	during LoadAndStartTheCoreClr), free the temp buffer at
 *	cvt_exit:.  E_POINTER is returned if the function pointer
 *	is NULL -- that means the CoreCLR was never loaded or
 *	hostfxr resolution failed; the caller should already have
 *	checked, but we defend the call site.
 *
 *	The dnh_init_params parameter type is a typedef for the
 *	platform's hostfxr_initialize_parameters struct; see
 *	GarudaStr.h for the typedef.  It is passed through
 *	unchanged -- the only string conversion needed is the
 *	runtime config path itself.
 *
 *	Note: dnh_init_params has its OWN string fields
 *	(host_path, dotnet_root) that are also UTF-8 on POSIX.
 *	The caller is responsible for filling those in UTF-8;
 *	this function does not transitively convert them.  See
 *	GarudaCoreClr.c's call site.
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
 * Why / How:
 *	Same shape as Cvt_pInitForRuntimeConfig but converts FOUR
 *	string arguments (assemblyPath, typeName, methodName,
 *	delegateTypeName) instead of one.  Each gets its own
 *	cvt_u8_decls(N) and cvt_u32_to_u8_body(cvtCtxN, ...) pair.
 *	The numbered cvt_u8_decls(1)..cvt_u8_decls(4) trick is what
 *	allows the body macro to use a unique struct name per call
 *	site (see the cvt_declare_context_type macro in the header
 *	for how the struct names are templatized).
 *
 *	delegateTypeName is OPTIONAL -- the underlying load_assembly_
 *	and_get_function_pointer_fn accepts NULL to mean "use the
 *	UnmanagedCallersOnly attribute on the target method", which
 *	is the modern preferred pattern.  When NULL, we skip the
 *	conversion and pass NULL straight through; the conversion
 *	context cvtCtx4 stays zeroed so cvt_ctx_cleanup is a no-op.
 *
 *	Cleanup is in reverse construction order (cvtCtx4 -> 1) at
 *	cvt_exit:, matching the convention used elsewhere in the
 *	package -- purely cosmetic for independent allocations but
 *	makes leak diagnostics consistent across the codebase.
 *
 *	Same E_POINTER-on-missing-function-pointer pattern as
 *	Cvt_pInitForRuntimeConfig.  Same Win32-bypass via the
 *	Wrp_* macro.
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
 * Why / How:
 *	The most-different shape in this file: this one has both
 *	an INPUT-side and an OUTPUT-side conversion, and the
 *	output buffer is caller-supplied with a fixed size.
 *
 *	The underlying GarudaPal-internal get_module_file_name on
 *	POSIX returns UTF-8 (resolved via dladdr + realpath).  Our
 *	caller wants UTF-32 wchar_t in `fileName` with `size`
 *	WCHAR units of capacity.  So:
 *
 *	  1. Allocate a UTF-8 staging buffer big enough to hold
 *	     `size` WCHARs worst-case-expanded as UTF-8.  The
 *	     worst-case expansion is UNI_UTF8_MAX_BYTES (4) per
 *	     UCS-4 unit.  We guard against size_t overflow on the
 *	     multiply (size > SIZE_MAX / UNI_UTF8_MAX_BYTES) and
 *	     bail with result=0 if it would wrap.
 *	  2. Call get_module_file_name into the UTF-8 staging
 *	     buffer.
 *	  3. Convert UTF-8 -> UTF-32 via cvt_u8_to_u32_body.
 *	  4. Copy the converted output into the caller's `fileName`
 *	     buffer with cvt_ctx_copy_clamped_with_nul, which
 *	     truncates to capacity-1 and NUL-terminates.
 *	  5. Free the UTF-8 staging buffer.
 *
 *	Returns 0 on any failure (overflow, alloc, get_module_file
 *	_name, conversion).  Returns the number of WCHARs written
 *	(NOT including NUL) on success -- same convention as the
 *	Win32 GetModuleFileNameW it imitates.
 *
 *	On Win32 this whole function is bypassed via Wrp_get_module_
 *	file_name #defining straight to GetModuleFileNameW, so the
 *	staging-buffer dance is paid only on POSIX where it's
 *	actually needed.
 *
 *	The cvtBuf0 free at cvt_exit: is unconditional rather than
 *	owned-flag-gated because cvtBuf0 is allocated outside the
 *	cvt_ctx machinery (it's the input to cvt_u8_to_u32_body
 *	rather than the output context's pStart).
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
