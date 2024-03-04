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

package ifneeded Eagle.Auxiliary 1.0 \
    [list source [file join $dir auxiliary.eagle]]

package ifneeded Eagle.Tcl.Compatibility 1.0 \
    [list source [file join $dir compat.eagle]]

package ifneeded Eagle.CSharp 1.0 \
    [list source [file join $dir csharp.eagle]]

package ifneeded Eagle.Database 1.0 \
    [list source [file join $dir database.eagle]]

package ifneeded Eagle.Execute 1.0 \
    [list source [file join $dir exec.eagle]]

package ifneeded Eagle.File 1.0 \
    [list source [file join $dir file1.eagle]]

package ifneeded Eagle.File.Types 1.0 \
    [list source [file join $dir file2.eagle]]

package ifneeded Eagle.File.Utf8 1.0 \
    [list source [file join $dir file2u.eagle]]

package ifneeded Eagle.File.Finder 1.0 \
    [list source [file join $dir file3.eagle]]

package ifneeded Eagle.Information 1.0 \
    [list source [file join $dir info.eagle]]

package ifneeded Eagle.Library 1.0 \
    [list source [file join $dir init.eagle]]

package ifneeded Eagle.List 1.0 \
    [list source [file join $dir list.eagle]]

package ifneeded Eagle.Object 1.0 \
    [list source [file join $dir object.eagle]]

package ifneeded Eagle.Package.Toolset 1.0 \
    [list source [file join $dir pkgt.eagle]]

package ifneeded Eagle.Platform 1.0 \
    [list source [file join $dir platform.eagle]]

package ifneeded Eagle.Process 1.0 \
    [list source [file join $dir process.eagle]]

package ifneeded Eagle.Runtime.Option 1.0 \
    [list source [file join $dir runopt.eagle]]

package ifneeded Eagle.Safe 1.0 \
    [list source [file join $dir safe.eagle]]

package ifneeded Eagle.Shell 1.0 \
    [list source [file join $dir shell.eagle]]

package ifneeded Eagle.Tcl.Shim 1.0 \
    [list source [file join $dir shim.eagle]]

package ifneeded Eagle.Test 1.0 \
    [list source [file join $dir test.eagle]]

package ifneeded Eagle.Test.Log 1.0 \
    [list source [file join $dir testlog.eagle]]

package ifneeded Eagle.Unknown.Object 1.0 \
    [list source [file join $dir unkobj.eagle]]

package ifneeded Eagle.Unzip 1.0 \
    [list source [file join $dir unzip.eagle]]

package ifneeded Eagle.Update 1.0 \
    [list source [file join $dir update.eagle]]
