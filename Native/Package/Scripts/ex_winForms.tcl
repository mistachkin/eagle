###############################################################################
#
# ex_winForms.tcl --
#
# Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
#
# See the file "license.terms" for information on usage and redistribution of
# this file, and for a DISCLAIMER OF ALL WARRANTIES.
#
# RCS: @(#) $Id: $
#
###############################################################################

package require Tk
package require Garuda

wm withdraw .

if {![info exists i]} then { set i 0 }; incr i

set toplevel [toplevel .example$i]

wm title $toplevel "Garuda Example (TkWindow #$i)"
wm geometry $toplevel 350x100

bind $toplevel <F2> {console show}

set script [string map [list %i% $i] {
  #
  # NOTE: This script can use any of the commands provided by
  #       Eagle (e.g. [object invoke] to invoke .NET Framework
  #       objects).
  #
  proc handleClickEvent { sender e } {
    set title "About Garuda Example #%i%"

    if {[tcl ready]} then {
      msgBox [appendArgs "Tcl version is: " \
          [tcl eval [tcl primary] info patchlevel] \n \
          "Eagle version is: " [info engine patchlevel]] $title
    } else {
      msgBox "Tcl is not ready." $title
    }
  }

  object load -import System.Windows.Forms
  interp alias {} msgBox {} object invoke MessageBox Show

  set form [object create -alias Form]

  $form Width 350; $form Height 100
  $form Text "Garuda Example (WinForm #%i%)"
  $form Show

  set button [object create -alias Button]

  $button Left [expr {([$form ClientSize.Width] - [$button Width]) / 2}]
  $button Top [expr {([$form ClientSize.Height] - [$button Height]) / 2}]

  $button Text "Click Here"
  $button add_Click handleClickEvent

  $form Controls.Add $button
}]

set button [button $toplevel.run -text "Click Here" \
    -command [list eagle $script]]

pack $button -padx 20 -pady 20 -ipadx 10 -ipady 10
