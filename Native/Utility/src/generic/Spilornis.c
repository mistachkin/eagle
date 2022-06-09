/*
 * Spilornis.c -- Eagle Native Utility Library (Spilornis)
 *
 * Copyright (c) 1987-1994 The Regents of the University of California.
 * Copyright (c) 1993-1996 Lucent Technologies.
 * Copyright (c) 1994-1998 Sun Microsystems, Inc.
 * Copyright (c) 1998-2000 Scriptics Corporation.
 * Copyright (c) 1998-2000 Ajuba Solutions.
 * Copyright (c) 2001-2002 by Kevin B. Kenny.  All rights reserved.
 * Contributions from Don Porter, NIST, 2002. (not subject to US copyright)
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * The vast majority of the source code in this file was copied from the Tcl
 * version 8.4 source code files "generic/tcl.h", "generic/tclParse.c", and
 * "generic/tclUtil.c" (which are covered by the terms of the Tcl license),
 * and then heavily modified.  All modifications are hereby released under
 * those same license terms (i.e. the terms of the Tcl license).
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#include <assert.h>		/* NOTE: For assert, etc. */

#if defined(HAVE_MALLOC_H)
#include <malloc.h>		/* NOTE: For _msize, malloc_usable_size, etc. */
#endif

#include <stdarg.h>		/* NOTE: For va_list, etc. */
#include <stdio.h>		/* NOTE: For fprintf, swprintf, etc. */
#include <stdlib.h>		/* NOTE: For getenv, calloc, free, etc. */
#include <string.h>		/* NOTE: For memset, etc. */
#include <limits.h>		/* NOTE: For USHRT_MAX, etc. */
#include <wchar.h>		/* NOTE: For wcslen, swprintf, wcsncpy, etc. */

#include "pkgVersion.h"		/* NOTE: Package version information. */
#include "rcVersion.h"		/* NOTE: Resource version information. */
#include "SpilornisInt.h"	/* NOTE: For private package API. */
#include "Spilornis.h"		/* NOTE: For public package API. */

/*
 * The following macros are used to check if a character is a space, a
 * decimal digit, or a hexadecimal digit.  For maximum portability, we
 * undefine them, if they exist, before defining them to point to our
 * local functions.
 */

#ifdef iswspace
#  undef iswspace
#endif

#ifdef iswbdigit
#  undef iswbdigit
#endif

#ifdef iswodigit
#  undef iswodigit
#endif

#ifdef iswdigit
#  undef iswdigit
#endif

#ifdef iswxdigit
#  undef iswxdigit
#endif

#define iswspace				EagleIsSpace
#define iswbdigit				EagleIsBinDigit
#define iswodigit				EagleIsOctDigit
#define iswdigit				EagleIsDecDigit
#define iswxdigit				EagleIsHexDigit

/*
 * The following values are used in the flags returned by
 * the EagleScanCountedElement function and used by the
 * EagleConvertCountedElement function.
 *
 * EAGLE_DONT_USE_BRACES -	1 means the string mustn't be enclosed in
 *				braces (e.g. it contains unmatched braces,
 *				or ends in a backslash character, or user
 *				just doesn't want braces);  handle all
 *				special characters by adding backslashes.
 * EAGLE_USE_BRACES -		1 means the string contains a special
 *				character that can be handled simply by
 *				enclosing the entire argument in braces.
 * EAGLE_BRACES_UNMATCHED -	1 means that braces aren't properly matched
 *				in the argument.
 * EAGLE_DONT_QUOTE_HASH -	1 means the caller insists that a leading hash
 * 				character ('#') should *not* be quoted. This
 * 				is appropriate when the caller can guarantee
 * 				the element is not the first element of a
 * 				list, so [eval] cannot mis-parse the element
 * 				as a comment.
 */

#define EAGLE_DONT_USE_BRACES			(1)
#define EAGLE_USE_BRACES			(2)
#define EAGLE_BRACES_UNMATCHED			(4)
#define EAGLE_DONT_QUOTE_HASH			(8)

/*
 * NOTE: Attempt to determine if the "wchar_t" data type is greater than
 *       what the CLR and Mono, et al, can handle.  Both the CLR and the
 *       Mono runtime only support two byte characters in their P/Invoke
 *       marshalling subsystems.  Unfortunately, it appears that various
 *       compiler runtimes on non-Windows platforms (e.g. gcc on Linux)
 *       define "wchar_t" data type to be four bytes.  Most functions in
 *       this file cannot adapt to this difference; however, the version
 *       introspection entry point (i.e. "Eagle_GetVersion") can, since
 *       it is limited to output only.  This helps to enable the managed
 *       integration code to detect that the library is not suitable for
 *       use on those platforms (i.e. by detecting the SIZE_OF_WCHAR_T=4
 *       datum in the returned version string).
 */

#if defined(WCHAR_MAX) && defined(USHRT_MAX) && (WCHAR_MAX > USHRT_MAX)
#define USE_WCHARSTRTOUSHORTSTR			1
#endif

/*
 * NOTE: Attempt to determine if we can use the Win32 specific stuff that
 *       we need.  If not, alternative stuff will be used.
 */

#if !defined(USE_SYSSTRINGLEN) && defined(_MSC_VER) && defined(_WIN32) && \
    (defined(_M_IX86) || defined(_M_IA64) || defined(_M_X64) || \
     defined(_M_ARM) || defined(_M_ARM64))
#define USE_SYSSTRINGLEN			1
#endif

/*
 * NOTE: Win32 API functions required by this file.  These functions are
 *       declared inline rather than simply including "windows.h" because
 *       that would bring in a ton of unrelated stuff that is completely
 *       unnecessary here.  Also, the "standard" Win32 type definitions
 *       would conflict with those already defined by this project.
 */

#if defined(_WIN32)
extern __declspec(dllimport) DWORD __stdcall GetEnvironmentVariableW(
			    LPCWSTR, LPWSTR, DWORD);

extern __declspec(dllimport) VOID __stdcall OutputDebugStringA(LPCSTR);
#endif

#if defined(_WIN32) && defined(USE_SYSSTRINGLEN) && USE_SYSSTRINGLEN
extern __declspec(dllimport) UINT __stdcall SysStringLen(BSTR);
#endif

/*
 * NOTE: Define a macro that makes [optionally] calling the SysStringLen
 *       Win32 API from within the Eagle_JoinList function easier.
 */

#if defined(_WIN32) && defined(USE_SYSSTRINGLEN) && USE_SYSSTRINGLEN
#define GetStringLen(i)			SysStringLen((BSTR)ppElements[i])
#else
#define GetStringLen(i)			pElementLengths[i]
#endif

/*
 * NOTE: Private functions defined in this file.
 */
#if defined(USE_WCHARSTRTOUSHORTSTR) && USE_WCHARSTRTOUSHORTSTR
static SIZE_T EagleWcharStrToUshortStr(LPWSTR wstr);
#endif

static INT EagleTracePrintf(LPCSTR format, ...);
static BOOL EagleIsSpace(WCHAR c);
static BOOL EagleIsBinDigit(WCHAR c);
static BOOL EagleIsOctDigit(WCHAR c);
static BOOL EagleIsDecDigit(WCHAR c);
static BOOL EagleIsHexDigit(WCHAR c);
static LPCWSTR EaglePrintf(SIZE_T length, LPCWSTR format, ...);
static SIZE_T EagleParseBin(LPCWSTR src, SIZE_T numChars,
			    LPUCSCHAR resultPtr);
