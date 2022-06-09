/*
 * DllMain.c -- Eagle Package for Tcl (Garuda)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#include "windows.h"	/* NOTE: For CreateMutex, etc. */

#include "tcl.h"	/* NOTE: For public Tcl API. */
#include "pkgVersion.h"	/* NOTE: Package version information. */
#include "GarudaInt.h"	/* NOTE: For private package API. */

/*
 * NOTE: These are the names of the mutexes we create and hold while this DLL
 *       is loaded.  This is to help prevent the setup from trying to install
 *       while the DLL is loaded.
 */

#define PACKAGE_MUTEX_NAME	    JOIN(PACKAGE_UNICODE_NAME, \
				    UNICODE_TEXT("_Setup"))

#define PACKAGE_GLOBAL_MUTEX_NAME   JOIN(UNICODE_TEXT("Global\\"), \
				    JOIN(PACKAGE_UNICODE_NAME, \
				    UNICODE_TEXT("_Setup")))

/*
 * NOTE: Functions defined in this file.
 */

BOOL WINAPI		DllMain(HINSTANCE hInstance, DWORD reason,
			    LPVOID reserved);

/*
 * NOTE: The global mutex that we create and hold while this DLL is loaded.
 *       This mutex should be visible in all user sessions.
 */

static HANDLE globalMutex = NULL;

/*
 * NOTE: The mutex that we create and hold while this DLL is loaded.
 */

static HANDLE mutex = NULL;

/*
 *----------------------------------------------------------------------
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
 *----------------------------------------------------------------------
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
	    /*
	     * NOTE: This library does not handle the DLL_THREAD_ATTACH and
	     *       DLL_THREAD_DETACH notifications.
	     */

	    DisableThreadLibraryCalls(hInstance);

	    /*
	     * NOTE: Save the package module handle for later usage.
	     */

	    SetPackageModule(hInstance);

	    /*
	     * NOTE: Attempt to create the global mutex now and make sure that
	     *       we got it.  If not, fail.
	     */

	    globalMutex = CreateMutexW(NULL, TRUE, PACKAGE_GLOBAL_MUTEX_NAME);

	    if (globalMutex == NULL) {
		result = FALSE;
		goto done;
	    }

	    /*
	     * NOTE: Attempt to create the [local] mutex now and make sure that
	     *       we got it.  If not, fail.
	     */

	    mutex = CreateMutexW(NULL, TRUE, PACKAGE_MUTEX_NAME);

	    if (mutex == NULL) {
		result = FALSE;
		goto done;
	    }
	    break;
	}
	case DLL_PROCESS_DETACH: {
	    /*
	     * NOTE: Currently, nothing special is done here, just jump to the
	     *       common cleanup code.
	     */

	    goto done;
	}
    }

done:
    if ((reason == DLL_PROCESS_DETACH) ||
	(!result && (reason == DLL_PROCESS_ATTACH))) {
	/*
	 * NOTE: Either we are being unloaded from the process or the loading
	 *       process has failed; therefore, release our mutexes now.
	 */

	if (mutex != NULL) {
	    CloseHandle(mutex);
	    mutex = NULL;
	}

	if (globalMutex != NULL) {
	    CloseHandle(globalMutex);
	    globalMutex = NULL;
	}

	/*
	 * NOTE: Also, reset the saved package module handle now.
	 */

	SetPackageModule(NULL);
    }

    return result;
}
