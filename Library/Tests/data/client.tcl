###############################################################################
#
# client.tcl --
#
# Extensible Adaptable Generalized Logic Engine (Eagle)
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
# NOTE: Always skip all network connectivity and download tests contained in
#       the test script to be evaluated.
#
set no(network) true

#
# BUGFIX: If we are running in Tcl, the [isEagle] proc will not be present yet
#         and we need it now; therefore, create it if necessary.
#
if {[llength [info commands isEagle]] == 0} then {
  #
  # NOTE: This is the procedure that detects whether or not we are running in
  #       Eagle (otherwise, we are running in vanilla Tcl).  This procedure
  #       must function correctly in both Tcl and Eagle and must return
  #       non-zero only when running in Eagle.
  #
  proc isEagle {} {
    return [expr {[info exists ::tcl_platform(engine)] && \
      [string compare -nocase eagle $::tcl_platform(engine)] == 0}]
  }
}

#
# NOTE: Add the test constraint that will allow us to run the isolated client
#       socket test.
#
if {[isEagle]} then {
  #
  # NOTE: This will be processed properly after the [source] of the test
  #       prologue file (below).
  #
  lappend argv -constraints client
} else {
  #
  # NOTE: Make sure the tcltest package is available.
  #
  package require tcltest

  #
  # NOTE: Add the constraint for the isolated client socket test.
  #
  tcltest::testConstraint client 1
}

#
# NOTE: Save the existing test path, if any.  If there is no test path, unset
#       the saved test path.  Before this script returns, the original test
#       path will be restored, unsetting it if necessary.
#
if {[info exists test_path]} then {
  set saved_test_path $test_path
} else {
  unset -nocomplain saved_test_path
}

#
# NOTE: Reset the test path to point to the parent directory of the directory
#       containing this script.
#
set test_path [file normalize [file dirname [file dirname [info script]]]]

#
# NOTE: Evaluate the test suite prologue now.
#
source [file join $test_path prologue.eagle]

#
# NOTE: Prevent nested calls into the test suite prologue and epilogue file
#       from being processed.
#
set no(prologue.eagle) true
set no(epilogue.eagle) true

#
# NOTE: Evaluate the primary [socket] command test file.  Only one test should
#       actually run.
#
source [file join $test_path socket.eagle]

#
# NOTE: Cleanup our entries from the global test exclusion array.
#
unset no(epilogue.eagle)
unset no(prologue.eagle)
unset no(network)

if {[array size no] == 0} then {unset no}

#
# NOTE: Evaluate the test suite epilogue now.
#
source [file join $test_path epilogue.eagle]

#
# NOTE: Restore the saved test path, if any.  If there is no saved test path,
#       then just unset the test path.
#
if {[info exists saved_test_path]} then {
  set test_path $saved_test_path
} else {
  unset -nocomplain test_path
}

#
# NOTE: Finally, always unset the saved test path.
#
unset -nocomplain saved_test_path
