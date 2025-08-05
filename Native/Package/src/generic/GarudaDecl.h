/*
 * GarudaDecl.h -- Eagle Package for Tcl (Garuda)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#ifndef _GARUDA_DECL_H_
#define _GARUDA_DECL_H_

PACKAGE_INTERN HMODULE 	GetPackageModule(void);
PACKAGE_INTERN void	SetPackageModule(HMODULE hModule);
PACKAGE_INTERN LPCWSTR	GetClrErrorMessage(LPCWSTR source, HRESULT hResult);
PACKAGE_INTERN void	TclLog(Tcl_Interp* interp, LPCWSTR logCommand, ...);
PACKAGE_INTERN BOOL	GetClrWasLoaded(void);
PACKAGE_INTERN BOOL	GetClrWasStarted(void);
PACKAGE_INTERN BOOL	GetClrBridgeStarted(void);
PACKAGE_INTERN void	SetClrBridgeStarted(BOOL bStarted);
PACKAGE_INTERN int	LoadAndStartTheClr(Tcl_Interp* interp,
			    LPCWSTR logCommand, BOOL bLoad,
			    BOOL bUseMinimumClr, BOOL bStart, BOOL bStrict);
PACKAGE_INTERN int	StopAndReleaseTheClr(Tcl_Interp* interp,
			    LPCWSTR logCommand, BOOL bRelease, BOOL bStrict);
PACKAGE_INTERN BOOL	CanExecuteClrCode(Tcl_Interp* interp);
PACKAGE_INTERN int	ExecuteClrMethod(HANDLE hModule,
			    ClrTclStubs* pTclStubs, Tcl_Interp* interp,
			    LPCWSTR logCommand, ClrMethodInfo* pMethodInfo,
			    LPCWSTR argument, MethodFlags methodFlags,
			    LPDWORD pReturnValue);
PACKAGE_INTERN HRESULT	GetCurrentAppDomainId(LPDWORD pAppDomainId);
PACKAGE_INTERN HRESULT	GetClrVersion(LPWSTR pVersion, LPDWORD pLength);
PACKAGE_INTERN HRESULT	DumpState(LPWSTR fileName, LONG lTclStubs,
			    HANDLE hTclModule, ClrTclStubs* pTclStubs,
			    LPWSTR pState, LPDWORD pLength);

/*
 * NOTE: This package is thread-safe and this mutex is used to protect access
 *       to the static state defined in this file.
 */

TCL_DECLARE_MUTEX(packageMutex);

#endif /* _GARUDA_DECL_H_ */
