###############################################################################
#
# helper.tcl -- Eagle Package for Tcl (Garuda)
#
# Extensible Adaptable Generalized Logic Engine (Eagle)
# Package Loading Helper File (Auxiliary)
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
# What this file does (architectural overview).
#
# helper.tcl is the engine room of Garuda's load-time orchestration.  It
# runs after one of three thin loader scripts (garuda.tcl, dotnet.tcl, or
# the [package require GarudaHelper] direct path) has set its preferred
# defaults into the ::Garuda namespace, and it is responsible for taking
# the user from "I just typed [package require ...]" to "the .dll is
# loaded, the right CLR is chosen, the assembly is found, the bridge is
# running, and [object] is registered" -- without ever asking the
# embedder questions, because there is no place to ask.
#
# Everything here lives in the ::Garuda namespace.  The file is split
# into six lettered sections by the banner comments below:
#
#   1. SHARED PROCEDURES        Tiny utilities that other Tcl files in
#                                this package (garuda.tcl, dotnet.tcl)
#                                also include -- kept short so they can
#                                be duplicated elsewhere without drift.
#   2. UTILITY PROCEDURES       The bulk of the file.  Everything from
#                                file-system probes to package-version
#                                comparison to runtime-detection logic
#                                to assembly-search algorithms lives
#                                here.  Read top-down; each proc
#                                generally calls only earlier procs.
#   3. PACKAGE HELPER PROCS     Glue: haveEagle, etc.
#   4. PACKAGE VARIABLE SETUP   setupHelperVariables -- the ~600-line
#                                proc that materializes the entire
#                                configuration namespace.  Six
#                                sub-banners inside.
#   5. PACKAGE STARTUP PROCS    setupForCoreClr, setupAndLoad -- the
#                                two procs that drive everything from
#                                the top.
#   6. PACKAGE STARTUP          Top-level invocation of setupAndLoad.
#
# The runtime-decision tree (high-level).
#
#   1. Detect what runtimes are available on this host
#      (attemptToDetectRuntimes -- looks for hostfxr / mscoree / etc).
#   2. Decide which runtime to use (shouldUseCoreClr).  Inputs:
#      embedder's pre-set ::Garuda::useCoreClr override, presence of
#      either runtime, platform constraints (POSIX -> CoreCLR forced).
#   3. Build the assembly search-path list (getLibraryPathList).
#      Multiple sources fan in here: the package's lib/ directory,
#      Tcl's auto_path, environment variables, Win32 registry on
#      Windows, "Program Files" subdirectories.
#   4. Probe each search-path entry (probeAssemblyFile) for the
#      configured Eagle.dll filename, returning the first match.
#   5. For CoreCLR, write the runtimeconfig.json (getCoreClr-
#      RuntimeConfiguration / writeCoreClrRuntimeConfiguration)
#      that hostfxr requires.
#   6. Hand the assembly path + runtime config path to the C side
#      via package variables that GetClrConfigInfo will read.
#   7. [load] Garuda.dll, which calls Garuda_Init, which reads the
#      configuration variables, calls Tcl_CreateObjCommand to
#      register [object], and (if startBridge is set) invokes the
#      managed startup method to bring up the Eagle bridge.
#
# Almost every proc in this file emits log output via maybeLogViaCommand,
# which routes through the ::Garuda::logCommand variable (default
# "tclLog").  Embedders that want to silence Garuda's chatter set
# logCommand to an empty string or to ::Garuda::noLog.  Embedders
# debugging a load failure set verbose=true and logCommand to a
# capturing proc.
#
# Concurrency note: this file's code runs at [package require] time,
# which is single-threaded by Tcl convention.  None of the procs here
# need to be re-entrant or thread-safe -- once setupAndLoad has
# returned and Garuda.dll is loaded, the C side takes over and uses
# its own packageMutex for cross-thread state.
#
# Search hint: the configuration variables that all the path-probing
# logic ultimately writes to are documented in setupHelperVariables's
# six sub-section banners (DIAGNOSTIC, CLR PRE-NAME, NAME, CLR
# POST-NAME, GENERAL, INTERPRETER, MANAGED ASSEMBLY NAME, MANAGED
# ASSEMBLY SEARCH).  When a config variable surfaces in the Garuda.c
# error path with no obvious origin, look there.
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

namespace eval ::Garuda {
  #############################################################################
  #**************************** SHARED PROCEDURES *****************************
  #############################################################################

  #
  # The "swallow log messages" command.  Default ::Garuda::logCommand
  # value when no embedder-supplied logger is configured.  Returns
  # empty (success) so the C side's TclLog dual-write logic still
  # mirrors to the platform debug stream -- return-error here would
  # suppress that as well.
  #
  proc noLog { string } {
    #
    # NOTE: Do nothing.  This will end up returning success to the native code
    #       that uses the configured log command.  Returning success from the
    #       configured log command means "yes, please log this to the attached
    #       debugger (and/or the system debugger) as well".  Returning an error
    #       from the configured log command will prevent this behavior.  Other
    #       than that, returning an error from the configured log command is
    #       completely harmless.
    #
  }

  #
  # Set-style [lappend]: add each arg to the named list, but only if
  # it isn't already present.  Linear scan via [lsearch -exact], which
  # is fine for the tens-of-elements lists this package uses.  Used
  # heavily by the directory-list builders to dedupe candidate paths
  # contributed by multiple sources (env / registry / library / etc).
  #
  # NOTE: Also defined in and used by "all.tcl".
  #
  proc lappendUnique { varName args } {
    upvar 1 $varName list

    foreach arg $args {
      if {[lsearch -exact $list $arg] == -1} then {
        lappend list $arg
      }
    }
  }

  #
  # Resolve a command name to its fully-qualified form via [namespace
  # which].  Returns the original name unchanged if [namespace which]
  # cannot resolve it (e.g. the command was deleted, or this is being
  # called for a pseudo-name like "<unknownCaller>").  Used by
  # maybeLogViaCommand to print "::Garuda::someProc: message" rather
  # than "someProc: message" so log lines are unambiguous when
  # multiple Tcl extensions log to the same channel.
  #
  # NOTE: Also defined in and used by "all.tcl".
  #
  proc maybeFullName { command } {
    set which [namespace which $command]

    if {[string length $which] > 0} then {
      return $which
    }

    return $command
  }

  #
  # Path-canonicalization helper.  See garuda.tcl for full design notes;
  # this is the same proc body, redefined here so helper.tcl can be
  # sourced standalone (via [package require GarudaHelper]) without
  # needing the loader-script copies.
  #
  # NOTE: Also defined in and used by "dotnet.tcl" and "garuda.tcl".
  #
  proc fileNormalize { path {force false} } {
    variable noNormalize

    if {$force || !$noNormalize} then {
      return [file normalize $path]
    }

    return $path
  }

  #
  # fileNormalize variant tailored for environment-variable values:
  # adds [string trim] to strip whitespace some shells leave around
  # exported paths (Windows %FOO% expansion in particular can produce
  # leading/trailing spaces that break [file exists] checks).
  #
  # NOTE: Also defined in and used by "all.tcl".
  #
  proc fileNormalizeFromEnvironment { path {force false} } {
    return [fileNormalize [string trim $path] $force]
  }

  #
  # The Tcl-side log primitive.  Counterpart to TclLog in Garuda.c --
  # but where TclLog is the C-to-Tcl bridge, this proc is what
  # Tcl-side helper.tcl code calls when it wants to emit a log
  # message.  Routes through the embedder-configured logCommand
  # (default ::Garuda::noLog), gated by ::Garuda::verbose.
  #
  # The minusLevels argument controls the caller-name prefix.  By
  # default the message reads as if it came from the proc that
  # called maybeLogViaCommand.  When minusLevels=N, the prefix
  # walks UP the call stack N additional frames -- used by deep
  # helpers like isValidDirectory and isValidFile so the log
  # reads "::Garuda::setupAndLoad: Checking for directory..."
  # rather than "::Garuda::isValidDirectory: ...".  The latter
  # would be technically true but useless for diagnosis; what
  # the embedder cares about is which TOP-level proc is doing
  # the file check.
  #
  # The two-pass [info level] loop with the caller-fallback
  # handles two edge cases: stack levels beyond the current
  # depth (returns "<unknownCaller>") and direct top-level
  # invocations (no enclosing proc).  In both cases we emit
  # the message rather than skipping it, since silent log
  # drops are worse than an imprecise caller name.
  #
  # The whole proc is no-op when verbose is false, so the
  # cost of the log calls scattered through helper.tcl is
  # essentially zero in production builds.
  #
  # NOTE: Also defined in and used by "all.tcl".
  #
  proc maybeLogViaCommand { message {minusLevels 0} } {
    variable logCommand
    variable verbose

    if {[info exists verbose] && $verbose} then {
      if {![string is integer -strict $minusLevels] || \
          $minusLevels < 0} then {
        set minusLevels 0
      }

      set finalLevel [expr {
        $minusLevels > 0 ? -1 - $minusLevels : ""
      }]

      foreach level [list $finalLevel -1] {
        if {[string is integer -strict $level] && [catch {
          maybeFullName [lindex [info level $level] 0]
        } caller] == 0} then {
          break
        } else {
          unset -nocomplain caller
        }
      }

      if {![info exists caller]} then {
        set caller <unknownCaller>
      }

      if {[info exists logCommand] && \
          [string length $logCommand] > 0} then {
        catch {
          eval $logCommand \
              [list "$caller: $message"]; # USER-DEFINED (?)
        }
      }
    }
  }

  #
  # Strict "is this a usable directory?" predicate.  Returns true ONLY
  # when path is non-empty, not "." / ".." (we want absolute paths,
  # not implicit relative-to-cwd ones -- a Garuda load may run with
  # different cwds at different points), and [file exists] +
  # [file isdirectory] both hold.  Logs every check so a
  # verbose-mode trace shows exactly which paths were probed and
  # rejected.  The minusLevels=1 hides this proc itself from the
  # logged caller name.
  #
  # NOTE: Also defined in and used by "all.tcl".
  #
  proc isValidDirectory { path } {
    #
    # NOTE: For now, just make sure the path refers to an existing directory.
    #
    maybeLogViaCommand "Checking for directory \"$path\" from \"[pwd]\"..." 1

    return [expr {[string length $path] > 0 && \
          $path ne "." && $path ne ".." && \
          [file exists $path] && [file isdirectory $path]}]
  }

  #
  # Sister of isValidDirectory for files.  Same strict
  # "non-empty, not . or .., exists, is a regular file" check.
  # Used everywhere Garuda needs to verify a discovered path
  # before committing to load from it.
  #
  # NOTE: Also defined in and used by "all.tcl".
  #
  proc isValidFile { path } {
    #
    # NOTE: For now, just make sure the path refers to an existing file.
    #
    maybeLogViaCommand "Checking for file \"$path\" from \"[pwd]\"..." 1

    return [expr {[string length $path] > 0 && \
          $path ne "." && $path ne ".." && \
          [file exists $path] && [file isfile $path]}]
  }

  #
  # The "are we on Win32?" predicate.  Reads tcl_platform(platform),
  # which Tcl sets to "windows" on every Windows variant (Win32,
  # Win64, ARM64) and to "unix" everywhere else.  Used by every
  # piece of Win32-specific logic in this file (registry probing,
  # PATH manipulation rules, drive-letter handling, etc).
  #
  # The "very precise, minimal, and safe" comment in the body is
  # intentional: tcl_platform has historically had several
  # Win32-related fields (osVersion, byteOrder, machine), and
  # earlier versions of this proc inspected several of them.
  # Reducing to just (platform eq "windows") closed several edge
  # cases (Cygwin, MSYS) where the secondary fields lied about
  # platform identity.
  #
  # NOTE: Also defined in and used by "all.tcl".
  #
  proc isWindows {} {
    global tcl_platform

    #
    # NOTE: Windows always requires "special handling".  The platform check
    #       here is very precise, minimal, and safe.
    #
    return [expr {[info exists tcl_platform(platform)] && \
        $tcl_platform(platform) eq "windows"}]
  }

  #
  # Returns true if the host has NO choice but to use CoreCLR.  Two
  # triggers, either is sufficient:
  #
  #   1. FORCE_DOTNET_CORE environment variable is set.  Embedder
  #      override for "I want CoreCLR even on a Win32 box that has
  #      both runtimes installed".
  #
  #   2. Not running on Win32.  The .NET Framework only exists on
  #      Windows; Mono historically supported some of the .NET
  #      Framework but never implemented the unmanaged hosting APIs
  #      Garuda needs (CLRCreateInstance, ICLRRuntimeHost).
  #      Therefore, on Linux/macOS/BSD, CoreCLR is the only option.
  #
  # Distinct from hasUseCoreClr (which reports whether ANY explicit
  # selection was made via variable or env var).  shouldForceCoreClr
  # answers the narrower question "is the .NET Framework even on
  # the table?".  Used by isDotNetCore as a short-circuit before
  # the more expensive runtime-detection logic.
  #
  # NOTE: Also defined in and used by "all.tcl".
  #
  proc shouldForceCoreClr {} {
    global env

    #
    # NOTE: Assume that the .NET Framework is only available on Windows
    #       -AND- that Mono will never support the native hosting APIs,
    #       hence the only option left is the .NET (Core?) runtime.
    #
    if {[info exists env(FORCE_DOTNET_CORE)] || ![isWindows]} then {
      return true
    } else {
      return false
    }
  }

  #
  # "Was an explicit CoreCLR-vs-Framework choice made?" predicate.
  # Returns true when EITHER ::Garuda::useCoreClr (the namespace
  # variable that loaders pre-set) OR env(UseCoreClr) (the
  # environment-variable override) holds a strict boolean.  When
  # true, the actual boolean value is written via [upvar] into the
  # caller-supplied varName.
  #
  # Source priority is variable BEFORE environment.  Embedders that
  # want to override the environment do so by setting the namespace
  # variable explicitly; embedders that want to honor the env var
  # do so by NOT setting the variable.  The env var name
  # "UseCoreClr" matches the Tcl-namespace variable name (case-
  # preserved) so embedders only have to remember one identifier.
  #
  # The default argument is the value to write into varName when
  # NEITHER source has an explicit setting -- but the proc still
  # returns false in that case, since "no explicit setting" is the
  # whole point of the predicate.  Callers like isDotNetCore use
  # the default to decide what to recommend in the absence of an
  # embedder choice.
  #
  # The two-argument signature (varName, default) is unusual: the
  # boolean return is "explicit setting?" and the upvar is "the
  # value, including the default-resolved one".  Caller idiom:
  #
  #     if {[hasUseCoreClr result myDefault]} then {
  #         # explicit setting; result is the embedder's choice
  #     } else {
  #         # no explicit setting; result is myDefault
  #     }
  #
  # NOTE: Also defined in and used by "all.tcl".
  #
  proc hasUseCoreClr { {varName ""} {default ""} } {
    global env
    variable useCoreClr

    if {[string length $varName] > 0} then {
      upvar 1 $varName result
    }

    if {[info exists useCoreClr] && \
        [string is boolean -strict $useCoreClr]} then {
      set result $useCoreClr

      if {[string is true -strict $result]} then {
        maybeLogViaCommand "Using CoreCLR (variable)..."
      } else {
        maybeLogViaCommand "Not using CoreCLR (variable)..."
      }

      return true; # NOTE: There was an explicit setting.
    }

    if {[info exists env(UseCoreClr)] && \
        [string is boolean -strict $env(UseCoreClr)]} then {
      set result $env(UseCoreClr)

      if {[string is true -strict $result]} then {
        maybeLogViaCommand "Using CoreCLR (environment)..."
      } else {
        maybeLogViaCommand "Not using CoreCLR (environment)..."
      }

      return true; # NOTE: There was an explicit setting.
    }

    set result $default

    if {[string is true -strict $result]} then {
      maybeLogViaCommand "Using CoreCLR (default)..."
    } else {
      maybeLogViaCommand "Not using CoreCLR (default)..."
    }

    return false; # NOTE: There was not an explicit setting.
  }

  #
  # The "are we running .NET (Core)?" sibling of shouldUseCoreClr.
  # Different decision tree:
  #
  #   1. shouldForceCoreClr -- platform-level forcing condition
  #      (non-Win32, or FORCE_DOTNET_CORE set).
  #   2. hasUseCoreClr -- explicit embedder/env override.
  #   3. Are we running INSIDE a `dotnet` host process?  If
  #      [info nameofexecutable] basename is "dotnet" we're being
  #      invoked from "dotnet myapp.dll" or similar -- the host
  #      already runs CoreCLR, so .NET Framework is impossible.
  #   4. Fall back to shouldUseCoreClr if available.
  #   5. If still unresolved, return the caller-supplied default.
  #
  # Distinct from shouldUseCoreClr in that this is the broader
  # "what runtime is the embedder ALREADY in?" question rather
  # than "what runtime should Garuda load?".  In practice they
  # usually agree, but the layered fallback above lets
  # isDotNetCore answer when shouldUseCoreClr can't (e.g. before
  # the rest of helper.tcl has been loaded -- see the [info
  # procs] guard).
  #
  # The "NOT USED: DO NOT REMOVE" comment on the proc def itself
  # is preserved verbatim -- this proc is part of the helper.tcl
  # contract for downstream tooling that may not be in this
  # source tree but expects to be able to call it.
  #
  # NOTE: Also defined in and used by "all.tcl".
  #
  proc isDotNetCore { {default ""} } {; # NOT USED: DO NOT REMOVE.
    if {[shouldForceCoreClr]} then {
      return true; # FORCED
    }

    if {[hasUseCoreClr result]} then {
      return $result; # EXPLICIT
    }

    if {[file rootname [file tail \
        [info nameofexecutable]]] eq "dotnet"} then {
      return true; # HACK: Running in .NET Core process.
    }

    if {[llength [info procs shouldUseCoreClr]] > 0} then {
      return [shouldUseCoreClr $default]
    }

    if {[string is true -strict $default]} then {
      maybeLogViaCommand "Using CoreCLR (default)..."
    } else {
      maybeLogViaCommand "Not using CoreCLR (default)..."
    }

    return $default
  }

