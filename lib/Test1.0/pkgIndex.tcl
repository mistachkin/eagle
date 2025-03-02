###############################################################################
#
# pkgIndex.tcl --
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

if {![package vsatisfies [package provide Tcl] 8.4]} {return}
if {[string length [package provide Eagle]] > 0} then {return}

package ifneeded Eagle.Test.Constraints 1.0 [list source [file join $dir \
    constraints.eagle]]
