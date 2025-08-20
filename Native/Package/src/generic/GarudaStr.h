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
 *          "done", which must call the cvt_cleanup() function (macro) on each
 *          converted UTF-X string lvalue, e.g. Cvt_GetUnicode*(), et al.
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

COMPILE_TIME_ASSERT(char_8bits_size_check, sizeof(char) == 1);
COMPILE_TIME_ASSERT(Tcl_UniChar_16bits_size_check, sizeof(Tcl_UniChar) == 2);
COMPILE_TIME_ASSERT(wchar_t_32bits_size_check, sizeof(wchar_t) == 4);

#if !defined(FACILITY_CUSTOMER_BIT)
#define FACILITY_CUSTOMER_BIT		(0x20000000)
#endif

#if !defined(FACILITY_CVTUTF)
#define FACILITY_CVTUTF			(2004)
#endif

#if !defined(FACILITY_CUSTOMER_CVTUTF)
#define FACILITY_CUSTOMER_CVTUTF \
			(((unsigned long)(FACILITY_CUSTOMER_BIT)) | \
			(((unsigned long)(FACILITY_CVTUTF)) << 16))
#endif

#if !defined(HRESULT_FROM_CVTUTF)
#define HRESULT_FROM_CVTUTF(x) \
		((HRESULT)((((unsigned long)(SEVERITY_ERROR)) << 31) | \
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
#define cvt_declare_context_type(name, type)	\
typedef struct name {				\
    size_t sizeOf;				\
    HRESULT hResult;				\
    type *pStart;				\
    type *pCurrent;				\
    size_t length0;				\
    size_t length1;				\
    size_t length2;				\
    size_t length3;				\
    size_t length4;				\
    size_t length5;				\
} name;
#endif

#if !defined(_CVT_CONTEXT_U8_DEFINED)
#define _CVT_CONTEXT_U8_DEFINED
cvt_declare_context_type(Cvt_Context_u8, UTF8);
#endif

#if !defined(_CVT_CONTEXT_U16_DEFINED)
#define _CVT_CONTEXT_U16_DEFINED
cvt_declare_context_type(Cvt_Context_u16, UTF16);
#endif

#if !defined(_CVT_CONTEXT_U32_DEFINED)
#define _CVT_CONTEXT_U32_DEFINED
cvt_declare_context_type(Cvt_Context_u32, UTF32);
#endif

#if !defined(cvt_decls)
#define cvt_decls()			char *cvtBuf0 = NULL;
#endif

#if !defined(cvt_u8_decls)
#define cvt_u8_decls(i)			Cvt_Context_u8 cvtCtx##i;
#endif

#if !defined(cvt_u16_decls)
#define cvt_u16_decls(i)		Cvt_Context_u16 cvtCtx##i;
#endif

#if !defined(cvt_u32_decls)
#define cvt_u32_decls(i)		Cvt_Context_u32 cvtCtx##i;
#endif

#if !defined(cvt_succeeded)
#define cvt_succeeded(a)		(SUCCEEDED((a).hResult))
#endif

#if !defined(cvt_cleanup)
#define cvt_cleanup(a) do {						\
    void *p = (a);							\
    if (p != NULL) {							\
	ckfree(p);							\
	(a) = p = NULL;							\
    }									\
} while(0);
#endif

#if !defined(cvt_ctx_initialize)
#define cvt_ctx_initialize(v)		memset(&(v), 0, sizeof((v)));
#endif

#if !defined(cvt_ctx_cleanup)
#define cvt_ctx_cleanup(v)		cvt_cleanup((v).pStart);
#endif

#if !defined(cvt_u8_to_u32_body)
#define cvt_u8_to_u32_body(a, b, c, d, e) do {				\
    ConversionResult crc;						\
    BOOL allocate = (e);						\
    const UTF8 *pSrc8 = (c);						\
    if (allocate) cvt_ctx_initialize((a));				\
    if (pSrc8 == NULL) {						\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = E_POINTER;					\
	cvt_ctx_cleanup((a));						\
	goto done;							\
    }									\
    (a).sizeOf = sizeof((a));						\
    (a).length0 = (d);							\
    (a).length1 = ((a).length0 > 0) ?					\
	(a).length0 : strlen((const char *)pSrc8);			\
    (a).length2 = (a).length1;						\
    (a).length3 = (a).length2 + 1;					\
    if (((a).length3 < (a).length1) ||					\
	((a).length3 >= cvt_max_utf32)) {				\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(targetExhausted);		\
	cvt_ctx_cleanup((a));						\
	(b) = NULL;							\
	goto done;							\
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
	cvt_ctx_cleanup((a));						\
	(b) = NULL;							\
	goto done;							\
    }									\
    if (allocate) {							\
	memset((a).pStart, 0, (a).length4);				\
    }									\
    (a).pCurrent = (a).pStart;						\
    crc = ConvertUTF8toUTF32(						\
	&pSrc8, pSrc8 + (a).length1, &((a).pCurrent),			\
	(a).pCurrent + (a).length2, strictConversion);			\
    if (crc != conversionOK) {						\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(crc);				\
	cvt_ctx_cleanup((a));						\
	(b) = NULL;							\
	goto done;							\
    }									\
    (a).length5 = (size_t)((a).pCurrent - (a).pStart);			\
    if ((a).length5 > (size_t)INT_MAX) {				\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(targetExhausted);		\
	cvt_ctx_cleanup((a));						\
	(b) = NULL;							\
	goto done;							\
    }									\
} while (0);
#endif