  #############################################################################
  #**************************** UTILITY PROCEDURES ****************************
  #############################################################################

  #
  # "Has Tcl already [load]ed this binary?" predicate.  Walks the
  # [info loaded] list looking for an exact match on the filename.
  # Returns true on first match, optionally (when varName is
  # supplied) writing the matched [info loaded] entry -- a 2-element
  # list of {filename packageName} -- into the caller's varName via
  # [upvar].
  #
  # Why exact filename match rather than [info loaded] with a
  # package-name argument?  Tcl's package-name lookup is case-
  # insensitive and disregards path differences, so "Garuda" loaded
  # from one directory would shadow "Garuda" from another and we
  # would falsely report the second as already-loaded.  Comparing
  # full filenames disambiguates.
  #
  # The Tcl-version comment in the body explains why the search is
  # an explicit foreach loop rather than [lsearch -exact -index 0]:
  # the latter is Tcl 8.5+, but Garuda supports 8.4 minimum.  Same
  # constraint affects the rest of this file's [lsearch] usage.
  #
  proc isLoaded { fileName {varName ""} } {
    #
    # NOTE: If requested by the caller, give them access to all loaded package
    #       entries that we may find.
    #
    if {[string length $varName] > 0} then {
      upvar 1 $varName loaded
    }

    #
    # NOTE: In Tcl 8.5 and higher, the [lsearch -exact -index] could be used
    #       here instead of this search loop; however, this package needs to
    #       work with Tcl 8.4 and higher.
    #
    foreach loaded [info loaded] {
      #
      # HACK: Exact matching is being used here.  Is this reliable?
      #
      if {[lindex $loaded 0] eq $fileName} then {
        maybeLogViaCommand "Package binary file \"$fileName\" is loaded."
        return true
      }
    }

    maybeLogViaCommand "Package binary file \"$fileName\" is not loaded."
    return false
  }

  #
  # Return the canonical Windows install directory.  Tries env(SystemRoot)
  # first, env(WinDir) second -- both should resolve to the same path on
  # a sane Windows host, but %SystemRoot% is the modern canonical name
  # (post-Win2K) and %WinDir% is the legacy fallback (DOS / Win9x).
  # Returns empty on non-Windows or if neither variable is set.
  # Used by getDotNetFrameworkDirectory to find Microsoft.NET\Framework.
  #
  proc getWindowsDirectory {} {
    global env

    if {[info exists env(SystemRoot)]} then {
      return [fileNormalizeFromEnvironment $env(SystemRoot) true]
    } elseif {[info exists env(WinDir)]} then {
      return [fileNormalizeFromEnvironment $env(WinDir) true]
    }

    return ""
  }

  #
  # Order-preserving deduplication.  Returns a new list containing
  # the elements of $list with second-and-later occurrences removed.
  # First-occurrence ordering matters because callers (especially
  # getProgramFilesDirectories) accumulate paths from sources of
  # different priority -- losing the order would mix preferred and
  # fallback paths randomly.  Tcl 8.5 has [lsort -unique] which
  # could replace this; we keep the explicit loop for the 8.4
  # compat envelope.
  #
  proc filterUnique { list } {
    set result [list]

    foreach item $list {
      if {[lsearch -exact $result $item] == -1} then {
        lappend result $item
      }
    }

    return $result
  }

  #
  # "Is this a syntactically-valid Tcl package version string?"
  # predicate.  The HACK comment in the body is the actual technique:
  # try [package vcompare $value 1.0] and see if it errors.
  # [package vcompare] has strict version syntax (digits, dots,
  # plus a/b/p suffix segments per Tcl's package versioning rules);
  # any input that survives the call is by definition valid.
  #
  # The empty-string short-circuit is necessary because [package
  # vcompare] would otherwise accept "" as a valid version (treats
  # it as "0.0.0").  We reject empty as invalid because the callers
  # use it as the "no version specified" sentinel.
  #
  # Tighter than extractPackageVersion: this proc rejects
  # pre-release suffix forms ("8.0-rc.1") that extractPackageVersion
  # accepts.  Use this when you need an unambiguous stable version,
  # use extractPackageVersion when you want to handle pre-release
  # builds too.
  #
  proc isValidPackageVersion { value } {
    #
    # HACK: Check if the value is a valid version number via
    #       the [package vcompare] sub-command; since we do
    #       not care about the actual comparison result, we
    #       can use any "well-known-to-be-a-valid-version"
    #       value as the (other) value to compare against
    #       (e.g. 1.0) and simply see if a script error ends
    #       up being caught.  No caught error means "valid".
    #
    return [expr {[string length $value] > 0 && \
        [catch {package vcompare $value 1.0}] == 0}]
  }

  #
  # Parse a CoreCLR-style version string with optional pre-release
  # suffix into its components.  Examples of inputs and what gets
  # extracted:
  #
  #     "8.0.5"         version="8.0.5",  releaseType="",     releaseSerial=""
  #     "8.0.0-rc.2"    version="8.0.0",  releaseType=<idx>,  releaseSerial=2
  #     "9.0.0-preview.7"  version="9.0.0", releaseType=<idx>, releaseSerial=7
  #
  # Where <idx> is the integer index into the getCoreClrReleaseTypes
  # list (so "alpha" < "beta" < "rc" etc by sort order).  The
  # numeric encoding is what comparePackageVersions uses to order
  # pre-releases -- a string compare on "rc" vs "preview" wouldn't
  # match the natural release-stage ordering.
  #
  # The three [upvar] outputs are independent -- caller can request
  # any subset by passing or omitting the varName arguments.
  #
  # Returns:
  #   2  Full match including pre-release suffix.
  #   1  Plain version match (no suffix).
  #   0  No match -- input doesn't start with digits-and-dots.
  #
  # The 0/1/2 return distinguishes "I parsed nothing" from "I
  # parsed a stable version" from "I parsed a pre-release".  Most
  # callers only check truthiness (>0) but filterPackageVersions
  # cares about the distinction.
  #
  proc extractPackageVersion {
          value {versionVarName ""} {releaseTypeVarName ""}
          {releaseSerialVarName ""} } {
    if {[string length $versionVarName] > 0} then {
      upvar 1 $versionVarName version
    }

    if {[string length $releaseTypeVarName] > 0} then {
      upvar 1 $releaseTypeVarName releaseType
    }

    if {[string length $releaseSerialVarName] > 0} then {
      upvar 1 $releaseSerialVarName releaseSerial
    }

    set pattern(1) {^(\d+(?:\.\d+)*)}

    if {[regexp -- $pattern(1) $value dummy version]} then {
      set releaseTypes [getCoreClrReleaseTypes]
      set pattern(2) ""

      append pattern(2) {^(?:\d+(?:\.\d+)*)(?:-(}
      append pattern(2) [join $releaseTypes |]
      append pattern(2) {))\.(\d+)$}

      if {[regexp -- \
          $pattern(2) $value dummy releaseType releaseSerial]} then {
        set releaseType [lsearch -exact $releaseTypes $releaseType]
        return 2
      } else {
        return 1
      }
    }

    return 0
  }

  #
  # Apply isValidPackageVersion / extractPackageVersion as a filter
  # over a list, with a strictness toggle.  When strict=true, only
  # plain Tcl-syntax versions survive.  When strict=false, also
  # admit pre-release versions ("8.0.0-rc.1") via extractPackageVersion.
  # Used by the CoreCLR runtime-version probe (probeCoreClrDirectories)
  # to throw out subdirectory names that aren't recognizable as
  # CoreCLR runtime versions.
  #
  # Calls filterUnique first to dedupe -- the input list is typically
  # the output of [glob], which on case-insensitive filesystems can
  # contain duplicates.
  #
  proc filterPackageVersions { list strict } {
    set result [list]

    foreach item [filterUnique $list] {
      if {[isValidPackageVersion $item] || \
          (!$strict && [extractPackageVersion $item])} then {
        lappend result $item
      }
    }

    return $result
  }

  #
  # Three-way version comparator suitable for [lsort -command].
  # Returns -1, 0, or 1 (per Tcl's [string compare] convention).
  # Errors out if NEITHER input is a recognizable version; callers
  # are expected to pre-filter via filterPackageVersions.
  #
  # Comparison precedence:
  #
  #   1. If both inputs pass isValidPackageVersion (plain Tcl syntax
  #      only -- no pre-release suffix), use [package vcompare]
  #      directly.  This is the simple-and-fast path.
  #
  #   2. Otherwise, extract the numeric portion of each via
  #      extractPackageVersion and compare those with [package
  #      vcompare].  When the numeric portions tie, break the tie
  #      by:
  #        - Stable (no suffix) vs pre-release: stable wins.
  #        - Both pre-release: lower releaseType index wins
  #          (alpha < beta < rc), then by releaseSerial.
  #        - Both stable but vcompare-equal AND both extracted:
  #          [string compare -nocase] as a last-resort tiebreak.
  #
  # The "stable wins" rule means "8.0.0" sorts AFTER "8.0.0-rc.5",
  # which is what users expect -- RTM versions outrank any pre-RTM
  # build of the same numeric version.  Callers wanting "highest
  # first" pass `-decreasing` to [lsort].
  #
  # NOTE: This primary ordering here is via [package vcompare]; failing
  #       that, we fallback to attempting to extract dotted (decimal)
  #       version numbers for both value arguments.  For any non-stable
  #       releases, they should always be sorted as "inferior" to their
  #       associated RTM releases, in relative order of their maturity.
  #       Callers wanting highest-first must pass the -decreasing option
  #       to [lsort].
  #
  proc comparePackageVersions { value1 value2 } {
    set ok1 [isValidPackageVersion $value1]
    set ok2 [isValidPackageVersion $value2]

    if {$ok1 && $ok2} then {
      return [package vcompare $value1 $value2]
    } else {
      set releaseType1 ""; set releaseSerial1 ""

      if {$ok1} then {
        set wasOk1 true; set version1 $value1
      } else {
        set wasOk1 false

        if {[extractPackageVersion \
            $value1 version1 releaseType1 releaseSerial1]} then {
          set ok1 true
        }
      }

      set releaseType2 ""; set releaseSerial2 ""

      if {$ok2} then {
        set wasOk2 true; set version2 $value2
      } else {
        set wasOk2 false

        if {[extractPackageVersion \
            $value2 version2 releaseType2 releaseSerial2]} then {
          set ok2 true
        }
      }

      if {$ok1 && $ok2} then {
        set result [package vcompare $version1 $version2]

        if {$result == 0} then {
          set wasOk1 [expr {$wasOk1 ? 1 : 0}]
          set wasOk2 [expr {$wasOk2 ? 1 : 0}]

          if {!$wasOk1 && !$wasOk2} then {
            if {[string is integer -strict $releaseType1] && \
                [string is integer -strict $releaseType2] && \
                [string is integer -strict $releaseSerial1] && \
                [string is integer -strict $releaseSerial2]} then {
              set result [expr {$releaseType1 - $releaseType2}]
              set result [expr {$result < 0 ? -1 : $result > 0 ? 1 : 0}]

              if {$result == 0} then {
                set result [expr {$releaseSerial1 - $releaseSerial2}]
                set result [expr {$result < 0 ? -1 : $result > 0 ? 1 : 0}]
              }
            } else {
              set result [string compare -nocase $value1 $value2]
            }
          } elseif {!$wasOk1 || !$wasOk2} then {
            set result [expr {$wasOk1 > $wasOk2 ? 1 : -1}]
          }
        }

        return $result
      }
    }

    error "cannot compare \"$value1\" versus \"$value2\" as versions"
  }

  #
  # Return a list of plausible "where could a system-wide CoreCLR /
  # .NET runtime install be?" directories.  This proc is the
  # deepest piece of platform-specific knowledge in helper.tcl --
  # it encodes years of accumulated learning about Microsoft's
  # ever-changing dotnet install layouts on Windows, macOS, and
  # Linux.
  #
  # Sources, in priority order:
  #
  #   1. Architecture-specific DOTNET_ROOT_<arch> env vars
  #      (DOTNET_ROOT_X64, DOTNET_ROOT_ARM64, etc).  Modern dotnet
  #      installs on Win64 set these per-architecture variants
  #      so a 32-bit and 64-bit runtime can coexist.
  #
  #   2. WoW64-specific DOTNET_ROOT(x86) and DOTNET_ROOT(x64).
  #      Apply when shouldConsiderForWoW64 is true (we're a 32-bit
  #      Tcl on a 64-bit Windows host) -- the x86 host pack lives
  #      under the x64 dotnet root in some installs.
  #
  #   3. The fallback DOTNET_ROOT.  Used by single-architecture
  #      hosts and by older dotnet installers.
  #
  #   4. Win32 ProgramFiles variants:
  #        ProgramFiles(x86)   WoW64 only
  #        ProgramFiles        always
  #        ProgramW6432        WoW64 only -- points to the 64-bit
  #                             "Program Files" from a 32-bit context
  #        ProgramFiles(Arm)   WoW64 only -- ARM-on-Windows variant
  #
  #   5. macOS conventional paths:
  #        /usr/local/share    Homebrew default for /usr/local
  #        /opt/homebrew/opt   Homebrew default on Apple Silicon
  #
  #   6. Linux conventional paths:
  #        /usr/local/share, /usr/share, /usr/lib64, /usr/lib
  #        /opt/Microsoft, /opt
  #
  # All env-var-derived paths go through fileNormalizeFromEnvironment
  # to strip whitespace; all paths are validated via isValidDirectory
  # before being included.  Final result is filterUnique'd to dedupe
  # cases where multiple env vars resolve to the same path.
  #
  # The URL https://urn.to/r/dotnetSdkVars in the original NOTE is the
  # canonical Microsoft reference for the DOTNET_* environment
  # variables and what they're supposed to mean -- refer to that for
  # newly-added variables, since this proc has historically lagged
  # Microsoft's additions.
  #
  # NOTE: On WoW64 the x86 host pack may reside under the x64 dotnet root,
  #       hence the ProgramW6432 / DOTNET_ROOT(x64) probes.  Documentation
  #       for the environment variables used by this procedure should be
  #       available at "https://urn.to/r/dotnetSdkVars".
  #
  proc getProgramFilesDirectories {} {
    global env
    global tcl_platform

    set wow64 [shouldConsiderForWoW64]
    set envVars [list]

    if {[info exists tcl_platform(machine)]} then {
      set architecture [getCoreClrPlatformArchitecture \
          $tcl_platform(machine)]

      if {[string length $architecture] > 0} then {
        lappend envVars DOTNET_ROOT_${architecture}
        lappend envVars DOTNET_ROOT_[string toupper $architecture]
      }
    }

    if {$wow64} then {
      lappend envVars DOTNET_ROOT(x86)
      lappend envVars DOTNET_ROOT(x64)
    }

    lappend envVars DOTNET_ROOT
    set result [list]

    foreach envVar $envVars {
      if {[info exists env($envVar)]} then {
        set directory [fileNormalizeFromEnvironment \
            $env($envVar)]

        if {[isValidDirectory $directory]} then {
          lappend result $directory
        }
      }
    }

    if {[isWindows]} then {
      if {$wow64 && [info exists env(ProgramFiles(x86))]} then {
        set directory [fileNormalizeFromEnvironment \
            ${env(ProgramFiles(x86))}]

        if {[isValidDirectory $directory]} then {
          lappend result $directory
        }
      }

      if {[info exists env(ProgramFiles)]} then {
        set directory [fileNormalizeFromEnvironment \
            $env(ProgramFiles)]

        if {[isValidDirectory $directory]} then {
          lappend result $directory
        }
      }

      if {$wow64 && [info exists env(ProgramW6432)]} then {
        set directory [fileNormalizeFromEnvironment \
            $env(ProgramW6432)]

        if {[isValidDirectory $directory]} then {
          lappend result $directory
        }
      }

      if {$wow64 && [info exists env(ProgramFiles(Arm))]} then {
        set directory [fileNormalizeFromEnvironment \
            ${env(ProgramFiles(Arm))}]

        if {[isValidDirectory $directory]} then {
          lappend result $directory
        }
      }
    } else {
      if {[isMacOS]} then {
        foreach directory [list /usr/local/share /opt/homebrew/opt] {
          if {[isValidDirectory $directory]} then {
            lappend result [fileNormalize $directory true]
          }
        }
      } else {
        foreach directory [list \
            /usr/local/share /usr/share /usr/lib64 /usr/lib \
            /opt/Microsoft /opt] {
          if {[isValidDirectory $directory]} then {
            lappend result [fileNormalize $directory true]
          }
        }
      }
    }

    return [filterUnique $result]
  }

  #
  # Return the list of subdirectory names under each ProgramFiles
  # directory that could contain a dotnet install.  Three values:
  #     ""                       -- direct (no subdirectory)
  #     "dotnet"                 -- Win32 / Linux convention
  #     "dotnet/libexec"         -- macOS Homebrew convention
  #
  # Cross-producted with getProgramFilesDirectories by the CoreCLR
  # version-probing logic to enumerate all plausible install locations.
  #
  proc getProgramFilesSubDirectories {} {
    return [list "" dotnet [file join dotnet libexec]]
  }

  #
  # Return the canonical .NET Framework install directory for the
  # given version.  Path layout is the well-known
  #
  #     %SystemRoot%\Microsoft.NET\Framework\v<version>
  #
  # Note the lowercase "v" prefix that .NET Framework uses (e.g.
  # "v4.0.30319" not "4.0.30319").  The [string trimleft $version v]
  # call lets callers pass the version with or without the prefix.
  #
  # Returns empty if not on Windows or %SystemRoot% / %WinDir% is
  # missing.  Note this proc does NOT check that the directory
  # exists -- that's checkDotNetFrameworkDirectory's job.
  #
  proc getDotNetFrameworkDirectory { version } {
    set directory [getWindowsDirectory]

    if {[string length $directory] > 0} then {
      return [file join $directory Microsoft.NET Framework \
          v[string trimleft $version v]]
    }

    return ""
  }