static SIZE_T EagleParseOct(LPCWSTR src, SIZE_T numChars,
			    LPUCSCHAR resultPtr);
static SIZE_T EagleParseDec(LPCWSTR src, SIZE_T numChars,
			    LPUCSCHAR resultPtr);
static SIZE_T EagleParseHex(LPCWSTR src, SIZE_T numChars,
			    LPUCSCHAR resultPtr);
static SIZE_T EagleParseBackslash(LPCWSTR src, SIZE_T numChars,
			    SIZE_T *readPtr, LPWSTR dst);
static SIZE_T EagleCopyAndCollapse(SIZE_T count, LPCWSTR src, LPWSTR dst);
static SIZE_T EagleScanCountedElement(LPCWSTR string, SIZE_T length,
			    FLAGS *flagPtr);
static SIZE_T EagleConvertCountedElement(LPCWSTR src, SIZE_T length,
			    LPWSTR dst, FLAGS flags);
static RETURNCODE EagleFindElement(LPCWSTR list, SIZE_T listLength,
			    LPCWSTR *elementPtr, LPCWSTR *nextPtr,
			    SIZE_T *sizePtr, LPBOOL bracePtr,
			    LPCWSTR *errorPtr);

#if defined(USE_WCHARSTRTOUSHORTSTR) && USE_WCHARSTRTOUSHORTSTR
/*
 *----------------------------------------------------------------------
 *
 * EagleWcharStrToUshortStr --
 *
 *	This function modifies a WCHAR based string to force the use
 *	of two byte characters.  This is only necessary on platforms
 *	where the wchar_t type is four bytes in size.  The string is
 *	modified in-place.
 *
 * Results:
 *	The number of characters converted.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static SIZE_T
EagleWcharStrToUshortStr(
    LPWSTR wstr)		/* The string to convert. */
{
    if (wstr != NULL) {
	SIZE_T i = 0;
	LPUSHORT sstr = (LPUSHORT)wstr;
	assert(sizeof(WCHAR) == 4);
	assert(sizeof(USHORT) == 2);
	while (1) {
	    assert(wstr[i] >= 0);
	    assert(wstr[i] <= USHRT_MAX);
	    sstr[i] = (USHORT)wstr[i];
	    if (sstr[i] == 0) break;
	    i++;
	}
	return i + 1;
    }
    return 0;
}
#endif

/*
 *----------------------------------------------------------------------
 *
 * EagleTracePrintf --
 *
 *	This function sends a printf-style formatted trace message to
 *	the connected debugger (Win32), if any -OR- to the standard
 *	error channel.
 *
 * Results:
 *	The number of characters written (which may be zero), -1 if
 *	the trace output was truncated, or -2 if trace output was
 *	disabled.
 *
 * Side effects:
 *	None.
 *
 *----------------------------------------------------------------------
 */

static INT
EagleTracePrintf(
    LPCSTR format,		/* The "printf-style" format string. */
    ...)			/* The extra arguments, if any. */
{
    va_list argList;
    CHAR buffer[LIBRARY_TRACE_BUFFER_LENGTH + 1];
    INT result;

#if defined(_WIN32)
    {
	WCHAR envBuffer[LIBRARY_VAR_BUFFER_LENGTH + 1];
	memset(envBuffer, 0, sizeof(envBuffer));
	if (GetEnvironmentVariableW( /* NON-PORTABLE */
		NO_TRACE_UNICODE_VAR_NAME, envBuffer,
		LIBRARY_VAR_BUFFER_LENGTH)) {
	    return -2; /* DISABLED */
	}
    }
#else
    if (getenv(NO_TRACE_VAR_NAME) != NULL) { /* POSIX, MSVC */
	return -2; /* DISABLED */
    }
#endif

    va_start(argList, format);

    memset(buffer, 0, sizeof(buffer));

    result = vsnprintf(buffer,
	LIBRARY_TRACE_BUFFER_LENGTH, format, argList);

    va_end(argList);

#if defined(_WIN32)
    OutputDebugStringA(buffer); /* NON-PORTABLE */
#else
    fprintf(stderr, "%s", buffer);
#endif

    return result;
}

/*
 *---------------------------------------------------------------------------
 *
 * EagleIsSpace --
 *
 *	Checks if the specified character is whitespace, i.e. either a space
 *	(0x20), a horizontal tab (0x09), a line feed (0x0a), a vertical tab
 *	(0x0b), a form feed (0x0c), or a carriage return (0x0d).
 *
 * Results:
 *	Non-zero if the specified character is whitespace, zero otherwise.
 *
 *---------------------------------------------------------------------------
 */

static BOOL
EagleIsSpace(
    WCHAR c)		/* The character to check. */
{
    switch (c) {
	case L' ':
	case L'\f':
	case L'\n':
	case L'\r':
	case L'\t':
	case L'\v':
	    return TRUE;
    }
    return FALSE;
}

/*
 *---------------------------------------------------------------------------
 *
 * EagleIsBinDigit --
 *
 *	Checks if the specified character is a binary digit.
 *
 * Results:
 *	Non-zero if the specified character is a binary digit, zero
 *	otherwise.
 *
 * Notes:
 *	Relies on the following properties of the ASCII character set, with
 *	which UTF-8/UTF-16 are compatible:
 *
 *	The digits '0' .. '9' and the letters 'A' .. 'Z' and 'a' .. 'z'
 *	occupy consecutive code points, and '0' < 'A' < 'a'.
 *
 *---------------------------------------------------------------------------
 */

static BOOL
EagleIsBinDigit(
    WCHAR c)		/* The character to check. */
{
    return ((c >= L'0') && (c <= L'1'));
}

/*
 *---------------------------------------------------------------------------
 *
 * EagleIsOctDigit --
 *
 *	Checks if the specified character is an octal digit.
 *
 * Results:
 *	Non-zero if the specified character is an octal digit, zero
 *	otherwise.
 *
 * Notes:
 *	Relies on the following properties of the ASCII character set, with
 *	which UTF-8/UTF-16 are compatible:
 *
 *	The digits '0' .. '9' and the letters 'A' .. 'Z' and 'a' .. 'z'
 *	occupy consecutive code points, and '0' < 'A' < 'a'.
 *
 *---------------------------------------------------------------------------
 */

static BOOL
EagleIsOctDigit(
    WCHAR c)		/* The character to check. */
{
    return ((c >= L'0') && (c <= L'7'));
}

/*
 *---------------------------------------------------------------------------
 *
 * EagleIsDecDigit --
 *
 *	Checks if the specified character is a decimal digit.
 *
 * Results:
 *	Non-zero if the specified character is a decimal digit, zero
 *	otherwise.
 *
 * Notes:
 *	Relies on the following properties of the ASCII character set, with
 *	which UTF-8/UTF-16 are compatible:
 *
 *	The digits '0' .. '9' and the letters 'A' .. 'Z' and 'a' .. 'z'
 *	occupy consecutive code points, and '0' < 'A' < 'a'.
 *
 *---------------------------------------------------------------------------
 */

static BOOL
EagleIsDecDigit(
    WCHAR c)		/* The character to check. */
{
    return ((c >= L'0') && (c <= L'9'));
}

