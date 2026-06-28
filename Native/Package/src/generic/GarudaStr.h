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
 * ============================================================================
 *
 * GarudaStr.h -- string-encoding conversion framework
 *
 * Why this file exists.
 *
 *   Garuda is a Tcl extension that bridges to .NET (the CLR / CoreCLR).
 *   Each side of the bridge has its own opinion about what a "wide
 *   character" is:
 *
 *     Tcl:                Tcl_UniChar = 16-bit (UTF-16) on every platform.
 *     Win32 wchar_t:      16-bit (UTF-16).
 *     POSIX wchar_t:      32-bit (UTF-32) on Linux / macOS / BSD.
 *     CLR (Win32):        wchar_t = 16-bit; runtime is UTF-16 native.
 *     CLR (CoreCLR/POSIX):the hostfxr / coreclr_delegates ABI uses
 *                         char* UTF-8 on POSIX (NOT wchar_t).
 *
 *   Garuda's caller-side convention is "WCHAR everywhere" -- every
 *   string-bearing function in the package takes LPWSTR / LPCWSTR.
 *   That is fine on Win32 (WCHAR == UTF-16 == what Tcl and the CLR want)
 *   but on POSIX every WCHAR (UTF-32) crossing a Tcl or CoreCLR boundary
 *   must be converted, in BOTH directions, with the right strictness
 *   semantics, and without leaking transient buffers under any error
 *   path.
 *
 *   This header is the conversion machinery.  The layered model is:
 *
 *     Layer 1: ConvertUTF_v2.c  (third-party, unmodified)
 *               UTF-{8,16,32} <-> {8,16,32} stream converters.
 *     Layer 2: GarudaStr.h      (this file)
 *               Macro-built bookkeeping wrappers around Layer 1 -- buffer
 *               sizing, ckalloc, error->HRESULT, ownership tracking,
 *               compile-time size assertions.  Used INTERNALLY by Layer 3.
 *     Layer 3: GarudaStr.c      (Cvt_* functions)
 *               Function-shaped wrappers that match the signature of a
 *               specific Tcl or CoreCLR API and hide the conversion
 *               between caller-WCHAR and target-encoding.  Each Cvt_*
 *               function is a thin sandwich of Layer-2 macros around
 *               one Tcl_* / CoreCLR call.
 *     Layer 4: Wrp_*            (defines at the bottom of this file)
 *               Build-time selection: on Win32 the Wrp_* names alias to
 *               the underlying Tcl_* / CoreCLR call directly (no Cvt_*
 *               trip); on POSIX they alias to the Cvt_* function.
 *               Callers in the rest of the package write Wrp_*; the
 *               right path is chosen at compile time.
 *
 * The cvt_exit: contract.
 *
 *   The Layer-2 conversion macros (cvt_*_to_*_body, etc.) do their
 *   bookkeeping inside a `do { ... } while (0)` block but ABANDON IT
 *   on any error via `goto cvt_exit`.  This means EVERY function that
 *   uses these macros MUST contain a label named exactly `cvt_exit:`,
 *   followed by cleanup code that:
 *
 *     - calls cvt_ctx_cleanup(ctx) on each conversion context, AND
 *     - frees any cvtBuf0 the function explicitly allocated, AND
 *     - returns whatever the function's natural return-value-on-error
 *       is (NULL, TCL_ERROR, E_FAIL, 0, etc).
 *
 *   This is unusual.  It is also non-negotiable: the macros encode a
 *   significant amount of control flow that would otherwise have to be
 *   duplicated at every call site.  If a future contributor wants to
 *   call a cvt_*_body macro from a function that does NOT have a
 *   cvt_exit: label, the build will fail with an undefined-label error
 *   at the goto.  That is intentional -- the macros are unsafe outside
 *   their cleanup contract.
 *
 * The owned flag.
 *
 *   Cvt_Context_UTF{8,16,32}.owned distinguishes two ownership cases
 *   on the same struct:
 *
 *     owned == 1:  pStart was attemptckalloc'd by the cvt_*_to_*_body
 *                   macro itself.  cvt_ctx_cleanup will ckfree it.
 *     owned == 0:  pStart points into externally-managed storage
 *                   (caller-supplied or stack-borrowed).  cvt_ctx_
 *                   cleanup will leave it alone.
 *
 *   The "transfer ownership" idiom (used by Cvt_GetUnicode*) sets
 *   owned=TRUE inside the conversion body, then steals the pointer
 *   (`unicode = pStart; pStart = NULL;`) before reaching cvt_exit:.
 *   The NULL-out makes cvt_ctx_cleanup a no-op, so the buffer
 *   survives function return as the caller's responsibility.
 *
 * HRESULT facility for ConvertUTF errors.
 *
 *   ConvertUTF returns its own enum (conversionOK, sourceExhausted,
 *   targetExhausted, sourceIllegal).  We map these into the HRESULT
 *   space using the customer-bit (FACILITY_CUSTOMER_BIT) plus a
 *   custom facility code (FACILITY_CVTUTF == 2004).  The HRESULT_FROM
 *   _CVTUTF macro produces a SEVERITY_ERROR HRESULT carrying the
 *   ConvertUTF return code in its low 16 bits, decodable later if a
 *   diagnostic routine wants to produce a specific message.  This
 *   design lets the entire package use one uniform error model
 *   (HRESULT) regardless of whether a failure originated in the CLR,
 *   Win32, or our own conversion layer.
 *
 * Compile-time size assertions.
 *
 *   The COMPILE_TIME_ASSERT block at the top encodes the platform
 *   invariants this file's design relies on.  Specifically: char==1,
 *   Tcl_UniChar==2, wchar_t==4, unsigned int==4 -- all on POSIX.  If a
 *   future platform breaks any of these (e.g. a tooling that uses
 *   2-byte wchar_t on Linux) the build fails at compile time rather
 *   than corrupting strings at runtime.
 *
 * Length math safety.
 *
 *   cvt_max_utf8 / cvt_max_utf16 / cvt_max_utf32 are SIZE_MAX divided
 *   by the unit size -- i.e. the largest count of units that fits in a
 *   size_t without overflowing on multiplication by sizeof(unit).  The
 *   conversion macros compare proposed allocation sizes against these
 *   limits BEFORE multiplying, so allocation sizes never wrap.
 *
 * Required headers (in calling translation units).
 *
 *   When using this header file, the following other headers are also
 *   (almost always) required:
 *
 *     #include <limits.h>
 *     #include <stddef.h>
 *     #include <string.h>
 *     #include <wchar.h>
 *     #include "ConvertUTF_v2.h"
 *
 * Strict conversion.
 *
 *   The Layer-2 macros pass strictConversion to the underlying
 *   ConvertUTF calls.  This rejects ill-formed sequences (lone
 *   surrogates, overlong UTF-8, codepoints above U+10FFFF) with a
 *   conversionResult code that becomes an HRESULT_FROM_CVTUTF
 *   failure.  We do not silently substitute U+FFFD; if a string
 *   isn't valid Unicode, the conversion fails loudly.  The caller's
 *   error-handling path treats this as a hard failure -- better than
 *   passing scrambled data to managed code that may then crash with
 *   a much less actionable diagnostic.
 *
 * ============================================================================
 */