  #
  # "Is .NET Framework $version actually installed on this host?"
  # predicate.  Builds the directory via getDotNetFrameworkDirectory
  # and tests existence via isValidDirectory.  Used by shouldUseCoreClr
  # in the .NET Framework branch -- we don't recommend a Framework
  # version that isn't there.
  #
  proc checkDotNetFrameworkDirectory { version } {
    set directory [getDotNetFrameworkDirectory $version]

    if {[string length $directory] > 0 && \
        [isValidDirectory $directory]} then {
      return true
    }

    return false
  }

  #
  # "Are we a 32-bit Tcl running on a 64-bit Windows host?" predicate.
  # WoW64 (Windows-on-Windows-64) is Microsoft's 32-bit emulation
  # layer; from a 32-bit process's view, the 64-bit "Program Files"
  # is exposed under the alternate name %ProgramW6432% while
  # %ProgramFiles% redirects to the 32-bit-only directory.  The
  # CoreCLR install can be in EITHER location depending on which
  # installer the user ran, so probe paths derived from this
  # predicate's true case look at both.
  #
  # tcl_platform(wordSize) is bytes-per-pointer: 4 on 32-bit Tcl,
  # 8 on 64-bit Tcl.  Combined with the isWindows guard, true
  # means specifically "32-bit Tcl on Windows" -- which is the
  # only context where WoW64 redirection matters.
  #
  proc shouldConsiderForWoW64 {} {
    global tcl_platform

    if {[isWindows] && \
        [info exists tcl_platform(wordSize)] && \
        $tcl_platform(wordSize) == 4} then {
      return true
    } else {
      return false
    }
  }

  #
  # "Are we on macOS?" predicate.  Reads tcl_platform(os), which
  # is "Darwin" on every macOS variant (the kernel name, not the
  # marketing name).  Distinct from isWindows -- both can return
  # false on a Linux box.
  #
  proc isMacOS {} {
    global tcl_platform

    return [expr {[info exists tcl_platform(os)] && \
        $tcl_platform(os) eq "Darwin"}]
  }

  #
  # [lreverse] portability shim.  Tcl 8.5 introduced the [lreverse]
  # core command; on 8.4 we fall back to a manual loop.  The
  # name reflects the "do this somehow, regardless of Tcl version"
  # intent.  Used by version-list ordering where we want highest-
  # first rather than [lsort -decreasing] (which has different
  # tiebreaking semantics for non-strict-version inputs).
  #
  proc somehowLreverse { list } {
    if {[llength [info commands ::lreverse]] > 0} then {
      return [::lreverse $list]
    } else {
      set result [list]

      foreach item $list {
        set result [linsert $result 0 $item]
      }

      return $result
    }
  }

  #
  # NOTE: This procedure returns the list of "well-known" release types for
  #       the CoreCLR.  This list may need to be updated if/when additional
  #       types are added by Microsoft.
  #
  # Order matters -- the list index of each name is the numeric ranking
  # used by extractPackageVersion / comparePackageVersions to sort
  # pre-releases.  Lower indices represent earlier release stages, so
  # "alpha" (index 1) sorts before "rc" (index 4) which sorts before
  # "stable" (index 5).  Adding a new type means adding it in the
  # right position; reordering the list silently breaks version
  # ordering of any existing pre-release version strings.
  #
  proc getCoreClrReleaseTypes {} {
    return [list dev alpha beta preview rc stable]
  }

  #
  # NOTE: This procedure is designed to return the operating system prefix
  #       portion of a CoreCLR "platform identifier", i.e. only for those
  #       operating systems that are supported by this package.
  #
  # Maps tcl_platform values to RID OS prefixes:
  #     Windows         -> "win"
  #     Darwin (macOS)  -> "osx"
  #     Linux           -> "linux"
  #     other           -> ""    (CoreCLR not supported here)
  #
  # The mapping is intentionally narrow -- Microsoft's RID space
  # has many more entries (alpine, ubuntu.20.04, debian.11, etc)
  # but Garuda treats all glibc-compatible Linux as "linux", which
  # is the generic-portable RID Microsoft itself recommends for
  # apps that don't pin to a specific distro.
  #
  proc getCoreClrPlatformOperatingSystem {} {
    global tcl_platform

    if {[isWindows]} then {
      return win
    } elseif {[isMacOS]} then {
      return osx
    } elseif {$tcl_platform(os) eq "Linux"} then {
      return linux
    } else {
      return ""
    }
  }

  #
  # NOTE: This procedure is designed to return the CPU architecture suffix
  #       portion of a CoreCLR "platform identifier", i.e. only for those
  #       CPU architectures that are supported by this package.
  #
  # Normalizes the many ways tcl_platform(machine) names a CPU into
  # the four RID arch suffixes:
  #     x86            <- intel, i386..i686, ia32_on_win64, x86
  #     x64            <- amd64, x86_64, x64
  #     arm            <- arm, armv6l, armv7l, armhf, armel
  #     arm64          <- aarch64, arm64
  #     "" (rejected)  <- anything else
  #
  # The "ia32_on_win64" alias deserves a note: Tcl uses this on
  # 32-bit builds running under WoW64 to make the host
  # architecture-confusion explicit.  We treat it as plain x86
  # for RID purposes -- the RID world doesn't distinguish "real
  # x86" from "x86-emulated-on-amd64".
  #
  proc getCoreClrPlatformArchitecture { machine } {
    switch -exact -- [string tolower $machine] {
      intel -
      i386 -
      i486 -
      i586 -
      i686 -
      ia32_on_win64 -
      x86 {
        return x86
      }
      amd64 -
      x86_64 -
      x64 {
        return x64
      }
      arm -
      armv6l -
      armv7l -
      armhf -
      armel {
        return arm
      }
      aarch64 -
      arm64 {
        return arm64
      }
      default {
        return ""
      }
    }
  }

  #
  # NOTE: This procedure is designed to return a CoreCLR "platform identifier",
  #       which will be used to help locate the correct CoreCLR runtime native
  #       host library for the operating system and machine associated with the
  #       current process.  The returned value will consist of a single string
  #       with two parts, separated by a dash.  The first is the prefix that
  #       denotes the operating system.  The second is the suffix that denotes
  #       the (CoreCLR-centric) processor architecture.  Some (valid?) example
  #       values are: "win-x86", "win-x64","linux-x64", and "osx-arm64".
  #
  # The "RID" terminology is Microsoft's: Runtime Identifier.  It is the
  # canonical key under which the CoreCLR install layout indexes
  # platform-specific binaries.  Two examples of how the RID matters:
  #
  #   1. The runtimes directory under a CoreCLR install is laid out as:
  #
  #        <root>/packs/Microsoft.NETCore.App.Host.<RID>/<version>/
  #          runtimes/<RID>/native/
  #
  #      (see getCoreClrRelativePath).  The RID appears twice in the
  #      path -- once selecting the host pack, once selecting the
  #      runtime variant -- so we MUST get it right to find any of
  #      coreclr.dll / libcoreclr.so / libcoreclr.dylib.
  #
  #   2. Eagle assemblies built against CoreCLR can be RID-specific
  #      (under runtimes/<RID>/lib/<tfm>/) when they include native
  #      bits.  The probe path constructed for findAssemblyFile uses
  #      the same RID so we don't accidentally pick a Linux assembly
  #      on macOS.
  #
  # The function delegates the OS half to getCoreClrPlatformOperating
  # System (which inspects $tcl_platform(os) / $tcl_platform(platform))
  # and the arch half to getCoreClrPlatformArchitecture (which
  # normalizes machine names like "amd64", "x86_64", "AMD64", "x64"
  # all to "x64").  Returns empty string if EITHER half cannot be
  # resolved -- the caller's contract is "if you don't get a non-empty
  # RID back, treat CoreCLR support as unavailable on this host".
  #
  proc getCoreClrPlatformRid { machine } {
    set prefix [getCoreClrPlatformOperatingSystem]

    if {[string length $prefix] == 0} then {
      return ""
    }

    set suffix [getCoreClrPlatformArchitecture $machine]

    if {[string length $suffix] == 0} then {
      return ""
    }

    return ${prefix}-${suffix}
  }

  #
  # NOTE: This procedure is designed to return a path, which will be relative
  #       to a return value from the [getProgramFilesDirectories] procedure,
  #       where the specified variant (i.e. the platform and version) of the
  #       CoreCLR runtime should be located.
  #
  # Two-mode operation:
  #
  #   With version    Return full path:
  #                     packs/Microsoft.NETCore.App.Host.<RID>/
  #                       <version>/runtimes/<RID>/native
  #                   This points directly at the directory
  #                   containing libcoreclr.* / coreclr.dll.
  #
  #   Without version Return version-less prefix:
  #                     packs/Microsoft.NETCore.App.Host.<RID>
  #                   Used as a [glob] base by probeCoreClrDirectories
  #                   to enumerate all installed versions for the
  #                   given RID.
  #
  # The "Microsoft.NETCore.App.Host.<RID>" naming is the
  # NuGet-style "host pack" identifier; this matches the
  # subdirectory name dotnet creates when a runtime is installed.
  #
  proc getCoreClrRelativePath { platform {version ""} } {
    set parts [list packs Microsoft.NETCore.App.Host.${platform}]

    if {[string length $version] > 0} then {
      lappend parts $version runtimes $platform native
    }

    return [eval file join $parts]
  }

  #
  # NOTE: This procedure is designed to build the CoreCLR runtime directory
  #       for the specified platform and version.  It may or may not exist.
  #       This relies upon the [getProgramFilesDirectories] procedure, which
  #       returns what are always assumed to be the possible roots of CoreCLR
  #       runtime directories, even on non-Windows platforms.
  #
  # Cross-products the directory list (getProgramFilesDirectories)
  # with the subdirectory list (getProgramFilesSubDirectories) and
  # appends the version-specific RID-relative path
  # (getCoreClrRelativePath).  Returns the FIRST cross-product entry
  # whose final path actually exists.
  #
  # When ::Garuda::useMinimumClr is FALSE (the default), the upstream
  # caller has typically picked the highest-version directory before
  # calling this.  When useMinimumClr is true, the lowest version was
  # selected.  Either way, this proc just builds and verifies a path
  # for the specific (platform, version) pair the caller already chose.
  #
  proc getCoreClrDirectory { platform version } {
    set directories [getProgramFilesDirectories]

    foreach directory $directories {
      foreach subDirectory [getProgramFilesSubDirectories] {
        set targetDirectory [file join $directory $subDirectory \
            [getCoreClrRelativePath $platform $version]]

        if {[isValidDirectory $targetDirectory]} then {
          return $targetDirectory
        }
      }
    }

    return ""
  }

  #
  # NOTE: This procedure is designed to return a CoreCLR runtime directory
  #       for the specified platform and pattern (a version [glob] string
  #       like "3.0.*", etc).  Upon success, the matching version will be
  #       placed into the "versionVarName" variable, e.g. "3.0.3".
  #
  # The "from a wildcard pattern, find the matching installed version"
  # workhorse.  Algorithm:
  #
  #   1. For each (programFilesDir, programFilesSubDir) pair, build
  #      the version-less prefix path and [glob] all subdirectories
  #      under it -- those are the installed runtime versions.
  #   2. Filter the glob output through filterPackageVersions to
  #      reject anything that doesn't look like a version string
  #      (avoids picking up README files, backup directories, etc).
  #   3. Sort the version list.  Default order is descending so the
  #      highest version is checked first; if useMinimumClr is set,
  #      ascending so the lowest installed version wins.  Custom
  #      [comparePackageVersions] handles pre-release ordering.
  #   4. Pick the first version that matches $pattern (a [glob]-style
  #      string like "3.0.*" or "8.*").  Write it into versionVarName
  #      and return the full runtime-native directory.
  #
  # The HACK comment in the body refers to the slightly weird use of
  # [glob] just to enumerate version subdirectory names -- we then
  # match those against $pattern manually rather than passing the
  # pattern directly to [glob], because we want comparePackageVersions
  # ordering rather than [glob]'s arbitrary order.
  #
  proc probeCoreClrDirectories { platform pattern versionVarName } {
    global env
    global tcl_platform
    variable useMinimumClr

    if {[string length $versionVarName] > 0} then {
      upvar 1 $versionVarName version
    }

    if {[string length $platform] > 0 && \
        [string length $pattern] > 0} then {
      set directories [getProgramFilesDirectories]

      foreach directory $directories {
        foreach subDirectory [getProgramFilesSubDirectories] {
          #
          # HACK: Yes, this is a bit odd.  We are grabbing a list of
          #       all sub-directory names so we can match it against
          #       a (wildcard) pattern and then the caller can check
          #       that a particular sub-directory within it actually
          #       exists.
          #
          set command [list lsort]

          #
          # NOTE: When the caller does not want the minimum version,
          #       sort in descending order, i.e. because the highest
          #       version should be checked first.
          #
          if {![info exists useMinimumClr] || !$useMinimumClr} then {
            lappend command -decreasing
          }

          set targetDirectory [file join $directory $subDirectory \
              [getCoreClrRelativePath $platform]]

          lappend command -command [list comparePackageVersions] \
              [filterPackageVersions [glob -nocomplain -directory \
              $targetDirectory -tails -types d *] false]

          set subSubDirectories [eval $command]

          foreach subSubDirectory $subSubDirectories {
            if {[string match $pattern $subSubDirectory]} then {
              set version $subSubDirectory; # 3.0.* ==> 3.0.3

              return [file join \
                  $targetDirectory $subSubDirectory runtimes $platform \
                  native]
            }
          }
        }
      }
    }

    return ""
  }

  #
  # NOTE: This procedure is designed to see if a CoreCLR runtime directory
  #       exists for the specified platform and pattern (a version [glob]
  #       string like "3.0.*", etc).  Upon success, the matching version
  #       will be placed into the "versionVarName" variable, e.g. "3.0.3".
  #
  # Boolean wrapper around probeCoreClrDirectories -- caller wants
  # yes/no rather than a path.  Optional versionVarName receives the
  # specific version that matched, in case the caller needs to know
  # ("yes, version 8.0.5 is installed" vs just "yes there's something").
  #
  # The "REDUNDANT" comment on the isValidDirectory call is correct
  # -- probeCoreClrDirectories already verified existence -- but it
  # remains as a defensive belt-and-braces check.  Cheap, harmless.
  #
  proc checkCoreClrDirectory { platform pattern {versionVarName ""} } {
    if {[string length $versionVarName] > 0} then {
      upvar 1 $versionVarName version
    }

    set directory [probeCoreClrDirectories $platform $pattern version]

    if {[string length $directory] > 0 && \
        [isValidDirectory $directory]} then {; # REDUNDANT
      return true
    }

    return false
  }

  #
  # NOTE: This procedure is used to find the "best" installed version of the
  #       CoreCLR runtime.  Depending on the value of "useMinimumClr", this
  #       could be the highest installed version (false, the package default)
  #       -OR- the lowest installed version (true, must be explicitly set).
  #
  # Iterates through ::Garuda::coreClrVersions (the configured
  # candidate-version patterns, e.g. "8.*", "7.*", "6.*") in order
  # and returns the first one that successfully resolves via
  # checkCoreClrDirectory.  The list is consulted in different
  # orders depending on useMinimumClr -- somehowLreverse'd when true
  # so we try the LOWEST configured version first.
  #
  # Returns true / false; when true, the matched runtime version
  # is written into versionVarName.  This is the function
  # setupForCoreClr calls to pin coreClrVersion.
  #
  proc checkCoreClrDirectories { platform {versionVarName ""} } {
    variable coreClrVersions
    variable useMinimumClr

    if {[string length $versionVarName] > 0} then {
      upvar 1 $versionVarName version
    }

    if {[info exists coreClrVersions]} then {
      if {[info exists useMinimumClr] && $useMinimumClr} then {
        set patterns [somehowLreverse $coreClrVersions]
      } else {
        set patterns $coreClrVersions
      }

      foreach pattern $patterns {
        if {[checkCoreClrDirectory $platform $pattern version]} then {
          return true
        }
      }
    }

    return false
  }

  #
  # NOTE: This procedure is designed to return the "dynamic token" values to
  #       use for the CoreCLR version-related values in the JSON string that
  #       will be used as the CoreCLR runtime configuration.
  #
  #
  # Decompose a CoreCLR version string into the (TFM, version) token
  # pair that getCoreClrRuntimeConfiguration plugs into the JSON
  # template.  Splits behavior at .NET 5.0:
  #
  #     version >= 5.0   tfm = "net<version>"        (e.g. "net8.0")
  #     version < 5.0    tfm = "netcoreapp<version>" (e.g. "netcoreapp3.1")
  #
  # The version is also normalized -- "3.0" becomes "3.0.0" because
  # framework.version in runtimeconfig.json wants a 3-component
  # SemVer-style string.
  #
  # Returns a 4-element list:  {targetFrameworkMoniker <tfm> version <ver>}
  # or empty list if the version is unparseable.  Caller pulls fields
  # by index; the list-of-pairs shape lets future fields (rollForward
  # etc.) be appended without breaking lookup.
  #
  proc getCoreClrRuntimeConfigurationTokens { version } {
    if {[string length $version] > 0} then {
      set parts [split $version .]
      set count [llength $parts]

      if {$count > 0} then {
        set result [list]

        if {$count > 2} then {
          set version [join [lrange $parts 0 1] .]
        } else {
          if {$count < 2} then {lappend parts 0}
          set version [join $parts .]
        }

        #
        # HACK: Check if the version of the CoreCLR requires the
        #       new target framework moniker (a.k.a. TFM) prefix,
        #       i.e. runtime name change ".NET Core" ==> ".NET".
        #       This became necessary starting with the .NET 5.0
        #       release.  The prefix strings are hard-coded here.
        #
        if {[package vcompare $version 5.0] >= 0} then {
          set targetFrameworkMoniker net${version}
        } else {
          set targetFrameworkMoniker netcoreapp${version}
        }

        lappend result targetFrameworkMoniker $targetFrameworkMoniker
        append version .0; # 3.0 ==> 3.0.0
        lappend result version $version

        return $result
      }
    }

    return [list]
  }

