/*
 * SpilornisWin32.h -- Eagle Native Utility Library (Spilornis)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#ifndef _SPILORNIS_WIN32_H_
#define _SPILORNIS_WIN32_H_

/*
 * NOTE: Win32 API functions required by this file.  These functions are
 *       declared inline rather than simply including "windows.h" because
 *       that would bring in a ton of unrelated stuff that is completely
 *       unnecessary here.  Also, the "standard" Win32 type definitions
 *       would conflict with those already defined by this project.
 */

#if defined(USE_HEAPAPI) && USE_HEAPAPI
extern __declspec(dllimport) BOOL __stdcall HeapValidate(
			    HANDLE, DWORD, LPCVOID);

extern __declspec(dllimport) LPVOID __stdcall HeapAlloc(
			    HANDLE, DWORD, SIZE_T);

extern __declspec(dllimport) SIZE_T __stdcall HeapSize(
			    HANDLE, DWORD, LPCVOID);

extern __declspec(dllimport) BOOL __stdcall HeapFree(
			    HANDLE, DWORD, LPVOID);
#endif

#if !defined(USE_NARROW_CHAR_T)
extern __declspec(dllimport) DWORD __stdcall GetEnvironmentVariableW(
			    LPCWSTR, LPWSTR, DWORD);
#endif

extern __declspec(dllimport) VOID __stdcall OutputDebugStringA(LPCSTR);

#if defined(USE_SYSSTRINGLEN) && USE_SYSSTRINGLEN
extern __declspec(dllimport) UINT __stdcall SysStringLen(BSTR);
#endif

#endif /* _SPILORNIS_WIN32_H_ */
