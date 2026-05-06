###############################################################################
#
# garuda.tcl -- Eagle Package for Tcl (Garuda)
#
# Extensible Adaptable Generalized Logic Engine (Eagle)
# Package Loading Helper File (Primary)
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
# What this file does.
#
# This is the "primary" loader script -- sourced by pkgIndex.tcl for the
# [package require Garuda] / [package require GarudaDotNetFx] / [package
# require GarudaDotNetCore] cases.  Its job is to set per-package
# configuration defaults appropriate for "I want Garuda fully loaded,
# CLR started, and the Eagle bridge connected, in one [package require]
# call".
#
# The mechanism is uniform across this file, dotnet.tcl, and helper.tcl:
# each loader script declares its preferred default values for a
# documented set of ::Garuda::* configuration variables, then sources
# helper.tcl which reads those variables and acts on them.  Variables
# that a downstream embedder pre-set BEFORE the [package require] are
# preserved unchanged (the [info exists] guards see the existing value
# and skip the assignment), letting embedders override anything they
# care about without forking the loader.
#
# Variables this file sets (default values shown):
#
#   setupAndLoad   true   Load Garuda.dll AND start the CLR AND start
#                          the bridge.  The "do everything" flag.
#
# Compare with dotnet.tcl, which sets setupAndLoad=true but
# startClr=false and startBridge=false -- a more conservative "load only"
# defaults profile for embedders that want to defer CLR commitment.
#
# Other variables (useCoreClr, noNormalize, methodFlags, ...) are NOT
# touched here and inherit helper.tcl's own defaults -- those are the
# more numerous configuration knobs that helper.tcl documents.
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
  # Defined here AND in helper.tcl because this file may run before
  # helper.tcl sources, and the package-startup section below uses it
  # immediately to discover packagePath.  Keep all copies in sync.
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

  #
  # Set this file's preferred defaults into the ::Garuda namespace.
  # Each variable uses the "guarded assignment" pattern:
  #
  #     variable foo
  #     if {![info exists foo]} then { set foo <our-default> }
  #
  # which preserves any value an embedder pre-set before [package require]
  # while still installing a default for the unconfigured case.  See the
  # file-level comment block for the variable inventory and how this
  # file's defaults differ from dotnet.tcl's.
  #
  proc setupGarudaVariables { directory } {
    ###########################################################################
    #************* NATIVE PACKAGE GENERAL CONFIGURATION VARIABLES *************
    ###########################################################################

    #
    # NOTE: For this package, attempt to setup and load the extension right
    #       now, start the (Core?)CLR, and connect the bridge (to Eagle).
    #
    variable setupAndLoad; # DEFAULT: true

    if {![info exists setupAndLoad]} then {
      set setupAndLoad true
    }
  }

  #############################################################################
  #***************************** PACKAGE STARTUP ******************************
  #############################################################################

  #
  # Three-step startup, all running in ::Garuda's namespace context:
  #
  #   1. Pin packagePath to this file's directory.  [info script] gives
  #      the .tcl path Tcl is currently sourcing; [file dirname] yields
  #      the lib/ directory which contains all of Garuda's auxiliary
  #      files (helper.tcl, the .dll/.so, the runtimeconfig.json, etc).
  #      Forced canonicalization (`force=true`) -- the package directory
  #      MUST be the resolved path so subsequent [file join] calls
  #      land where the package files actually live, even if
  #      noNormalize is set globally.
  #
  #   2. Apply this file's configuration defaults via setupGarudaVariables.
  #      No-op for any variable an embedder pre-set.
  #
  #   3. Source helper.tcl with [uplevel 1] so its top-level code runs in
  #      the same namespace context as this file (::Garuda) -- without
  #      uplevel it would source into setupGarudaVariables's stack frame,
  #      which would scope its variables incorrectly.  The same uplevel
  #      pattern appears in dotnet.tcl.
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
  setupGarudaVariables $packagePath

  #
  # NOTE: Now that the startup parameters have been overridden, call into
  #       the normal package loading script.
  #
  uplevel 1 [list source [file join $packagePath helper.tcl]]
}