  #
  # NOTE: This procedure is designed to return a valid JSON string that will
  #       be used as the entire contents of the file containing the CoreCLR
  #       runtime configuration, for the specified runtime version.
  #
  # The runtimeconfig.json file is mandatory for any CoreCLR-hosted
  # application; hostfxr_initialize_for_runtime_config (which Garuda
  # calls in C -- see GarudaCoreClr.c) takes the path to this file as
  # its core argument.  The file tells hostfxr (a) which target
  # framework moniker to bind against ("net5.0", "net8.0", etc.) and
  # (b) which Microsoft.NETCore.App version to use, plus optional
  # rollForward policy that controls "if my exact version is missing,
  # may I use a newer one?".
  #
  # The returned JSON has this shape:
  #
  #     {
  #       "runtimeOptions": {
  #         "tfm": "net8.0",
  #         "framework": {
  #           "name": "Microsoft.NETCore.App",
  #           "version": "8.0.0"
  #         },
  #         "rollForward": "LatestMajor"   <-- only when compatibility
  #       }
  #     }
  #
  # Three knobs:
  #
  #   version          The CoreCLR version (e.g. "8.0").  This drives
  #                    both the TFM (net8.0) and the framework version
  #                    (8.0.0 -- note the .0 patch suffix added by
  #                    getCoreClrRuntimeConfigurationTokens).
  #
  #   compatibility    Default true.  Adds a rollForward clause so
  #                    hostfxr accepts a newer runtime if the exact
  #                    one isn't installed.  Set false for strict
  #                    "exactly this version or fail" semantics.
  #
  #   latestMajor      Only consulted when compatibility=true.  True =
  #                    "LatestMajor" rollForward (e.g. accept .NET 9
  #                    if asked for .NET 8 and 9 is installed).  False
  #                    = "LatestMinor" with applyPatches (more
  #                    conservative -- accept patch + minor bumps
  #                    only).
  #
  # The TFM-string switch at .NET 5.0 reflects Microsoft's rebrand:
  # everything before 5.0 was "netcoreapp" (3.1 etc), 5.0 onwards is
  # "net" (5.0, 6.0, 7.0...).  The package vcompare check picks the
  # right prefix automatically.
  #
  # Returns empty string if the version cannot be tokenized -- usually
  # means the embedder gave us a malformed version string.  Caller
  # treats empty as "skip writing the file".
  #
  proc getCoreClrRuntimeConfiguration {
          version {compatibility true} {latestMajor true} } {
    set tokens [getCoreClrRuntimeConfigurationTokens $version]
    if {[llength $tokens] == 0} then {return ""}
    set targetFrameworkMoniker [lindex $tokens 1]
    set version [lindex $tokens 3]

    if {$compatibility} then {
      if {$latestMajor} then {
        set extra {,
          "rollForward": "LatestMajor"
        }
      } else {
        set extra {,
          "rollForward": "LatestMinor",
          "applyPatches": true
        }
      }
    } else {
      set extra ""
    }

    return [string map [list \
        %targetFrameworkMoniker% $targetFrameworkMoniker \
        %version% $version %extra% $extra] [string trim {
      {
        "runtimeOptions": {
          "tfm": "%targetFrameworkMoniker%",
          "framework": {
            "name": "Microsoft.NETCore.App",
            "version": "%version%"
          }%extra%
        }
      }
    }]]
  }

  #
  # NOTE: This procedure is used to write the CoreCLR runtime configuration
  #       file, if necessary (i.e. it does not already exist and the package
  #       is being loaded for use with the CoreCLR).
  #
  # Composition: getCoreClrRuntimeConfiguration(version) builds the
  # JSON, writeFile dumps it.  No additional cleverness -- but the
  # write is unconditional (no "is it already there?" check at this
  # layer), so callers (setupForCoreClr) are responsible for the
  # not-already-present gate.
  #
  proc writeCoreClrRuntimeConfiguration { fileName version } {
    return [writeFile \
        $fileName [getCoreClrRuntimeConfiguration $version]]
  }

  #
  # "Is this directory already present in this PATH-style list?"
  # predicate.  Used by addToPath to avoid double-adding the runtime
  # directory if the embedder happened to set PATH already.
  #
  # Case-folds both inputs on Windows because filesystem case-
  # insensitivity means "C:\Program Files\dotnet" and "C:\program
  # files\dotnet" are the same directory but lsearch -exact would
  # see them as different.  Tcl 8.4 has no [lsearch -nocase]
  # (TIP #241 added it in 8.5), so we fall back to lowering the
  # whole list.
  #
  # The "shimmering" warning in the original NOTE refers to Tcl's
  # internal-representation conversion: applying [string tolower]
  # to a list forces the list rep to its string rep and back again.
  # Acceptable here because the input is bounded (a PATH split has
  # tens of entries) and called rarely (once per addToPath call).
  #
  # HACK: This procedure was blatantly stolen from "Eagle1.0/platform.eagle".
  #
  proc foundInPath { directories directory } {
    if {[isWindows]} then {
      #
      # HACK: Causes shimmering of "$directories" list representation.  Must
      #       use [string tolower] here anyhow because Tcl 8.4 lacks -nocase
      #       option for [lsearch] (please see TIP #241).
      #
      set directories [string tolower $directories]
      set directory [string tolower $directory]
    }

    if {[lsearch -exact $directories $directory] != -1} then {
      return true
    } else {
      return false
    }
  }

  #
  # Append a directory to the platform's loader search path env var.
  # The variable's name varies by platform:
  #
  #     Windows  PATH                  searched by LoadLibrary chains
  #     macOS    DYLD_LIBRARY_PATH     searched by dlopen
  #     Linux    LD_LIBRARY_PATH       searched by dlopen
  #
  # Used by setupForCoreClr to ensure the CoreCLR's runtime
  # directory (where coreclr.dll / libcoreclr.so / libcoreclr.dylib
  # live) is reachable when hostfxr later triggers a chain of
  # LoadLibrary / dlopen calls during runtime initialization.
  # Without this, those calls fail with vague "couldn't find
  # runtime" errors and the embedder is left scratching their head.
  #
  # Idempotent: foundInPath checks the existing value first, so
  # re-loads of Garuda don't accumulate path duplicates.
  #
  # Returns true if the variable was updated, false if the
  # directory was already present.
  #
  # The path-separator detection prefers tcl_platform(pathSeparator)
  # over hard-coding ; vs : because exotic platforms (some embedded
  # Tcl builds) override the separator.
  #
  # HACK: This procedure was blatantly stolen from "Eagle1.0/platform.eagle".
  #
  proc addToPath { directory } {
    global env
    global tcl_platform

    #
    # NOTE: This should work properly in both Tcl and Eagle.
    #       Normalize to an operating system native path.
    #
    set directory [file nativename $directory]

    #
    # NOTE: On Windows, use PATH; otherwise (i.e. Unix), use
    #       LD_LIBRARY_PATH.
    #
    if {[isWindows]} then {
      set name PATH
    } elseif {[isMacOS]} then {
      set name DYLD_LIBRARY_PATH
    } else {
      set name LD_LIBRARY_PATH
    }

    #
    # NOTE: Make sure the directory is not already in the
    #       loader search path.
    #
    if {[info exists tcl_platform(pathSeparator)]} then {
      set separator $tcl_platform(pathSeparator)
    } elseif {[isWindows]} then {
      set separator \;
    } else {
      set separator :
    }

    #
    # NOTE: Does the necessary environment variable exist?
    #
    if {[info exists env($name)]} then {
      #
      # NOTE: Grab the value of the environment variable.
      #
      set value $env($name)

      #
      # NOTE: Check if the directory is already present in the
      #       list from the environment.
      #
      if {![foundInPath [split $value $separator] $directory]} then {
        #
        # NOTE: Append the directory to the loader search path.
        #       This allows us to subsequently load DLLs that
        #       implicitly attempt to load other DLLs that are
        #       not in the application directory.
        #
        set env($name) [join [list $value $directory] $separator]

        #
        # NOTE: Yes, we altered the search path.
        #
        return true
      }
    } else {
      #
      # NOTE: Create the loader search path with the directory.
      #
      set env($name) $directory

      #
      # NOTE: Yes, we created the search path.
      #
      return true
    }

    #
    # NOTE: No, we did not alter the search path.
    #
    return false
  }

  #
  # Slurp a file into a Tcl_Obj as raw bytes.  Binary encoding +
  # binary translation = "no charset interpretation, no CRLF
  # rewriting" -- what we get out matches the file's bytes exactly.
  # Used for reading runtimeconfig.json (so we don't accidentally
  # smuggle a BOM into the JSON parser) and for getClrVersion's
  # PE-header byte parse.
  #
  proc readFile { fileName } {
    set channel [open $fileName RDONLY]
    fconfigure $channel -encoding binary -translation binary
    set result [read $channel]
    close $channel
    return $result
  }

  #
  # Counterpart of readFile.  Same binary mode (no encoding
  # translation, no CRLF rewriting).  Truncates the target file
  # if it exists, creates it otherwise.  Used to write the
  # runtimeconfig.json file Garuda generates for CoreCLR.
  #
  proc writeFile { fileName data } {
    set channel [open $fileName {WRONLY CREAT TRUNC}]
    fconfigure $channel -encoding binary -translation binary
    puts -nonewline $channel $data
    close $channel
    return ""
  }

  #
  # Extract the .NET CLR version that a managed assembly was built
  # against by reading a string out of the binary file directly.
  # Used by shouldUseMinimumClr to decide whether a Garuda.dll
  # variant needs the v2 or v4 CLR.
  #
  # Why parse the binary instead of asking .NET?  Because at the
  # point this is called, we have NOT YET committed to loading any
  # CLR.  The whole purpose of the proc is to inform the choice of
  # WHICH runtime to load.  Loading the .NET hosting interface to
  # ask "what CLR does this DLL want" would defeat the question.
  # So we read the bytes ourselves.
  #
  # The technique:
  #
  #   1. The CLR-version string lives in the .NET metadata header
  #      of the assembly's PE file, as a NUL-terminated UCS-2
  #      Pascal-style record preceded by the literal sentinel
  #      "ClrVersion\0" (also UCS-2).
  #   2. We slurp the file and search for that 22-byte sentinel.
  #   3. Then search forward for the next \x00\x00\x00 (the UCS-2
  #      NUL terminator, with a leading byte-order-leftover from
  #      the previous character).
  #   4. Convert that range from UTF-16 ("unicode" in Tcl's
  #      [encoding] terminology) back to a Tcl string.
  #
  # The result is a string like "v2.0.50727" or "v4.0.30319".
  # Returns empty string if the sentinel isn't found OR if running
  # in a safe interpreter (where readFile would fail anyway).
  #
  # The technique works because the sentinel is unique enough that
  # false positives are vanishingly rare in real .NET assemblies.
  # It is, however, fragile to changes in PE/COFF or .NET metadata
  # layout -- if Microsoft moves the version string, this proc has
  # to be updated.  In practice the layout has been stable for
  # 20+ years so the risk is low.
  #
  proc getClrVersion { fileName } {
    #
    # NOTE: This procedure may not work properly within a safe interpreter;
    #       therefore, handle that case specially.
    #
    if {![interp issafe] && [isValidFile $fileName]} then {
      #
      # NOTE: The string "ClrVersion\0", encoded in UCS-2, represented as
      #       byte values.
      #
      append header \x43\x00\x6C\x00\x72\x00\x56\x00\x65\x00\x72
      append header \x00\x73\x00\x69\x00\x6F\x00\x6E\x00\x00\x00

      #
      # NOTE: Read all the data from the package binary file.
      #
      set data [readFile $fileName]

      #
      # NOTE: Search for the header string within the binary data.
      #
      set index(0) [string first $header $data]

      #
      # NOTE: No header string, return nothing.
      #
      if {$index(0) == -1} then {
        return ""
      }

      #
      # NOTE: Advance the first index to just beyond the header.
      #
      incr index(0) [string length $header]

      #
      # NOTE: Search for the following NUL character, encoded in UCS-2,
      #       represented as byte values.  Due to how the characters are
      #       encoded, this search also includes the trailing zero byte
      #       from the previous character.
      #
      set index(1) [string first \x00\x00\x00 $data $index(0)]

      #
      # NOTE: No following NUL character, return nothing.
      #
      if {$index(1) == -1} then {
        return ""
      }

      #
      # NOTE: Grab the CLR version number embedded in the file data just
      #       after the header.
      #
      return [encoding convertfrom unicode [string range $data $index(0) \
          $index(1)]]
    }

    #
    # NOTE: This is a safe interpreter, for now just skip trying to read
    #       from the package binary file and return nothing.
    #
    return ""
  }

  #
  # NOTE: This procedure attempts to detect if the CLR / CoreCLR runtimes
  #       are installed on this machine.  If so, the appropriate variable
  #       specified by the caller will be set to non-zero.
  #
  # The detection technique is "is the matching Garuda shared library
  # present in the package's lib/ directory?".  Each variant of Garuda
  # (Garuda.dll for .NET Framework, GarudaCore.dll for CoreCLR -- see
  # getPackageBinaryFileNameOnly for the naming) is built against a
  # specific runtime and links against its hosting headers.  If the
  # binary is on disk, the runtime that binary targets is the one we
  # CAN load -- but presence of the binary does not, by itself, prove
  # the runtime is INSTALLED on this host.  The next layer up
  # (shouldUseCoreClr) does that final check via probe routines.
  #
  # The two output variables are written via [upvar] to the caller's
  # scope rather than returned because the proc reports two
  # independent boolean facts (CLR present? CoreCLR present?) and a
  # 2-element list return would lose the variable-name labels.
  # Caller idiom:
  #
  #   set haveClr false
  #   set haveCoreClr false
  #   attemptToDetectRuntimes haveClr haveCoreClr
  #
  # Returns nothing of consequence; caller reads the upvar'd
  # variables.  The maybeLogViaCommand calls leave a "Found" /
  # "Missing" trail in the log for each variant -- useful when
  # debugging "why didn't Garuda pick the runtime I expected?".
  #
  proc attemptToDetectRuntimes { haveClrVarName haveCoreClrVarName } {
    variable packageName
    variable packagePath

    upvar 1 $haveClrVarName haveClr
    upvar 1 $haveCoreClrVarName haveCoreClr

    if {[info exists packageName] && [info exists packagePath] && \
        [isValidDirectory $packagePath]} then {
      set fileName(1) [file join $packagePath \
          [getPackageBinaryFileNameOnly $packageName false]]; # CLR

      if {[isValidFile $fileName(1)]} then {
        maybeLogViaCommand \
            "Found CLR shared library \"$fileName(1)\" (installed)..."

        set haveClr true
      } else {
        maybeLogViaCommand \
            "Missing CLR shared library \"$fileName(1)\" (installed)..."
      }

      set fileName(2) [file join $packagePath \
          [getPackageBinaryFileNameOnly $packageName true]]; # CoreCLR

      if {[isValidFile $fileName(2)]} then {
        maybeLogViaCommand \
            "Found CoreCLR shared library \"$fileName(2)\" (installed)..."

        set haveCoreClr true
      } else {
        maybeLogViaCommand \
            "Missing CoreCLR shared library \"$fileName(2)\" (installed)..."
      }
    }
  }