#if !defined(cvt_u16_to_u32_body)
#define cvt_u16_to_u32_body(a, b, c, d, e) do {				\
    ConversionResult crc;						\
    BOOL allocate = (e);						\
    const UTF16 *pSrc16 = (c);						\
    if (allocate) cvt_ctx_initialize((a));				\
    if (pSrc16 == NULL) {						\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = E_POINTER;					\
	cvt_ctx_cleanup((a));						\
	goto done;							\
    }									\
    (a).sizeOf = sizeof((a));						\
    (a).length0 = (d);							\
    (a).length1 = ((a).length0 > 0) ?					\
	(a).length0 : Tcl_UniCharLen((Tcl_UniStr)pSrc16);		\
    (a).length2 = (a).length1;						\
    (a).length3 = (a).length2 + 1;					\
    if (((a).length3 < (a).length1) ||					\
	((a).length3 >= cvt_max_utf32)) {				\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(targetExhausted);		\
	cvt_ctx_cleanup((a));						\
	(b) = NULL;							\
	goto done;							\
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
	cvt_ctx_cleanup((a));						\
	(b) = NULL;							\
	goto done;							\
    }									\
    if (allocate) {							\
	memset((a).pStart, 0, (a).length4);				\
    }									\
    (a).pCurrent = (a).pStart;						\
    crc = ConvertUTF16toUTF32(						\
	&pSrc16, pSrc16 + (a).length1, &((a).pCurrent),			\
	(a).pCurrent + (a).length2, strictConversion);			\
    if (crc != conversionOK) {						\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(crc);				\
	cvt_ctx_cleanup((a));						\
	(b) = NULL;							\
	goto done;							\
    }									\
    (a).length5 = (size_t)((a).pCurrent - (a).pStart);			\
    if ((a).length5 > (size_t)INT_MAX) {				\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(targetExhausted);		\
	cvt_ctx_cleanup((a));						\
	(b) = NULL;							\
	goto done;							\
    }									\
} while (0);
#endif

#if !defined(cvt_u32_to_u8_body)
#define cvt_u32_to_u8_body(a, b, c, d, e) do {				\
    ConversionResult crc;						\
    BOOL allocate = (e);						\
    const UTF32 *pSrc32 = (c);						\
    if (allocate) cvt_ctx_initialize((a));				\
    if (pSrc32 == NULL) {						\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = E_POINTER;					\
	cvt_ctx_cleanup((a));						\
	goto done;							\
    }									\
    (a).length0 = (d);							\
    (a).length1 = ((a).length0 > 0) ?					\
	(a).length0 : wcslen((const wchar_t *)pSrc32);			\
    (a).length2 = (a).length1 * UNI_UTF8_MAX_BYTES;			\
    (a).length3 = (a).length2 + 1;					\
    if (((a).length3 < (a).length1) ||					\
	((a).length3 >= cvt_max_utf8)) {				\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(targetExhausted);		\
	cvt_ctx_cleanup((a));						\
	(b) = NULL;							\
	goto done;							\
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
	cvt_ctx_cleanup((a));						\
	(b) = NULL;							\
	goto done;							\
    }									\
    if (allocate) {							\
	memset((a).pStart, 0, (a).length4);				\
    }									\
    (a).pCurrent = (a).pStart;						\
    crc = ConvertUTF32toUTF8(						\
	&pSrc32, pSrc32 + (a).length1, &((a).pCurrent),			\
	(a).pCurrent + (a).length2, strictConversion);			\
    if (crc != conversionOK) {						\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(crc);				\
	cvt_ctx_cleanup((a));						\
	(b) = NULL;							\
	goto done;							\
    }									\
    (a).length5 = (size_t)((a).pCurrent - (a).pStart);			\
    if ((a).length5 > (size_t)INT_MAX) {				\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(targetExhausted);		\
	cvt_ctx_cleanup((a));						\
	(b) = NULL;							\
	goto done;							\
    }									\
} while (0)
#endif