/*
 *---------------------------------------------------------------------------
 *
 * EagleIsHexDigit --
 *
 *	Checks if the specified character is a hexadecimal digit.
 *
 * Results:
 *	Non-zero if the specified character is a hexadecimal digit, zero
 *	otherwise.
 *
 * Notes:
 *	Relies on the following properties of the ASCII character set, with
 *	which UTF-8/UTF-16 are compatible:
 *
 *	The digits '0' .. '9' and the letters 'A' .. 'Z' and 'a' .. 'z'
 *	occupy consecutive code points, and '0' < 'A' < 'a'.
 *
 *---------------------------------------------------------------------------
 */

static BOOL
EagleIsHexDigit(
    WCHAR c)		/* The character to check. */
{
    return (((c >= L'0') && (c <= L'9')) || ((c >= L'A') && (c <= L'F')) ||
	((c >= L'a') && (c <= L'f')));
}

/*
 *---------------------------------------------------------------------------
 *
 * EaglePrintf --
 *
 *	Prints a formatted string into a new memory buffer obtained from
 *	the Eagle_AllocateMemory function.
 *
 * Results:
 *	The newly allocated memory buffer containing the formatted string
 *	-OR- NULL if the necessary memory could not be obtained.
 *
 *---------------------------------------------------------------------------
 */

static LPCWSTR
EaglePrintf(
    SIZE_T length,	/* The estimated length of the final string -OR-
			 * zero if the default length should be used. */
    LPCWSTR format,	/* The format string for vswprintf(). */
    ...)		/* The optional list of arguments containing the
			 * various pieces of data to insert into the result
			 * string, if any. */
{
    SIZE_T size;
    LPWSTR z;
    va_list ap;

    assert(length >= 0);
    assert(format != NULL);

    if (length == 0) length = LIBRARY_RESULT_LENGTH;
    size = (length + 1 /* NUL */) * sizeof(WCHAR);

    assert(size > 0);
    assert(size <= LIBRARY_MAXIMUM_SIZE_T);
    z = Eagle_AllocateMemory(size);
    if (z == NULL) return NULL;

    va_start(ap, format);
    vswprintf(z, size, format, ap);
    va_end(ap);

    return z;
}

/*
 *---------------------------------------------------------------------------
 *
 * EagleParseBin --
 *
 *	Scans a binary number as a character value (e.g., for parsing
 *	\B escape sequences).  At most numChars characters are scanned.
 *
 * Results:
 *	The numeric value is stored in dst.  Returns the number of
 *	characters consumed.
 *
 * Notes:
 *	Relies on the following properties of the ASCII character set, with
 *	which UTF-8/UTF-16 are compatible:
 *
 *	The digits '0' .. '9' and the letters 'A' .. 'Z' and 'a' .. 'z'
 *	occupy consecutive code points, and '0' < 'A' < 'a'.
 *
 *---------------------------------------------------------------------------
 */

static SIZE_T
EagleParseBin(
    LPCWSTR src,		/* First character to parse. */
    SIZE_T numChars,		/* Max number of characters to scan */
    LPUCSCHAR dst)		/* Points to storage provided by
				 * caller where the UCS-2 character
				 * resulting from the conversion is
				 * to be written. */
{
    UCSCHAR result = 0;
    LPCWSTR p = src;

    assert(src != NULL);
    assert(dst != NULL);

    while (numChars--) {
	WCHAR digit = *p;

	if (!iswbdigit(digit))
	    break;

	p++;

	result <<= 1;
	result |= (digit - L'0');
    }

    *dst = result;
    return (SIZE_T)(p - src);
}

/*
 *---------------------------------------------------------------------------
 *
 * EagleParseOct --
 *
 *	Scans an octal number as a character value (e.g., for parsing
 *	\o escape sequences).  At most numChars characters are scanned.
 *
 * Results:
 *	The numeric value is stored in dst.  Returns the number of
 *	characters consumed.
 *
 * Notes:
 *	Relies on the following properties of the ASCII character set, with
 *	which UTF-8/UTF-16 are compatible:
 *
 *	The digits '0' .. '9' and the letters 'A' .. 'Z' and 'a' .. 'z'
 *	occupy consecutive code points, and '0' < 'A' < 'a'.
 *
 *---------------------------------------------------------------------------
 */

static SIZE_T
EagleParseOct(
    LPCWSTR src,		/* First character to parse. */
    SIZE_T numChars,		/* Max number of characters to scan */
    LPUCSCHAR dst)		/* Points to storage provided by
				 * caller where the UCS-2 character
				 * resulting from the conversion is
				 * to be written. */
{
    UCSCHAR result = 0;
    LPCWSTR p = src;

    assert(src != NULL);
    assert(dst != NULL);

    while (numChars--) {
	WCHAR digit = *p;

	if (!iswodigit(digit))
	    break;

	p++;

	result <<= 3;
	result |= (digit - L'0');
    }

    *dst = result;
    return (SIZE_T)(p - src);
}

/*
 *---------------------------------------------------------------------------
 *
 * EagleParseDec --
 *
 *	Scans an decimal number as a character value (e.g., for parsing
 *	\d escape sequences).  At most numChars characters are scanned.
 *
 * Results:
 *	The numeric value is stored in dst.  Returns the number of
 *	characters consumed.
 *
 * Notes:
 *	Relies on the following properties of the ASCII character set, with
 *	which UTF-8/UTF-16 are compatible:
 *
 *	The digits '0' .. '9' and the letters 'A' .. 'Z' and 'a' .. 'z'
 *	occupy consecutive code points, and '0' < 'A' < 'a'.
 *
 *---------------------------------------------------------------------------
 */

static SIZE_T
EagleParseDec(
    LPCWSTR src,		/* First character to parse. */
    SIZE_T numChars,		/* Max number of characters to scan */
    LPUCSCHAR dst)		/* Points to storage provided by
				 * caller where the UCS-2 character
				 * resulting from the conversion is
				 * to be written. */
{
    UCSCHAR result = 0;
    LPCWSTR p = src;

    assert(src != NULL);
    assert(dst != NULL);

    while (numChars--) {
	WCHAR digit = *p;

	if (!iswdigit(digit))
	    break;

	p++;

	result *= 10;
	result += (digit - L'0');
    }

    *dst = result;
    return (SIZE_T)(p - src);
}

/*
 *---------------------------------------------------------------------------
 *
 * EagleParseHex --
 *
 *	Scans a hexadecimal number as a character value (e.g., for parsing
 *	\x, \X, \u, and \U escape sequences).  At most numChars characters
 *	are scanned.
 *
 * Results:
 *	The numeric value is stored in dst.  Returns the number of
 *	characters consumed.
 *
 * Notes:
 *	Relies on the following properties of the ASCII character set, with
 *	which UTF-8/UTF-16 are compatible:
 *
 *	The digits '0' .. '9' and the letters 'A' .. 'Z' and 'a' .. 'z'
 *	occupy consecutive code points, and '0' < 'A' < 'a'.
 *
 *---------------------------------------------------------------------------
 */

static SIZE_T
EagleParseHex(
    LPCWSTR src,		/* First character to parse. */
    SIZE_T numChars,		/* Max number of characters to scan */
    LPUCSCHAR dst)		/* Points to storage provided by
				 * caller where the UCS-2 character
				 * resulting from the conversion is
				 * to be written. */
{
    UCSCHAR result = 0;
    LPCWSTR p = src;

    assert(src != NULL);
    assert(dst != NULL);

    while (numChars--) {
	WCHAR digit = *p;

	if (!iswxdigit(digit))
	    break;

	p++;
	result <<= 4;

	if (digit >= L'a') {
	    result |= (10 + digit - L'a');
	} else if (digit >= L'A') {
	    result |= (10 + digit - L'A');
	} else {
	    result |= (digit - L'0');
	}
    }

    *dst = result;
    return (SIZE_T)(p - src);
}