  #
  # The runtime-selection algorithm.  Returns a boolean: true => CoreCLR,
  # false => .NET Framework.  Decision tree (first match wins):
  #
  #   1. Embedder explicitly set ::Garuda::useCoreClr (or environment
  #      variable hasUseCoreClr equivalent).  If so, that wins --
  #      embedders are trusted to know what they want.  This is the
  #      "I have a Linux box, force CoreCLR" / "this AppDomain code
  #      requires .NET Framework, force CLR" path.
  #
  #   2. Otherwise, in a non-safe interp, run attemptToDetectRuntimes
  #      to see which shared-library variants are on disk.  Then,
  #      based on the configured packageBinaryFileNameOnly:
  #        * If it contains "Core" (i.e. we're configured for the
  #          CoreCLR variant), require CoreCLR detection AND verify
  #          a CoreCLR is actually installed on this host (not just
  #          that the .so is shipping with us).  If both, return
  #          true.
  #        * If it does NOT contain "Core" (.NET Framework variant),
  #          require Framework detection AND verify at least one
  #          ::Garuda::clrVersions entry is installed via
  #          checkDotNetFrameworkDirectory.  If so, return false.
  #
  #   3. If neither runtime can be confirmed, fall through to the
  #      caller-supplied default.  If the default is unparseable as
  #      a boolean, return false (the legacy-compatible answer --
  #      historically this package was .NET Framework only).
  #
  # The "configured filename gates the answer" check at step 2 is a
  # critical correctness invariant: you cannot load Garuda.dll into a
  # CoreCLR-hosting process and you cannot load GarudaCore.dll into a
  # .NET Framework process.  The two binaries link against different
  # hosting libraries.  This proc therefore refuses to recommend a
  # runtime for which we don't have the matching binary, even if
  # that runtime is installed on the host.
  #
  # WARNING: Other than appending to the configured log file, if any, this
  #          procedure is absolutely forbidden from having any side effects.
  #
  # The no-side-effects rule matters because this proc may be called
  # multiple times during startup (once during setupHelperVariables to
  # decide which configuration variables to populate, again during
  # setupForCoreClr's gate, possibly more).  Any side effect would
  # compound across calls.  In particular: do NOT cache the answer in
  # a namespace variable from inside this proc -- let the caller cache
  # if they want, so the cache lifetime is explicit.
  #
  proc shouldUseCoreClr { {default ""} } {
    global tcl_platform
    variable clrVersions
    variable packageBinaryFileNameOnly

    #
    # NOTE: The package -OR- environment has been explicitly configured
    #       to use CoreCLR runtime?
    #
    if {[hasUseCoreClr result]} then {
      return $result; # EXPLICIT
    }

    #
    # NOTE: Is the Garuda extension shared library even available with
    #       this package deployment?  First, check for CoreCLR variant;
    #       failing that, check for the .NET Framework variant.  Either
    #       way, log our discoveries.
    #
    if {![interp issafe]} then {
      #
      # NOTE: Attempt to detect if the CLR / CoreCLR runtimes.  If one
      #       of them is missing, it cannot be selected.
      #
      set haveClr false
      set haveCoreClr false

      attemptToDetectRuntimes haveClr haveCoreClr

      #
      # NOTE: What is the primary file name configured for use with this
      #       package?  This is important because it must match with the
      #       selected runtime, i.e. you cannot load "Garuda.dll" into a
      #       CoreCLR-based process and you cannot load "GarudaCore.dll"
      #       into a CLR-based process.
      #
      if {[info exists packageBinaryFileNameOnly] && \
          [string match *Core* $packageBinaryFileNameOnly]} then {
        if {$haveCoreClr && [info exists tcl_platform(machine)]} then {
          set platform [getCoreClrPlatformRid $tcl_platform(machine)]

          if {[string length $platform] > 0} then {
            if {[checkCoreClrDirectories $platform version]} then {
              maybeLogViaCommand "Using CoreCLR $version (installed)..."
              return true
            }
          }
        }
      } else {
        if {$haveClr && [info exists clrVersions]} then {
          foreach version $clrVersions {
            if {[checkDotNetFrameworkDirectory $version]} then {
              maybeLogViaCommand "Using CLR $version (installed)..."
              return false
            }
          }
        }
      }
    }

    #
    # NOTE: Fallback to the default setting, which depends on the caller.
    #
    if {[string is true -strict $default]} then {
      maybeLogViaCommand "Using CoreCLR (default)..."
    } else {
      maybeLogViaCommand "Not using CoreCLR (default)..."
    }

    #
    # NOTE: At this point, check if the caller provided default can be
    #       used directly; otherwise, fallback to the system default,
    #       which will be false for legacy compatibility reasons.
    #
    if {[string is boolean -strict $default]} then {
      return $default
    }

    return false
  }

  #
  # ".NET Framework v2 vs v4?" decision.  Returns true => use the
  # minimum supported CLR (v2.0.50727), false => use the latest
  # (v4.0.30319).  Only consulted when shouldUseCoreClr returned
  # false (i.e. we've already chosen the .NET Framework path).
  #
  # Decision tree (first match wins):
  #
  #   1. ::Garuda::useMinimumClr explicitly set => that wins.
  #   2. UseMinimumClr environment variable set => that wins.
  #   3. The latest CLR is NOT installed (checkDotNetFrameworkDirectory
  #      returns false for the highest entry in clrVersions) =>
  #      forced to true (we have no choice).
  #   4. ::Garuda::env(NoClrVersion) NOT set, and getClrVersion
  #      reports the assembly was built for the minimum CLR =>
  #      true.
  #   5. Otherwise => the caller-supplied default (defaults to true,
  #      conservative -- don't accidentally bind to a runtime the
  #      assembly wasn't built for).
  #
  # Step 4 is the interesting one -- it parses the .NET assembly's
  # metadata to see which CLR version it was compiled against, so a
  # CLRv2-targeted Eagle.dll automatically picks v2 even if v4 is
  # installed.  Without that, we'd default to v4 and the assembly
  # would fail to load with a TypeLoadException.  The NoClrVersion
  # env var is the override for embedders who specifically want to
  # force a CLR upgrade (rare, mostly used during testing).
  #
  # WARNING: Other than appending to the configured log file, if any, this
  #          procedure is absolutely forbidden from having any side effects.
  #
  proc shouldUseMinimumClr { fileName {default true} } {
    global env
    variable clrVersions
    variable useMinimumClr

    #
    # NOTE: The package has been configured to use the minimum supported CLR
    #       version; therefore, return true.
    #
    if {[info exists useMinimumClr] && $useMinimumClr} then {
      maybeLogViaCommand "Using minimum CLR version (variable)..."
      return true
    }

    #
    # NOTE: The environment has been configured to use the minimum supported
    #       CLR version?
    #
    if {[info exists env(UseMinimumClr)]} then {
      set result $env(UseMinimumClr)

      if {$result} then {
        maybeLogViaCommand \
            "Using minimum CLR version (environment)..."
      } else {
        maybeLogViaCommand \
            "Using latest CLR version (environment)..."
      }

      return $result
    }

    #
    # NOTE: The latest supported version of the CLR is not installed on this
    #       machine; therefore, return true.
    #
    if {![checkDotNetFrameworkDirectory [lindex $clrVersions end]]} then {
      maybeLogViaCommand "Using minimum CLR version (missing)..."
      return true
    }

    #
    # NOTE: Unless forbidden from doing so, check the version of the CLR that
    #       this package binary was compiled for (i.e. the CLR version is
    #
    if {![info exists env(NoClrVersion)]} then {
      set version [getClrVersion $fileName]

      #
      # NOTE: The CLR version was not queried from the package binary, return
      #       the specified default result.
      #
      if {[string length $version] == 0} then {
        if {$default} then {
          maybeLogViaCommand "Using minimum CLR version (default)..."
        } else {
          maybeLogViaCommand "Using latest CLR version (default)..."
        }

        return $default
      }

      #
      # NOTE: The CLR version queried from the package binary is the minimum
      #       supported; therefore, return true.
      #
      if {$version eq [lindex $clrVersions 0]} then {
        maybeLogViaCommand "Using minimum CLR version (assembly)..."
        return true
      }
    }

    #
    # NOTE: Ok, just use the latest supported version of the CLR.
    #
    maybeLogViaCommand "Using latest CLR version..."
    return false
  }

  #
  # "Should the bridge create the Eagle interpreter inside its own
  # AppDomain?" decision.  Returns true => yes (.NET Framework only;
  # CoreCLR ignores this).  Folded into the methodFlags bitfield via
  # MaybeCombineMethodFlags's METHOD_USE_ISOLATION bit.
  #
  # Why isolate?  An isolated Eagle interpreter cannot affect (or be
  # affected by) the host AppDomain's loaded assemblies, type system,
  # or static state.  Useful when the embedder loads multiple Eagle
  # interpreters that should not see each other's globals, or when
  # the embedder wants to be able to unload Eagle cleanly (only
  # AppDomain unload truly unloads managed assemblies).
  #
  # Same env-var-then-variable-then-default pattern as the other
  # should* predicates.  Defaults to false because isolation has a
  # nontrivial AppDomain-creation cost and most embedders don't
  # need the separation.
  #
  # WARNING: Other than appending to the configured log file, if any, this
  #          procedure is absolutely forbidden from having side effects.
  #
  proc shouldUseIsolation {} {
    global env
    variable useIsolation

    #
    # NOTE: The package has been configured to use interpreter isolation;
    #       therefore, return true.
    #
    if {[info exists useIsolation] && $useIsolation} then {
      maybeLogViaCommand "Using interpreter isolation (variable)..."
      return true
    }

    #
    # NOTE: The environment has been configured to use interpreter isolation?
    #
    if {[info exists env(UseIsolation)]} then {
      set result $env(UseIsolation)

      if {$result} then {
        maybeLogViaCommand "Using interpreter isolation (environment)..."
      } else {
        maybeLogViaCommand "Not using interpreter isolation (environment)..."
      }

      return $result
    }

    #
    # NOTE: Ok, disable interpreter isolation.
    #
    maybeLogViaCommand "Not using interpreter isolation..."
    return false
  }

  #
  # "Should the Eagle interpreter run in safe mode?" decision.
  # Returns true => yes (folded into methodFlags as the
  # METHOD_USE_SAFE_INTERP bit).  Eagle's safe mode disables
  # filesystem access, exec, network operations, and other commands
  # that could compromise the host process -- equivalent in spirit to
  # Tcl's [interp create -safe] but applied to the Eagle side.
  #
  # Embedders that load untrusted Eagle scripts (or scripts that
  # they want sandbox-restricted regardless of trust) set this true.
  # Most embedders leave it false because Eagle scripts are usually
  # trusted code under their own control.
  #
  # Same env-var/variable/default pattern.
  #
  # WARNING: Other than appending to the configured log file, if any, this
  #          procedure is absolutely forbidden from having side effects.
  #
  proc shouldUseSafeInterp {} {
    global env
    variable useSafeInterp

    #
    # NOTE: The package has been configured to use a "safe" interpreter;
    #       therefore, return true.
    #
    if {[info exists useSafeInterp] && $useSafeInterp} then {
      maybeLogViaCommand "Using a \"safe\" interpreter (variable)..."
      return true
    }

    #
    # NOTE: The environment has been configured to use a "safe" interpreter?
    #
    if {[info exists env(UseSafeInterp)]} then {
      set result $env(UseSafeInterp)

      if {$result} then {
        maybeLogViaCommand "Using a \"safe\" interpreter (environment)..."
      } else {
        maybeLogViaCommand "Not using a \"safe\" interpreter (environment)..."
      }

      return $result
    }

    #
    # NOTE: Ok, disable "safe" interpreter use.
    #
    maybeLogViaCommand "Not using a \"safe\" interpreter..."
    return false
  }

  #
  # Return the platform's preferred temp directory.  The implementation
  # is the "create-and-immediately-discard a tempfile, then [file
  # dirname] it" trick -- Tcl gives us no direct accessor, but [file
  # tempfile] (8.6+) does the platform-specific thing internally and
  # the resulting path's directory is the answer.  Used by
  # getRuntimeConfigPath to locate where to write the
  # runtimeconfig.json when no embedder-supplied location exists.
  #
  # The Tcl 8.6 dependency is a soft constraint -- Garuda's general
  # 8.4 floor doesn't apply to CoreCLR-only paths, since CoreCLR
  # support is itself newer than 8.4.
  #
  proc getTemporaryDirectory {} {
    #
    # HACK: The [file tempfile] sub-command requires Tcl 8.6.
    #
    close [file tempfile fileName]
    file delete $fileName
    return [file dirname $fileName]
  }

  #
  # Compute where to write the runtimeconfig.json file that
  # writeCoreClrRuntimeConfiguration produces (and that hostfxr
  # demands).  Three sources, in priority:
  #
  #   1. RuntimeConfigPath environment variable.  Hard override --
  #      embedder has chosen a specific path.
  #
  #   2. Otherwise, in a non-safe interp, derive from
  #      [info nameofexecutable]:
  #        <tempdir>/<exe-basename>-<pid>-<seconds>.runtimeconfig.json
  #      The pid + seconds tail keeps the file unique across
  #      concurrent runs of the same executable; tempdir prevents
  #      cluttering the user's working directory.  The TEMP /
  #      TMPDIR env-var fallbacks handle hosts where [file
  #      tempfile] (Tcl 8.6+) isn't available.
  #
  #   3. Safe interp => return empty.  We can't write anywhere
  #      meaningful in a safe interp; safe-interp embedders are
  #      expected to pre-set RuntimeConfigPath to a path their
  #      controlling untrusted-side scripts can't tamper with.
  #
  # The unique-per-pid filename matters because multiple Tcl
  # processes may load Garuda concurrently and we don't want them
  # racing on the same temp file (one process truncating while
  # another is reading).  The pid + clock-seconds combo is
  # collision-free in practice.
  #
  proc getRuntimeConfigPath {} {
    global env

    if {[info exists env(RuntimeConfigPath)]} then {
      set path $env(RuntimeConfigPath)

      maybeLogViaCommand \
          "Using runtime configuration path \"$path\" (environment)..."

      return $path
    } elseif {![interp issafe]} then {
      set fileName [info nameofexecutable]

      maybeLogViaCommand "Detected executable file name \"$fileName\"..."

      set fileNameOnly [file tail $fileName]

      if {[string length $fileNameOnly] > 0 && \
          [isValidFile $fileName]} then {
        append fileNameOnly - [pid] - [clock seconds]
        append fileNameOnly .runtimeconfig.json

        if {[catch {getTemporaryDirectory} directory]} then {
          if {[isWindows]} then {
            if {[info exists env(TEMP)]} then {
              set directory $env(TEMP)
            } else {
              set directory [file dirname $fileName]
            }
          } else {
            if {[info exists env(TMPDIR)]} then {
              set directory $env(TMPDIR)
            } else {
              set directory /tmp; # TODO: Portable?
            }
          }
        }

        set path [file join $directory $fileNameOnly]

        maybeLogViaCommand \
            "Using runtime configuration path \"$path\" (default)..."

        return $path
      }
    }

    return ""
  }

  #
  # Map (packageName, useCoreClr) to the platform-specific
  # shared-library file name.  Five components combine:
  #
  #     <prefix><name><variant><ext>
  #
  # where prefix is "" on Windows / "lib" elsewhere, name is the
  # package name (typically "Garuda"), variant is "Core" for the
  # CoreCLR build / "" for .NET Framework, and ext comes from
  # [info sharedlibextension] (".dll" / ".so" / ".dylib").
  #
  # Examples:
  #     Garuda      false   Win32  -> Garuda.dll
  #     Garuda      true    Win32  -> GarudaCore.dll
  #     Garuda      true    Linux  -> libGarudaCore.so
  #     Garuda      true    macOS  -> libGarudaCore.dylib
  #
  # Used by attemptToDetectRuntimes to know which file to look
  # for when checking what the package shipped against.
  #
  proc getPackageBinaryFileNameOnly { packageName useCoreClr } {
    set result [expr {[isWindows] ? "" : "lib"}]

    if {[string is true -strict $useCoreClr]} then {
      append result ${packageName}Core
    } else {
      append result ${packageName}
    }

    append result [info sharedlibextension]
    return $result
  }

  #
  # Return the fully-qualified Eagle assembly type name to instantiate
  # for the bridge.  Differs by runtime:
  #
  #   useCoreClr=true:
  #     "Eagle._Components.Public.NativePackage, Eagle, Version=1.0,
  #      Culture=neutral"
  #
  #   useCoreClr=false:
  #     "Eagle._Components.Public.NativePackage"
  #
  # The CoreCLR form requires the full assembly-qualified-name
  # because hostfxr's load_assembly_and_get_function_pointer takes
  # AQNs.  The .NET Framework form gets away with the type name
  # only because ICLRRuntimeHost::ExecuteInDefaultAppDomain takes a
  # separate assembly-path argument.
  #
  proc getPackageAssemblyTypeName { useCoreClr } {
    if {[string is true -strict $useCoreClr]} then {
      return "Eagle._Components.Public.NativePackage,\
              Eagle, Version=1.0, Culture=neutral"
    } else {
      return Eagle._Components.Public.NativePackage
    }
  }

  #
  # Parse the env(MethodFlags) string into a methodFlags integer.
  # The env-var format is a comma-or-space-separated list of flag
  # specifiers, each of which is one of:
  #
  #     METHOD_<NAME>      add the named METHOD_<NAME> bit
  #     +METHOD_<NAME>     same -- explicit "set" sign
  #     -METHOD_<NAME>     unset the named bit
  #     <integer>          add literal value
  #     +<integer>         same
  #     -<integer>         AND-NOT the literal value
  #     0xHEX              add hex value (with sign prefix accepted)
  #
  # Bit names resolve to ::Garuda::METHOD_<NAME> values populated
  # by setupMethodFlagsVariables.  Unknown names produce an error
  # in strict mode, are silently skipped otherwise.
  #
  # Used by setupHelperVariables to seed ::Garuda::methodFlags from
  # the environment, giving embedders a way to twiddle bridge
  # behavior without writing Tcl code.
  #
  # Returns the assembled bitfield as a Tcl integer.  The default
  # (no env var or empty value) is 0 -- meaning "use built-in
  # defaults", which the C side fills in.
  #
  proc getMethodFlags { {strict false} } {
    global env
    variable METHOD_LOG_EXECUTE
    variable METHOD_NONE
    variable METHOD_PROTOCOL_V1R2
    variable METHOD_USE_ISOLATION
    variable METHOD_USE_SAFE_INTERP

    set result 0x0; # SYSTEM DEFAULT (COMPAT: Eagle beta)

    if {[info exists env(MethodFlags)]} then {
      foreach flag [split \
          [string map [list , " "] $env(MethodFlags)] " "] {
        set flag [string trim $flag]

        if {[string length $flag] > 0} then {
          set add true

          if {[string index $flag 0] eq "-"} then {
            set flag [string range $flag 1 end]
            set add false
          }

          if {[string index $flag 0] eq "+"} then {
            set flag [string range $flag 1 end]
            set add true
          }

          if {[string is integer -strict $flag] || \
              [regexp -nocase -- {^0x[0-9A-F]+$} $flag]} then {
            if {$add} then {
              set result [expr {$result | $flag}]
            } else {
              set result [expr {$result & (~$flag)}]
            }

            continue
          }

          if {[regexp -- {^METHOD_[0-9A-Z_]+$} $flag]} then {
            set varName ::[namespace current]::$flag; # ::Garuda::METHOD_*

            if {[info exists $varName]} then {
              if {$add} then {
                set result [expr {$result | [set $varName]}]
              } else {
                set result [expr {$result & (~[set $varName])}]
              }

              continue
            }
          }

          if {$strict} then {
            error "unrecognized method flag value: $flag"
          }
        }
      }
    }

    return $result
  }

