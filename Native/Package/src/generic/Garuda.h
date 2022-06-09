/*
 * Garuda.h -- Eagle Package for Tcl (Garuda)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#ifndef _GARUDA_H_
#define _GARUDA_H_

/*
 * WARNING: These variables are used during the Eagle loading and unloading
 *          process to help locate and execute the necessary assembly, type,
 *          and method.  If you change any of these variable names, please
 *          update the "helper.tcl" file for this package as well.  These
 *          variables are considered part of the "official public API" for
 *          this package; therefore, change them with extreme care.  Prior
 *          to being used, all of these variables will be prefixed with the
 *          package name as the namespace; therefore, all these values must
 *          begin with "::".
 */

#define VERBOSE_VAR_NAME				"::verbose"
#define LOG_COMMAND_VAR_NAME				"::logCommand"
#define NO_NORMALIZE_VAR_NAME				"::noNormalize"
#define ASSEMBLY_PATH_VAR_NAME				"::assemblyPath"
#define TYPE_NAME_VAR_NAME				"::typeName"
#define STARTUP_METHOD_VAR_NAME				"::startupMethodName"
#define CONTROL_METHOD_VAR_NAME				"::controlMethodName"
#define DETACH_METHOD_VAR_NAME				"::detachMethodName"
#define SHUTDOWN_METHOD_VAR_NAME			"::shutdownMethodName"
#define METHOD_ARGUMENTS_VAR_NAME			"::methodArguments"
#define METHOD_FLAGS_VAR_NAME				"::methodFlags"
#define LOAD_CLR_VAR_NAME				"::loadClr"
#define START_CLR_VAR_NAME				"::startClr"
#define START_BRIDGE_VAR_NAME				"::startBridge"
#define STOP_CLR_VAR_NAME				"::stopClr"
#define USE_MINIMUM_CLR_VAR_NAME			"::useMinimumClr"
#define USE_ISOLATION					"::useIsolation"
#define USE_SAFE_INTERP					"::useSafeInterp"

/*
 * NOTE: These are the public functions exported by this library.
 */

#ifndef PACKAGE_EXTERN
#define PACKAGE_EXTERN
#endif

PACKAGE_EXTERN int	Garuda_Init(Tcl_Interp *interp);
PACKAGE_EXTERN int	Garuda_SafeInit(Tcl_Interp *interp);
PACKAGE_EXTERN int	Garuda_Unload(Tcl_Interp *interp, int flags);
PACKAGE_EXTERN int	Garuda_SafeUnload(Tcl_Interp *interp, int flags);

#endif /* _GARUDA_H_ */
