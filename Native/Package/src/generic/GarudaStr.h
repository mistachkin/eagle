/*
 * GarudaStr.h -- Eagle Package for Tcl (Garuda)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#ifndef _GARUDA_STR_H_
#define _GARUDA_STR_H_

/*
 * WARNING: Using the family of functions (macros) contained in this file will
 *          require every such calling function to have a cleanup label named
 *          "cvt_exit", which must call the cvt_cleanup() function (macro) on
 *          each converted UTF-X string lvalue, e.g. Cvt_GetUnicode*(), et al.
 *
 * WARNING: The HRESULT should be checked via cvt_succeeded() after conversion
 *          operations and/or (?) after the "done" cleanup label, if necessary.
 *          Since strictConversion is used, there are several possible errors,
 *          including input that does not conform to the Unicode Standard.
 *
 * WARNING: When using this header file, the following other headers are also
 *          (almost always) required:
 *
 *          #include <limits.h>
 *          #include <stddef.h>
 *          #include <string.h>
 *          #include <wchar.h>
 *          #include "ConvertUTF_v2.h"
 */

#ifndef COMPILE_TIME_ASSERT
#  define COMPILE_TIME_ASSERT(name, expr) typedef char name[(expr) ? 1 : -1]
#endif

#if !defined(_WIN32)
COMPILE_TIME_ASSERT(char_8bits_size_check, sizeof(char) == 1);
COMPILE_TIME_ASSERT(Tcl_UniChar_16bits_size_check, sizeof(Tcl_UniChar) == 2);
COMPILE_TIME_ASSERT(wchar_t_32bits_size_check, sizeof(wchar_t) == 4);
COMPILE_TIME_ASSERT(unsigned_int_32bits_size_check, sizeof(unsigned int) == 4);

COMPILE_TIME_ASSERT(UTF8_size_check, sizeof(UTF8) >= sizeof(char));
COMPILE_TIME_ASSERT(UTF16_size_check, sizeof(UTF16) >= sizeof(Tcl_UniChar));
COMPILE_TIME_ASSERT(UTF32_size_check, sizeof(UTF32) >= sizeof(wchar_t));
#endif /* !defined(_WIN32) */

#if !defined(FACILITY_CUSTOMER_BIT)
#define FACILITY_CUSTOMER_BIT		(0x20000000)
#endif

#if !defined(FACILITY_CVTUTF)
#define FACILITY_CVTUTF			(2004)
#endif

#if !defined(FACILITY_CUSTOMER_CVTUTF)
#define FACILITY_CUSTOMER_CVTUTF \
			(((unsigned int)(FACILITY_CUSTOMER_BIT)) | \
			(((unsigned int)(FACILITY_CVTUTF)) << 16))
#endif

#if !defined(HRESULT_FROM_CVTUTF)
#define HRESULT_FROM_CVTUTF(x) \
		((HRESULT)((((unsigned int)(SEVERITY_ERROR)) << 31) | \
		(FACILITY_CUSTOMER_CVTUTF) | ((x) & 0xFFFF)))
#endif

#if !defined(SIZE_T_MAX)
#define SIZE_T_MAX			((size_t)(~(size_t)0))
#endif

#if !defined(_CVT_MAX_UTF8_DEFINED)
#define _CVT_MAX_UTF8_DEFINED
static const size_t cvt_max_utf8 = SIZE_T_MAX / sizeof(UTF8);
#endif

#if !defined(_CVT_MAX_UTF16_DEFINED)
#define _CVT_MAX_UTF16_DEFINED
static const size_t cvt_max_utf16 = SIZE_T_MAX / sizeof(UTF16);
#endif

#if !defined(_CVT_MAX_UTF32_DEFINED)
#define _CVT_MAX_UTF32_DEFINED
static const size_t cvt_max_utf32 = SIZE_T_MAX / sizeof(UTF32);
#endif

/*
 * NOTE: This structure contains the information used by this package to
 *       execute a CLR method.
 */

#if !defined(cvt_declare_context_type)
#define cvt_declare_context_type(type)		\
typedef struct Cvt_Context_##type {		\
    size_t sizeOf;				\
    HRESULT hResult;				\
    int owned;					\
    type *pStart;				\
    type *pCurrent;				\
    size_t length0;				\
    size_t length1;				\
    size_t length2;				\
    size_t length3;				\
    size_t length4;				\
    size_t length5;				\
} Cvt_Context_##type;
#endif

#if !defined(_CVT_CONTEXT_U8_DEFINED)
#define _CVT_CONTEXT_U8_DEFINED
cvt_declare_context_type(UTF8);
#endif