  #
  # Resolve a list of environment-variable names (with optional
  # per-variable suffixes) into a list of valid file/directory paths.
  # Used by setupAndLoad's useEnvironment branch to gather candidate
  # locations from EAGLE_INSTALL, EagleInstall, etc.
  #
  # The "varSuffixes" cross-product is for variants like
  # ${var}_X64 / ${var}_DEBUG / ${var}_NETSTANDARD20 that some
  # installers set alongside the bare variable.  Each suffix is
  # checked first (most-specific), then the bare name (most-generic),
  # so a more-specific suffix wins when both exist.
  #
  # All values are [string trim]'d to handle the whitespace that
  # some shells leave around exported variables.  Each candidate
  # is validated via isValidDirectory / isValidFile before being
  # included -- non-existent paths are silently dropped, not
  # propagated to confuse downstream probe code.
  #
  proc getEnvironmentPathList { varNames varSuffixes } {
    global env

    set result [list]

    #
    # NOTE: Check for a valid file or directory name in the values of each
    #       environment variable name specified by the caller.  If so, add
    #       it to the result list.
    #
    foreach varName $varNames {
      #
      # NOTE: Check each of the environment variable name suffixes specified
      #       by the caller prior to trying the environment variable name by
      #       itself.
      #
      foreach varSuffix $varSuffixes {
        set newVarName ${varName}${varSuffix}

        if {[info exists env($newVarName)]} then {
          set path [string trim $env($newVarName)]

          if {[isValidDirectory $path] || [isValidFile $path]} then {
            lappend result $path
          }
        }
      }

      if {[info exists env($varName)]} then {
        set path [string trim $env($varName)]

        if {[isValidDirectory $path] || [isValidFile $path]} then {
          lappend result $path
        }
      }
    }

    return $result
  }

  #
  # Walk a Windows registry key looking for a named value under each
  # subkey, collecting valid paths.  Used by setupAndLoad's
  # useRegistry branch to find Eagle installs that registered
  # themselves under HKLM\Software\<...> via the legacy installer.
  #
  # Algorithm: enumerate subkeys of $rootKeyName (each typically a
  # version string like "1.0"), read the named value (typically
  # "Path") from each, validate, accumulate.
  #
  # No-op on non-Windows (registry doesn't exist).  Also a no-op if
  # `package require registry` fails -- Tcl-without-the-registry-
  # package is rare on Windows but possible on stripped-down builds.
  #
  # The registry probe is the OLDEST of Garuda's discovery paths; it
  # predates the env-var conventions and the per-package lib/
  # subdirectory.  Modern installs typically don't write to the
  # registry, so this branch is silent on a clean modern host --
  # it remains for backward compat with legacy installs.
  #
  proc getRegistryPathList { rootKeyName valueName } {
    set result [list]

    if {[isWindows]} then {; # NOTE: Registry for Tcl on Windows only.
      if {[catch {package require registry}] == 0 && \
          [catch {registry keys $rootKeyName} keyNames] == 0} then {
        foreach keyName $keyNames {
          set subKeyName $rootKeyName\\$keyName

          if {[catch {
            registry get $subKeyName $valueName
          } path] == 0} then {
            set path [string trim $path]

            if {[isValidDirectory $path] || \
                [isValidFile $path]} then {
              lappend result $path
            }
          }
        }
      }
    }

    return $result
  }

  #
  # Return a list of candidate directories to search for Eagle.dll, derived
  # from the Tcl interpreter's own [info library] location and walked
  # upward toward the volume root.
  #
  # The walk pattern is: starting from [info library] (typically
  # something like "/usr/local/lib/tcl8.6" or "C:/Tcl/lib/tcl8.6"),
  # check several Eagle-related subdirectory names at each level, then
  # ascend via [file dirname] and repeat -- stopping when we hit any
  # filesystem volume root (so we don't start probing "/" or "C:/"
  # itself, which would scan the whole filesystem).
  #
  # The six per-level subdirectory probes are:
  #   <dir>/Eagle/bin,  <dir>/bin,  <dir>/Eagle,
  #   <parent-of-dir>/Eagle/bin,  <parent-of-dir>/bin,  <parent-of-dir>/Eagle
  #
  # The redundancy is deliberate -- different deployments place Eagle.dll
  # at different points in their hierarchy, and a small number of
  # duplicate stat calls is far cheaper than missing the assembly and
  # forcing the embedder to debug a load failure.
  #
  # The empty-volumes guard at the top is a paranoid edge: [file volumes]
  # has been observed to return empty on some embedded Tcl builds; with
  # no volume markers the upward walk would never terminate.  Better to
  # return empty than loop forever.
  #
  # Returned list is unordered (caller can [lsort -unique] or fold into
  # a larger list).  Each entry is verified to exist via isValidDirectory
  # / isValidFile before being included -- this proc never returns a
  # non-existent path.
  #
  proc getLibraryPathList {} {
    #
    # NOTE: Grab the list of volumes mounted on the local machine.
    #
    set volumes [file volumes]

    #
    # NOTE: If there are no volumes, the search loop in this procedure will
    #       not work correctly; therefore, just return an empty list in that
    #       case.
    #
    if {[llength $volumes] == 0} then {
      return [list]
    }

    #
    # TODO: Start out with an empty list of candidate paths.  Then, use the
    #       Tcl core script library path as the basis for searching for the
    #       Eagle CLR assembly directory.  In the future, additional script
    #       library paths may need to be added here.
    #
    set result [list]

    foreach directory [list [info library]] {
      #
      # NOTE: The directory name cannot be an empty string.  In addition,
      #       it cannot be the root of any volume, because that condition
      #       is used to mark the end of the search; however, within the
      #       loop body itself, the internal calls to [file dirname] MAY
      #       refer to the root of a volume (i.e. when joining candidate
      #       directory names with it).
      #
      while {[string length $directory] > 0 && \
          [lsearch -exact $volumes $directory] == -1} {
        set path [file join $directory Eagle bin]

        if {[isValidDirectory $path] || [isValidFile $path]} then {
          lappend result $path
        }

        set path [file join $directory bin]

        if {[isValidDirectory $path] || [isValidFile $path]} then {
          lappend result $path
        }

        set path [file join $directory Eagle]

        if {[isValidDirectory $path] || [isValidFile $path]} then {
          lappend result $path
        }

        set path [file join [file dirname $directory] Eagle bin]

        if {[isValidDirectory $path] || [isValidFile $path]} then {
          lappend result $path
        }

        set path [file join [file dirname $directory] bin]

        if {[isValidDirectory $path] || [isValidFile $path]} then {
          lappend result $path
        }

        set path [file join [file dirname $directory] Eagle]

        if {[isValidDirectory $path] || [isValidFile $path]} then {
          lappend result $path
        }

        set directory [file dirname $directory]
      }
    }

    return $result
  }

  #
  # Cross-product expansion: for every (directory, configuration,
  # subDirectory) triple, produce the candidate path
  #     <directory>/<configuration>/Eagle/bin/<subDirectory>
  # and include only the ones that exist.
  #
  # Used by setupAndLoad's useRelativePath branch to enumerate all
  # the places Eagle.dll might live RELATIVE to the package's own
  # lib/ directory and a few ancestors.  Targets developer-tree
  # layouts where Eagle's source-tree build outputs are in
  # standard configuration-specific subdirs (e.g. Debug/Eagle/bin/
  # NetStandard20/).
  #
  # The fixed "Eagle/bin" component is the convention Eagle's own
  # build system writes to; if a developer is using a different
  # layout, useEnvironment / useRegistry / useLibrary are the
  # paths that will find their Eagle.dll instead.
  #
  proc getRelativePathList { directories configurations subDirectories } {
    set result [list]

    foreach directory $directories {
      foreach configuration $configurations {
        foreach subDirectory $subDirectories {
          set path [file join \
              $directory $configuration Eagle bin $subDirectory]

          if {[isValidDirectory $path] || [isValidFile $path]} then {
            lappend result $path
          }

          set path [file join \
              $directory $configuration bin $subDirectory]

          if {[isValidDirectory $path] || [isValidFile $path]} then {
            lappend result $path
          }

          set path [file join \
              $directory $configuration Eagle $subDirectory]

          if {[isValidDirectory $path] || [isValidFile $path]} then {
            lappend result $path
          }

          set path [file join $directory $configuration $subDirectory]

          if {[isValidDirectory $path] || [isValidFile $path]} then {
            lappend result $path
          }
        }
      }
    }

    return $result
  }

  #
  # Probe ONE assembly-file location formed by the cross-product of the
  # given (directory, configuration, subDirectory, fileName).  Returns
  # the full path on success, empty string on failure.  This is the
  # innermost-loop helper for findAssemblyFile.
  #
  # Why so many candidate path shapes?
  #
  #   The "where in your tree does Eagle.dll live?" question has
  #   different answers for different deployment scenarios:
  #
  #     - Eagle source tree, dev build:
  #         <root>/Eagle/bin/<Configuration>/bin/<SubDirectory>/<CLR>/Eagle.dll
  #     - Eagle source tree, release build (no <Configuration>):
  #         <root>/Eagle/bin/<SubDirectory>/<CLR>/Eagle.dll
  #     - Installed via NuGet / xcopy:
  #         <root>/bin/<Configuration>/<SubDirectory>/<CLR>/Eagle.dll
  #     - Flat installation:
  #         <root>/<SubDirectory>/Eagle.dll
  #
  #   Plus variants where <CLR> is omitted (older deployments before
  #   the CLR-version split), where <Configuration> is absent (release
  #   builds), and where assemblyBaseName ("Eagle") is or isn't part
  #   of the path.  This proc enumerates 8 path shapes when
  #   $configuration is non-empty and 4 when it is empty.
  #
  # The "<CLR>" component is one of CoreCLR / CLRv2 / CLRv4 chosen by
  # the runtime-selection logic -- see shouldUseCoreClr (returns the
  # CoreCLR vs CLR axis) and shouldUseMinimumClr (returns CLRv2 vs
  # CLRv4 within the .NET Framework branch).  A wrong CLR-pinned
  # assembly would FAIL at load time inside the C side, so this
  # probe deliberately tries the CLR-tagged path FIRST and the
  # untagged path as a backup -- embedders who only ship one variant
  # can use either layout.
  #
  # The "directory might already be a file" early-exit at the top is a
  # convenience for callers who pre-resolved an exact path: pass it as
  # `directory` with empty/wildcard configuration/subDirectory/fileName
  # and this proc returns it unchanged if the file exists.  Used by
  # the embedder-pre-set-assemblyPath fast path.
  #
  proc probeAssemblyFile { directory configuration subDirectory fileName } {
    variable assemblyBaseName
    variable packageBinaryFileName

    set path $directory; # maybe it is really a file?

    if {[isValidFile $path]} then {
      return $path
    }

    if {[shouldUseCoreClr]} then {
      set clrPath CoreCLR
    } elseif {[shouldUseMinimumClr $packageBinaryFileName]} then {
      set clrPath CLRv2
    } else {
      set clrPath CLRv4
    }

    if {[string length $configuration] > 0} then {
      set path [file join \
          $directory $assemblyBaseName bin $configuration bin \
          $subDirectory $clrPath $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join \
          $directory $assemblyBaseName bin $configuration bin \
          $subDirectory $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join \
          $directory bin $configuration bin $subDirectory \
          $clrPath $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join \
          $directory bin $configuration bin $subDirectory \
          $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join \
          $directory $assemblyBaseName bin $configuration \
          $subDirectory $clrPath $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join \
          $directory $assemblyBaseName bin $configuration \
          $subDirectory $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join \
          $directory bin $configuration $subDirectory $clrPath \
          $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join \
          $directory bin $configuration $subDirectory $fileName]

      if {[isValidFile $path]} then {
        return $path
      }
    } else {
      set path [file join \
          $directory $assemblyBaseName bin $subDirectory $clrPath \
          $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join \
          $directory $assemblyBaseName bin $subDirectory $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join \
          $directory bin $subDirectory $clrPath $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join \
          $directory bin $subDirectory $fileName]

      if {[isValidFile $path]} then {
        return $path
      }
    }

    return ""
  }

  #
  # Cross-product probe: walk every (directory, configuration,
  # subDirectory, fileName) combination and return the first existing
  # match.  This is the orchestrator that drives probeAssemblyFile.
  #
  # The four input lists multiply out to N*M*K*L candidate locations,
  # which sounds expensive but in practice each list is short
  # (directories ~10, configurations 2 (Debug/Release), subDirectories
  # ~5 (TFM names), fileNames 2-3 (Eagle.dll variants)).  The
  # arithmetic peaks at a few hundred [file exists] calls -- cheap
  # enough to do once at startup.
  #
  # Loop order is significant: directories outermost, fileNames
  # innermost.  This means the proc finds the first directory that
  # contains ANY acceptable Eagle.dll variant, then within that dir
  # picks the first configuration / subDir / fileName that exists.
  # An embedder who wants to control which directory wins puts their
  # preferred dir first in the directories list (which is exactly
  # what setupAndLoad does -- useRelativePath entries come before
  # useEnvironment, registry, library entries).
  #
  # Returns the path of the first match, or empty string if none of
  # the cross-product candidates exists.  Caller treats empty as "no
  # Eagle.dll found anywhere we know how to look", which downstream
  # falls back to the package-dir default (and almost certainly
  # fails at C-side load time, but produces a deterministic error
  # message).
  #
  proc findAssemblyFile {
          directories configurations subDirectories fileNames } {
    foreach directory $directories {
      foreach configuration $configurations {
        foreach subDirectory $subDirectories {
          foreach fileName $fileNames {
            set path [probeAssemblyFile \
                $directory $configuration $subDirectory $fileName]

            if {[isValidFile $path]} then {
              return $path
            }
          }
        }
      }
    }

    return ""
  }

  #############################################################################
  #************************ PACKAGE HELPER PROCEDURES *************************
  #############################################################################

  #
  # The "is Eagle actually loaded and responsive?" predicate.
  # Exported from this namespace into the global namespace
  # (see the [namespace export]/[namespace import] dance at the
  # bottom of helper.tcl) so embedders can write
  #     if {[haveEagle version]} then { ... }
  # without qualifying with "::Garuda::".
  #
  # Three-step verification, in increasing depth:
  #
  #   1. [info commands ::eagle] -- does the [eagle] command even
  #      exist?  Created by Garuda_Init when the bridge starts.
  #   2. Run a trivial Eagle script that reads
  #      $::tcl_platform(engine) and check the answer is "Eagle".
  #      A non-Eagle command named "eagle" (unlikely but not
  #      impossible -- embedder could have created one) would fail
  #      here.
  #   3. Pull $::eagle_platform(patchLevel) and verify it looks
  #      like a 4-part version (e.g. "1.0.7654.32109").  This
  #      confirms we have a fully-initialized Eagle, not one in
  #      the middle of constructing.
  #
  # If all three pass and varName was supplied, the patch level
  # is written into the caller's varName.  Returns true / false.
  #
  # The caller can use this either to gate optional
  # Eagle-dependent code paths or to wait for the bridge to
  # complete startup (the third check rejects partially-
  # constructed bridges).
  #
  proc haveEagle { {varName ""} } {
    #
    # NOTE: Attempt to determine if Eagle has been loaded successfully and is
    #       currently available for use.  First, check that there is a global
    #       command named "eagle".  Second, make sure we can use that command
    #       to evaluate a trivial Eagle script that fetches the name of the
    #       script engine itself from the Eagle interpreter.  Finally, compare
    #       that result with "eagle" to make sure it is really Eagle.
    #
    if {[llength [info commands ::eagle]] > 0 && [catch {::eagle {
      set ::tcl_platform(engine)
    }} engine] == 0 && [string equal -nocase $engine Eagle]} then {
      #
      # NOTE: Ok, it looks like Eagle is loaded and ready for use.  If the
      #       caller wants the patch level, use the specified variable name
      #       to store it in the context of the caller.
      #
      if {[string length $varName] > 0} then {
        upvar 1 $varName version
      }

      #
      # NOTE: Fetch full patch level of the Eagle script engine and verify
      #       the result looks like a formally correct patch level using a
      #       suitable regular expression.
      #
      set pattern {^\d+\.\d+\.\d+\.\d+$}

      if {[catch {::eagle {
        set ::eagle_platform(patchLevel)
      }} version] == 0 && [regexp -- $pattern $version]} then {
        return true
      }
    }

    return false
  }

  #############################################################################
  #********************* PACKAGE VARIABLE SETUP PROCEDURE *********************
  #############################################################################

