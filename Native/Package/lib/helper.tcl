###############################################################################
#
# helper.tcl -- Eagle Package for Tcl (Garuda)
#
# Extensible Adaptable Generalized Logic Engine (Eagle)
# Package Loading Helper File (Primary)
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
  # NOTE: Also defined in and used by "dotnet.tcl".
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
    return [expr {[string length $path] > 0 && [file exists $path] && \
        [file isdirectory $path]}]
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
    return [expr {[string length $path] > 0 && [file exists $path] && \
        [file isfile $path]}]
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
      return [fileNormalize $env(SystemRoot) true]
    } elseif {[info exists env(WinDir)]} then {
      return [fileNormalize $env(WinDir) true]
    }

    return ""
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

  proc readFile { fileName } {
    set channel [open $fileName RDONLY]
    fconfigure $channel -encoding binary -translation binary
    set result [read $channel]
    close $channel
    return $result
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
    #       CLR version; therefore, return true.
    #
    if {[info exists env(UseMinimumClr)]} then {
      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          eval $logCommand [list \
              "$caller: Using minimum CLR version (environment)..."]
        }
      }

      return true
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
    # NOTE: Ok, use the latest supported version of the CLR.
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
    # NOTE: The environment has been configured to use interpreter isolation;
    #       therefore, return true.
    #
    if {[info exists env(UseIsolation)]} then {
      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          eval $logCommand [list \
              "$caller: Using interpreter isolation (environment)..."]
        }
      }

      return true
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
    # NOTE: The environment has been configured to use a "safe" interpreter;
    #       therefore, return true.
    #
    if {[info exists env(UseSafeInterp)]} then {
      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          eval $logCommand [list \
              "$caller: Using a \"safe\" interpreter (environment)..."]
        }
      }

      return true
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

  proc getRelativePathList { directories configurations } {
    set result [list]

    foreach directory $directories {
      foreach configuration $configurations {
        set path [file join $directory $configuration Eagle bin]

        if {[isValidDirectory $path] || [isValidFile $path]} then {
          lappend result $path
        }

        set path [file join $directory $configuration bin]

        if {[isValidDirectory $path] || [isValidFile $path]} then {
          lappend result $path
        }

        set path [file join $directory $configuration Eagle]

        if {[isValidDirectory $path] || [isValidFile $path]} then {
          lappend result $path
        }

        set path [file join $directory $configuration]

        if {[isValidDirectory $path] || [isValidFile $path]} then {
          lappend result $path
        }
      }
    }

    return $result
  }

  proc probeAssemblyFile { directory configuration fileName } {
    variable assemblyBaseName
    variable packageBinaryFileName

    set path $directory; # maybe it is really a file?

    if {[isValidFile $path]} then {
      return $path
    }

    set clrPath [expr {
      [shouldUseMinimumClr $packageBinaryFileName] ? "CLRv2" : "CLRv4"
    }]

    if {[string length $configuration] > 0} then {
      set path [file join $directory $assemblyBaseName bin \
          $configuration bin $clrPath $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join $directory $assemblyBaseName bin \
          $configuration bin $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join $directory bin $configuration bin \
          $clrPath $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join $directory bin $configuration bin \
          $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join $directory $assemblyBaseName bin \
          $configuration $clrPath $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join $directory $assemblyBaseName bin \
          $configuration $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join $directory bin $configuration \
          $clrPath $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join $directory bin $configuration \
          $fileName]

      if {[isValidFile $path]} then {
        return $path
      }
    } else {
      set path [file join $directory $assemblyBaseName bin \
          $clrPath $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join $directory $assemblyBaseName bin \
          $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join $directory bin $clrPath $fileName]

      if {[isValidFile $path]} then {
        return $path
      }

      set path [file join $directory bin $fileName]

      if {[isValidFile $path]} then {
        return $path
      }
    }

    return ""
  }

  proc findAssemblyFile { directories configurations fileNames } {
    foreach directory $directories {
      foreach configuration $configurations {
        foreach fileName $fileNames {
          set path [probeAssemblyFile $directory $configuration $fileName]

          if {[isValidFile $path]} then {
            return $path
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
    if {[llength [info commands ::eagle]] > 0 && \
        [catch {::eagle {set ::tcl_platform(engine)}} engine] == 0 && \
        [string equal -nocase $engine eagle]} then {
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
      if {[catch {::eagle {set ::eagle_platform(patchLevel)}} \
              version] == 0} then {
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

  proc setupHelperVariables { directory } {
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
    #       the package test suite.
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

    ###########################################################################
    #********************* NATIVE PACKAGE NAME VARIABLES **********************
    ###########################################################################

    #
    # NOTE: The name of the package we will provide to Tcl.
    #
    variable packageName; # DEFAULT: Garuda

    if {![info exists packageName]} then {
      set packageName [lindex [split [string trim [namespace current] :] :] 0]
    }

    #
    # NOTE: The name of the dynamic link library containing the native code for
    #       this package.
    #
    variable packageBinaryFileNameOnly; # DEFAULT: Garuda.dll

    if {![info exists packageBinaryFileNameOnly]} then {
      set packageBinaryFileNameOnly $packageName[info sharedlibextension]
    }

    #
    # NOTE: The fully qualified file name for the package binary.
    #
    variable packageBinaryFileName; # DEFAULT: ${directory}/Garuda.dll

    if {![info exists packageBinaryFileName]} then {
      set packageBinaryFileName [fileNormalize [file join $directory \
          $packageBinaryFileNameOnly] true]
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
      set typeName Eagle._Components.Public.NativePackage
    }

    #
    # NOTE: The name of the CLR method to execute when starting up the bridge
    #       between Eagle and Tcl.  This is used by the code in the CLR
    #       assembly manager contained in this package.
    #
    variable startupMethodName; # DEFAULT: Startup

    if {![info exists startupMethodName]} then {
      set startupMethodName Startup
    }

    #
    # NOTE: The name of the CLR method to execute when issuing control
    #       directives to the bridge between Eagle and Tcl.  This is used by
    #       the code in the CLR assembly manager contained in this package.
    #
    variable controlMethodName; # DEFAULT: Control

    if {![info exists controlMethodName]} then {
      set controlMethodName Control
    }

    #
    # NOTE: The name of the managed method to execute when detaching a specific
    #       Tcl interpreter from the bridge between Eagle and Tcl.  This is
    #       used by the code in the CLR assembly manager contained in this
    #       package.
    #
    variable detachMethodName; # DEFAULT: Detach

    if {![info exists detachMethodName]} then {
      set detachMethodName Detach
    }

    #
    # NOTE: The name of the managed method to execute when completely shutting
    #       down the bridge between Eagle and Tcl.  This is used by the code in
    #       the CLR assembly manager contained in this package.
    #
    variable shutdownMethodName; # DEFAULT: Shutdown

    if {![info exists shutdownMethodName]} then {
      set shutdownMethodName Shutdown
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
    #
    variable methodFlags; # DEFAULT: 0x0

    if {![info exists methodFlags]} then {
      set methodFlags 0x0
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
    #*************** NATIVE PACKAGE CLR CONFIGURATION VARIABLES ***************
    ###########################################################################

    #
    # NOTE: This is the list of CLR versions supported by this package.  In
    #       the future, this list may need to be updated.
    #
    variable clrVersions; # DEFAULT: "v2.0.50727 v4.0.30319"

    if {![info exists clrVersions]} then {
      set clrVersions [list v2.0.50727 v4.0.30319]
    }

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
    # NOTE: The Eagle build configurations we know about and support.  This
    #       list is used during the CLR assembly search process in the [setup]
    #       procedure (below).
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
      # NOTE: Finally, always add the default build configurations last.
      #
      lappend assemblyConfigurations Debug Release ""
    }

    #
    # NOTE: The possible file names for the Eagle CLR assembly, where X is the
    #       major version of the CLR.
    #
    variable assemblyFileNames; # DEFAULT: "Eagle_CLRvX.dll Eagle.dll"

    if {![info exists assemblyFileNames]} then {
      set assemblyFileNames [list]

      #
      # NOTE: If the minimum supported version of the CLR has been (or will be)
      #       loaded, add the decorated Eagle assembly file name specific to
      #       CLR version 2.0.50727; otherise, add the decorated Eagle assembly
      #       file name specific to CLR version 4.0.30319.
      #
      if {[shouldUseMinimumClr $packageBinaryFileName]} then {
        #
        # NOTE: Either we cannot or should not use the latest known version of
        #       the CLR; therefore, use the minimum supported version.  In this
        #       situation, the Eagle assembly specific to the v2 CLR will be
        #       checked first.
        #
        lappend assemblyFileNames Eagle_CLRv2.dll
      } else {
        #
        # NOTE: The latest known version of the CLR is available for use and we
        #       have not been prevented from using it.  In this situation, the
        #       Eagle assembly specific to the v4 CLR will be checked first.
        #
        # TODO: Should we provide the ability to fallback to the v2 CLR version
        #       of the assembly here (i.e. should "Eagle_CLRv2.dll" be added to
        #       this list right after "Eagle_CLRv4.dll")?  This is always legal
        #       because the v4 CLR can load v2 CLR assemblies.
        #
        lappend assemblyFileNames Eagle_CLRv4.dll
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

  proc setupAndLoad { directory } {
    variable assemblyConfigurations
    variable assemblyFileNames
    variable assemblyPath
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
      # NOTE: The managed assembly path has been pre-configured by an external
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
      # NOTE: Build the list of directories to search for the managed assembly.
      #
      set directories [list]

      if {$useRelativePath} then {
        eval lappendUnique directories [getRelativePathList [list \
            $directory [file dirname $directory] \
            [file dirname [file dirname $directory]] \
            [file dirname [file dirname [file dirname $directory]]]] \
            $assemblyConfigurations]
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
      # NOTE: Attempt to find the Eagle managed assembly file using the list of
      #       candidate directories.
      #
      set path [findAssemblyFile $directories $assemblyConfigurations \
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
  setupHelperVariables $packagePath

  #
  # NOTE: Finally, attempt to setup and load the package right now.
  #
  setupAndLoad $packagePath
}