/*
 *---------------------------------------------------------------------------
 *
 * EagleParseBackslash --
 *
 *	Scans up to numChars characters starting at src, consuming a
 *	backslash sequence as defined by Tcl's parsing rules.
 *
 * Results:
 *	Records at readPtr the number of bytes making up the backslash
 *	sequence.  Records at dst the UCS-2 encoded equivalent of that
 *	backslash sequence.  Returns the number of "characters" written to
 *	dst, at most 2.  Either readPtr or dst may be NULL, if the results
 *	are not needed, but the return value is the same either way.
 *
 * Side effects:
 * 	None.
 *
 *---------------------------------------------------------------------------
 */

static SIZE_T
EagleParseBackslash(
    LPCWSTR src,	/* Points to the backslash character of a
			 * a backslash sequence */
    SIZE_T numChars,	/* Max number of characters to scan */
    SIZE_T *readPtr,	/* NULL, or points to storage where the
			 * number of characters scanned should be
			 * written. */
    LPWSTR dst)		/* NULL, or points to buffer where the UTF-16
			 * encoding of the backslash sequence is to be
			 * written. */
{
    LPCWSTR p = (src + 1);
    UCSCHAR result;
    SIZE_T count;
    BYTE buf[sizeof(UCSCHAR)];

    assert(sizeof(WCHAR) == sizeof(unsigned short));
    assert(sizeof(UCSCHAR) >= (2 * sizeof(WCHAR)));
    assert(src != NULL);
    assert(numChars >= 0);

    /*
     * If no characters can be consumed, do nothing and return zero.
     */

    if (numChars == 0) {
	if (readPtr != NULL) {
	    *readPtr = 0;
	}
	return 0;
    }

    if (dst == NULL) {
	memset(buf, 0, sizeof(buf));
	dst = (LPWSTR)buf;
    }

    if (numChars == 1) {
	/* Can only scan the backslash.  Return it. */
	result = L'\\';
	count = 1;
	goto done;
    }

    count = 2;
    switch (*p) {
	case L'\0':
	    result = L'\\';
	    count = 1;
	    break;
	case L'a':
	    result = L'\a';
	    break;
	case L'b':
	    result = L'\b';
	    break;
	case L'f':
	    result = L'\f';
	    break;
	case L'n':
	    result = L'\n';
	    break;
	case L'r':
	    result = L'\r';
	    break;
	case L't':
	    result = L'\t';
	    break;
	case L'v':
	    result = L'\v';
	    break;
	case L'\\': /* COMPAT: Eagle only. */
	    result = L'\\';
	    break;
	case L'B':  /* COMPAT: Eagle only. */
	    count += EagleParseBin(p + 1, numChars - 2, &result);
	    if (count == 2) {
		/* No digits -> This is just "B". */
		result = L'B';
	    } else {
		/* Keep only the last byte (8 bin digits) */
		result = (BYTE)result;
	    }
	    break;
	case L'o': /* COMPAT: Eagle only. */
	    count += EagleParseOct(p + 1, numChars - 2, &result);
	    if (count == 2) {
		/* No digits -> This is just "o". */
		result = L'o';
	    } else {
		/* Keep only the last byte (3 oct digits) */
		result = (BYTE)result;
	    }
	    break;
	case L'd': /* COMPAT: Eagle only. */
	    count += EagleParseDec(p + 1, numChars - 2, &result);
	    if (count == 2) {
		/* No digits -> This is just "d". */
		result = L'd';
	    } else {
		/* Keep only the last byte (~3 dec digits) */
		result = (BYTE)result;
	    }
	    break;
	case L'x':
	    count += EagleParseHex(p + 1, numChars - 2, &result);
	    if (count == 2) {
		/* No digits -> This is just "x". */
		result = L'x';
	    } else {
		/* Keep only the last byte (2 hex digits) */
		result = (BYTE)result;
	    }
	    break;
	case L'X': /* COMPAT: Eagle only. */
	    count += EagleParseHex(p + 1, numChars - 2, &result);
	    if (count == 2) {
		/* No digits -> This is just "X". */
		result = L'X';
	    }
	    break;
	case L'u':
	    count += EagleParseHex(
		    p + 1, (numChars > 5) ? 4 : numChars - 2, &result);
	    if (count == 2) {
		/* No digits -> This is just "u". */
		result = L'u';
	    }
	    break;
	case L'U': /* COMPAT: Tcl only. */
	    count += EagleParseHex(
		    p + 1, (numChars > 9) ? 8 : numChars - 2, &result);
	    if (count == 2) {
		/* No digits -> This is just "U". */
		result = L'U';
	    }
	    break;
	case L'\n':
	    count--;
	    do {
		p++; count++;
	    } while ((count < numChars) && ((*p == L' ') || (*p == L'\t')));
	    result = L' ';
	    break;
	default:
	    /*
	     * Check for an octal number \oo?o?
	     */
	    if (iswdigit(*p) && (*p < L'8')) { /* INTL: digit */
		result = (WCHAR)(*p - L'0');
		p++;
		if ((numChars == 2) || !iswdigit(*p) /* INTL: digit */
			|| (*p >= L'8')) {
		    break;
		}
		count = 3;
		result = (WCHAR)((result << 3) + (*p - L'0'));
		p++;
		if ((numChars == 3) || !iswdigit(*p) /* INTL: digit */
			|| (*p >= L'8')) {
		    break;
		}
		count = 4;
		result = (WCHAR)((result << 3) + (*p - L'0'));
		break;
	    }
	    result = *p;
	    break;
    }

done:

    if (readPtr != NULL) {
	*readPtr = count;
    }
    dst[0] = (WCHAR)result;
    if ((result & ~USHRT_MAX) != 0) {
	dst[1] = *(((LPCWSTR)&result) + 1);
	return 2;
    }
    return 1;
}

/*
 *---------------------------------------------------------------------------
 *
 * EagleCopyAndCollapse --
 *
 *	Copy a string and eliminate any backslashes that aren't in braces.
 *
 * Results:
 *	Count characters that get copied from src to dst. Along the way,
 *	backslash sequences are substituted in the copy.  After scanning
 *	count characters from src, a null character is placed at the end
 *	of dst.  Returns the number of characters that got written to dst.
 *
 * Side effects:
 *	None.
 *
 *---------------------------------------------------------------------------
 */

static SIZE_T
EagleCopyAndCollapse(
    SIZE_T count,		/* Number of characters to copy from src. */
    LPCWSTR src,		/* Copy from here... */
    LPWSTR dst)			/* ... to here. */
{
    SIZE_T newCount = 0;

    assert(count >= 0);
    assert(src != NULL);
    assert(dst != NULL);

    while (count > 0) {
	WCHAR c = *src;
	if (c == L'\\') {
	    SIZE_T numRead;
	    SIZE_T numWrite = EagleParseBackslash(src, count, &numRead, dst);

	    dst += numWrite;
	    newCount += numWrite;
	    src += numRead;
	    count -= numRead;
	} else {
	    *dst = c;
	    dst++;
	    newCount++;
	    src++;
	    count--;
	}
    }
    return newCount;
}

