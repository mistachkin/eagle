###############################################################################
#
# pkgIndex.tcl -- Eagle Package for Tcl (Garuda)
#
# Extensible Adaptable Generalized Logic Engine (Eagle)
# Package Index File
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
# Tcl's [package require] machinery reads this file once per `auto_path`
# directory at startup.  This file's job is to register the package names
# Garuda exposes WITHOUT actually loading anything yet -- the [source] call
# only fires when an embedder later calls [package require <name>], and
# only the variant they asked for runs.
#
# Five package names are registered, each tied to a different "what should
# happen at load time" recipe:
#
#   GarudaHelper       Source helper.tcl directly.  Variables-only setup;
#                      DOES NOT load Garuda.dll.  Used by tooling that
#                      wants to inspect Garuda's runtime-detection logic
#                      without committing to a CLR load.
#
#   dotnet             Source dotnet.tcl, which sets defaults that
#                      configure helper.tcl for "load DLL but defer CLR
#                      start and bridge startup".  Embedders later call
#                      [garuda clrstart] / [garuda startup] manually.
#                      Auto-detects the runtime (CoreCLR vs .NET
#                      Framework) based on what's installed.
#
#   Garuda             Source garuda.tcl, which sets defaults for
#                      "load + start + bridge in one step".  This is the
#                      most common entry point for embedders that want
#                      Eagle scripting available immediately.
#                      Auto-detects the runtime.
#
#   GarudaDotNetFx     Same as Garuda but pre-pins useCoreClr=false,
#                      forcing the .NET Framework runtime (Win32-only).
#                      Used when an embedder needs real AppDomain support
#                      or assemblies built for .NET Framework specifically.
#
#   GarudaDotNetCore   Same as Garuda but pre-pins useCoreClr=true,
#                      forcing the .NET (Core) runtime.  Cross-platform;
#                      required on Linux/macOS; preferred on modern
#                      Windows for new code.
#
# The two early returns at the top are intentional gates:
#
#   * Tcl 8.4 minimum:  Garuda uses [expr] "eq" / "ne" operators and other
#                       8.4-and-later features.  Older interpreters get
#                       silent return so [package require] reports
#                       "package not found" (clearer than a load-time
#                       parse error).
#
#   * Eagle present:    If we're already running INSIDE Eagle (the managed
#                       interpreter that Garuda exists to bridge to),
#                       loading this package would create an infinite
#                       loop.  Same silent return pattern.
#
# The string-map dance in GarudaDotNetFx / GarudaDotNetCore is the way to
# embed the directory path inside a literal script body that gets stored
# by [package ifneeded] for deferred evaluation -- `$dir` is only valid in
# this file's scope, so we substitute its value into the script text now
# and let the resulting text run later in its own namespace.
#
###############################################################################

if {![package vsatisfies [package provide Tcl] 8.4]} then {return}
if {[string length [package provide Eagle]] > 0} then {return}

package ifneeded GarudaHelper 1.0 \
    [list source [file join $dir helper.tcl]]; # NOTE: Skip extension load.

package ifneeded dotnet 1.0 \
    [list source [file join $dir dotnet.tcl]]; # NOTE: Auto-detect runtime.

package ifneeded Garuda 1.0 \
    [list source [file join $dir garuda.tcl]]; # NOTE: Auto-detect runtime.

package ifneeded GarudaDotNetFx 1.0 \
    [string map [list %dir% $dir] [list namespace eval ::Garuda {
      variable useCoreClr false
      uplevel 1 {source [file join {%dir%} garuda.tcl]}
    }]]; # NOTE: Force use of .NET Framework runtime and load extension.

package ifneeded GarudaDotNetCore 1.0 \
    [string map [list %dir% $dir] [list namespace eval ::Garuda {
      variable useCoreClr true
      uplevel 1 {source [file join {%dir%} garuda.tcl]}
    }]]; # NOTE: Force use of .NET (Core?) runtime and load extension.
