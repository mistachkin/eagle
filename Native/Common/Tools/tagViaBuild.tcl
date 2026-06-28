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
    # <help>
    # This procedure computes the "build" and "revision" components of a
    # version number from the current date and time, following the same
    # convention the .NET Framework uses when it auto-generates those two
    # fields.  It exists so the build tooling can synthesize an
    # ever-increasing version without being told an explicit patch level.
    #
    # How it works: the build number is the count of whole days elapsed since
    # the .NET build-number epoch of midnight on January 1st, 2000; the
    # revision number is the count of whole two-second intervals elapsed since
    # local midnight today.  Both are derived from [clock seconds] and
    # [clock scan].
    #
    # Tricky details: the epoch and midnight are computed in local time (no
    # GMT flag is used), so the values are relative to the machine's time
    # zone, exactly mirroring the .NET behavior this imitates.
    #
    # Arguments:
    #   None.
    #
    # Results:
    #   A two-element list whose first element is the build number and whose
    #   second element is the revision number, both non-negative integers.
    # </help>

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
    # <help>
    # This procedure searches upward from a starting directory for the nearest
    # ancestor that contains a Fossil "manifest" file, returning that ancestor.
    # It exists so the version tagger can locate the repository's checkout root
    # in order to read source provenance from the manifest, which is the
    # fallback used when the fossil executable itself is not usable.
    #
    # How it works: starting at the given directory it checks for a regular
    # file named "manifest"; if found, that directory is returned.  Otherwise
    # it moves to the parent directory with [file dirname] and repeats,
    # stopping when the directory name becomes empty or names a volume root (as
    # reported by [file volumes]).
    #
    # Tricky details: only a regular file (not a directory) named "manifest"
    # qualifies.  The first matching ancestor encountered while ascending is
    # returned, so the closest enclosing checkout wins.
    #
    # Arguments:
    #   directory -- The absolute directory at which to begin the upward
    #                search.
    #
    # Results:
    #   The absolute path of the nearest ancestor directory containing a
    #   "manifest" file, or the empty string if none is found before reaching a
    #   volume root.
    # </help>

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
    # <help>
    # This procedure extracts the source identifier and commit timestamp for a
    # Fossil checkout by reading its "manifest" and "manifest.uuid" files from
    # the specified directory.  It exists as the manifest-based fallback for
    # obtaining source provenance when the live fossil command (see
    # [getFossilSourceInfo]) is unavailable, and it relies on the parent
    # repository having the "manifest" setting enabled so those files are
    # written into the checkout.
    #
    # How it works: it requires both files to be present.  It reads the
    # "manifest" file and extracts the commit date from its "D" record using a
    # regular expression, then reformats that value from the manifest's
    # ISO-style form into a space-separated form suffixed with " UTC" (dropping
    # the fractional seconds).  It then reads the source ID from
    # "manifest.uuid" and validates it as a 40-to-64 character lowercase
    # hexadecimal string.
    #
    # Tricky details: the procedure is fail-safe -- if either file is missing,
    # the timestamp cannot be parsed, or the ID fails validation, it returns an
    # empty list rather than raising an error.
    #
    # Arguments:
    #   directory -- The directory containing the Fossil "manifest" and
    #                "manifest.uuid" files (typically the checkout root located
    #                by [getFossilManifestDirectory]).
    #
    # Results:
    #   A two-element list of the source ID followed by the reformatted commit
    #   timestamp, or an empty list if the information could not be obtained.
    # </help>

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
    # <help>
    # This procedure obtains the source identifier and timestamp of the current
    # Fossil checkout by running the Fossil "info" command.  It is the
    # preferred, live source of provenance for the version tagger and is tried
    # before the manifest-file fallback ([getFossilManifestInfo]).
    #
    # How it works: it runs the external "fossil info" command and matches its
    # "checkout:" output line with a regular expression to capture the checkout
    # ID and its timestamp.
    #
    # Tricky details: it is fail-safe -- the [exec] is wrapped so that if
    # Fossil is not installed or the current directory is not inside an active
    # checkout, the failure is swallowed and an empty list is returned.  It
    # relies on the fossil executable being present on the PATH.
    #
    # Arguments:
    #   None.  It operates on the process's current working directory.
    #
    # Results:
    #   A two-element list of the checkout ID followed by its timestamp, or an
    #   empty list when Fossil is unavailable or there is no active checkout.
    # </help>

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
    # <help>
    # This procedure assembles a complete four-part dotted version string of
    # the form major.minor.build.revision, where the caller supplies the major
    # and minor components and the build and revision components are derived
    # from the current date and time.  It exists to produce a full version
    # number for the resource header when no explicit patch level has been
    # provided to the build.
    #
    # How it works: it starts with the caller's major and minor numbers,
    # appends the build and revision pair returned by [getClockBuildNumber],
    # and joins all four components with periods.
    #
    # Arguments:
    #   major -- The major version component (the leading number).
    #   minor -- The minor version component (the second number).
    #
    # Results:
    #   A dotted version string with four components, for example a value like
    #   "1.0.<build>.<revision>".
    # </help>

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
    # <help>
    # This procedure ensures the Eagle script library package (Eagle.Library)
    # is loaded into the current native Tcl interpreter, adding its directory
    # to the Tcl auto-path first if necessary.  It exists because this build
    # tool, although it runs under native Tcl, reuses utility procedures
    # provided by the Eagle script library (such as [appendArgs], [readFile], and
    # [writeFile]), so that library must be available before the tagging
    # procedures run.
    #
    # How it works: it derives the project root directory by going three levels
    # up from the supplied tool path, computes the library directory as
    # lib/Eagle1.0 beneath that root, appends it to the global auto-path if it
    # is not already present, and then issues a [package require] for
    # Eagle.Library.
    #
    # Tricky details: the three-levels-up calculation assumes this tool resides
    # at a fixed depth beneath the project root (the Native common tools
    # location).  The [package require] is a no-op if the library has already
    # been loaded.
    #
    # Arguments:
    #   toolPath -- The normalized directory containing this tool, used as the
    #               anchor for locating the project root and script library.
    #
    # Results:
    #   Returns the empty string on success; its purpose is the side effect of
    #   making the Eagle library commands available.  Raises an error if the
    #   library package cannot be found or loaded.
    # </help>

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
    # <help>
    # This procedure stamps the real version number into the native resource
    # header file (src/generic/rcVersion.h beneath the given project path),
    # replacing the placeholder version that is checked into source control.
    # It exists so the compiled native libraries carry an accurate, build-time
    # version in their Windows resource information.
    #
    # How it works: after ensuring the Eagle library is loaded (via
    # [requireEagleLibrary]), it reads the header file and normalizes it to
    # Unix line endings.  It builds a regular expression that matches the dummy
    # "major.minor.X.X" version (using the caller's major and minor as fixed
    # anchors), and determines the replacement version: the PATCHLEVEL
    # environment variable when it is set, otherwise a clock-derived version
    # from [getVersionViaClock].  It then performs the substitution twice -- in
    # both the period-delimited form and the comma-delimited form that Windows
    # resource files also use -- and, only if at least one replacement was
    # made, rewrites the file converted back to DOS (CRLF) line endings.
    #
    # Tricky details: the file is left untouched when nothing matched, avoiding
    # spurious rewrites.  The dual period/comma substitution is required
    # because resource version fields appear in both notations.  Only the
    # build and revision portions are variable; major and minor are matched
    # literally.
    #
    # Arguments:
    #   toolPath -- The tool directory, forwarded to [requireEagleLibrary].
    #   path     -- The native project directory whose src/generic/rcVersion.h
    #               is to be updated.
    #   major    -- The major version component used to anchor the match.
    #   minor    -- The minor version component used to anchor the match.
    #
    # Results:
    #   Returns the empty string; its purpose is the side effect of updating
    #   rcVersion.h in place when a matching placeholder is present.
    # </help>

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
    # <help>
    # This procedure stamps the Fossil source identifier and commit timestamp
    # into the native package version header file (src/generic/pkgVersion.h
    # beneath the given project path), replacing the placeholder values checked
    # into source control.  It exists so the compiled native package records
    # the exact source revision it was built from.
    #
    # How it works: after ensuring the Eagle library is loaded (via
    # [requireEagleLibrary]), it reads the header file and normalizes it to
    # Unix line endings.  It then obtains the source ID and timestamp using a
    # two-tier strategy: first the live checkout via [getFossilSourceInfo], and
    # if that yields nothing, the manifest files located by
    # [getFossilManifestDirectory] and parsed by [getFossilManifestInfo].  When
    # provenance is available, it replaces the SOURCE_ID and SOURCE_TIMESTAMP
    # string values via regular-expression substitution and, only if a
    # replacement was made, rewrites the file converted back to DOS (CRLF) line
    # endings.
    #
    # Tricky details: if neither the live checkout nor the manifest files yield
    # the source information, the header is left completely unchanged (the
    # placeholders remain).  Likewise the file is not rewritten unless a
    # substitution actually occurred.
    #
    # Arguments:
    #   toolPath -- The tool directory, forwarded to [requireEagleLibrary].
    #   path     -- The native project directory whose src/generic/pkgVersion.h
    #               is to be updated; it is also the starting point for the
    #               manifest-file search.
    #
    # Results:
    #   Returns the empty string; its purpose is the side effect of updating
    #   pkgVersion.h in place when source provenance can be determined and a
    #   matching placeholder is present.
    # </help>

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