/*
 *---------------------------------------------------------------------------
 *
 * EagleScanCountedElement --
 *
 *	This procedure is a companion procedure to
 *	EagleConvertCountedElement.  It scans a string to see what needs to
 *	be done to it (e.g. add backslashes or enclosing braces) to make the
 *	string into a valid Tcl list element.  If length is -1, then the
 *	string is scanned up to the first NUL character.
 *
 * Results:
 *	The return value is an overestimate of the number of characters that
 *	will be needed by EagleConvertCountedElement to produce a valid list
 *	element from string.  The word at *flagPtr is filled in with a value
 *	needed by EagleConvertCountedElement when doing the actual
 *	conversion.
 *
 * Side effects:
 *	None.
 *
 *---------------------------------------------------------------------------
 */

static SIZE_T
EagleScanCountedElement(
    LPCWSTR string,		/* String to convert to Tcl list element. */
    SIZE_T length,		/* Number of characters in string, or -1. */
    FLAGS *flagPtr)		/* Where to store information to guide
				 * EagleConvertCountedElement. */
{
    FLAGS flags;
    int nestingLevel;
    LPCWSTR p, lastChar;

    assert((length >= 0) || (length == (SIZE_T)-1));
    assert(flagPtr != NULL);

    /*
     * This procedure and EagleConvertCountedElement together do two things:
     *
     * 1. They produce a proper list, one that will yield back the argument
     *    strings when evaluated or when disassembled with Eagle_SplitList.
     *    This is the most important thing.
     *
     * 2. They try to produce legible output, which means minimizing the use
     *    of backslashes (using braces instead).  However, there are some
     *    situations where backslashes must be used (e.g. an element like
     *    "{abc": the leading brace will have to be backslashed.  For each
     *    element, one of three things must be done:
     *
     *    (a) Use the element as-is (it doesn't contain any special
     *        characters).  This is the most desirable option.
     *
     *    (b) Enclose the element in braces, but leave the contents alone.
     *        This happens if the element contains embedded space, or if it
     *        contains characters with special interpretation ($, [, ;, or \),
     *        or if it starts with a brace or double-quote, or if there are
     *        no characters in the element.
     *
     *    (c) Don't enclose the element in braces, but add backslashes to
     *        prevent special interpretation of special characters.  This is
     *        a last resort used when the argument would normally fall under
     *        case (b) but contains unmatched braces.  It also occurs if the
     *        last character of the argument is a backslash or if the element
     *        contains a backslash followed by newline.
     *
     * The procedure figures out how many characters will be needed to store
     * the result (actually, it overestimates). It also collects information
     * about the element in the form of a flags word.
     *
     * Note: list elements produced by this procedure and
     * EagleConvertCountedElement must have the property that they can be
     * enclosed in curly braces to make sub-lists.  This means, for example,
     * that we must not leave unmatched curly braces in the resulting list
     * element.
     */

    nestingLevel = 0;
    flags = 0;
    if (string == NULL) {
	string = L"";
    }
    if (length == (SIZE_T)-1) {
	length = (SIZE_T)wcslen(string);
    }
    lastChar = string + length;
    p = string;
    if ((p == lastChar) || (*p == L'{') || (*p == L'"')) {
	flags |= EAGLE_USE_BRACES;
    }
    for (; p < lastChar; p++) {
	switch (*p) {
	    case L'{':
		nestingLevel++;
		break;
	    case L'}':
		nestingLevel--;
		if (nestingLevel < 0) {
		    flags |= EAGLE_DONT_USE_BRACES | EAGLE_BRACES_UNMATCHED;
		}
		break;
	    case L'[':
	    case L'$':
	    case L';':
	    case L' ':
	    case L'\f':
	    case L'\n':
	    case L'\r':
	    case L'\t':
	    case L'\v':
		flags |= EAGLE_USE_BRACES;
		break;
	    case L'\\':
		/*
		 * The first portion of this check used to compare "p + 1"
		 * against "lastChar" with the "==" operator; however, the
		 * static analysis tool thinks that could lead to accessing
		 * one character beyond the end of the string; therefore,
		 * the check was changed to use the ">=" operator instead.
		 *
		 * Really, the loop invariant of the enclosing "for" loop
		 * already defends against this, since the value of "p" is
		 * guaranteed to be less than "lastChar" here; therefore,
		 * the maximum value of "p + 1" is "lastChar", which would
		 * cause the short-circuit semantics of the "if" statement,
		 * thus preventing access to "p[1]".  Changing this to use
		 * the ">=" operator should make it much easier for the
		 * static analysis tool to figure all this out (i.e. since
		 * it should then assert "(p + 1) < lastChar" at the point
		 * where "p[1]" is used).
		 */
		if (((p + 1) >= lastChar) || (p[1] == L'\n')) {
		    flags = EAGLE_DONT_USE_BRACES | EAGLE_BRACES_UNMATCHED;
		} else {
		    SIZE_T size;

		    EagleParseBackslash(
			p, (SIZE_T)(lastChar - p), &size, NULL);

		    p += size - 1;
		    flags |= EAGLE_USE_BRACES;
		}
		break;
	}
    }
    if (nestingLevel != 0) {
	flags = EAGLE_DONT_USE_BRACES | EAGLE_BRACES_UNMATCHED;
    }
    *flagPtr = flags;

    /*
     * Allow enough space to backslash every character plus leave
     * two spaces for braces.
     */

    return (SIZE_T)((2 * (p - string)) + 2);
}

/*
 *---------------------------------------------------------------------------
 *
 * EagleConvertCountedElement --
 *
 *	This is a companion procedure to EagleScanCountedElement.  Given the
 *	information produced by EagleScanCountedElement, this procedure
 *	converts a string to a list element equal to that string.
 *
 * Results:
 *	Information is copied to *dst in the form of a list element identical
 *	to src (i.e. if Eagle_SplitList is applied to dst it will produce a
 *	string identical to src).  The return value is a count of the number
 *	of characters copied (not including the terminating NULL character).
 *
 * Side effects:
 *	None.
 *
 *---------------------------------------------------------------------------
 */

