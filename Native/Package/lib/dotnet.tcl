###############################################################################
#
# dotnet.tcl -- Eagle Package for Tcl (Garuda)
#
# Extensible Adaptable Generalized Logic Engine (Eagle)
# Package Loading Helper File (Secondary)
#
# Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
#
# See the file "license.terms" for information on usage and redistribution of
# this file, and for a DISCLAIMER OF ALL WARRANTIES.
#
# RCS: @(#) $Id: $
#
###############################################################################

#
# NOTE: This script file uses features that are only present in Tcl 8.4 or
#       higher (e.g. the "eq" operator for [expr], etc).
#
if {![package vsatisfies [package provide Tcl] 8.4]} then {
  error "need Tcl 8.4 or higher"
}

#
# NOTE: This script file uses features that are not available or not needed
#       in Eagle (e.g. the "http" and "tls" packages, etc).
#
if {[catch {package present Eagle}] == 0} then {
  error "need native Tcl"
}

###############################################################################

namespace eval ::Garuda {
  #############################################################################
  #**************************** SHARED PROCEDURES *****************************
  #############################################################################

  #
  # NOTE: Also defined in and used by "helper.tcl".
  #
  proc fileNormalize { path {force false} } {
    variable noNormalize

    if {$force || !$noNormalize} then {
      return [file normalize $path]
    }

    return $path
  }

  #############################################################################
  #********************* PACKAGE VARIABLE SETUP PROCEDURE *********************
  #############################################################################

  proc setupDotnetVariables { directory } {
    ###########################################################################
    #************* NATIVE PACKAGE GENERAL CONFIGURATION VARIABLES *************
    ###########################################################################

    #
    # NOTE: For this package, the CLR is not started (by default).  Later,
    #       the [garuda clrstart] sub-command can be used to start the CLR.
    #
    variable startClr; # DEFAULT: false

    if {![info exists startClr]} then {
      set startClr false
    }

    #
    # NOTE: For this package, the bridge is not built (by default).  Later,
    #       the [garuda startup] sub-command can be used to build the bridge.
    #
    variable startBridge; # DEFAULT: false

    if {![info exists startBridge]} then {
      set startBridge false
    }
  }

  #############################################################################
  #***************************** PACKAGE STARTUP ******************************
  #############################################################################

  #
  # NOTE: Next, save the package path for later use.
  #
  variable packagePath

  if {![info exists packagePath]} then {
    set packagePath [fileNormalize [file dirname [info script]] true]
  }

  #
  # NOTE: Next, setup the script variables associated with this package.
  #
  setupDotnetVariables $packagePath

  #
  # NOTE: Now that the startup parameters have been overridden, call into
  #       the normal package loading script.
  #
  uplevel 1 [list source [file join $packagePath helper.tcl]]
}