/*
 * COMPILE_TIME_ASSERT(name, expr)
 *   Negative-array-size trick.  If `expr` is FALSE at compile time, the
 *   typedef declares an array of size -1, which is a hard compile error
 *   pointed-to by the typedef name (so the diagnostic identifies which
 *   invariant broke).  Standard C99 idiom that predates _Static_assert.
 */
#ifndef COMPILE_TIME_ASSERT
#  define COMPILE_TIME_ASSERT(name, expr) typedef char name[(expr) ? 1 : -1]
#endif

/*
 * The size invariants this file's design depends on (POSIX only -- Win32
 * never enters the conversion paths so the assertions are unnecessary
 * there).  If your platform breaks any of these the build fails here
 * rather than scrambling strings at runtime.
 */
#if !defined(_WIN32)
COMPILE_TIME_ASSERT(char_8bits_size_check, sizeof(char) == 1);
COMPILE_TIME_ASSERT(Tcl_UniChar_16bits_size_check, sizeof(Tcl_UniChar) == 2);
COMPILE_TIME_ASSERT(wchar_t_32bits_size_check, sizeof(wchar_t) == 4);
COMPILE_TIME_ASSERT(unsigned_int_32bits_size_check, sizeof(unsigned int) == 4);

COMPILE_TIME_ASSERT(UTF8_size_check, sizeof(UTF8) >= sizeof(char));
COMPILE_TIME_ASSERT(UTF16_size_check, sizeof(UTF16) >= sizeof(Tcl_UniChar));
COMPILE_TIME_ASSERT(UTF32_size_check, sizeof(UTF32) >= sizeof(wchar_t));
#endif

