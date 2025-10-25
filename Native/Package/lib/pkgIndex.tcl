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
