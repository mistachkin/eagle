###############################################################################
#
# tagViaBuild.tcl --
#
# Extensible Adaptable Generalized Logic Engine (Eagle)
# Version Tag Tool
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

namespace eval ::Eagle::Tools::TagViaBuild {
  #############################################################################
  #***************************** TOOL PROCEDURES ******************************
  #############################################################################

  proc getClockBuildNumber {} {
    #
    # NOTE: What time is it, in seconds since the epoch, now?
    #
    set now [clock seconds]

    #
    # NOTE: What time was it, in seconds since the epoch, at midnight on
    #       January 1st, 2000 (i.e. the .NET Framework build number epoch)?
    #
    set epoch [clock scan "2000-01-01 00:00:00"]

    #
    # NOTE: What is the date today in "yyyy-mm-dd" format?
    #
    set yyyy_mm_dd [clock format $now -format {%Y-%m-%d}]

    #
    # NOTE: What time was it, in seconds since the epoch, at midnight today?
    #
    set midnight [clock scan [appendArgs $yyyy_mm_dd " 00:00:00"]]

    #
    # NOTE: How many whole days have elapsed since January 1st, 2000?
    #
    set build [expr {($now - $epoch) / (3600 * 24)}]

    #
    # NOTE: How many whole two second intervals have elapsed since midnight
    #       today?
    #
    set revision [expr {($now - $midnight) / 2}]

    #
    # NOTE: Return the calculated build and revision numbers to the caller.
    #
    return [list $build $revision]
  }

  proc getFossilManifestDirectory { directory } {
    #
    # NOTE: Keep going until the directory name is empty -OR- represents the
    #       root of the associated volume.
    #
    while {[string length $directory] > 0 && \
        [lsearch -exact [file volumes] $directory] == -1} {
      #
      # NOTE: Does this directory have the "manifest" file?
      #
      if {[file exists [file join $directory manifest]] && \
          [file isfile [file join $directory manifest]]} then {
        #
        # NOTE: Return the directory containing the "manifest" file.
        #
        return $directory
      }

      #
      # NOTE: Keep going up the directory tree...
      #
      set directory [file dirname $directory]
    }

    #
    # NOTE: The "manifest" file was not found, return nothing.
    #
    return ""
  }

  proc getFossilManifestInfo { directory } {
    #
    # NOTE: Verify that the "manifest" file exists in the directory.  If not,
    #       return nothing.
    #
    set fileName(1) [file join $directory manifest]

    if {![file exists $fileName(1)]} then {
      return [list]
    }

    #
    # NOTE: Also verify that the "manifest.uuid" file exists in the directory.
    #       If not, return nothing.
    #
    set fileName(2) [file join $directory manifest.uuid]

    if {![file exists $fileName(2)]} then {
      return [list]
    }

    #
    # NOTE: Read all the data out of the "manifest" file.
    #
    set data [readFile $fileName(1)]

    #
    # NOTE: Setup the regular expression pattern used to extract the source
    #       timestamp from the "manifest" file.
    #
    set pattern(1) {^D (\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})\.\d{3}$}

    #
    # NOTE: Attempt to extract the source timestamp from the "manifest" file
    #       data.  If that fails, return nothing.
    #
    if {![regexp -line -- $pattern(1) $data dummy timeStamp]} then {
      return [list]
    }

    #
    # NOTE: Transform the source timestamp into the expected format.  Here
    #       is an example:
    #
    #       "D 2012-12-13T11:52:56" --> "2012-12-13 11:52:56 UTC"
    #
    set timeStamp [appendArgs [string map [list T " "] $timeStamp] " UTC"]

    #
    # NOTE: Read the source ID from the "manifest.uuid" file and then attempt
    #       to validate it.  If that fails, return nothing.
    #
    set id [string trim [readFile $fileName(2)]]

    #
    # NOTE: Setup the regular expression pattern used to validate the source
    #       ID.
    #
    set pattern(2) {^[0-9a-f]{40,64}$}