static SIZE_T
EagleConvertCountedElement(
    LPCWSTR src,		/* Source information for list element. */
    SIZE_T length,		/* Number of characters in src, or -1. */
    LPWSTR dst,			/* Place to put list-ified element. */
    FLAGS flags)		/* Flags produced by Tcl_ScanElement. */
{
    LPWSTR p = dst;
    LPCWSTR lastChar;

    assert((length >= 0) || (length == (SIZE_T)-1));
    assert(dst != NULL);

    /*
     * See the comment block at the beginning of the Tcl_ScanElement
     * code for details of how this works.
     */

    if (src && (length == (SIZE_T)-1)) {
	length = (SIZE_T)wcslen(src);
    }
    if ((src == NULL) || (length == 0)) {
	p[0] = L'{';
	p[1] = L'}';
	return 2;
    }
    lastChar = src + length;
    if ((*src == L'#') && !(flags & EAGLE_DONT_QUOTE_HASH)) {
	flags |= EAGLE_USE_BRACES;
    }
    if ((flags & EAGLE_USE_BRACES) && !(flags & EAGLE_DONT_USE_BRACES)) {
	*p = L'{';
	p++;
	for (; src != lastChar; src++, p++) {
	    *p = *src;
	}
	*p = L'}';
	p++;
    } else {
	if (*src == L'{') {
	    /*
	     * Can't have a leading brace unless the whole element is
	     * enclosed in braces.  Add a backslash before the brace.
	     * Furthermore, this may destroy the balance between open
	     * and close braces, so set EAGLE_BRACES_UNMATCHED.
	     */

	    p[0] = L'\\';
	    p[1] = L'{';
	    p += 2;
	    src++;
	    flags |= EAGLE_BRACES_UNMATCHED;
	} else if ((*src == L'#') && !(flags & EAGLE_DONT_QUOTE_HASH)) {
	    /*
	     * Leading '#' could be seen by [eval] as the start of
	     * a comment, if on the first element of a list, so
	     * quote it.
	     */

	    p[0] = L'\\';
	    p[1] = L'#';
	    p += 2;
	    src++;
	}
	for (; src != lastChar; src++) {
	    switch (*src) {
		case L']':
		case L'[':
		case L'$':
		case L';':
		case L' ':
		case L'\\':
		case L'"':
		    *p = L'\\';
		    p++;
		    break;
		case L'{':
		case L'}':
		    /*
		     * It may not seem necessary to backslash braces, but
		     * it is.  The reason for this is that the resulting
		     * list element may actually be an element of a sub-list
		     * enclosed in braces (e.g. if Tcl_DStringStartSublist
		     * has been invoked), so there may be a brace mismatch
		     * if the braces aren't backslashed.
		     */

		    if (flags & EAGLE_BRACES_UNMATCHED) {
			*p = L'\\';
			p++;
		    }
		    break;
		case L'\f':
		    *p = L'\\';
		    p++;
		    *p = L'f';
		    p++;
		    continue;
		case L'\n':
		    *p = L'\\';
		    p++;
		    *p = L'n';
		    p++;
		    continue;
		case L'\r':
		    *p = L'\\';
		    p++;
		    *p = L'r';
		    p++;
		    continue;
		case L'\t':
		    *p = L'\\';
		    p++;
		    *p = L't';
		    p++;
		    continue;
		case L'\v':
		    *p = L'\\';
		    p++;
		    *p = L'v';
		    p++;
		    continue;
	    }
	    *p = *src;
	    p++;
	}
    }
    return (SIZE_T)(p - dst);
}

/*
 *---------------------------------------------------------------------------
 *
 * EagleFindElement --
 *
 *	Given a pointer into a Tcl list, locate the first (or next) element
 *	in the list.
 *
 * Results:
 *	The return value is normally EAGLE_OK, which means that the element
 *	was successfully located.  If EAGLE_ERROR is returned it means that
 *	list didn't have proper list structure; the interp's result contains
 *	a more detailed error message.
 *
 *	If EAGLE_OK is returned, then *elementPtr will be set to point to
 *	the first element of list, and *nextPtr will be set to point to the
 *	character just after any white space following the last character
 *	that's part of the element. If this is the last argument in the
 *	list, then *nextPtr will point just after the last character in the
 *	list (i.e., at the character at list+listLength). If sizePtr is
 *	non-NULL, *sizePtr is filled in with the number of characters in the
 *	element.  If the element is in braces, then *elementPtr will point
 *	to the character after the opening brace and *sizePtr will not
 *	include either of the braces. If there isn't an element in the list,
 *	*sizePtr will be zero, and both *elementPtr and *termPtr will point
 *	just after the last character in the list. Note: this procedure does
 *	NOT collapse backslash sequences.
 *
 * Side effects:
 *	None.
 *
 *---------------------------------------------------------------------------
 */

static RETURNCODE
EagleFindElement(
    LPCWSTR list,		/* Points to the first character of a string
				 * containing a Tcl list with zero or more
				 * elements (possibly in braces). */
    SIZE_T listLength,		/* Number of characters in the string. */
    LPCWSTR *elementPtr,	/* Where to put address of first significant
				 * character in first element of list. */
    LPCWSTR *nextPtr,		/* Fill in with location of character just
				 * after all white space following end of
				 * argument (next arg or end of list). */
    SIZE_T *sizePtr,		/* If non-zero, fill in with size of
				 * element. */
    LPBOOL bracePtr,		/* If non-zero, fill in with non-zero/zero
				 * to indicate that arg was/wasn't
				 * in braces. */
    LPCWSTR *errorPtr)		/* Where to put the error message, if any. */
{
    LPCWSTR p = list;
    LPCWSTR elemStart;		/* Points to first character of first element. */
    LPCWSTR limit;		/* Points just after list's last character. */
    int openBraces = 0;		/* Brace nesting level during parse. */
    BOOL inQuotes = FALSE;
    SIZE_T size = 0;		/* lint. */
    SIZE_T numChars;
    LPCWSTR p2;

    assert(list != NULL);
    assert(listLength >= 0);
    assert(elementPtr != NULL);
    assert(nextPtr != NULL);

    /*
     * Skim off leading white space and check for an opening brace or
     * quote. We treat embedded NULs in the list as characters belonging
     * to a list element.
     */

    limit = (list + listLength);
    while ((p < limit) && iswspace(*p)) { /* INTL: ISO space. */
	p++;
    }
    if (p == limit) { /* no element found */
	elemStart = limit;
	goto done;
    }

    if (*p == L'{') {
	openBraces++;
	p++;
    } else if (*p == L'"') {
	inQuotes = TRUE;
	p++;
    }
    elemStart = p;

    /*
     * Find element's end (a space, close brace, or the end of the string).
     */

    while (p < limit) {
	switch (*p) {

	    /*
	     * Open brace: don't treat specially unless the element is in
	     * braces. In this case, keep a nesting count.
	     */

	    case L'{':
		if (openBraces != 0) {
		    openBraces++;
		}
		break;

	    /*
	     * Close brace: if element is in braces, keep nesting count and
	     * quit when the last close brace is seen.
	     */

	    case L'}':
		if (openBraces > 1) {
		    openBraces--;
		} else if (openBraces == 1) {
		    size = (SIZE_T)(p - elemStart);
		    p++;
		    if ((p >= limit)
			    || iswspace(*p)) { /* INTL: ISO space. */
			goto done;
		    }

		    /*
		     * Garbage after the closing brace; return an error.
		     */

		    if (errorPtr != NULL) {
			p2 = p;
			while ((p2 < limit)
				&& (!iswspace(*p2)) /* INTL: ISO space. */
				&& (p2 < (p + 20))) {
			    p2++;
			}
			*errorPtr = EaglePrintf(0,
				L"list element in braces followed by "
				L"\"%.*ls\" %ls", (int)(p2 - p), p,
				L"instead of space");
		    }
		    return EAGLE_ERROR;
		}
		break;

	    /*
	     * Backslash:  skip over everything up to the end of the
	     * backslash sequence.
	     */

	    case L'\\': {
		EagleParseBackslash(p, (SIZE_T)(limit - p), &numChars, NULL);
		p += (numChars - 1);
		break;
	    }

	    /*
	     * Space: ignore if element is in braces or quotes; otherwise
	     * terminate element.
	     */

	    case L' ':
	    case L'\f':
	    case L'\n':
	    case L'\r':
	    case L'\t':
	    case L'\v':
		if ((openBraces == 0) && !inQuotes) {
		    size = (SIZE_T)(p - elemStart);
		    goto done;
		}
		break;

	    /*
	     * Double-quote: if element is in quotes then terminate it.
	     */

	    case L'"':
		if (inQuotes) {
		    size = (SIZE_T)(p - elemStart);
		    p++;
		    if ((p >= limit)
			    || iswspace(*p)) { /* INTL: ISO space */
			goto done;
		    }

		    /*
		     * Garbage after the closing quote; return an error.
		     */

		    if (errorPtr != NULL) {
			p2 = p;
			while ((p2 < limit)
				&& (!iswspace(*p2)) /* INTL: ISO space */
				&& (p2 < (p + 20))) {
			    p2++;
			}
			*errorPtr = EaglePrintf(0,
				L"list element in quotes followed by "
				L"\"%.*ls\" %ls", (int)(p2 - p), p,
				L"instead of space");
		    }
		    return EAGLE_ERROR;
		}
		break;
	}
	p++;
    }

    /*
     * End of list: terminate element.
     */

    if (p == limit) {
	if (openBraces != 0) {
	    if (errorPtr != NULL) {
		*errorPtr = EaglePrintf(0, L"unmatched open brace in list");
	    }
	    return EAGLE_ERROR;
	} else if (inQuotes) {
	    if (errorPtr != NULL) {
		*errorPtr = EaglePrintf(0, L"unmatched open quote in list");
	    }
	    return EAGLE_ERROR;
	}
	size = (SIZE_T)(p - elemStart);
    }

done:

    while ((p < limit) && iswspace(*p)) { /* INTL: ISO space. */
	p++;
    }
    *elementPtr = elemStart;
    *nextPtr = p;
    if (sizePtr != NULL) {
	*sizePtr = size;
    }
    if (bracePtr != NULL) {
	*bracePtr = (openBraces != 0);
    }

    return EAGLE_OK;
}

