/*
 * JustWait.c --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#include "windows.h"

#ifndef MAX_DIGITS
#  define MAX_DIGITS (5) /* NOTE: Tens-of-thousands of milliseconds maximum. */
#endif

static DWORD getMilliseconds(LPCWSTR zValue);

/*
 *------------------------------------------------------------------------
 *
 * mainCRTStartup --
 *
 *	This utility program waits for a specified (or default) number of
 *	milliseconds and then returns zero (success) as the exit code.  No
 *	exit code is used to indicate failure because this utility program
 *	cannot fail.
 *
 * Results:
 *	Zero on success.  Always return zero.
 *
 * Side effects:
 *	Time passes.
 *
 *------------------------------------------------------------------------
 */

int mainCRTStartup()
{
    DWORD dwMilliseconds = 0;
    LPCWSTR zCmdLine = GetCommandLineW();
    if( zCmdLine!=NULL ){
	int argc = 0;
	LPWSTR *argv = CommandLineToArgvW(zCmdLine, &argc);
	if( argv!=NULL ){
	    LPCWSTR zMilliseconds = NULL;
	    if( argc>=2 ){
		zMilliseconds = argv[1];
		dwMilliseconds = getMilliseconds(zMilliseconds);
	    }
	    if( zMilliseconds!=NULL && dwMilliseconds>0 ){
		HANDLE hConsoleOutput = GetStdHandle(STD_OUTPUT_HANDLE);
		if( hConsoleOutput!=NULL ){
		    DWORD dwNumberOfCharsWritten = 0;
		    WriteConsoleW(hConsoleOutput, L"Waiting for \"", 13,
			&dwNumberOfCharsWritten, NULL);
		    WriteConsoleW(hConsoleOutput, zMilliseconds,
			lstrlenW(zMilliseconds), &dwNumberOfCharsWritten,
			NULL);
		    WriteConsoleW(hConsoleOutput, L"\" milliseconds...\n", 18,
			&dwNumberOfCharsWritten, NULL);
		}
	    }
	    LocalFree(argv);
	    argv = NULL;
	}
    }
    Sleep(dwMilliseconds);
    ExitProcess(0);
    return 0; /* NOT REACHED */
}

/*
 *------------------------------------------------------------------------
 *
 * getMilliseconds --
 *
 *	This function attempts to interpret the specified string value as
 *	an unsigned decimal integer.  The minimum value that may be
 *	returned is zero.  The maximum value that may be returned is the
 *	decimal number composed of MAX_DIGITS digits and where each digit
 *	is a nine.
 *
 * Results:
 *	The number of milliseconds upon success; otherwise, zero.  There
 *	is no return value reserved to indicate failure because zero is
 *	included in the normal result domain.
 *
 * Side effects:
 *	None.
 *
 *------------------------------------------------------------------------
 */

static DWORD getMilliseconds(
    LPCWSTR zValue)	/* String representing unsigned decimal integer. */
{
    DWORD v = 0;
    int i, c;
    if( zValue==NULL ) return 0;
    while( zValue[0]==L'0' ) zValue++;
    for(i=0; i<MAX_DIGITS && (c = (zValue[i] - L'0'))>=0 && c<=9; i++){
	v = (v * 10) + c;
    }
    return v;
}