#if !defined(cvt_u32_to_u16_body)
#define cvt_u32_to_u16_body(a, b, c, d, e) do {				\
    ConversionResult crc;						\
    BOOL allocate = (e);						\
    const UTF32 *pSrc32 = (c);						\
    if (allocate) cvt_ctx_initialize((a));				\
    if (pSrc32 == NULL) {						\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = E_POINTER;					\
	cvt_ctx_cleanup((a));						\
	goto done;							\
    }									\
    (a).length0 = (d);							\
    (a).length1 = ((a).length0 > 0) ?					\
	(a).length0 : wcslen((const wchar_t *)pSrc32);			\
    (a).length2 = (a).length1 * 2;					\
    (a).length3 = (a).length2 + 1;					\
    if (((a).length3 < (a).length1) ||					\
	((a).length3 >= cvt_max_utf16)) {				\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(targetExhausted);		\
	cvt_ctx_cleanup((a));						\
	(b) = NULL;							\
	goto done;							\
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
	cvt_ctx_cleanup((a));						\
	(b) = NULL;							\
	goto done;							\
    }									\
    if (allocate) {							\
	memset((a).pStart, 0, (a).length4);				\
    }									\
    (a).pCurrent = (a).pStart;						\
    crc = ConvertUTF32toUTF16(						\
	&pSrc32, pSrc32 + (a).length1, &((a).pCurrent),			\
	(a).pCurrent + (a).length2, strictConversion);			\
    if (crc != conversionOK) {						\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(crc);				\
	cvt_ctx_cleanup((a));						\
	(b) = NULL;							\
	goto done;							\
    }									\
    (a).length5 = (size_t)((a).pCurrent - (a).pStart);			\
    if ((a).length5 > (size_t)INT_MAX) {				\
	if (!allocate && (cvtBuf0 != NULL)) {				\
	    ckfree((void *)cvtBuf0);					\
	    cvtBuf0 = NULL;						\
	}								\
	(a).hResult = HRESULT_FROM_CVTUTF(targetExhausted);		\
	cvt_ctx_cleanup((a));						\
	(b) = NULL;							\
	goto done;							\
    }									\
} while (0);
#endif

#if !defined(Cvt_NewUnicodeObj)
#define Cvt_NewUnicodeObj(a, b, c) do {					\
    cvt_decls();							\
    cvt_u16_decls(1);							\
    cvt_u32_to_u16_body(cvtCtx1, (a), (b), (c), 1);			\
    (a) = Tcl_NewUnicodeObj(cvtCtx1.pStart, (int)cvtCtx1.length5);	\
    cvt_ctx_cleanup(cvtCtx1);						\
} while(0);
#endif

#if !defined(Cvt_AppendUnicodeToObj)
#define Cvt_AppendUnicodeToObj(a, b, c) do {				\
    cvt_decls();							\
    cvt_u16_decls(1);							\
    cvt_u32_to_u16_body(cvtCtx1, cvtCtx1.pStart, (b), (c), 1);		\
    Tcl_AppendUnicodeToObj((a), cvtCtx1.pStart,				\
	(int)cvtCtx1.length5);						\
    cvt_ctx_cleanup(cvtCtx1);						\
} while(0);
#endif

#if !defined(Cvt_GetUnicode)
#define Cvt_GetUnicode(a, b, c) do {					\
    /*									\
     * HACK: Caller receives ownership of UTF-32 string pStart -AND-	\
     *       must call cvt_cleanup(a) when done with it, thus freeing	\
     *       its memory via ckfree(a).  It should be noted that "a" is	\
     *       required to be an lvalue of a compatible type and will be	\
     *       set to null by the cvt_cleanup(a) call.			\
     */									\
    cvt_decls();							\
    cvt_u32_decls(1);							\
    cvt_u16_to_u32_body(cvtCtx1, (a), Tcl_GetUnicode((b)), (c), 1);	\
    Tcl_SetByteArrayObj((b), (const unsigned char *)cvtCtx1.pStart,	\
	(int)cvtCtx1.length4);						\
    (a) = cvtCtx1.pStart;						\
} while(0);
#endif

#if !defined(Cvt_GetUnicodeFromObj)
#define Cvt_GetUnicodeFromObj(a, b, c) do {				\
    /*									\
     * HACK: Caller receives ownership of UTF-32 string pStart -AND-	\
     *       must call cvt_cleanup(a) when done with it, thus freeing	\
     *       its memory via ckfree(a).  It should be noted that "a" is	\
     *       required to be an lvalue of a compatible type and will be	\
     *       set to null by the cvt_cleanup(a) call.			\
     */									\
    cvt_decls();							\
    cvt_u32_decls(1);							\
    cvt_u16_to_u32_body(cvtCtx1, (a), Tcl_GetUnicode((b)), (*(c)), 1);	\
    Tcl_SetByteArrayObj((b), (const unsigned char *)cvtCtx1.pStart,	\
	(int)cvtCtx1.length4);						\
    (a) = cvtCtx1.pStart;						\
} while(0);
#endif