#if !defined(_CVT_CONTEXT_U16_DEFINED)
#define _CVT_CONTEXT_U16_DEFINED
cvt_declare_context_type(UTF16);
#endif

#if !defined(_CVT_CONTEXT_U32_DEFINED)
#define _CVT_CONTEXT_U32_DEFINED
cvt_declare_context_type(UTF32);
#endif

#if !defined(cvt_decls)
#define cvt_decls()			Tcl_Obj *cvtObj0 = NULL;	\
					UTF8 *cvtBuf0 = NULL;
#endif

#if !defined(cvt_u8_decls)
#define cvt_u8_decls(i)			Cvt_Context_UTF8 cvtCtx##i = {0};
#endif

#if !defined(cvt_u16_decls)
#define cvt_u16_decls(i)		Cvt_Context_UTF16 cvtCtx##i = {0};
#endif

#if !defined(cvt_u32_decls)
#define cvt_u32_decls(i)		Cvt_Context_UTF32 cvtCtx##i = {0};
#endif

#if !defined(cvt_succeeded)
#define cvt_succeeded(a)		(SUCCEEDED((a).hResult))
#endif

#if !defined(cvt_failed)
#define cvt_failed(a)			(FAILED((a).hResult))
#endif

#if !defined(cvt_cleanup)
#define cvt_cleanup(a) do {						\
    void *p = (a);							\
    if (p != NULL) {							\
	ckfree(p);							\
	p = NULL;							\
    }									\
    (a) = NULL;								\
} while(0);
#endif

#if !defined(cvt_ctx_initialize)
#define cvt_ctx_initialize(v)		memset(&(v), 0, sizeof((v)));
#endif

#if !defined(cvt_ctx_cleanup)
#define cvt_ctx_cleanup(v) do {						\
    HRESULT hCtxRes0 = (v).hResult;					\
    if (FAILED(hCtxRes0)) {						\
	TracePrintf("FAILED cvt_ctx: hResult = 0x%08x\n",		\
	    (unsigned int)(hCtxRes0));					\
    }									\
    if ((v).owned) {							\
	cvt_cleanup((v).pStart);					\
    }									\
    (v).pStart = NULL;							\
} while (0);
#endif

#if !defined(cvt_u8_to_u32_body)
#define cvt_u8_to_u32_body(a, b, c, d) do {				\
    ConversionResult crc;						\
    BOOL allocate = (d);						\
    const UTF8 *pSrc = (b);						\
    if (allocate) cvt_ctx_initialize((a));				\
    (a).owned = allocate;						\
    if (pSrc == NULL) {							\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = E_POINTER;					\
	goto cvt_exit;							\
    }									\
    (a).sizeOf = sizeof((a));						\
    (a).length0 = (c);							\
    (a).length1 = ((a).length0 > 0) ?					\
	(a).length0 : strlen((const char *)pSrc);			\
    (a).length2 = (a).length1;						\
    (a).length3 = (a).length2 + 1;					\
    if (((a).length3 < (a).length1) ||					\
	((a).length3 >= cvt_max_utf32)) {				\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(targetExhausted);		\
	goto cvt_exit;							\
    }									\
    (a).length4 = (a).length3 * sizeof(UTF32);				\
    if (allocate) {							\
	(a).pStart = (UTF32 *)attemptckalloc((a).length4);		\
    }									\
    if ((a).pStart == NULL) {						\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = allocate ? E_OUTOFMEMORY : E_POINTER;		\
	goto cvt_exit;							\
    }									\
    if (allocate) {							\
	memset((a).pStart, 0, (a).length4);				\
    }									\
    (a).pCurrent = (a).pStart;						\
    crc = ConvertUTF8toUTF32(						\
	&pSrc, pSrc + (a).length1, &((a).pCurrent),			\
	(a).pCurrent + (a).length2, strictConversion);			\
    if (crc != conversionOK) {						\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(crc);				\
	goto cvt_exit;							\
    }									\
    (a).length5 = (size_t)((a).pCurrent - (a).pStart);			\
    if ((a).length5 > (size_t)INT_MAX) {				\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(targetExhausted);		\
	goto cvt_exit;							\
    }									\
} while (0);
#endif

