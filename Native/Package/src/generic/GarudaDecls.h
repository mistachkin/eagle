/*
 * GarudaDecls.h -- Eagle Package for Tcl (Garuda)
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

PACKAGE_INTERN int	TracePrintf(LPCSTR format, ...);
PACKAGE_INTERN HMODULE	GetPackageModule(void);
PACKAGE_INTERN void	SetPackageModule(HMODULE hModule);
PACKAGE_INTERN LPCWSTR	GetClrErrorMessage(LPCWSTR source, HRESULT hResult);
PACKAGE_INTERN void	TclLog(Tcl_Interp* interp, LPCWSTR logCommand, ...);

#if defined(USE_CORE_CLR)
PACKAGE_INTERN BOOL	GetCoreClrWasLoaded(void);
PACKAGE_INTERN BOOL	GetCoreClrWasStarted(void);
PACKAGE_INTERN BOOL	GetCoreClrBridgeStarted(void);
PACKAGE_INTERN void	SetCoreClrBridgeStarted(BOOL bStarted);
PACKAGE_INTERN int	LoadAndStartTheCoreClr(Tcl_Interp *interp,
			    LPCWSTR logCommand, LPCWSTR runtimeConfigPath,
			    BOOL bLoad, BOOL bUseMinimumClr, BOOL bStart,
			    BOOL bStrict);
PACKAGE_INTERN int	StopAndReleaseTheCoreClr(Tcl_Interp *interp,
			    LPCWSTR logCommand, BOOL bRelease, BOOL bStrict);
PACKAGE_INTERN BOOL	CanExecuteCoreClrCode(Tcl_Interp *interp);
PACKAGE_INTERN int	ExecuteCoreClrMethod(HMODULE hModule,
			    ClrTclStubs *pTclStubs, Tcl_Interp *interp,
			    LPCWSTR logCommand, ClrMethodInfo* pMethodInfo,
			    LPCWSTR argument, MethodFlags methodFlags,
			    LPDWORD pReturnValue);
PACKAGE_INTERN HRESULT	GetCurrentCoreClrAppDomainId(LPDWORD pAppDomainId);
PACKAGE_INTERN HRESULT	GetCoreClrVersion(LPWSTR pVersion, LPDWORD pLength);
PACKAGE_INTERN HRESULT	DumpCoreClrState(LPWSTR fileName, LONG lTclStubs,
			    HMODULE hTclModule, ClrTclStubs *pTclStubs,
			    LPWSTR pState, LPDWORD pLength);
#else
PACKAGE_INTERN BOOL	GetClrWasLoaded(void);
PACKAGE_INTERN BOOL	GetClrWasStarted(void);
PACKAGE_INTERN BOOL	GetClrBridgeStarted(void);
PACKAGE_INTERN void	SetClrBridgeStarted(BOOL bStarted);
PACKAGE_INTERN int	LoadAndStartTheClr(Tcl_Interp *interp,
			    LPCWSTR logCommand, LPCWSTR runtimeConfigPath,
			    BOOL bLoad, BOOL bUseMinimumClr, BOOL bStart,
			    BOOL bStrict);
PACKAGE_INTERN int	StopAndReleaseTheClr(Tcl_Interp *interp,
			    LPCWSTR logCommand, BOOL bRelease, BOOL bStrict);
PACKAGE_INTERN BOOL	CanExecuteClrCode(Tcl_Interp *interp);
PACKAGE_INTERN int	ExecuteClrMethod(HMODULE hModule,
			    ClrTclStubs *pTclStubs, Tcl_Interp *interp,
			    LPCWSTR logCommand, ClrMethodInfo *pMethodInfo,
			    LPCWSTR argument, MethodFlags methodFlags,
			    LPDWORD pReturnValue);
PACKAGE_INTERN HRESULT	GetCurrentClrAppDomainId(LPDWORD pAppDomainId);
PACKAGE_INTERN HRESULT	GetClrVersion(LPWSTR pVersion, LPDWORD pLength);
PACKAGE_INTERN HRESULT	DumpClrState(LPWSTR fileName, LONG lTclStubs,
			    HMODULE hTclModule, ClrTclStubs *pTclStubs,
			    LPWSTR pState, LPDWORD pLength);
#endif

/*
 * NOTE: This mutex is used to protect access to all static state.  This
 *       should be using the TCL_DECLARE_MUTEX macro; however, this mutex
 *       cannot be static as it is needed by multiple source code files.
 */

#if defined(TCL_THREADS)
extern Tcl_Mutex packageMutex;

/*
 * NOTE: On non-Windows, this structure is used to "simulate" mutexes that
 *       are capable of being used recursively.
 */

#if defined(USE_CORE_CLR) && !defined(_WIN32)
extern pthread_owner_t packageOwner;
#endif
#endif

#endif /* _GARUDA_DECL_H_ */
