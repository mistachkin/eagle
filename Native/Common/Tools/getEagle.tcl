###############################################################################
#
# getEagle.tcl --
#
# Extensible Adaptable Generalized Logic Engine (Eagle)
# Eagle Distribution File Downloader
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

namespace eval ::Eagle::Tools::GetEagle {
  #############################################################################
  #********************** TOOL VARIABLE SETUP PROCEDURE ***********************
  #############################################################################

  #
  # NOTE: This procedure sets up the default values for all configuration
  #       parameters used by this tool.  If the force argument is non-zero,
  #       any existing values will be overwritten and set back to their
  #       default values.
  #
  proc setupGetEagleVariables { force } {
    ###########################################################################
    #***************************** TOOL VARIABLES *****************************
    ###########################################################################

    #
    # NOTE: Prevent progress messages from being displayed while downloading
    #       from the Eagle web site?  By default, this is disabled.  If this
    #       is enabled, this script may appear to "hang" because no progress
    #       messages will be displayed.
    #
    variable quiet; # DEFAULT: false

    if {$force || ![info exists quiet]} then {
      set quiet false
    }

    #
    # NOTE: The base URI for the Eagle distribution web site.
    #
    variable baseUri; # DEFAULT: https://eagle.to/

    if {$force || ![info exists baseUri]} then {
      set baseUri https://eagle.to/
    }

    #
    # NOTE: The URI where the Eagle "update" file may be found.  This file will
    #       contain textual, line-oriented data describing the latest version
    #       available, on a per-vendor basis.
    #
    variable stableUri; # DEFAULT: ${baseUri}stable.txt

    if {$force || ![info exists stableUri]} then {
      set stableUri [appendArgs \
          {${baseUri}} stable.txt]
    }

    #
    # NOTE: The "root name" of the file name to be downloaded.  Normally, this
    #       will be the Eagle binary distribution; however, there are several
    #       other valid choices.
    #
    variable rootName; # DEFAULT: EagleBinary

    if {$force || ![info exists rootName]} then {
      set rootName EagleBinary
    }

    #
    # NOTE: The URI where the Eagle binary distribution may be found.  Setting
    #       this variable requires the patch level, which will not be known
    #       until later; therefore, the [subst] command will be used on the
    #       value of this variable at that point.
    #
    variable binaryUri; # DEFAULT: ${binaryBaseUri}/releases/.../...

    if {$force || ![info exists binaryUri]} then {
      set binaryUri [appendArgs \
          {${binaryBaseUri}} /releases/ {${patchLevel}} / {${rootName}} \
          {${patchLevel}} .exe]
    }

    #
    # NOTE: The regular expression pattern used to extract the download URI
    #       from the downloaded Eagle release data from "stable.txt" (see
    #       above).
    #
    variable binaryBaseUriPattern; # DEFAULT: ... {([^\t]+)} ...

    if {$force || ![info exists binaryBaseUriPattern]} then {
      set binaryBaseUriPattern [appendArgs \
          {\t} {([^\t]+)} {\t} {[0-9a-f]{32}} {\t} {[0-9a-f]{40}} {\t} \
          {[0-9a-f]{128}} {\t}]
    }

    #
    # NOTE: The regular expression pattern used to extract the patch level from
    #       the downloaded Eagle release data from "stable.txt" (see above).
    #
    variable patchLevelPattern; # DEFAULT: ... {(\d+\.\d+\.\d+\.\d+)} ...

    if {$force || ![info exists patchLevelPattern]} then {
      set patchLevelPattern [appendArgs \
          ^1 {\t} 29c6297630be05eb {\t} Eagle {\t} invariant {\t} \
          {(\d+\.\d+\.\d+\.\d+)} {\t}]
    }
  }

  #############################################################################
  #************************** TOOL STARTUP PROCEDURE **************************
  #############################################################################

