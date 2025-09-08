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
  # NOTE: Also defined in and used by "all.tcl".
  #
  proc fileNormalizeFromEnvironment { path {force false} } {
    return [fileNormalize [string trim $path] $force]
  }

  #
  # NOTE: Also defined in and used by "all.tcl".
  #
  proc isValidDirectory { path } {
    variable logCommand
    variable verbose

    if {$verbose} then {
      catch {
        set caller [maybeFullName [lindex [info level -1] 0]]

        eval $logCommand [list \
            "$caller: Checking for directory \"$path\" from \"[pwd]\"..."]
      }
    }

    #
    # NOTE: For now, just make sure the path refers to an existing directory.
    #
    return [expr {[string length $path] > 0 && \
          $path ne "." && $path ne ".." && \
          [file exists $path] && [file isdirectory $path]}]
  }

  #
  # NOTE: Also defined in and used by "all.tcl".
  #
  proc isValidFile { path } {
    variable logCommand
    variable verbose

    if {$verbose} then {
      catch {
        set caller [maybeFullName [lindex [info level -1] 0]]

        eval $logCommand [list \
            "$caller: Checking for file \"$path\" from \"[pwd]\"..."]
      }
    }

    #
    # NOTE: For now, just make sure the path refers to an existing file.
    #
    return [expr {[string length $path] > 0 && \
          $path ne "." && $path ne ".." && \
          [file exists $path] && [file isfile $path]}]
  }

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
  # NOTE: Also defined in and used by "all.tcl".
  #
  proc hasUseCoreClr { {varName ""} {default ""} } {
    global env
    variable logCommand
    variable useCoreClr
    variable verbose

    if {[string length $varName] > 0} then {
      upvar 1 $varName result
    }

    if {[info exists useCoreClr] && \
        [string is boolean -strict $useCoreClr]} then {
      set result $useCoreClr

      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          if {[string is true -strict $result]} then {
            eval $logCommand [list \
                "$caller: Using CoreCLR (variable)..."]
          } else {
            eval $logCommand [list \
                "$caller: Not using CoreCLR (variable)..."]
          }
        }
      }

      return true; # NOTE: There was an explicit setting.
    }

    if {[info exists env(UseCoreClr)] && \
        [string is boolean -strict $env(UseCoreClr)]} then {
      set result $env(UseCoreClr)

      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          if {[string is true -strict $result]} then {
            eval $logCommand [list \
                "$caller: Using CoreCLR (environment)..."]
          } else {
            eval $logCommand [list \
                "$caller: Not using CoreCLR (environment)..."]
          }
        }
      }

      return true; # NOTE: There was an explicit setting.
    }

    set result $default

    if {$verbose} then {
      catch {
        set caller [maybeFullName [lindex [info level 0] 0]]

        if {[string is true -strict $result]} then {
          eval $logCommand [list \
              "$caller: Using CoreCLR (default)..."]
        } else {
          eval $logCommand [list \
              "$caller: Not using CoreCLR (default)..."]
        }
      }
    }

    return false; # NOTE: There was not an explicit setting.
  }

  #
  # NOTE: Also defined in and used by "all.tcl".
  #
  proc isDotNetCore { {default ""} } {; # NOT USED: DO NOT REMOVE.
    global env

    if {[info exists env(FORCE_DOTNET_CORE)] || ![isWindows]} then {
      #
      # NOTE: Assume that the .NET Framework is only available on Windows
      #       -AND- that Mono will never support the native hosting APIs,
      #       hence the only option left is the .NET (Core?) runtime.
      #
      return true
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

    if {$verbose} then {
      catch {
        set caller [maybeFullName [lindex [info level 0] 0]]

        if {[string is true -strict $default]} then {
          eval $logCommand [list \
              "$caller: Using CoreCLR (default)..."]
        } else {
          eval $logCommand [list \
              "$caller: Not using CoreCLR (default)..."]
        }
      }
    }

    return $default
  }

  #############################################################################
  #**************************** UTILITY PROCEDURES ****************************
  #############################################################################

  proc isLoaded { fileName {varName ""} } {
    variable logCommand
    variable verbose

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
        if {$verbose} then {
          catch {
            set caller [maybeFullName [lindex [info level 0] 0]]

            eval $logCommand [list \
                "$caller: Package binary file \"$fileName\" is loaded."]
          }
        }

        return true
      }
    }

    if {$verbose} then {
      catch {
        set caller [maybeFullName [lindex [info level 0] 0]]

        eval $logCommand [list \
            "$caller: Package binary file \"$fileName\" is not loaded."]
      }
    }

    return false
  }

  proc getWindowsDirectory {} {
    global env

    if {[info exists env(SystemRoot)]} then {
      return [fileNormalizeFromEnvironment $env(SystemRoot) true]
    } elseif {[info exists env(WinDir)]} then {
      return [fileNormalizeFromEnvironment $env(WinDir) true]
    }

    return ""
  }

  proc filterUnique { list } {
    set result [list]

    foreach item $list {
      if {[lsearch -exact $result $item] == -1} then {
        lappend result $item
      }
    }

    return $result
  }

  proc isValidVersion { value } {
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

  proc extractVersion {
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

  proc filterVersions { list strict } {
    set result [list]

    foreach item [filterUnique $list] {
      if {[isValidVersion $item] || \
          (!$strict && [extractVersion $item])} then {
        lappend result $item
      }
    }

    return $result
  }

  #
  # NOTE: This primary ordering here is via [package vcompare]; failing
  #       that, we fallback to attempting to extract dotted (decimal)
  #       version numbers for both value arguments.  For any non-stable
  #       releases, they should always be sorted as "inferior" to their
  #       associated RTM releases, in relative order of their maturity.
  #       Callers wanting highest-first must pass the -decreasing option
  #       to [lsort].
  #
  proc compareVersions { value1 value2 } {
    set ok1 [isValidVersion $value1]
    set ok2 [isValidVersion $value2]

    if {$ok1 && $ok2} then {
      return [package vcompare $value1 $value2]
    } else {
      set releaseType1 ""; set releaseSerial1 ""

      if {$ok1} then {
        set wasOk1 true; set version1 $value1
      } else {
        set wasOk1 false

        if {[extractVersion \
            $value1 version1 releaseType1 releaseSerial1]} then {
          set ok1 true
        }
      }

      set releaseType2 ""; set releaseSerial2 ""

      if {$ok2} then {
        set wasOk2 true; set version2 $value2
      } else {
        set wasOk2 false

        if {[extractVersion \
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

  proc getProgramFilesSubDirectories {} {
    return [list "" dotnet [file join dotnet libexec]]
  }

  proc getFrameworkDirectory { version } {
    set directory [getWindowsDirectory]

    if {[string length $directory] > 0} then {
      return [file join $directory Microsoft.NET Framework \
          v[string trimleft $version v]]
    }

    return ""
  }

  proc checkFrameworkDirectory { version } {
    set directory [getFrameworkDirectory $version]

    if {[string length $directory] > 0 && \
        [isValidDirectory $directory]} then {
      return true
    }

    return false
  }

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

  proc isMacOS {} {
    global tcl_platform

    return [expr {[info exists tcl_platform(os)] && \
        $tcl_platform(os) eq "Darwin"}]
  }

  #
  # NOTE: This procedure returns the list of "well-known" release types for
  #       the CoreCLR.  This list may need to be updated if/when additional
  #       types are added by Microsoft.
  #
  proc getCoreClrReleaseTypes {} {
    return [list dev alpha beta preview rc stable]
  }

  #
  # NOTE: This procedure is designed to return the operating system prefix
  #       portion of a CoreCLR "platform identifier", i.e. only for those
  #       operating systems that are supported by this package.
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

          lappend command -command [list compareVersions] \
              [filterVersions [glob -nocomplain -directory \
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
  proc checkCoreClrDirectories { platform {versionVarName ""} } {
    variable coreClrVersions
    variable useMinimumClr

    if {[string length $versionVarName] > 0} then {
      upvar 1 $versionVarName version
    }

    if {[info exists coreClrVersions]} then {
      if {[info exists useMinimumClr] && $useMinimumClr} then {
        set patterns [lreverse $coreClrVersions]
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
        #       new "tfm", i.e. per the runtime name change from
        #       ".NET Core" ==> ".NET".  This became necessary
        #       starting at the .NET 5.0 release.  The necessary
        #       prefix strings are hard-coded here.
        #
        if {[package vcompare $version 5.0] >= 0} then {
          set tfm net${version}
        } else {
          set tfm netcoreapp${version}
        }

        lappend result tfm $tfm
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
  proc getCoreClrRuntimeConfiguration {
          version {maximizeCompatibility false} } {
    set tokens [getCoreClrRuntimeConfigurationTokens $version]
    if {[llength $tokens] == 0} then {return ""}
    set tfm [lindex $tokens 1]; set version [lindex $tokens 3]

    if {$maximizeCompatibility} then {
      #
      # TODO: Are these reasonable settings?
      #
      set extra {,
        "rollForward": "LatestMinor",
        "applyPatches": true
      }
    } else {
      set extra ""
    }

    return [string map [list \
        %tfm% $tfm %version% $version %extra% $extra] [string trim {
      {
        "runtimeOptions": {
          "tfm": "%tfm%",
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
  proc writeCoreClrRuntimeConfiguration { fileName version } {
    return [writeFile \
        $fileName [getCoreClrRuntimeConfiguration $version]]
  }

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
      # BUGBUG: Consider exact case only for now.
      #
      if {[lsearch -exact \
          [split $value $separator] $directory] == -1} then {
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

  proc readFile { fileName } {
    set channel [open $fileName RDONLY]
    fconfigure $channel -encoding binary -translation binary
    set result [read $channel]
    close $channel
    return $result
  }

  proc writeFile { fileName data } {
    set channel [open $fileName {WRONLY CREAT TRUNC}]
    fconfigure $channel -encoding binary -translation binary
    puts -nonewline $channel $data
    close $channel
    return ""
  }

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
  # WARNING: Other than appending to the configured log file, if any, this
  #          procedure is absolutely forbidden from having any side effects.
  #
  proc shouldUseCoreClr { {default ""} } {
    global tcl_platform
    variable logCommand
    variable packageName
    variable packagePath
    variable verbose

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
      set haveClr false
      set haveCoreClr false

      if {[info exists packageName] && [info exists packagePath] && \
          [isValidDirectory $packagePath]} then {
        set fileName(1) [file join $packagePath \
            [getPackageBinaryFileNameOnly $packageName false]]; # CLR

        set fileName(2) [file join $packagePath \
            [getPackageBinaryFileNameOnly $packageName true]]; # CoreCLR

        if {[isValidFile $fileName(1)]} then {
          if {$verbose} then {
            catch {
              set caller [maybeFullName [lindex [info level 0] 0]]

              eval $logCommand [list \
                  "$caller: Found CLR shared library \"$fileName(1)\"\
                  (installed)..."]
            }
          }

          set haveClr true
        } else {
          if {$verbose} then {
            catch {
              set caller [maybeFullName [lindex [info level 0] 0]]

              eval $logCommand [list \
                  "$caller: Missing CLR shared library \"$fileName(1)\"\
                  (installed)..."]
            }
          }
        }

        if {[isValidFile $fileName(2)]} then {
          if {$verbose} then {
            catch {
              set caller [maybeFullName [lindex [info level 0] 0]]

              eval $logCommand [list \
                  "$caller: Found CoreCLR shared library \"$fileName(2)\"\
                  (installed)..."]
            }
          }

          set haveCoreClr true
        } else {
          if {$verbose} then {
            catch {
              set caller [maybeFullName [lindex [info level 0] 0]]

              eval $logCommand [list \
                  "$caller: Missing CoreCLR shared library \"$fileName(2)\"\
                  (installed)..."]
            }
          }
        }
      }

      #
      # NOTE: Check if supported versions of the CoreCLR are installed
      #       on this machine.
      #
      if {$haveCoreClr && [info exists tcl_platform(machine)]} then {
        set platform [getCoreClrPlatformRid $tcl_platform(machine)]

        if {[string length $platform] > 0} then {
          if {[checkCoreClrDirectories $platform version]} then {
            if {$verbose} then {
              catch {
                set caller [maybeFullName [lindex [info level 0] 0]]

                eval $logCommand [list \
                    "$caller: Using CoreCLR $version (installed)..."]
              }
            }

            return true
          }
        }
      }
    }

    #
    # NOTE: Fallback to the default setting, which depends on the caller.
    #
    if {$verbose} then {
      catch {
        set caller [maybeFullName [lindex [info level 0] 0]]

        if {[string is true -strict $default]} then {
          eval $logCommand [list \
              "$caller: Using CoreCLR (default)..."]
        } else {
          eval $logCommand [list \
              "$caller: Not using CoreCLR (default)..."]
        }
      }
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
  # WARNING: Other than appending to the configured log file, if any, this
  #          procedure is absolutely forbidden from having any side effects.
  #
  proc shouldUseMinimumClr { fileName {default true} } {
    global env
    variable clrVersions
    variable logCommand
    variable useMinimumClr
    variable verbose

    #
    # NOTE: The package has been configured to use the minimum supported CLR
    #       version; therefore, return true.
    #
    if {[info exists useMinimumClr] && $useMinimumClr} then {
      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          eval $logCommand [list \
              "$caller: Using minimum CLR version (variable)..."]
        }
      }

      return true
    }

    #
    # NOTE: The environment has been configured to use the minimum supported
    #       CLR version?
    #
    if {[info exists env(UseMinimumClr)]} then {
      set result $env(UseMinimumClr)

      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          if {$result} then {
            eval $logCommand [list \
                "$caller: Using minimum CLR version (environment)..."]
          } else {
            eval $logCommand [list \
                "$caller: Using latest CLR version (environment)..."]
          }
        }
      }

      return $result
    }

    #
    # NOTE: The latest supported version of the CLR is not installed on this
    #       machine; therefore, return true.
    #
    if {![checkFrameworkDirectory [lindex $clrVersions end]]} then {
      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          eval $logCommand [list \
              "$caller: Using minimum CLR version (missing)..."]
        }
      }

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
        if {$verbose} then {
          catch {
            set caller [maybeFullName [lindex [info level 0] 0]]

            if {$default} then {
              eval $logCommand [list \
                  "$caller: Using minimum CLR version (default)..."]
            } else {
              eval $logCommand [list \
                  "$caller: Using latest CLR version (default)..."]
            }
          }
        }

        return $default
      }

      #
      # NOTE: The CLR version queried from the package binary is the minimum
      #       supported; therefore, return true.
      #
      if {$version eq [lindex $clrVersions 0]} then {
        if {$verbose} then {
          catch {
            set caller [maybeFullName [lindex [info level 0] 0]]

            eval $logCommand [list \
                "$caller: Using minimum CLR version (assembly)..."]
          }
        }

        return true
      }
    }

    #
    # NOTE: Ok, just use the latest supported version of the CLR.
    #
    if {$verbose} then {
      catch {
        set caller [maybeFullName [lindex [info level 0] 0]]

        eval $logCommand [list \
            "$caller: Using latest CLR version..."]
      }
    }

    return false
  }

  #
  # WARNING: Other than appending to the configured log file, if any, this
  #          procedure is absolutely forbidden from having side effects.
  #
  proc shouldUseIsolation {} {
    global env
    variable logCommand
    variable useIsolation
    variable verbose

    #
    # NOTE: The package has been configured to use interpreter isolation;
    #       therefore, return true.
    #
    if {[info exists useIsolation] && $useIsolation} then {
      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          eval $logCommand [list \
              "$caller: Using interpreter isolation (variable)..."]
        }
      }

      return true
    }

    #
    # NOTE: The environment has been configured to use interpreter isolation?
    #
    if {[info exists env(UseIsolation)]} then {
      set result $env(UseIsolation)

      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          if {$result} then {
            eval $logCommand [list \
                "$caller: Using interpreter isolation (environment)..."]
          } else {
            eval $logCommand [list \
                "$caller: Not using interpreter isolation (environment)..."]
          }
        }
      }

      return $result
    }

    #
    # NOTE: Ok, disable interpreter isolation.
    #
    if {$verbose} then {
      catch {
        set caller [maybeFullName [lindex [info level 0] 0]]

        eval $logCommand [list \
            "$caller: Not using interpreter isolation..."]
      }
    }

    return false
  }

  #
  # WARNING: Other than appending to the configured log file, if any, this
  #          procedure is absolutely forbidden from having side effects.
  #
  proc shouldUseSafeInterp {} {
    global env
    variable logCommand
    variable useSafeInterp
    variable verbose

    #
    # NOTE: The package has been configured to use a "safe" interpreter;
    #       therefore, return true.
    #
    if {[info exists useSafeInterp] && $useSafeInterp} then {
      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          eval $logCommand [list \
              "$caller: Using a \"safe\" interpreter (variable)..."]
        }
      }

      return true
    }

    #
    # NOTE: The environment has been configured to use a "safe" interpreter?
    #
    if {[info exists env(UseSafeInterp)]} then {
      set result $env(UseSafeInterp)

      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          if {$result} then {
            eval $logCommand [list \
                "$caller: Using a \"safe\" interpreter (environment)..."]
          } else {
            eval $logCommand [list \
                "$caller: Not using a \"safe\" interpreter (environment)..."]
          }
        }
      }

      return $result
    }

    #
    # NOTE: Ok, disable "safe" interpreter use.
    #
    if {$verbose} then {
      catch {
        set caller [maybeFullName [lindex [info level 0] 0]]

        eval $logCommand [list \
            "$caller: Not using a \"safe\" interpreter..."]
      }
    }

    return false
  }

  proc getTemporaryDirectory {} {
    #
    # HACK: The [file tempfile] sub-command requires Tcl 8.6.
    #
    close [file tempfile fileName]
    file delete $fileName
    return [file dirname $fileName]
  }

  proc getRuntimeConfigPath {} {
    global env
    variable logCommand
    variable verbose

    if {[info exists env(RuntimeConfigPath)]} then {
      set path $env(RuntimeConfigPath)

      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          eval $logCommand [list \
              "$caller: Using runtime configuration path\
              \"$path\" (environment)..."]
        }
      }

      return $path
    } elseif {![interp issafe]} then {
      set fileName [info nameofexecutable]

      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          eval $logCommand [list \
              "$caller: Detected executable file name \"$fileName\"..."]
        }
      }

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
            set directory /tmp; # TODO: Portable?
          }
        }

        set path [file join $directory $fileNameOnly]

        if {$verbose} then {
          catch {
            set caller [maybeFullName [lindex [info level 0] 0]]

            eval $logCommand [list \
                "$caller: Using runtime configuration path\
                \"$path\" (default)..."]
          }
        }

        return $path
      }
    }

    return ""
  }

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

  proc getPackageAssemblyTypeName { useCoreClr } {
    if {[string is true -strict $useCoreClr]} then {
      return "Eagle._Components.Public.NativePackage,\
              Eagle, Version=1.0, Culture=neutral"
    } else {
      return Eagle._Components.Public.NativePackage
    }
  }

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

  proc getRegistryPathList { rootKeyName valueName } {
    set result [list]

    if {[isWindows]} then {
      catch {
        package require registry; # NOTE: Tcl for Windows only.

        foreach keyName [registry keys $rootKeyName] {
          set subKeyName $rootKeyName\\$keyName

          if {[catch {string trim [registry get \
                  $subKeyName $valueName]} path] == 0} then {
            if {[isValidDirectory $path] || [isValidFile $path]} then {
              lappend result $path
            }
          }
        }
      }
    }

    return $result
  }

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

  proc haveEagle { {varName ""} } {
    #
    # NOTE: Attempt to determine if Eagle has been loaded successfully and is
    #       currently available for use.  First, check that there is a global
    #       command named "eagle".  Second, make sure we can use that command
    #       to evaluate a trivial Eagle script that fetches the name of the
    #       script engine itself from the Eagle interpreter.  Finally, compare
    #       that result with "eagle" to make sure it is really Eagle.
    #
    if {[llength [info commands ::eagle]] > 0 && [catch {
      ::eagle {set ::tcl_platform(engine)}
    } engine] == 0 && [string equal -nocase $engine eagle]} then {
      #
      # NOTE: Ok, it looks like Eagle is loaded and ready for use.  If the
      #       caller wants the patch level, use the specified variable name
      #       to store it in the context of the caller.
      #
      if {[string length $varName] > 0} then {
        upvar 1 $varName version
      }

      #
      # NOTE: Fetch the full patch level of the Eagle script engine.
      #
      if {[catch {
        ::eagle {set ::eagle_platform(patchLevel)}
      } version] == 0} then {
        #
        # NOTE: Finally, verify that the result looks like a proper patch
        #       level using a suitable regular expression.
        #
        if {[regexp -- {^\d+\.\d+\.\d+\.\d+$} $version]} then {
          return true
        }
      }
    }

    return false
  }

  #############################################################################
  #********************* PACKAGE VARIABLE SETUP PROCEDURE *********************
  #############################################################################

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
      if {[hasUseCoreClr result]} then {
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
    variable packageBinaryFileName; # DEFAULT: ${directory}/Garuda[Core].dll

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

  proc setupForCoreClr {} {
    global tcl_platform
    variable coreClrVersion
    variable logCommand
    variable runtimeConfigPath
    variable verbose

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
      if {![info exists coreClrVersion] && \
          [checkCoreClrDirectories $platform version]} then {
        if {$verbose} then {
          catch {
            set caller [maybeFullName [lindex [info level 0] 0]]

            eval $logCommand [list \
                "$caller: Using CoreCLR $version (installed)..."]
          }
        }

        set coreClrVersion $version; # NOTE: Select "best" version.
      }
    }

    #
    # NOTE: If necessary, write the runtime configuration file needed by the
    #       CoreCLR.  Also, add to the PATH environment variable when needed
    #       to load the CoreCLR runtime.
    #
    if {![interp issafe] && [info exists coreClrVersion]} then {
      if {[info exists runtimeConfigPath] && \
          ![isValidFile $runtimeConfigPath]} then {
        writeCoreClrRuntimeConfiguration $runtimeConfigPath $coreClrVersion

        if {$verbose} then {
          catch {
            set caller [maybeFullName [lindex [info level 0] 0]]

            eval $logCommand [list \
                "$caller: Wrote CoreCLR $coreClrVersion configuration\
                to file \"$runtimeConfigPath\"..."]
          }
        }
      }

      if {[string length $platform] > 0} then {
        set runtimeDirectory [getCoreClrDirectory \
            $platform $coreClrVersion]

        if {[string length $runtimeDirectory] > 0 && \
            [addToPath $runtimeDirectory]} then {
          if {$verbose} then {
            catch {
              set caller [maybeFullName [lindex [info level 0] 0]]

              eval $logCommand [list \
                  "$caller: Added CoreCLR $coreClrVersion runtime\
                  directory \"$runtimeDirectory\" to PATH..."]
            }
          }
        }
      }
    }
  }

  proc setupAndLoad { directory } {
    variable assemblyConfigurations
    variable assemblyFileNames
    variable assemblyPath
    variable assemblySubDirectories
    variable envVars
    variable envVarSuffixes
    variable logCommand
    variable packageBinaryFileName
    variable packageName
    variable rootRegistryKeyName
    variable useEnvironment
    variable useLibrary
    variable useRegistry
    variable useRelativePath
    variable verbose

    if {[info exists assemblyPath]} then {
      #
      # NOTE: Managed assembly path has been pre-configured by an external
      #       script; therefore, just use it verbatim.
      #
      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          eval $logCommand [list \
              "$caller: Using existing assembly path \"$assemblyPath\"..."]
        }
      }
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

      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          eval $logCommand [list \
              "$caller: Final list of directories to search: $directories"]
        }
      }

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

        if {$verbose} then {
          catch {
            set caller [maybeFullName [lindex [info level 0] 0]]

            eval $logCommand [list \
                "$caller: Using default assembly path \"$assemblyPath\"..."]
          }
        }
      }
    }

    #
    # NOTE: Attempt to load the dynamic link library for the package now that
    #       the managed assembly path has been set [to something].
    #
    if {$verbose} then {
      catch {
        set caller [maybeFullName [lindex [info level 0] 0]]

        eval $logCommand [list \
            "$caller: Using final assembly path \"$assemblyPath\"..."]
      }
    }

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
