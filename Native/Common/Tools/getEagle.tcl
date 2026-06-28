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
    # <help>
    # This procedure establishes the default values for every configuration
    # variable used by the getEagle distribution downloader tool, storing them
    # as variables in this tool's namespace.  It exists to keep all tunable
    # settings (the web site base URI, the "stable.txt" release-information
    # URI, the binary distribution root name and URI template, the two regular
    # expression patterns used to parse the release information, and the quiet
    # flag) in one place with sensible out-of-the-box values, while still
    # allowing a caller to override any of them beforehand.
    #
    # How it works: each variable is assigned only when the force argument is
    # true OR the variable does not already exist.  This "set only if missing"
    # idiom means a caller may pre-set any variable to a custom value and have
    # it preserved on a non-forced call, whereas a forced call deliberately
    # discards any customizations and restores the documented defaults.
    #
    # Tricky details: the stableUri and binaryUri defaults intentionally
    # contain unevaluated dollar-brace placeholders (for example a baseUri,
    # binaryBaseUri, patchLevel, and rootName reference).  These are NOT
    # expanded here because the patch level and binary base URI are not known
    # until the release information has been downloaded; [downloadEagle]
    # expands them later with [subst].  The patchLevelPattern is pinned to a
    # specific vendor line in the tab-delimited release file (record id 1, a
    # specific public key token, the Eagle name, and the invariant culture), so
    # it only matches that vendor's official entry.
    #
    # Arguments:
    #   force -- A boolean.  When non-zero, every configuration variable is
    #            overwritten with its default value; when zero, only variables
    #            that do not already exist are assigned.
    #
    # Results:
    #   Returns the empty string.  Its purpose is the side effect of populating
    #   the tool's namespace configuration variables.
    # </help>

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
    variable baseUri; # DEFAULT: https://urn.to/r/eagle

    if {$force || ![info exists baseUri]} then {
      set baseUri https://urn.to/r/eagle
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
    # <help>
    # This procedure downloads the latest stable Eagle binary distribution and
    # saves it to a local file.  It is the main entry point of the getEagle
    # tool and exists so that a native Tcl environment (this script refuses to
    # run under Eagle) can bootstrap a current Eagle release without manual
    # intervention, for example as part of a build or setup step.
    #
    # How it works: it first expands the configured stable-release URI with
    # [subst] and fetches that release-information file over HTTP(S) using the
    # shared [getFileViaHttp] helper.  It then extracts the latest patch level
    # and the binary base URI from that tab-delimited data using the two
    # configured regular expression patterns, erroring out if either cannot be
    # determined.  With the patch level and base URI now known, it expands the
    # binary distribution URI template (again via [subst]) and downloads that
    # file.  Finally it writes the downloaded bytes to the destination with
    # [writeFile] and returns the local file name.
    #
    # Tricky and security details: the directory argument is accepted for
    # signature consistency with other tool startup procedures but is not used.
    # When fileName is empty, the destination is the per-user temporary
    # directory (from the TEMP environment variable) joined with the file name
    # taken from the tail of the download URI.  Both transfers are performed in
    # binary mode.  Transport security is delegated entirely to
    # [getFileViaHttp] (which upgrades to and requires TLS); this procedure
    # performs NO signature or checksum verification of the downloaded
    # distribution itself, so the integrity of the result rests on the secure
    # transport and on the trustworthiness of the configured release-info and
    # download URIs.  Progress reporting is suppressed when the configured
    # quiet flag is set, which can make a large download appear to hang.
    #
    # Arguments:
    #   directory -- Unused; present only for a uniform tool startup signature.
    #   fileName  -- The local path to write the distribution to.  When empty,
    #                a name is derived automatically under the temporary
    #                directory.
    #   channel   -- An output channel for progress messages.  When empty, no
    #                progress messages are written.
    #
    # Results:
    #   Returns the local file name where the downloaded binary distribution
    #   was saved.  Raises an error if the patch level or the binary base URI
    #   cannot be extracted from the release information, or if any download or
    #   file write fails.
    # </help>

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