  #
  # Define the ::Garuda::METHOD_* namespace variables that mirror
  # the C-side MethodFlags enum (see GarudaInt.h).  Tcl-side code
  # uses the names ::Garuda::METHOD_USE_ISOLATION etc rather than
  # bare hex constants so that embedders can write methodFlag
  # configuration in readable form.
  #
  # The Tcl-side values MUST match the C enum exactly -- getMethodFlags
  # OR-merges these into the methodFlags integer that gets passed
  # across the bridge protocol, and the C side decodes by bit value.
  # If a C-side bit is renumbered, both sides must change in lockstep
  # or behavior diverges silently (e.g. METHOD_USE_ISOLATION's bit
  # would suddenly mean METHOD_LOG_EXECUTE).
  #
  # Each variable uses the standard guarded-assignment so embedders
  # can pre-set non-default values for testing -- but they shouldn't,
  # because the C side reads its own enum, not these.  The variables
  # are PURELY a Tcl-side spelling convenience.
  #
  # Currently defined (subset of the C MethodFlags enum):
  #
  #     METHOD_NONE              0x0
  #     METHOD_PROTOCOL_V1R2     0x40
  #     METHOD_LOG_EXECUTE       0x80
  #     METHOD_USE_ISOLATION     0x800
  #     METHOD_USE_SAFE_INTERP   0x1000
  #
  # Other C-side bits exist but are not currently exposed Tcl-side.
  # Add new ones here when the embedder configuration surface needs
  # them; keep the order aligned with the C enum for readability.
  #
  proc setupMethodFlagsVariables {} {
    #
    # HACK: Setup the "commonly used" subset of the method flags defined in
    #       the MethodFlags enumeration in the "GarudaInt.h" source file...
    #       If those (numeric) values ever change (unlikely?), updates will
    #       be needed in this procedure as well.
    #
    variable METHOD_NONE

    if {![info exists METHOD_NONE]} then {
      set METHOD_NONE 0x0; # NOTE: Use module exports lookups, etc.
    }

    variable METHOD_PROTOCOL_V1R2

    if {![info exists METHOD_PROTOCOL_V1R2]} then {
      set METHOD_PROTOCOL_V1R2 0x40; # NOTE: Use ClrTclStubs mechanism.
    }

    variable METHOD_LOG_EXECUTE

    if {![info exists METHOD_LOG_EXECUTE]} then {
      set METHOD_LOG_EXECUTE 0x80; # NOTE: Log managed method execution.
    }

    variable METHOD_USE_ISOLATION

    if {![info exists METHOD_USE_ISOLATION]} then {
      set METHOD_USE_ISOLATION 0x800; # NOTE: Use new Eagle interpreter.
    }

    variable METHOD_USE_SAFE_INTERP

    if {![info exists METHOD_USE_SAFE_INTERP]} then {
      set METHOD_USE_SAFE_INTERP 0x1000; # NOTE: Use "safe" Eagle subset.
    }
  }

  #
  # The configuration-namespace builder.  This is the single largest
  # proc in the package -- it materializes EVERY ::Garuda::* variable
  # the rest of the package and the C-side Garuda_Init reads.  Every
  # variable uses the standard guarded-assignment pattern:
  #
  #     variable foo
  #     if {![info exists foo]} then { set foo <our-default> }
  #
  # so that an embedder pre-setting a value before [package require]
  # is preserved.  Loader scripts (garuda.tcl, dotnet.tcl) and
  # external configuration (env vars, registry) feed in via this
  # mechanism without any explicit override plumbing here.
  #
  # The body is divided into seven sub-banner sections that group
  # variables by purpose.  Read these as a tour of Garuda's
  # configuration surface:
  #
  #   DIAGNOSTIC      verbose, logCommand, traceCommand --
  #                   how/whether the package emits log output.
  #   CLR PRE-NAME    useCoreClr, clrVersions -- runtime selection
  #                   knobs consulted BEFORE the package binary
  #                   filename is resolved.
  #   NAME            packageName, packageBinaryFileName,
  #                   assemblyBaseName, assemblyFileNames --
  #                   the names of the files we'll be loading.
  #   CLR POST-NAME   coreClrVersion, runtimeConfigPath -- runtime
  #                   knobs consulted AFTER the package binary
  #                   filename is resolved (these depend on
  #                   knowing the binary).
  #   GENERAL         setupAndLoad, startClr, startBridge,
  #                   stopClr, methodFlags -- the lifecycle
  #                   switches for what happens during load.
  #   INTERPRETER     useIsolation, useSafeInterp -- bridge
  #                   AppDomain / safe-interp flags.
  #   ASSEMBLY NAME   assemblyConfigurations, assemblySubDirectories
  #                   -- the parts of the assembly path that
  #                   findAssemblyFile cross-products with the
  #                   directory list.
  #   ASSEMBLY SEARCH useRelativePath, useEnvironment, useRegistry,
  #                   useLibrary, envVars, envVarSuffixes,
  #                   rootRegistryKeyName -- toggles for which
  #                   sources getLibraryPathList / setupAndLoad
  #                   pulls candidate directories from.
  #
  # Variable defaults are chosen with two hierarchies in mind:
  #
  #   1. "Most embedders should never need to override this" --
  #      the assembly base names, the registry root, the search
  #      toggles all default to "discover everything".
  #
  #   2. "This MUST be embedder-controllable" -- the lifecycle
  #      flags (setupAndLoad, startClr, startBridge) default
  #      conservatively in helper.tcl itself, but the upstream
  #      loader (garuda.tcl vs dotnet.tcl vs GarudaDotNetFx etc)
  #      pre-sets them according to that loader's intent.
  #      Helper.tcl never overrides what the loader sets.
  #
  # Adding a new variable:
  #   * Pick the right sub-banner (or, if it's a new category,
  #     add a new banner with the existing comment style).
  #   * Use the guarded-assignment pattern.
  #   * Document the DEFAULT value in a comment immediately above
  #     the [variable] declaration; tooling and embedders rely on
  #     that comment.
  #   * If the variable is read by the C side, ensure
  #     GetClrConfigInfo in Garuda.c picks it up.
  #
  proc setupHelperVariables { directory } {
    global env

    ###########################################################################
    #*********** NATIVE PACKAGE DIAGNOSTIC CONFIGURATION VARIABLES ************
    ###########################################################################

    #
    # NOTE: Display diagnostic messages while starting up this package?  This
    #       is used by the code in the CLR assembly manager contained in this
    #       package.  This is also used by the package test suite.
    #
    variable verbose; # DEFAULT: false

    if {![info exists verbose]} then {
      set verbose false
    }

    #
    # NOTE: The Tcl command used to log warnings, errors, and other messages
    #       generated by the package.  This is used by the code in the CLR
    #       assembly manager contained in this package.  This is also used by
    #       the package test suite.  When logging, this can be set to things
    #       like "::tclLog" for ease-of-use.
    #
    variable logCommand; # DEFAULT: [namespace current]::noLog

    if {![info exists logCommand]} then {
      set logCommand [namespace current]::noLog
    }

    #
    # NOTE: When this is non-zero, the [file normalize] sub-command will not
    #       be used on the assembly path.  This is necessary in some special
    #       environments due to a bug in Tcl where it will resolve junctions
    #       as part of the path normalization process.
    #
    variable noNormalize; # DEFAULT: false

    if {![info exists noNormalize]} then {
      set noNormalize false
    }

    #
    # NOTE: This is used when loading / starting the CoreCLR runtime.  It is
    #       currently required by the configuration loading subsystem; however,
    #       the CoreCLR-support subsystem itself is capable of falling back to
    #       querying the executable file name itself.
    #
    variable runtimeConfigPath; # DEFAULT: ${TMP}/<exeName>.runtimeconfig.json

    if {![info exists runtimeConfigPath]} then {
      set runtimeConfigPath [getRuntimeConfigPath]
    }

    ###########################################################################
    #***************** NATIVE PACKAGE CLR PRE-NAME VARIABLES ******************
    ###########################################################################

    #
    # NOTE: The name of the package we will provide to Tcl.
    #
    variable packageName; # DEFAULT: Garuda

    if {![info exists packageName]} then {
      set packageName [lindex [split [string trim [namespace current] :] :] 0]
    }

    #
    # NOTE: This is the list of CLR versions supported by this package.  In
    #       the future, this list may need to be updated.
    #
    variable clrVersions; # DEFAULT: "v2.0.50727 v4.0.30319"

    if {![info exists clrVersions]} then {
      set clrVersions [list v2.0.50727 v4.0.30319]
    }

    #
    # NOTE: This is the list of CoreCLR versions supported by this package,
    #       in (strict) order of preference.  In the future, this list may
    #       need to be updated.
    #
    variable coreClrVersions; # DEFAULT: "10.0.* [...] 3.0.*"

    if {![info exists coreClrVersions]} then {
      set coreClrVersions [list \
          10.0.* 9.0.* 8.0.* 7.0.* 6.0.* 5.0.* 3.1.* 3.0.*]
    }

    #
    # NOTE: This is the version of the CoreCLR that should be used by this
    #       package.
    #
    variable coreClrVersion; # DEFAULT: <unset>

    #
    # NOTE: Use the CoreCLR only?  By default, we will attempt to detect if
    #       this setting should be enabled.  This check must be done prior
    #       to figuring out the package binary file name (below), which is
    #       slightly different between the .NET Framework and .NET Core.
    #
    variable useCoreClr; # DEFAULT: ""

    if {![info exists useCoreClr]} then {
      #
      # HACK: This will always set the specified (namespace?) variable to
      #       something.  If there is no explicit override set, a default
      #       value of empty string will be used.
      #
      if {[shouldForceCoreClr]} then {
        set useCoreClr true; # FORCED
      } elseif {[hasUseCoreClr result]} then {
        set useCoreClr $result
      } else {
        set useCoreClr ""
      }
    } elseif {$verbose} then {
      #
      # HACK: Make sure the setting value ends up in the log file.
      #
      hasUseCoreClr; # NOTE: No side effects.
    }

    ###########################################################################
    #********************* NATIVE PACKAGE NAME VARIABLES **********************
    ###########################################################################

    #
    # NOTE: The name of the dynamic link library containing the native code for
    #       this package.
    #
    variable packageBinaryFileNameOnly; # DEFAULT: [lib]Garuda[Core].(dll|so)

    if {![info exists packageBinaryFileNameOnly]} then {
      set packageBinaryFileNameOnly \
          [getPackageBinaryFileNameOnly $packageName $useCoreClr]
    }

    #
    # NOTE: The fully qualified file name for the package binary.
    #
    variable packageBinaryFileName; # DEFAULT: ${directory}/${fileNameOnly}

    if {![info exists packageBinaryFileName]} then {
      set packageBinaryFileName [fileNormalize [file join $directory \
          $packageBinaryFileNameOnly] true]
    }

    ###########################################################################
    #***************** NATIVE PACKAGE CLR POST-NAME VARIABLES *****************
    ###########################################################################

    #
    # NOTE: Use the minimum supported version of the CLR?  By default, we want
    #       to load the latest known version of the CLR (e.g. "v4.0.30319").
    #       However, this loading behavior can now be overridden by setting the
    #       environment variable named "UseMinimumClr" [to anything] -OR- by
    #       setting this Tcl variable to non-zero.  In that case, the minimum
    #       supported version of the CLR will be loaded instead (e.g.
    #       "v2.0.50727").  This Tcl variable is primarily used by the compiled
    #       code for this package.
    #
    variable useMinimumClr; # DEFAULT: false

    if {![info exists useMinimumClr]} then {
      set useMinimumClr [shouldUseMinimumClr $packageBinaryFileName]
    } elseif {$verbose} then {
      #
      # HACK: Make sure the setting value ends up in the log file.
      #
      shouldUseMinimumClr $packageBinaryFileName; # NOTE: No side effects.
    }

    ###########################################################################
    #************* NATIVE PACKAGE GENERAL CONFIGURATION VARIABLES *************
    ###########################################################################

    #
    # NOTE: The fully qualified path and file name for the Eagle CLR assembly
    #       to be loaded.  This is used by the code in the CLR assembly manager
    #       contained in this package.
    #
    variable assemblyPath; # DEFAULT: <unset>

    #
    # NOTE: The fully qualified type name of the CLR method(s) to execute
    #       within the Eagle CLR assembly.  This is used by the code in the
    #       CLR assembly manager contained in this package.
    #
    variable typeName; # DEFAULT: Eagle._Components.Public.NativePackage

    if {![info exists typeName]} then {
      set typeName [getPackageAssemblyTypeName $useCoreClr]
    }

    #
    # NOTE: The name of the CLR method to execute when starting up the bridge
    #       between Eagle and Tcl.  This is used by the code in the CLR
    #       assembly manager contained in this package.
    #
    variable startupMethodName; # DEFAULT: Startup[Core]Clr

    if {![info exists startupMethodName]} then {
      if {[string is true -strict $useCoreClr]} then {
        set startupMethodName StartupCoreClr
      } else {
        set startupMethodName StartupClr
      }
    }

    #
    # NOTE: The name of the CLR method to execute when issuing control
    #       directives to the bridge between Eagle and Tcl.  This is used by
    #       the code in the CLR assembly manager contained in this package.
    #
    variable controlMethodName; # DEFAULT: Control[Core]Clr

    if {![info exists controlMethodName]} then {
      if {[string is true -strict $useCoreClr]} then {
        set controlMethodName ControlCoreClr
      } else {
        set controlMethodName ControlClr
      }
    }

    #
    # NOTE: The name of the managed method to execute when detaching a specific
    #       Tcl interpreter from the bridge between Eagle and Tcl.  This is
    #       used by the code in the CLR assembly manager contained in this
    #       package.
    #
    variable detachMethodName; # DEFAULT: Detach[Core]Clr

    if {![info exists detachMethodName]} then {
      if {[string is true -strict $useCoreClr]} then {
        set detachMethodName DetachCoreClr
      } else {
        set detachMethodName DetachClr
      }
    }

    #
    # NOTE: The name of the managed method to execute when completely shutting
    #       down the bridge between Eagle and Tcl.  This is used by the code in
    #       the CLR assembly manager contained in this package.
    #
    variable shutdownMethodName; # DEFAULT: Shutdown[Core]Clr

    if {![info exists shutdownMethodName]} then {
      if {[string is true -strict $useCoreClr]} then {
        set shutdownMethodName ShutdownCoreClr
      } else {
        set shutdownMethodName ShutdownClr
      }
    }

    #
    # NOTE: The user arguments to pass to all of the managed methods.  If this
    #       value is specified, it MUST be a well-formed Tcl list.  This is
    #       used by the code in the CLR assembly manager contained in this
    #       package.
    #
    variable methodArguments; # DEFAULT: NONE

    if {![info exists methodArguments]} then {
      set methodArguments [list]
    }

    #
    # NOTE: The extra method flags to use when invoking the CLR methods.  Refer
    #       to the MethodFlags enumeration for full details.  This is used by
    #       the code in the CLR assembly manager contained in this package.  An
    #       example of a useful value here is 0x40 (i.e. METHOD_PROTOCOL_V1R2).
    #       Please see the associated procedure [setupMethodFlagsVariables] for
    #       a list of "commonly used" values.
    #
    variable methodFlags; # DEFAULT: 0x0

    if {![info exists methodFlags]} then {
      set methodFlags [getMethodFlags]
    }

    #
    # NOTE: Load the CLR immediately upon loading the package?  This is used
    #       by the code in the CLR assembly manager contained in this package.
    #
    variable loadClr; # DEFAULT: true

    if {![info exists loadClr]} then {
      set loadClr true
    }

    #
    # NOTE: Start the CLR immediately upon loading the package?  This is used
    #       by the code in the CLR assembly manager contained in this package.
    #
    variable startClr; # DEFAULT: true

    if {![info exists startClr]} then {
      set startClr true
    }

    #
    # NOTE: Start the bridge between Eagle and Tcl immediately upon loading
    #       the package?  This is used by the code in the CLR assembly manager
    #       contained in this package.
    #
    variable startBridge; # DEFAULT: true

    if {![info exists startBridge]} then {
      set startBridge true
    }

    #
    # NOTE: Attempt to stop and release the CLR when unloading the package?
    #       This is used by the code in the CLR assembly manager contained
    #       in this package.
    #
    variable stopClr; # DEFAULT: true

    if {![info exists stopClr]} then {
      set stopClr true
    }

    ###########################################################################
    #*********** NATIVE PACKAGE INTERPRETER CONFIGURATION VARIABLES ***********
    ###########################################################################

    #
    # NOTE: Use an isolated Eagle interpreter even if the Tcl interpreter that
    #       the package has been loaded into is "unsafe"?
    #
    variable useIsolation; # DEFAULT: false

    if {![info exists useIsolation]} then {
      set useIsolation [shouldUseIsolation]
    } elseif {$verbose} then {
      #
      # HACK: Make sure the setting value ends up in the log file.
      #
      shouldUseIsolation; # NOTE: No side effects.
    }

    #
    # NOTE: Use a "safe" Eagle interpreter even if the Tcl interpreter that the
    #       package has been loaded into is "unsafe"?
    #
    variable useSafeInterp; # DEFAULT: false

    if {![info exists useSafeInterp]} then {
      set useSafeInterp [shouldUseSafeInterp]
    } elseif {$verbose} then {
      #
      # HACK: Make sure the setting value ends up in the log file.
      #
      shouldUseSafeInterp; # NOTE: No side effects.
    }

    ###########################################################################
    #******************** MANAGED ASSEMBLY NAME VARIABLES *********************
    ###########################################################################

    #
    # NOTE: The Eagle build configurations we know about and support.
    #       This list is used during the CLR assembly search process in the
    #       [setupAndLoad] procedure (below).
    #
    variable assemblyConfigurations; # DEFAULT: {Debug Release ""}

    if {![info exists assemblyConfigurations]} then {
      set assemblyConfigurations [list]

      #
      # HACK: When running under the auspices of the Eagle test suite, select
      #       the matching build configuration and suffix, if any.
      #
      set assemblyConfiguration ""

      if {[info exists ::test_flags(-configuration)] && \
          [string length $::test_flags(-configuration)] > 0} then {
        append assemblyConfiguration $::test_flags(-configuration)

        if {[info exists ::test_flags(-suffix)] && \
            [string length $::test_flags(-suffix)] > 0} then {
          append assemblyConfiguration  $::test_flags(-suffix)
        }
      }

      if {[string length $assemblyConfiguration] > 0} then {
        lappend assemblyConfigurations $assemblyConfiguration
      }

      #
      # NOTE: Remove the temporary assembly configuration variable.
      #
      unset assemblyConfiguration

      #
      # NOTE: If there is a build suffix, use it to enhance the default list
      #       of configurations.
      #
      if {[info exists ::test_flags(-suffix)] && \
          [string length $::test_flags(-suffix)] > 0} then {
        #
        # NOTE: First, add each of the default configurations with the build
        #       suffix appended to them.
        #
        lappend assemblyConfigurations DebugDll${::test_flags(-suffix)}
        lappend assemblyConfigurations ReleaseDll${::test_flags(-suffix)}
        lappend assemblyConfigurations Debug${::test_flags(-suffix)}
        lappend assemblyConfigurations Release${::test_flags(-suffix)}
      }

      #
      # NOTE: If we are dealing with the CoreCLR runtime, also append those
      #       specific configurations (with their suffixes) as well.
      #
      if {[string is true -strict $useCoreClr]} then {
        lappend assemblyConfigurations \
            DebugNetStandard2X DebugNetStandard21 DebugNetStandard20

        lappend assemblyConfigurations \
            ReleaseNetStandard2X ReleaseNetStandard21 ReleaseNetStandard20
      }

      #
      # NOTE: Finally, always add the default build configurations last.
      #
      lappend assemblyConfigurations DebugDll ReleaseDll
      lappend assemblyConfigurations Debug Release ""
    }

    #
    # NOTE: The Eagle build sub-directories we know about and support.
    #       This list is used during the CLR assembly search process in the
    #       [setupAndLoad] procedure (below).
    #
    variable assemblySubDirectories; # DEFAULT: {netstandard2.X ... ""}

    if {![info exists assemblySubDirectories]} then {
      set assemblySubDirectories [list]

      if {[string is true -strict $useCoreClr]} then {
        lappend assemblySubDirectories \
            netcoreapp3.0 netcoreapp2.0 netstandard2.X netstandard2.1 \
            netstandard2.0
      }

      lappend assemblySubDirectories ""
    }

    #
    # NOTE: The possible file names for the Eagle CLR assembly, where X is the
    #       major version of the CLR.
    #
    variable assemblyFileNames; # DEFAULT: "Eagle_CLRvX.dll Eagle.dll"

    if {![info exists assemblyFileNames]} then {
      set assemblyFileNames [list]

      #
      # NOTE: When targeted at the CoreCLR, use only those Eagle assembly file
      #       names.
      #
      if {[shouldUseCoreClr]} then {
        #
        # NOTE: If a supported version of the CoreCLR has been (or will be)
        #       loaded, add the decorated Eagle assembly file name specific
        #       to CoreCLR; it should be built against the .NET Standard 2.0
        #       or 2.1, e.g. .NET Core 2.x, .NET Core 3.x, or .NET 5+.
        #
        lappend assemblyFileNames Eagle_CoreCLR.dll
      } else {
        #
        # NOTE: If the minimum supported version of the CLR has been (or will
        #       be) loaded, add the decorated Eagle assembly file name specific
        #       to CLR version 2.0.50727; otherwise, add the decorated Eagle
        #       assembly file name specific to CLR version 4.0.30319.
        #
        if {[shouldUseMinimumClr $packageBinaryFileName]} then {
          #
          # NOTE: Either we cannot or should not use the latest known version
          #       of the CLR; therefore, use the minimum supported version.  In
          #       this situation, the Eagle assembly specific to the v2 CLR
          #       will be checked first.
          #
          lappend assemblyFileNames Eagle_CLRv2.dll
        } else {
          #
          # NOTE: The latest known version of the CLR is available for use and
          #       we have not been prevented from using it.  In this situation,
          #       the Eagle assembly specific to the v4 CLR will be checked
          #       first.
          #
          # TODO: Should we eventually provide the ability to fallback to the
          #       v2 CLR version of the assembly here (i.e. should the file
          #       name "Eagle_CLRv2.dll" be added to this list right after the
          #       file name "Eagle_CLRv4.dll")?  This is always legal because
          #       the v4 CLR can load v2 CLR assemblies.
          #
          lappend assemblyFileNames Eagle_CLRv4.dll
        }
      }

      #
      # NOTE: Fallback to the generic assembly file name that is CLR version
      #       neutral (i.e. the version of the CLR it refers to is unknown).
      #
      lappend assemblyFileNames Eagle.dll
    }

    #
    # NOTE: The base name for the Eagle CLR assembly.
    #
    variable assemblyBaseName; # DEFAULT: Eagle

    if {![info exists assemblyBaseName]} then {
      set assemblyBaseName [file rootname [lindex $assemblyFileNames end]]
    }

    ###########################################################################
    #******************* MANAGED ASSEMBLY SEARCH VARIABLES ********************
    ###########################################################################

    #
    # NOTE: Use the configured environment variables when searching for the
    #       Eagle CLR assembly?
    #
    variable useEnvironment; # DEFAULT: true

    if {![info exists useEnvironment]} then {
      set useEnvironment true
    }

    #
    # NOTE: The environment variable names to check when attempting to find the
    #       Eagle root directory.  This list is used during the assembly search
    #       process from within the [setupAndLoad] procedure.
    #
    variable envVars; # DEFAULT: "Eagle_Dll Eagle EagleLkg Lkg"

    if {![info exists envVars]} then {
      set envVars [list Eagle_Dll Eagle EagleLkg Lkg]
    }

    #
    # NOTE: The strings to append to the environment variable names listed
    #       above when attempting to find the Eagle root directory.  This list
    #       is used during the assembly search process from within the
    #       [setupAndLoad] procedure.
    #
    variable envVarSuffixes; # DEFAULT: "Temp Build"

    if {![info exists envVarSuffixes]} then {
      set envVarSuffixes [list Temp Build]
    }

    #
    # NOTE: Use the various relative paths based on the location of this script
    #       file?  This is primarily for use during development, when the Eagle
    #       CLR assembly will be in the build output directory.
    #
    variable useRelativePath; # DEFAULT: true

    if {![info exists useRelativePath]} then {
      set useRelativePath true
    }

    #
    # NOTE: Use the configured Windows registry keys when searching for the
    #       Eagle CLR assembly?
    #
    variable useRegistry; # DEFAULT: true

    if {![info exists useRegistry]} then {
      set useRegistry true
    }

    #
    # NOTE: Use the various Tcl script library directories when searching for
    #       the Eagle CLR assembly?
    #
    variable useLibrary; # DEFAULT: true

    if {![info exists useLibrary]} then {
      set useLibrary true
    }

    #
    # NOTE: The registry key where all the versions of Eagle installed on this
    #       machine (via the setup) can be found.
    #
    variable rootRegistryKeyName; # DEFAULT: HKEY_LOCAL_MACHINE\Software\Eagle

    if {![info exists rootRegistryKeyName]} then {
      set rootRegistryKeyName HKEY_LOCAL_MACHINE\\Software\\Eagle
    }
  }