#if !defined(cvt_u16_to_u32_body)
#define cvt_u16_to_u32_body(a, b, c, d) do {				\
    ConversionResult crc;						\
    BOOL allocate = (d);						\
    const UTF16 *pSrc = (b);						\
    if (allocate) cvt_ctx_initialize((a));				\
    (a).owned = allocate;						\
    if (pSrc == NULL) {							\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = E_POINTER;					\
	goto cvt_exit;							\
    }									\
    (a).sizeOf = sizeof((a));						\
    (a).length0 = (c);							\
    (a).length1 = ((a).length0 > 0) ?					\
	(a).length0 : Tcl_UniCharLen((Tcl_UniStr)pSrc);			\
    (a).length2 = (a).length1;						\
    (a).length3 = (a).length2 + 1;					\
    if (((a).length3 < (a).length1) ||					\
	((a).length3 >= cvt_max_utf32)) {				\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(targetExhausted);		\
	goto cvt_exit;							\
    }									\
    (a).length4 = (a).length3 * sizeof(UTF32);				\
    if (allocate) {							\
	(a).pStart = (UTF32 *)attemptckalloc((a).length4);		\
    }									\
    if ((a).pStart == NULL) {						\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = allocate ? E_OUTOFMEMORY : E_POINTER;		\
	goto cvt_exit;							\
    }									\
    if (allocate) {							\
	memset((a).pStart, 0, (a).length4);				\
    }									\
    (a).pCurrent = (a).pStart;						\
    crc = ConvertUTF16toUTF32(						\
	&pSrc, pSrc + (a).length1, &((a).pCurrent),			\
	(a).pCurrent + (a).length2, strictConversion);			\
    if (crc != conversionOK) {						\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(crc);				\
	goto cvt_exit;							\
    }									\
    (a).length5 = (size_t)((a).pCurrent - (a).pStart);			\
    if ((a).length5 > (size_t)INT_MAX) {				\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(targetExhausted);		\
	goto cvt_exit;							\
    }									\
} while (0);
#endif

#if !defined(cvt_u32_to_u8_body)
#define cvt_u32_to_u8_body(a, b, c, d) do {				\
    ConversionResult crc;						\
    BOOL allocate = (d);						\
    const UTF32 *pSrc = (b);						\
    if (allocate) cvt_ctx_initialize((a));				\
    (a).owned = allocate;						\
    if (pSrc == NULL) {							\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = E_POINTER;					\
	goto cvt_exit;							\
    }									\
    (a).length0 = (c);							\
    (a).length1 = ((a).length0 > 0) ?					\
	(a).length0 : wcslen((const wchar_t *)pSrc);			\
    (a).length2 = (a).length1 * UNI_UTF8_MAX_BYTES;			\
    (a).length3 = (a).length2 + 1;					\
    if (((a).length3 < (a).length1) ||					\
	((a).length3 >= cvt_max_utf8)) {				\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(targetExhausted);		\
	goto cvt_exit;							\
    }									\
    (a).length4 = (a).length3 * sizeof(UTF8);				\
    if (allocate) {							\
	(a).pStart = (UTF8 *)attemptckalloc((a).length4);		\
    }									\
    if ((a).pStart == NULL) {						\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = allocate ? E_OUTOFMEMORY : E_POINTER;		\
	goto cvt_exit;							\
    }									\
    if (allocate) {							\
	memset((a).pStart, 0, (a).length4);				\
    }									\
    (a).pCurrent = (a).pStart;						\
    crc = ConvertUTF32toUTF8(						\
	&pSrc, pSrc + (a).length1, &((a).pCurrent),			\
	(a).pCurrent + (a).length2, strictConversion);			\
    if (crc != conversionOK) {						\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(crc);				\
	goto cvt_exit;							\
    }									\
    (a).length5 = (size_t)((a).pCurrent - (a).pStart);			\
    if ((a).length5 > (size_t)INT_MAX) {				\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(targetExhausted);		\
	goto cvt_exit;							\
    }									\
} while (0)
#endif

#if !defined(cvt_u32_to_u16_body)
#define cvt_u32_to_u16_body(a, b, c, d) do {				\
    ConversionResult crc;						\
    BOOL allocate = (d);						\
    const UTF32 *pSrc = (b);						\
    if (allocate) cvt_ctx_initialize((a));				\
    (a).owned = allocate;						\
    if (pSrc == NULL) {							\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = E_POINTER;					\
	goto cvt_exit;							\
    }									\
    (a).length0 = (c);							\
    (a).length1 = ((a).length0 > 0) ?					\
	(a).length0 : wcslen((const wchar_t *)pSrc);			\
    (a).length2 = (a).length1 * 2;					\
    (a).length3 = (a).length2 + 1;					\
    if (((a).length3 < (a).length1) ||					\
	((a).length3 >= cvt_max_utf16)) {				\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(targetExhausted);		\
	goto cvt_exit;							\
    }									\
    (a).length4 = (a).length3 * sizeof(UTF16);				\
    if (allocate) {							\
	(a).pStart = (UTF16 *)attemptckalloc((a).length4);		\
    }									\
    if ((a).pStart == NULL) {						\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = E_OUTOFMEMORY;					\
	goto cvt_exit;							\
    }									\
    if (allocate) {							\
	memset((a).pStart, 0, (a).length4);				\
    }									\
    (a).pCurrent = (a).pStart;						\
    crc = ConvertUTF32toUTF16(						\
	&pSrc, pSrc + (a).length1, &((a).pCurrent),			\
	(a).pCurrent + (a).length2, strictConversion);			\
    if (crc != conversionOK) {						\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(crc);				\
	goto cvt_exit;							\
    }									\
    (a).length5 = (size_t)((a).pCurrent - (a).pStart);			\
    if ((a).length5 > (size_t)INT_MAX) {				\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(targetExhausted);		\
	goto cvt_exit;							\
    }									\
} while (0);
#endif