    if {![regexp -line -- $pattern(2) $id]} then {
      return [list]
    }

    return [list $id $timeStamp]
  }

  proc getFossilSourceInfo {} {
    #
    # NOTE: Build the pattern used to match (and extract) the source ID and
    #       timestamp information from the output of [exec]'ing the Fossil
    #       "info" command.
    #
    set pattern {^checkout:\s+([^\s]+)\s+(.*)\s+}

    #
    # NOTE: Query the source identifier from Fossil, if available.  If we are
    #       not within an active checkout, this will fail, and this procedure
    #       will simply return an empty string.
    #
    if {[catch {set exec [exec -- fossil info]}] == 0 && \
        [regexp -line -- $pattern $exec dummy id timeStamp]} then {
      #
      # NOTE: Apparently, Fossil is available, there is an active checkout,
      #       and we were able to extract the necessary information from the
      #       Fossil "info" command.
      #
      return [list $id $timeStamp]
    }

    return [list]
  }

  proc getVersionViaClock { major minor } {
    #
    # NOTE: First, use the major and minor version numbers provided by the
    #       caller.
    #
    set result [list $major $minor]

    #
    # NOTE: Next, append the build and revision numbers based on the current
    #       date and time.
    #
    eval lappend result [getClockBuildNumber]

    #
    # NOTE: Finally, join the components of the version number with periods
    #       and return it.
    #
    return [join $result .]
  }

  proc requireEagleLibrary { toolPath } {
    #
    # NOTE: Reference the Tcl auto-path now because we need to read, and
    #       possibly modify it, below.
    #
    global auto_path

    #
    # NOTE: Figure out the location for the root of the project working
    #       directory.
    #
    set rootPath [file dirname [file dirname [file dirname $toolPath]]]

    #
    # NOTE: Figure out the location of the Eagle script library files.
    #
    set libPath [file join $rootPath lib Eagle1.0]

    #
    # NOTE: If the Eagle script library directory is not yet included in
    #       the Tcl auto-path, add it now.
    #
    if {[lsearch -exact $auto_path $libPath] == -1} then {
      lappend auto_path $libPath
    }

    #
    # NOTE: Attempt to require the Eagle script library.  If it has already
    #       been loaded, this will be a no-op.
    #
    package require Eagle.Library
  }

  #############################################################################
  #************************** TOOL STARTUP PROCEDURE **************************
  #############################################################################

  proc tagRcVersion { toolPath path major minor } {
    #
    # NOTE: Reference the Tcl environment array now because we need to read
    #       it, below.
    #
    global env

    #
    # NOTE: Attempt to require the Eagle library package now.
    #
    requireEagleLibrary $toolPath

    #
    # NOTE: Figure out the location of the resource header file.
    #
    set fileName [file join $path src generic rcVersion.h]

    #
    # NOTE: Read file and normalize data to Unix line-endings.
    #
    set data [string map [list \r\n \n] [readFile $fileName]]; # Unix

    #
    # NOTE: Build the pattern to match the existing dummy version in the
    #       resource header file.
    #
    set pattern [string map \
        [list . \\.] [appendArgs $major . $minor .\\d+.\\d+]]

    #
    # NOTE: Build the replacement string that contains the actual major,
    #       minor, build, and revision numbers corresponding to the current
    #       date and time.
    #
    if {[info exists env(PATCHLEVEL)]} then {
      set subSpec $env(PATCHLEVEL)
    } else {
      set subSpec [getVersionViaClock $major $minor]
    }

    #
    # NOTE: Initially, no replacements have been made in the file data.
    #
    set count 0

    #
    # NOTE: Perform the replacements for the resource version, delimited by
    #       both periods and commas.
    #
    incr count [regsub -all -nocase -- $pattern $data $subSpec data]

    incr count [regsub -all -nocase -- [string map [list \\. ,] $pattern] \
        $data [string map [list . ,] $subSpec] data]

    #
    # NOTE: If we actually replaced anything, we need to write back to the
    #       original file; otherwise, leave it alone.
    #
    if {$count > 0} then {
      #
      # NOTE: Normalize data to DOS line-endings and rewrite file.
      #
      writeFile $fileName [string map [list \n \r\n] $data]; # DOS.
    }
  }

  proc tagPkgVersion { toolPath path } {
    #
    # NOTE: Attempt to require the Eagle library package now.
    #
    requireEagleLibrary $toolPath

    #
    # NOTE: Figure out the location of the package version header file.
    #
    set fileName [file join $path src generic pkgVersion.h]

    #
    # NOTE: Read file and normalize data to Unix line-endings.
    #
    set data [string map [list \r\n \n] [readFile $fileName]]; # Unix

    #
    # NOTE: Attempt to obtain the source ID and timestamp from the Fossil
    #       checkout information.  This assumes that the current directory
    #       is within a checkout for the parent repository.
    #
    foreach {id timeStamp} [getFossilSourceInfo] break

    #
    # NOTE: If querying the source ID and timestamp via the checkout failed
    #       (perhaps becase this directory is not within a checkout), search
    #       for the manifest files instead.  This assumes that the "manifest"
    #       setting is enabled for the parent repository.
    #
    if {![info exists id] || ![info exists timeStamp]} then {
      #
      # NOTE: Search for the manifest files in all the directories up to the
      #       root of the volume.
      #
      set directory [getFossilManifestDirectory $path]

      #
      # NOTE: Make sure the search for the manifest files succeeded.
      #
      if {[string length $directory] > 0} then {
        #
        # NOTE: Attempt to extract the source ID and timestamp from the
        #       manifest files.
        #
        foreach {id timeStamp} [getFossilManifestInfo $directory] break
      }
    }

    #
    # NOTE: Make sure fetching the source ID and timestamp succeeded.
    #
    if {[info exists id] && [info exists timeStamp]} then {
      #
      # NOTE: Build the patterns to match the existing dummy source ID and
      #       timestamp in the package header file.
      #
      set pattern(1) {SOURCE_ID\t\t".*?"}
      set pattern(2) {SOURCE_TIMESTAMP\t".*?"}

      #
      # NOTE: Build the replacement strings that contain the actual source
      #       ID and timestamp.
      #
      set subSpec1 [appendArgs SOURCE_ID\t\t \" $id \"]
      set subSpec2 [appendArgs SOURCE_TIMESTAMP\t \" $timeStamp \"]

      #
      # NOTE: Initially, no replacements have been made in the file data.
      #
      set count 0

      #
      # NOTE: Perform the replacements for the source ID and timestamp.
      #
      incr count [regsub -nocase -- $pattern(1) $data $subSpec1 data]
      incr count [regsub -nocase -- $pattern(2) $data $subSpec2 data]

      #
      # NOTE: If we actually replaced anything, we need to write back to the
      #       original file; otherwise, leave it alone.
      #
      if {$count > 0} then {
        #
        # NOTE: Normalize data to DOS line-endings and rewrite file.
        #
        writeFile $fileName [string map [list \n \r\n] $data]; # DOS.
      }
    }
  }

  #############################################################################
  #******************************* TOOL STARTUP *******************************
  #############################################################################

  #
  # NOTE: Save the script path for later use, if necessary.
  #
  if {![info exists toolPath]} then {
    set toolPath [file normalize [file dirname [info script]]]
  }

  #
  # NOTE: Figure out the path where the project source code should be
  #       located, if necessary.
  #
  if {![info exists path]} then {
    set path [expr {[info exists argv] && [llength $argv] > 0 ? \
        [file normalize [lindex $argv 0]] : $toolPath}]
  }

  #
  # NOTE: Attempt to tag both the resource and package headers now.
  #
  tagRcVersion $toolPath $path 1 0; tagPkgVersion $toolPath $path
}