  #############################################################################
  #************************ PACKAGE STARTUP PROCEDURE *************************
  #############################################################################

  #
  # CoreCLR-specific pre-load setup.  Runs only when shouldUseCoreClr
  # returns true (and only when the interp is not safe -- see the gate
  # at the call site).  Three responsibilities:
  #
  #   1. Resolve the CoreCLR Runtime Identifier (RID) for this host:
  #      "win-x64", "linux-arm64", "osx-x64", etc.  The RID is the
  #      coordinate the CoreCLR install layout indexes by, and we
  #      need it for both runtime-version probing and for adding the
  #      runtime's shared-library directory to PATH.
  #
  #   2. If ::Garuda::coreClrVersion is not pre-set, scan the
  #      installed runtimes (checkCoreClrDirectories) and pick the
  #      "best" (latest by comparePackageVersions) that exists for
  #      this RID.  Embedders that need a specific version pre-set
  #      coreClrVersion before [package require] and this discovery
  #      is skipped.
  #
  #   3. If ::Garuda::runtimeConfigPath is set but the file does not
  #      exist, materialize it via writeCoreClrRuntimeConfiguration.
  #      This file is the runtimeconfig.json that hostfxr requires
  #      (see GarudaCoreClr.c for what it does with it).  Garuda
  #      writes its own minimal one because we don't want to depend
  #      on the embedder shipping one.
  #
  #   4. Add the per-RID runtime directory (where coreclr.dll /
  #      libcoreclr.so / libcoreclr.dylib actually lives) to the
  #      process PATH so that the dlopen / LoadLibrary chain
  #      hostfxr triggers can find it.  Without this step,
  #      coreclr_initialize fails with a vague "couldn't find
  #      runtime" error.
  #
  # The interp-issafe check at the runtime-config-write step exists
  # because the file-write functions are blocked in safe interps.
  # Running this proc inside a safe interp is a programming error
  # higher up (the call site at the bottom of the file already
  # gates against it); the inner check is a belt-and-braces guard.
  #
  proc setupForCoreClr {} {
    global tcl_platform
    variable coreClrVersion
    variable runtimeConfigPath

    #
    # NOTE: Several places below need the current platform identifier.
    #
    if {[info exists tcl_platform(machine)]} then {
      set platform [getCoreClrPlatformRid $tcl_platform(machine)]
    } else {
      set platform ""; # NOTE: Unknown, need machine.
    }

    #
    # NOTE: If the CoreCLR is being used for this load operation,
    #       we must attempt to figure out the "best" (i.e. latest)
    #       installed version, if that has not been done already.
    #
    if {[string length $platform] > 0} then {
      if {![info exists coreClrVersion] || \
          [string length $coreClrVersion] == 0} then {
        if {[checkCoreClrDirectories $platform version]} then {
          maybeLogViaCommand "Selected CoreCLR $version (installed)..."
          set coreClrVersion $version; # NOTE: Select "best" version.
        }
      }
    }

    #
    # NOTE: If necessary, write the runtime configuration file needed by the
    #       CoreCLR.  Also, add to the PATH environment variable when needed
    #       to load the CoreCLR runtime.
    #
    if {![interp issafe] && [info exists coreClrVersion] && \
        [string length $coreClrVersion] > 0} then {
      if {[info exists runtimeConfigPath] && \
          ![isValidFile $runtimeConfigPath]} then {
        writeCoreClrRuntimeConfiguration $runtimeConfigPath $coreClrVersion

        maybeLogViaCommand \
            "Wrote CoreCLR $coreClrVersion configuration to file\
            \"$runtimeConfigPath\"..."
      }

      if {[string length $platform] > 0} then {
        set runtimeDirectory [getCoreClrDirectory $platform $coreClrVersion]

        if {[string length $runtimeDirectory] > 0 && \
            [addToPath $runtimeDirectory]} then {
          maybeLogViaCommand "Added CoreCLR $coreClrVersion runtime\
                             directory \"$runtimeDirectory\" to PATH..."
        }
      }
    }
  }

  #
  # The "find Eagle.dll, point Garuda.dll at it, and [load] it" proc.
  #
  # This is the last thing the package-startup section runs (after
  # setupHelperVariables has populated the configuration namespace and
  # setupForCoreClr has done its CoreCLR-specific dance, when needed).
  # By the time this returns, Garuda.dll has been [load]ed and the C
  # side has run Garuda_Init, which means [object] is registered, the
  # CLR is up (if startClr was true), and the bridge is connected
  # (if startBridge was true).
  #
  # Path-discovery sequence:
  #
  #   1. If ::Garuda::assemblyPath was pre-set by an embedder OR by an
  #      earlier setupForCoreClr call, use it verbatim -- no probing.
  #      The "verbatim" path is the embedder-takes-responsibility
  #      contract; if they got it wrong, the C-side load will surface
  #      a clear error.
  #
  #   2. Otherwise, build a list of candidate directories drawn from
  #      up to four sources, in this order (matched in
  #      first-match-wins fashion later):
  #        * useRelativePath -> the package's own dir + 1/2/3 parents,
  #          each crossed with assemblyConfigurations and
  #          assemblySubDirectories (Debug/Release x bin/obj/...).
  #          This finds Eagle.dll when the developer is running out
  #          of an Eagle source tree.
  #        * useEnvironment -> EAGLE_INSTALL / EagleInstall / etc.
  #          environment variables.  For deployed embedders.
  #        * useRegistry -> Windows registry entries under
  #          HKLM\Software\<name>\<version>\Path.  Legacy install
  #          paths, kept for backward compat.
  #        * useLibrary -> the package's own lib/ subdirectory plus
  #          system locations like Program Files, Tcl auto_path
  #          entries.  The catch-all bucket.
  #
  #      Each toggle is independently controllable from the embedder,
  #      so a paranoid host can disable e.g. registry probing.
  #
  #   3. findAssemblyFile cross-walks the candidate directories
  #      against assemblyFileNames (Eagle.dll, Eagle.Beta.dll, etc).
  #      Returns the first file found.
  #
  #   4. If nothing is found, fall back to "<package-dir>/<last
  #      assembly name>" -- almost certainly broken, but produces a
  #      consistent error message rather than silently failing.
  #
  # The fileNormalize at the end is critical: the C side may have a
  # different cwd than this script, so the assemblyPath we hand it
  # MUST be absolute and canonical.  fileNormalize calls [file
  # normalize] unconditionally (not gated by ::Garuda::noNormalize)
  # because path correctness is non-negotiable here.
  #
  # The final [load] is what triggers Garuda_Init in C.  After this
  # returns, the package is ready for use.
  #
  proc setupAndLoad { directory } {
    variable assemblyConfigurations
    variable assemblyFileNames
    variable assemblyPath
    variable assemblySubDirectories
    variable envVars
    variable envVarSuffixes
    variable packageBinaryFileName
    variable packageName
    variable rootRegistryKeyName
    variable useEnvironment
    variable useLibrary
    variable useRegistry
    variable useRelativePath

    if {[info exists assemblyPath]} then {
      #
      # NOTE: Managed assembly path has been pre-configured by an external
      #       script; therefore, just use it verbatim.
      #
      maybeLogViaCommand "Using existing assembly path \"$assemblyPath\"..."
    } else {
      #
      # NOTE: Build list of directories to search for the managed assembly.
      #
      set directories [list]

      if {$useRelativePath} then {
        set parentDirectory(1) [file dirname $directory]
        set parentDirectory(2) [file dirname $parentDirectory(1)]
        set parentDirectory(3) [file dirname $parentDirectory(2)]

        eval lappendUnique directories [getRelativePathList \
            [list $directory $parentDirectory(1) \
            $parentDirectory(2) $parentDirectory(3)] \
            $assemblyConfigurations $assemblySubDirectories]
      }

      if {$useEnvironment} then {
        eval lappendUnique directories [getEnvironmentPathList \
            $envVars $envVarSuffixes]
      }

      if {$useRegistry} then {
        eval lappendUnique directories [getRegistryPathList \
            $rootRegistryKeyName Path]
      }

      if {$useLibrary} then {
        eval lappendUnique directories [getLibraryPathList]
      }

      maybeLogViaCommand "Final list of directories to search: $directories"

      #
      # NOTE: Attempt to find the Eagle managed assembly file using the list
      #       of candidate directories.
      #
      set path [findAssemblyFile \
          $directories $assemblyConfigurations $assemblySubDirectories \
          $assemblyFileNames]

      if {[isValidFile $path]} then {
        #
        # NOTE: This will end up being used by code (the native code for this
        #       package) that may have a different current working directory;
        #       therefore, make sure to normalize it first.
        #
        set assemblyPath [fileNormalize $path]
      }

      #
      # NOTE: If no managed assembly path could be found, use the default one.
      #       This is very unlikely to result in the package being successfully
      #       loaded.
      #
      if {![info exists assemblyPath] || \
          [string length $assemblyPath] == 0} then {
        #
        # NOTE: Choose the last (default) managed assembly file name residing
        #       in the same directory as the package.  This will end up being
        #       used by code (the native code for this package) that may have
        #       a different current working directory; therefore, make sure to
        #       normalize it first.
        #
        set assemblyPath [fileNormalize [file join $directory [lindex \
            $assemblyFileNames end]]]

        maybeLogViaCommand "Using default assembly path \"$assemblyPath\"..."
      }
    }

    #
    # NOTE: Attempt to load the dynamic link library for the package now that
    #       the managed assembly path has been set [to something].
    #
    maybeLogViaCommand "Using final assembly path \"$assemblyPath\"..."
    load $packageBinaryFileName $packageName
  }

  #############################################################################
  #***************************** PACKAGE STARTUP ******************************
  #############################################################################

  #
  # NOTE: First, arrange to have the "haveEagle" helper procedure exported
  #       from this namespace and imported into the global namespace.
  #
  set namespace [namespace current]; namespace export -clear haveEagle
  namespace eval :: [list namespace forget ::${namespace}::*]
  namespace eval :: [list namespace import -force ::${namespace}::haveEagle]

  #
  # NOTE: Next, save the package path for later use.
  #
  variable packagePath

  if {![info exists packagePath]} then {
    set packagePath [fileNormalize [file dirname [info script]] true]
  }

  #
  # NOTE: Next, setup the script variables associated with this package.
  #
  setupMethodFlagsVariables
  setupHelperVariables $packagePath

  #
  # NOTE: Next, if necessary, perform the specific setup actions needed
  #       to integrate with the CoreCLR.
  #
  variable hasUseCoreClrResult; # TEMPORARY

  if {[hasUseCoreClr hasUseCoreClrResult] && \
      [string is true -strict $hasUseCoreClrResult]} then {
    #
    # TODO: Current CoreCLR support is not designed to work with "safe"
    #       Tcl interpreters unless the ::Garuda::coreClrVersion -AND-
    #       ::Garuda::runtimeConfigPath variables are pre-setup and the
    #       appropriate CoreCLR host shared library is already present
    #       (and/or unnecessary?) in the PATH, i.e. automatic detection
    #       cannot work because "safe" Tcl interpreters do not have the
    #       normal file system access.
    #
    if {![interp issafe]} then {
      setupForCoreClr
    }
  }

  unset -nocomplain hasUseCoreClrResult

  #
  # NOTE: Finally, maybe attempt to setup and load the extension right
  #       now.
  #
  variable setupAndLoad

  if {[info exists setupAndLoad] && $setupAndLoad} then {
    setupAndLoad $packagePath
  }

  #
  # NOTE: Provide the Garuda "helper" package to the interpreter.
  #
  package provide GarudaHelper 1.0
}