/*
 * HRESULT layout for ConvertUTF errors.
 *
 *   HRESULT bit layout:
 *     [31]      severity (1 = error)
 *     [30]      reserved
 *     [29]      customer bit (1 = our facility, NOT Microsoft's)
 *     [28]      reserved
 *     [27..16]  facility (12 bits)
 *     [15..0]   code
 *
 *   We pick FACILITY_CVTUTF = 2004 (arbitrary, no clash with documented
 *   Microsoft facilities) and combine it with FACILITY_CUSTOMER_BIT so
 *   the resulting HRESULT cannot collide with any system-defined one.
 *   The low 16 bits carry the ConvertUTF return code (conversionOK,
 *   sourceExhausted, targetExhausted, sourceIllegal) which a downstream
 *   diagnostic routine could decode for a precise error message.
 */
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

/*
 * Per-encoding overflow limits.
 *
 *   cvt_max_utf{8,16,32} = SIZE_MAX / sizeof(unit).  These are the
 *   largest possible unit-counts that can be passed to attemptckalloc
 *   without (count * sizeof(unit)) wrapping the size_t multiplication.
 *   The body macros compare the proposed length against these BEFORE
 *   multiplying, so allocation sizes never wrap.
 *
 *   The static-const definitions are guarded so that multiple
 *   inclusions of this header in one translation unit do not produce
 *   redefinition errors.
 */
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
 * cvt_declare_context_type(type)
 *
 *   Templated-by-macro context-struct generator.  Expanded for UTF8,
 *   UTF16, UTF32 below to produce three parallel struct types
 *   Cvt_Context_UTF8 / _UTF16 / _UTF32, each of which carries:
 *
 *     sizeOf        struct size (for forward-compat, like the rest of
 *                    Garuda's public structs).
 *     hResult       success / failure code; SUCCEEDED() / FAILED()
 *                    via the cvt_succeeded / cvt_failed macros below.
 *     owned         1 = ckalloc'd by the body macro and freed by
 *                    cvt_ctx_cleanup; 0 = caller-owned, leave alone.
 *     pStart        first byte of the converted output (or input).
 *     pCurrent      moving cursor used by ConvertUTF; on success,
 *                    `pCurrent - pStart` is the converted unit count.
 *     length0..5    six length fields used as scratch space for the
 *                    sizing math (see body macros).  Each step
 *                    promotes the previous value through one
 *                    transformation:
 *                      length0  caller-supplied count (or 0 = strlen)
 *                      length1  resolved count after strlen / wcslen
 *                      length2  byte-count of source/output (varies
 *                               by direction)
 *                      length3  + 1 for NUL terminator
 *                      length4  * sizeof(unit) for allocation
 *                      length5  final converted unit count (post-call)
 *                    The names are intentionally numeric -- they
 *                    are intermediate buffers, not user-facing
 *                    quantities.
 *
 *   Done as a macro rather than a generic struct so that the per-
 *   encoding `pStart` / `pCurrent` pointers carry their type
 *   information (UTF8* / UTF16* / UTF32*) -- no void* casts inside the
 *   body macros and the compiler catches any wrong-encoding usage.
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

/*
 * Per-function declaration helpers.
 *
 *   cvt_decls()       Declare cvtObj0 (transient Tcl_Obj* result) and
 *                      cvtBuf0 (transient UTF8* staging buffer).  Most
 *                      callers use one or both; harmless if unused.
 *   cvt_u{8,16,32}_decls(i)
 *                     Declare a numbered Cvt_Context_UTF{8,16,32}
 *                      named `cvtCtxN` (N = i).  Numbered so a single
 *                      function can have multiple conversion contexts
 *                      live at once (e.g. Cvt_pLoadAssemblyAndGetFunc
 *                      Ptr converts four arguments).
 *
 *   These all expand to plain declarations, so they MUST appear at
 *   the top of a function (C89-style scope) before any statements.
 */
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

/*
 * Status accessors.  Use cvt_succeeded / cvt_failed instead of
 * touching ctx.hResult directly so callers don't need to know which
 * SUCCEEDED/FAILED variant the package uses.
 */
#if !defined(cvt_succeeded)
#define cvt_succeeded(a)		(SUCCEEDED((a).hResult))
#endif

#if !defined(cvt_failed)
#define cvt_failed(a)			(FAILED((a).hResult))
#endif

/*
 * cvt_cleanup(p)
 *   ckfree-and-NULL pattern.  Used by the body macros to free
 *   intermediate buffers on error and by callers wanting to release
 *   externally-allocated cvtBuf0 etc.  Idempotent on a NULL pointer.
 *   The `p` local prevents double-evaluation of the argument
 *   expression.
 */
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

/*
 * cvt_ctx_initialize(v)
 *   Zero a context struct.  Most callers don't need this since
 *   cvt_u{8,16,32}_decls(i) zero-initializes via "= {0}" -- but the
 *   body macros call cvt_ctx_initialize when `allocate` is set so
 *   that contexts reused across iterations start fresh.
 */
#if !defined(cvt_ctx_initialize)
#define cvt_ctx_initialize(v)		memset(&(v), 0, sizeof((v)));
#endif

/*
 * cvt_ctx_cleanup(v)
 *   The cleanup-label workhorse.  Every cvt_exit: label calls this
 *   on each declared context.  Three responsibilities:
 *
 *     1. If the context recorded a failure, log it via TracePrintf so
 *        debug builds can see what went wrong.  This catches
 *        conversion failures that the function would otherwise swallow
 *        on its way to returning NULL/TCL_ERROR/etc.
 *     2. If owned == TRUE (we ckalloc'd the buffer), free it.
 *     3. NULL out pStart unconditionally so a stale pointer cannot
 *        leak into a later use of the same context (rare, but the
 *        defense is cheap).
 *
 *   Note: the trailing semicolon is OUTSIDE the do/while(0) so
 *   callers write `cvt_ctx_cleanup(ctx);` with the natural punctuation.
 *   The convention is consistent across all macros in this file.
 */
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

/*
 * cvt_<src>_to_<dst>_body(ctx, src, count, allocate)
 *
 *   The four conversion bodies (u8->u32, u16->u32, u32->u8, u32->u16) all
 *   share the same shape.  Read this comment once; the other three
 *   are minor variations on it.
 *
 *   Arguments:
 *     ctx       a Cvt_Context_<dst> declared via cvt_uN_decls(i).
 *     src       const <src>* input pointer.  May be NULL -- that's a
 *               legitimate "I have nothing to convert" case which
 *               sets ctx.hResult = E_POINTER and gotos cvt_exit.
 *     count     Number of input units; if 0, the macro auto-sizes
 *               via strlen / wcslen / Tcl_UniCharLen.
 *     allocate  Boolean.  TRUE means the macro will attemptckalloc
 *               ctx.pStart and set ctx.owned = TRUE so cvt_ctx_cleanup
 *               will free it.  FALSE means ctx.pStart was filled in
 *               by the caller (a stack buffer or similar) and must
 *               be left alone.
 *
 *   Sequence:
 *     1. Validate src; on NULL, drop a free of cvtBuf0 (if it was
 *        allocated for the externally-supplied path) and exit.
 *     2. length0..length5 cascade through the sizing math:
 *        length0 = caller count; length1 = resolved count;
 *        length2 = byte/unit count for output; length3 = +1 NUL;
 *        length4 = * sizeof(unit) for ckalloc; length5 = post-
 *        conversion unit count.  See cvt_declare_context_type's
 *        comment for the per-stage meaning.
 *     3. Overflow check on length3 vs cvt_max_<dst> -- if the math
 *        would wrap or exceed addressable space, fail with
 *        targetExhausted.
 *     4. attemptckalloc the destination buffer (when allocate),
 *        zero it, set ctx.pCurrent = ctx.pStart.
 *     5. ConvertUTF<src>to<dst> with strictConversion.  Failure
 *        encodes the conversionResult into HRESULT_FROM_CVTUTF.
 *     6. INT_MAX overflow check on length5 -- Tcl APIs take int
 *        lengths so we must ensure the converted unit count fits.
 *     7. On any error path: free the externally-allocated cvtBuf0
 *        if non-NULL (the body macro is responsible for this when
 *        `allocate` is FALSE); set ctx.hResult; goto cvt_exit.
 *
 *   The `cvtBuf0` cleanup repeated at every error site looks
 *   redundant but is correct: if the caller pre-allocated cvtBuf0
 *   (the !allocate case), the body macro is the one in scope to
 *   free it on failure, NOT the caller (the caller's cvt_exit:
 *   would not yet know about cvtBuf0's lifetime if the body fails
 *   mid-allocation).  Repetition rather than a helper macro
 *   keeps the failure handling visible inline.
 *
 *   On success, ctx.pStart points at the converted buffer with
 *   ctx.length5 valid units.  On failure, ctx.hResult names the
 *   problem and the buffer (if any) has been freed.  Either way
 *   cvt_ctx_cleanup at cvt_exit: handles the rest.
 */

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

/*
 * cvt_u16_to_u32_body
 *   UTF-16 -> UTF-32.  Shape identical to cvt_u8_to_u32_body except
 *   that auto-sizing (length0 == 0) calls Tcl_UniCharLen instead of
 *   strlen, and the per-element source size is 2 not 1.  Used by
 *   Cvt_GetUnicode / Cvt_GetUnicodeFromObj on POSIX to read a
 *   Tcl_Obj's UTF-16 storage and produce a fresh UTF-32 buffer.
 */

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

/*
 * cvt_u32_to_u8_body
 *   UTF-32 -> UTF-8.  Shape identical to cvt_u8_to_u32_body but the
 *   output side has variable per-codepoint byte width (1..4) so the
 *   length math at length2 multiplies length1 by UNI_UTF8_MAX_BYTES
 *   (4) -- worst case capacity.  Used by Cvt_pInitForRuntimeConfig
 *   and Cvt_pLoadAssemblyAndGetFuncPtr on POSIX, where hostfxr's
 *   ABI is char* UTF-8 even though our caller uses UTF-32 wchar_t.
 */

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

/*
 * cvt_u32_to_u16_body
 *   UTF-32 -> UTF-16.  Shape identical to cvt_u8_to_u32_body but the
 *   output side (UTF-16) needs at most 2 units per UTF-32 unit
 *   (surrogate pairs for supplementary-plane codepoints), so length2
 *   multiplies by 2.  Used by Cvt_NewUnicodeObj and Cvt_AppendUnicode
 *   ToObj on POSIX to feed Tcl_NewUnicodeObj/Tcl_AppendUnicodeToObj
 *   the UTF-16 input they require.
 *
 *   Note that this macro never throws E_OUTOFMEMORY-vs-E_POINTER like
 *   the others -- the !allocate branch on alloc failure here returns
 *   E_OUTOFMEMORY only.  Asymmetric with the other three; cosmetic
 *   only (no functional difference because the !allocate path is
 *   never used by callers of this specific macro).
 */

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

/*
 * cvt_copy_clamped_with_nul(written_out, dst, dst_capacity, src, src_len)
 *
 *   Bounded memmove with NUL terminator -- the canonical "copy this
 *   converted string into a caller-supplied fixed buffer" helper.
 *   `written_out` (lvalue) receives the number of units actually
 *   copied (NOT including NUL).  Truncates silently if src is longer
 *   than dst can hold, leaving exactly capacity-1 units copied plus
 *   the NUL.  If dst_capacity is 0, written_out=0 and nothing is
 *   touched.
 *
 *   memmove (not memcpy) is used because dst and src in some call
 *   chains can be derived from overlapping storage (rare but
 *   possible during the truncate-in-place pattern).  The cost vs
 *   memcpy is negligible.
 *
 * cvt_ctx_copy_clamped_with_nul(written_out, ctx, dst, dst_capacity)
 *
 *   Convenience over cvt_copy_clamped_with_nul that pulls src and
 *   src_len from a Cvt_Context_*'s pStart and length5.  Used by
 *   Cvt_get_module_file_name to deliver the converted module path
 *   into the caller's fixed-size fileName buffer.
 */

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

/*
 * Layer-4 selection: Wrp_* macros.
 *
 *   The Wrp_* names are what the rest of the package uses.  At
 *   compile time they alias either to the Cvt_* converting functions
 *   (POSIX + CoreCLR -- wchar_t mismatches the target ABI) or to the
 *   underlying Tcl_* / CoreCLR API directly (Win32 or non-CoreCLR --
 *   no conversion needed).
 *
 *   This indirection means:
 *     - Caller code writes Wrp_NewUnicodeObj(s, n) regardless of
 *       platform; the right path is chosen at compile time.
 *     - The Cvt_* functions and the cvt_*_body macros are entirely
 *       compiled out on Win32.
 *     - Adding a new Tcl_* / CoreCLR API to the bridge requires
 *       defining a Cvt_* wrapper, declaring it here, and adding both
 *       Wrp_* defines (POSIX-side and Win32-side).
 *
 *   The PACKAGE_INTERN visibility annotation marks these as internal
 *   to the package's DLL; they are not part of the public API.
 *
 *   The dnh_init_params typedef shortens the very-long
 *   hostfxr_initialize_parameters name used by the CoreCLR SDK
 *   header.  Local convenience only; no functional change.
 */
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