#if !defined(Cvt_setenv)
#define Cvt_setenv(a, b, c, d) do {					\
    cvt_decls();							\
    cvt_u8_decls(1);							\
    cvt_u8_decls(2);							\
    cvt_u32_to_u8_body(cvtCtx1, cvtCtx1.pStart, (b), 0, 1);		\
    cvt_u32_to_u8_body(cvtCtx2, cvtCtx2.pStart, (c), 0, 1);		\
    (a) = setenv((const char *)cvtCtx1.pStart,				\
	(const char *)cvtCtx2.pStart, (d));				\
    cvt_ctx_cleanup(cvtCtx2);						\
    cvt_ctx_cleanup(cvtCtx1);						\
} while(0);
#endif

#if !defined(Cvt_unsetenv)
#define Cvt_unsetenv(a, b) do {						\
    cvt_decls();							\
    cvt_u8_decls(1);							\
    cvt_u32_to_u8_body(cvtCtx1, cvtCtx1.pStart, (b), 0, 1);		\
    (a) = unsetenv((const char *)cvtCtx1.pStart);			\
    cvt_ctx_cleanup(cvtCtx1);						\
} while(0);
#endif

#if !defined(Cvt_dlopen)
#define Cvt_dlopen(a, b, c) do {					\
    cvt_decls();							\
    cvt_u8_decls(1);							\
    cvt_u32_to_u8_body(cvtCtx1, cvtCtx1.pStart, (b), 0, 1);		\
    (a) = dlopen((const char *)cvtCtx1.pStart, (c));			\
    cvt_ctx_cleanup(cvtCtx1);						\
} while(0);
#endif

#if !defined(Cvt_pInitForRuntimeConfig)
#define Cvt_pInitForRuntimeConfig(a, b, c, d) do {			\
    cvt_decls();							\
    cvt_u8_decls(1);							\
    cvt_u32_to_u8_body(cvtCtx1, cvtCtx1.pStart, (b), 0, 1);		\
    (a) = uFunctions.pInitForRuntimeConfig(				\
	(const char *)cvtCtx1.pStart, (c), (d));			\
    cvt_ctx_cleanup(cvtCtx1);						\
} while(0);
#endif

#if !defined(Cvt_pLoadAssemblyAndGetFuncPtr)
#define Cvt_pLoadAssemblyAndGetFuncPtr(a, b, c, d, e, f, g) do {	\
    cvt_decls();							\
    cvt_u8_decls(1);							\
    cvt_u8_decls(2);							\
    cvt_u8_decls(3);							\
    cvt_u8_decls(4);							\
    cvt_u32_to_u8_body(cvtCtx1, cvtCtx1.pStart, (b), 0, 1);		\
    cvt_u32_to_u8_body(cvtCtx2, cvtCtx2.pStart, (c), 0, 1);		\
    cvt_u32_to_u8_body(cvtCtx3, cvtCtx3.pStart, (d), 0, 1);		\
    cvt_u32_to_u8_body(cvtCtx4, cvtCtx4.pStart, (e), 0, 1);		\
    (a) = uCoreClrFunctions.pLoadAssemblyAndGetFuncPtr(			\
	cvtCtx1.pStart, cvtCtx2.pStart, cvtCtx3.pStart,			\
	cvtCtx4.pStart, (f), (g));					\
    cvt_ctx_cleanup(cvtCtx4);						\
    cvt_ctx_cleanup(cvtCtx3);						\
    cvt_ctx_cleanup(cvtCtx2);						\
    cvt_ctx_cleanup(cvtCtx1);						\
} while(0);
#endif

#if !defined(Cvt_get_module_file_name)
#define Cvt_get_module_file_name(a, b, c, d) do {			\
    cvt_decls();							\
    cvt_u32_decls(1);							\
    cvt_ctx_initialize(cvtCtx1);					\
    cvtBuf0 = (UTF8 *)attemptckalloc((d) + 1);				\
    if (cvtBuf0 == NULL) {						\
	(a) = 0;							\
	goto done;							\
    }									\
    memset(cvtBuf0, 0, ((d) + 1) * sizeof(UTF8));			\
    (a) = get_module_file_name((b), cvtBuf0, (d));			\
    cvtCtx1.pStart = (c);						\
    cvt_u8_to_u32_body(cvtCtx1, cvtCtx1.pStart, cvtBuf0, 0, 0);		\
    ckfree((void *)cvtBuf0);						\
} while(0);
#endif

#endif /* _GARUDA_STR_H_ */
