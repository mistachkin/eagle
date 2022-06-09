/*
 * DllMain.c -- Eagle Native Utility Library (Spilornis)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#include "windows.h"	/* NOTE: For DisableThreadLibraryCalls, etc. */

/*
 * NOTE: Functions defined in this file.
 */

BOOL WINAPI		DllMain(HINSTANCE hInstance, DWORD reason,
			    LPVOID reserved);

/*
 *---------------------------------------------------------------------------
 *
 * DllMain --
 *
 *	This routine is called by the CRT library initialization
 *	code, or the DllEntryPoint routine.  It is responsible for
 *	initializing various dynamically loaded libraries.  Nothing
 *	overly complex or creative should be done in this function
 *	because the loader lock is held while it is executing (i.e.
 *	we cannot do anything that would cause another DLL to be
 *	loaded or unloaded, either directly or indirectly).
 *
 * Results:
 *	TRUE on sucess, FALSE on failure.  The result is ignored by
 *	Windows unless the reason is DLL_PROCESS_ATTACH.
 *
 * Side effects:
 *	None.
 *
 *---------------------------------------------------------------------------
 */

BOOL WINAPI DllMain(
    HINSTANCE hInstance,	/* The handle to the DLL module.  The
				 * value is the base address of the
				 * DLL.  The HINSTANCE of a DLL is the
				 * same as the HMODULE of the DLL, so
				 * it can be used in calls to functions
				 * that require a module handle. */
    DWORD reason,		/* The reason code that indicates why
				 * the DLL entry-point function is
				 * being called. */
    LPVOID reserved)		/* If reason is DLL_PROCESS_ATTACH,
				 * reserved is NULL for dynamic loads
				 * and non-NULL for static loads.  If
				 * reason is DLL_PROCESS_DETACH,
				 * reserved is NULL if FreeLibrary has
				 * been called or the DLL load failed
				 * and non-NULL if the process is
				 * terminating. */
{
    BOOL result = TRUE;

    switch (reason) {
	case DLL_PROCESS_ATTACH: {
	    DisableThreadLibraryCalls(hInstance);
	    break;
	}
    }

    return result;
}
