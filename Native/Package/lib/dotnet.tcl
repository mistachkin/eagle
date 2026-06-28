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
# What this file does.
#
# This is the "secondary" loader script -- sourced by pkgIndex.tcl when
# [package require dotnet] is called.  It is the conservative sibling of
# garuda.tcl: it loads Garuda.dll but does NOT start the CLR or build
# the bridge.  Embedders that want fine-grained control over CLR
# lifecycle (e.g. delaying the cost until they know they need it, or
# loading multiple assemblies before bridge startup) come in this way
# and drive the rest manually via [garuda clrstart] / [garuda startup]
# sub-commands.
#
# Variables this file sets (default values shown):
#
#   setupAndLoad   true    Load the .dll.
#   startClr       false   Do NOT call ICLRRuntimeHost::Start (or the
#                          CoreCLR equivalent) at load time -- that
#                          stays the embedder's call.
#   startBridge    false   Do NOT invoke the managed startup method.
#
# Compare with garuda.tcl, which sets only setupAndLoad=true and lets
# helper.tcl's own defaults turn on startClr/startBridge.  The
# difference is the embedder mental model -- "I want everything wired
# up now" (garuda.tcl) vs "I want to drive the lifecycle myself"
# (dotnet.tcl).
#
# The remaining structure (fileNormalize / setupDotnetVariables /
# 3-step startup) is intentionally identical in shape to garuda.tcl;
# see that file's comments for the per-step explanation.  This file
# diverges only in the defaults block.
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
  # Apply this file's "load-only, defer-everything-else" defaults.  Same
  # guarded-assignment pattern as setupGarudaVariables; see garuda.tcl
  # for why we use the [info exists] guard rather than unconditional
  # assignment.
  #
  proc setupDotnetVariables { directory } {
    ###########################################################################
    #************* NATIVE PACKAGE GENERAL CONFIGURATION VARIABLES *************
    ###########################################################################

    #
    # NOTE: For this package, attempt to setup and load the extension right
    #       now.
    #
    variable setupAndLoad; # DEFAULT: true

    if {![info exists setupAndLoad]} then {
      set setupAndLoad true
    }

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