#if !defined(cvt_copy_clamped_with_nul)
#define cvt_copy_clamped_with_nul(a, b, c, d, e) do {			\
    size_t copied0 = 0;							\
    size_t capacity0  = (c);						\
    if (capacity0 > 0) {						\
	size_t length0 = (e);						\
	copied0 = (length0 < (capacity0 - 1)) ?				\
	    length0 : (capacity0 - 1);					\
	if (copied0 > 0) {						\
	    memmove((b), (d), copied0 * sizeof(*(b)));			\
	}								\
	(b)[copied0] = 0;						\
    }									\
    (a) = copied0;							\
} while (0);
#endif

#if !defined(cvt_ctx_copy_clamped_with_nul)
#define cvt_ctx_copy_clamped_with_nul(a, b, c, d)			\
    cvt_copy_clamped_with_nul((a), (c), (d), (b).pStart, (b).length5)
#endif

#if defined(USE_CORE_CLR) && !defined(_WIN32)
/*
 * HACK: Make using the (ugly) "hostfxr_initialize_parameters" CoreCLR SDK
 *       struct type a bit easier.
 */

typedef const struct hostfxr_initialize_parameters dnh_init_params;

PACKAGE_INTERN Tcl_Obj *Cvt_NewUnicodeObj(LPCWSTR unicode, int length);
PACKAGE_INTERN int	Cvt_AppendUnicodeToObj(Tcl_Obj *objPtr,
			    LPCWSTR unicode, int length);
PACKAGE_INTERN LPWSTR	Cvt_GetUnicode(Tcl_Obj *objPtr);
PACKAGE_INTERN LPWSTR	Cvt_GetUnicodeFromObj(Tcl_Obj *objPtr,
			    int *lengthPtr);
PACKAGE_INTERN int32_t	Cvt_pInitForRuntimeConfig(LPCWSTR runtimeConfigPath,
			    dnh_init_params *parameters,
			    hostfxr_handle *hostContextHandle);
PACKAGE_INTERN int	Cvt_pLoadAssemblyAndGetFuncPtr(LPCWSTR assemblyPath,
			    LPCWSTR typeName, LPCWSTR methodName,
			    LPCWSTR delegateTypeName, void *pReserved,
			    void **ppDelegate);
PACKAGE_INTERN size_t	Cvt_get_module_file_name(HMODULE hModule,
			    LPWSTR fileName, size_t size);

#  define Wrp_NewUnicodeObj			Cvt_NewUnicodeObj
#  define Wrp_AppendUnicodeToObj		Cvt_AppendUnicodeToObj
#  define Wrp_GetUnicode			Cvt_GetUnicode
#  define Wrp_GetUnicodeFromObj			Cvt_GetUnicodeFromObj
#  define Wrp_pInitForRuntimeConfig		Cvt_pInitForRuntimeConfig
#  define Wrp_pLoadAssemblyAndGetFuncPtr	Cvt_pLoadAssemblyAndGetFuncPtr
#  define Wrp_get_module_file_name		Cvt_get_module_file_name
#else
#  define Wrp_NewUnicodeObj			Tcl_NewUnicodeObj
#  define Wrp_AppendUnicodeToObj		Tcl_AppendUnicodeToObj
#  define Wrp_GetUnicode			Tcl_GetUnicode
#  define Wrp_GetUnicodeFromObj			Tcl_GetUnicodeFromObj
#  define Wrp_pInitForRuntimeConfig		uCoreClrFunctions.pInitForRuntimeConfig
#  define Wrp_pLoadAssemblyAndGetFuncPtr	uCoreClrFunctions.pLoadAssemblyAndGetFuncPtr
#  define Wrp_get_module_file_name		GetModuleFileNameW
#endif

#endif /* _GARUDA_STR_H_ */