/*
 *---------------------------------------------------------------------------
 *
 * Eagle_GetVersion --
 *
 *	This function returns the string representation of the version of
 *	this library.
 *
 * Results:
 *	The string representation of the version of this library -OR- NULL if
 *	the version cannot be obtained.
 *
 * Side effects:
 *	None.
 *
 *---------------------------------------------------------------------------
 */

LPCWSTR
Eagle_GetVersion(VOID)
{
    SIZE_T size = (LIBRARY_VERSION_LENGTH + 1 /* NUL */) * sizeof(WCHAR);
    LPWSTR pBuffer = Eagle_AllocateMemory(size);

    if (pBuffer == NULL) {
	return NULL;
    }

    swprintf(pBuffer, LIBRARY_VERSION_LENGTH, LIBRARY_VERSION_FORMAT,
	LIBRARY_UNICODE_NAME, LIBRARY_UNICODE_PATCH_LEVEL,
	LIBRARY_UNICODE_SOURCE_ID, LIBRARY_UNICODE_SOURCE_TIMESTAMP,
#if defined(_DEBUG)
	L" DEBUG",
#else
	L" RELEASE",
#endif
	L" SIZE_OF_WCHAR_T=", (int)sizeof(WCHAR),
#if defined(USE_32BIT_SIZE_T)
	L" USE_32BIT_SIZE_T=" UNICODIFY(STRINGIFY(USE_32BIT_SIZE_T)),
#else
	L"",
#endif
#if defined(USE_SYSSTRINGLEN)
	L" USE_SYSSTRINGLEN=" UNICODIFY(STRINGIFY(USE_SYSSTRINGLEN))
#else
	L""
#endif
    );

#if defined(USE_WCHARSTRTOUSHORTSTR) && USE_WCHARSTRTOUSHORTSTR
    EagleWcharStrToUshortStr(pBuffer);
#endif

    return pBuffer;
}

/*
 *---------------------------------------------------------------------------
 *
 * Eagle_AllocateMemory --
 *
 *	This function allocates a block of memory of at least the specified
 *	size.
 *
 * Results:
 *	The pointer to the new memory block -OR- NULL if the memory could not
 *	be obtained.
 *
 * Side effects:
 *	None.
 *
 *---------------------------------------------------------------------------
 */

LPVOID
Eagle_AllocateMemory(
    SIZE_T size)	/* The size, in bytes, of the memory block to be
			 * allocated. */
{
    LPVOID pMemory = NULL;

    assert(sizeof(BYTE) >= 1);
    assert(size >= 0);

    if (size > 0) {
	assert(size <= LIBRARY_MAXIMUM_SIZE_T);
	pMemory = calloc(size, sizeof(BYTE));
    }

    LIBRARY_DEBUG(("Eagle_AllocateMemory: 0x%p, requested %d bytes, "
	"received %d bytes\n", pMemory, (int)size, (pMemory != NULL) ?
	(int)Eagle_MemorySize(pMemory) : 0));

    if (pMemory == NULL) {
	LIBRARY_TRACE(("Eagle_AllocateMemory: out of memory (%d)\n",
	    (int)size));
    }

    return pMemory;
}

/*
 *---------------------------------------------------------------------------
 *
 * Eagle_FreeMemory --
 *
 *	This function frees a block of memory that was previous allocated by
 *	the Eagle_AllocateMemory function.
 *
 * Results:
 *	None.
 *
 * Side effects:
 *	None.
 *
 *---------------------------------------------------------------------------
 */

VOID
Eagle_FreeMemory(
    LPVOID pMemory)	/* The memory block to free.  This memory block must
			 * have been obtained from Eagle_AllocateMemory. */
{
    LIBRARY_DEBUG(("Eagle_FreeMemory: 0x%p, received %d bytes\n", pMemory,
	(pMemory != NULL) ? (int)Eagle_MemorySize(pMemory) : 0));

    if (pMemory == NULL)
	return;

#if !defined(NDEBUG)
    memset(pMemory, LIBRARY_FREED_MEMORY, Eagle_MemorySize(pMemory));
#endif

    free(pMemory);
}

/*
 *---------------------------------------------------------------------------
 *
 * Eagle_FreeElements --
 *
 *	This function frees all memory that is being used to contain the
 *	array of list elements that was allocated by the Eagle_SplitList
 *	function.
 *
 * Results:
 *	None.
 *
 * Side effects:
 *	None.
 *
 *---------------------------------------------------------------------------
 */

VOID
Eagle_FreeElements(
    SIZE_T elementCount,	/* The number of list elements. */
    LPWSTR *ppElements)		/* The list element array to free. */
{
    /*
     * NOTE: The space to hold the element pointers as well as the element
     *       strings themselves is actually one big block; therefore, just
     *       call the normal memory freeing function on it.
     */

    Eagle_FreeMemory(ppElements);
}

/*
 *---------------------------------------------------------------------------
 *
 * Eagle_SplitList --
 *
 *	Splits a list up into its constituent elements.
 *
 * Results
 *	The return value is normally EAGLE_OK, which means that the list was
 *	successfully split up.  If EAGLE_ERROR is returned, it means that
 *	"list" did not have proper list structure; in that case, the error
 *	message will contain a more details.
 *
 * Side effects:
 *	Memory is allocated and possibly freed.
 *
 *---------------------------------------------------------------------------
 */