  #
  # NOTE: This procedure attempts to download the latest version of the
  #       configured Eagle release package.  The directory argument is not
  #       used.  The fileName argument is the location the release package
  #       should be written.  If the fileName is an empty string, the release
  #       package will be written to the configured temporary directory.  The
  #       channel argument is an output channel where progress messages should
  #       be written.  If the channel argument is an empty string, no progress
  #       messages will be written.
  #
  proc downloadEagle { directory fileName channel } {
    global env
    variable baseUri
    variable binaryBaseUriPattern
    variable binaryUri
    variable patchLevelPattern
    variable quiet
    variable rootName
    variable stableUri

    #
    # NOTE: Figure out the final Eagle release information download URI,
    #       replacing any contained variable references as necessary using
    #       [subst].
    #
    set uri [subst -nobackslashes -nocommands $stableUri]

    #
    # NOTE: Attempt to fetch the latest Eagle release information.
    #
    set data [getFileViaHttp $uri 20 $channel $quiet -binary true]

    #
    # NOTE: Attempt to extract the patch level from the Eagle release
    #       information.  If this fails, the full URI for the Eagle
    #       binary distribution download cannot be determined.
    #
    set patchLevel [string trim [lindex [regexp -line -inline -- \
        $patchLevelPattern $data] end]]

    if {[string length $patchLevel] == 0} then {
      error "cannot determine Eagle patch level"
    }

    #
    # NOTE: Attempt to extract the base (download) URI from the Eagle
    #       release information.  If this fails, the full URI for the
    #       Eagle binary distribution download cannot be determined.
    #
    set binaryBaseUri [string trim [lindex [regexp -line -inline -- \
        $binaryBaseUriPattern $data] end]]

    if {[string length $binaryBaseUri] == 0} then {
      error "cannot determine Eagle binary base URI"
    }

    #
    # NOTE: Figure out the final Eagle binary distribution download URI,
    #       replacing any contained variable references as necessary using
    #       [subst].
    #
    set uri [subst -nobackslashes -nocommands $binaryUri]

    #
    # NOTE: Figure out where the downloaded Eagle binary distribution files
    #       are going to reside.
    #
    if {[string length $fileName] == 0} then {
      #
      # HACK: Using [file tail] to grab file name from URI.
      #
      set fileName [file join $env(TEMP) [file tail $uri]]
    }

    #
    # NOTE: Attempt to fetch the binary distribution file for Eagle.  This
    #       should be the latest available version.
    #
    set data [getFileViaHttp $uri 20 $channel $quiet -binary true]

    #
    # NOTE: Write the downloaded file data to the specified local file.
    #
    writeFile $fileName $data

    #
    # NOTE: Unless quiet mode is enabled, print the downloaded file name.
    #
    if {!$quiet} then {
      pageOut $channel [appendArgs "file: " $fileName \n]
    }

    #
    # NOTE: Return the name of the local file where the binary distribution
    #       file for the latest Eagle was saved.
    #
    return $fileName
  }

  #############################################################################
  #******************************* TOOL STARTUP *******************************
  #############################################################################

  #
  # NOTE: Next, save the tool path for later use.
  #
  if {![info exists toolPath]} then {
    set toolPath [file normalize [file dirname [info script]]]
  }

  #
  # NOTE: Add the tool path to the auto-path.
  #
  lappend ::auto_path $toolPath

  #
  # NOTE: Attempt to load the common tools package.
  #
  package require Eagle.Tools.Common

  #
  # NOTE: Attempt to import the procedures exposed by the common tools
  #       package.
  #
  namespace import \
      ::Eagle::Tools::Common::appendArgs \
      ::Eagle::Tools::Common::getFileViaHttp \
      ::Eagle::Tools::Common::pageOut \
      ::Eagle::Tools::Common::writeFile

  #
  # NOTE: First, setup the variables associated with this tool.
  #
  setupGetEagleVariables false

  #
  # NOTE: Finally, attempt to download the latest Eagle binary distribution
  #       file right now.
  #
  downloadEagle $toolPath [lindex $argv 0] stdout
}