RETURNCODE
Eagle_SplitList(
    SIZE_T length,		/* Length of string with list structure. */
    LPCWSTR pText,		/* Pointer to string with list structure. */
    LPSIZE_T pElementCount,	/* The number of list elements found. */
    LPSIZE_T *ppElementLengths,	/* The string lengths of the list elements. */
    LPCWSTR **pppElements,	/* The array of list elements. */
    LPCWSTR *ppError)		/* The error message, if any. */
{
    LPCWSTR l, q, element;
    LPSIZE_T argc;
    LPWSTR *argv;
    LPWSTR p;
    SIZE_T allocSize;
    SIZE_T listLength, size, i, j, elSize;
    BOOL brace;
    RETURNCODE result;

    assert(length >= 0);
    assert(pText != NULL);
    assert(pElementCount != NULL);
    assert(ppElementLengths != NULL);
    assert(pppElements != NULL);
    assert(ppError != NULL);

    /*
     * Figure out how much space to allocate.  There must be enough
     * space for both the array of pointers and also for a copy of
     * the list.  To estimate the number of pointers needed, count
     * the number of space characters in the list.
     */

    for (size = 2, j = 0, l = pText; j < length; j++, l++) {
	if (iswspace(*l)) { /* INTL: ISO space. */
	    size++;
	    /* Consecutive space can only count as a single list delimiter */
	    while (1) {
		WCHAR next = *(l + 1);
		if ((j + 1) >= length) {
		    break;
		}
		j++; l++;
		if (iswspace(next)) {
		    continue;
		}
		break;
	    }
	}
    }
    listLength = length;
    allocSize = size * sizeof(SIZE_T);
    assert(allocSize > 0);
    assert(allocSize <= LIBRARY_MAXIMUM_SIZE_T);
    argc = Eagle_AllocateMemory(allocSize);
    if (argc == NULL) {
	if (ppError != NULL) {
	    *ppError = EaglePrintf(0,
		L"out of memory for list element lengths (%d)",
		(int)allocSize);
	}
	return EAGLE_ERROR;
    }
    allocSize = (size * sizeof(LPWSTR)) + ((listLength + 1) * sizeof(WCHAR));
    assert(allocSize > 0);
    assert(allocSize <= LIBRARY_MAXIMUM_SIZE_T);
    argv = Eagle_AllocateMemory(allocSize);
    if (argv == NULL) {
	if (ppError != NULL) {
	    *ppError = EaglePrintf(0,
		L"out of memory for list element pointers (%d)",
		(int)allocSize);
	}
	Eagle_FreeMemory(argc);
	return EAGLE_ERROR;
    }
    q = pText + length;
    for (i = 0, p = (LPWSTR)(((LPBYTE)argv) + (size * sizeof(LPWSTR)));
	    listLength > 0; i++) {
	LPCWSTR prevList = pText;

	result = EagleFindElement(pText, listLength, &element, &pText,
				  &elSize, &brace, ppError);
	if (result != EAGLE_OK) {
	    Eagle_FreeMemory(argc);
	    Eagle_FreeMemory(argv);
	    return result;
	}
	listLength -= (SIZE_T)(pText - prevList);
	if (element == q) {
	    break;
	}
	if (i >= size) {
	    if (ppError != NULL) {
		*ppError = EaglePrintf(0, L"wrong estimated list size");
	    }
	    Eagle_FreeMemory(argc);
	    Eagle_FreeMemory(argv);
	    return EAGLE_ERROR;
	}
	if (brace) {
	    wcsncpy(p, element, elSize);
	} else {
	    elSize = EagleCopyAndCollapse(elSize, element, p);
	}
	argc[i] = elSize;
	argv[i] = p;
	p += elSize + 1;
    }

    argc[i] = 0;
    argv[i] = NULL;

    *pElementCount = i;
    *ppElementLengths = argc;
    *pppElements = (LPCWSTR *)argv; /* incompatible-pointer-types warning? */

    return EAGLE_OK;
}

/*
 *---------------------------------------------------------------------------
 *
 * Eagle_JoinList --
 *
 *	Given a collection of strings, merge them together into a single
 *	string that has proper Tcl list structure (i.e. Eagle_SplitList may
 *	be used to retrieve strings equal to the original elements).
 *
 * Results:
 *	A standard Eagle return code.
 *
 * Side effects:
 *	Memory is allocated and possibly freed.
 *
 *---------------------------------------------------------------------------
 */

RETURNCODE
Eagle_JoinList(
    SIZE_T elementCount,	/* The number of list elements present. */
    LPCSIZE_T pElementLengths,	/* The string lengths of the list elements. */
    LPCWSTR *ppElements,	/* The list elements. */
    LPSIZE_T pLength,		/* The string length of the resulting list. */
    LPCWSTR *ppText,		/* The textual representation of the list. */
    LPCWSTR *ppError)		/* The error message, if any. */
{
    FLAGS localFlags[LIBRARY_LOCAL_FLAGS], *flagPtr;
    SIZE_T allocSize;
    SIZE_T i, numChars;
    LPWSTR result, dst;

    assert(elementCount >= 0);

#if !defined(USE_SYSSTRINGLEN) || !USE_SYSSTRINGLEN
    assert(pElementLengths != NULL);
#else
    assert(pElementLengths == NULL);
#endif

    assert(ppElements != NULL);
    assert(ppText != NULL);
    assert(ppError != NULL);

    /*
     * Pass 1: estimate space, gather flags.
     */

    if (elementCount <= LIBRARY_LOCAL_FLAGS) {
	flagPtr = localFlags;
    } else {
	assert(elementCount > 0);
	allocSize = elementCount * sizeof(FLAGS);
	assert(allocSize > 0);
	assert(allocSize <= LIBRARY_MAXIMUM_SIZE_T);
	flagPtr = Eagle_AllocateMemory(allocSize);
	if (flagPtr == NULL) {
	    if (ppError != NULL) {
		*ppError = EaglePrintf(0,
		    L"out of memory for list element flags (%d)",
		    (int)allocSize);
	    }
	    return EAGLE_ERROR;
	}
    }

    numChars = 0;
    for (i = 0; i < elementCount; i++) {
	/*
	 * If necessary, add space for the list element separator.
	 */

	if (i > 0) {
	    numChars++; /* +1 SPACE */
	}

	numChars += EagleScanCountedElement(ppElements[i],
	    GetStringLen(i) /* NON-PORTABLE? */, &flagPtr[i]);
    }

    /*
     * Pass 2: copy into the result area.
     */

    allocSize = (numChars + 1) * sizeof(WCHAR);
    assert(allocSize > 0);
    assert(allocSize <= LIBRARY_MAXIMUM_SIZE_T);
    result = Eagle_AllocateMemory(allocSize);
    if (result == NULL) {
	if (ppError != NULL) {
	    *ppError = EaglePrintf(0,
		L"out of memory for list element text (%d)",
		(int)allocSize);
	}
	if (flagPtr != localFlags) {
	    Eagle_FreeMemory(flagPtr);
	}
	return EAGLE_ERROR;
    }

    numChars = 0;
    dst = result;
    for (i = 0; i < elementCount; i++) {
	SIZE_T eleChars = 0;

	/*
	 * If necessary, add the list element separator.
	 */

	if (i > 0) {
	    *dst = L' ';
	    eleChars++;
	    dst++;
	}

	eleChars += EagleConvertCountedElement(
		ppElements[i], GetStringLen(i) /* NON-PORTABLE? */, dst,
		flagPtr[i] | ((i == 0) ? 0 : EAGLE_DONT_QUOTE_HASH));

	numChars += eleChars;
	dst += (eleChars - (i > 0));
    }

    if (flagPtr != localFlags) {
	Eagle_FreeMemory(flagPtr);
    }

    *pLength = numChars;
    *ppText = result;

    return EAGLE_OK;
}
