###############################################################################
#
# all.tcl --
#
# This file contains a top-level script to run all of the Garuda tests.
# Execute it by invoking "source all.eagle".
#
# Extensible Adaptable Generalized Logic Engine (Eagle)
# Test Suite File
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

  #
  # NOTE: Stolen from "helper.tcl" because this procedure is needed prior to
  #       the Garuda package being loaded.
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
  # NOTE: Stolen from "helper.tcl" because this procedure is needed prior to
  #       the Garuda package being loaded.
  #
  proc maybeFullName { command } {
    set which [namespace which $command]

    if {[string length $which] > 0} then {
      return $which
    }

    return $command
  }

  #
  # NOTE: Stolen from "helper.tcl" because this procedure is needed prior to
  #       the Garuda package being loaded.
  #
  proc fileNormalize { path {force false} } {
    variable noNormalize

    if {$force || !$noNormalize} then {
      return [file normalize $path]
    }

    return $path
  }

  #
  # NOTE: Stolen from "helper.tcl" because this procedure is needed prior to
  #       the Garuda package being loaded.
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
  # NOTE: Stolen from "helper.tcl" because this procedure is needed prior to
  #       the Garuda package being loaded.
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

  proc findPackagePath {
          varNames varSuffixes name version platforms configurations directory
          binaryFileName indexFileName } {
    global env

    #
    # NOTE: Construct the name of the base name of the directory that should
    #       contain the package itself, including its binary.
    #
    set nameAndVersion [join [list $name $version] ""]

    #
    # NOTE: Check if the package can be found using the list of environment
    #       variables specified by the caller.
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
          set path [file join [string trim $env($newVarName)] \
              $binaryFileName]

          if {[isValidFile $path]} then {
            set path [file join [file dirname $path] \
                $indexFileName]

            if {[isValidFile $path]} then {
              return [file dirname $path]
            }
          }
        }
      }

      if {[info exists env($varName)]} then {
        set path [file join [string trim $env($varName)] \
            $binaryFileName]

        if {[isValidFile $path]} then {
          set path [file join [file dirname $path] \
              $indexFileName]

          if {[isValidFile $path]} then {
            return [file dirname $path]
          }
        }
      }
    }

    #
    # NOTE: Check the in-development directories for the package being tested,
    #       based on the provided build platforms and configurations.
    #
    foreach platform $platforms {
      foreach configuration $configurations {
        set path [file join $directory bin $platform \
            $configuration $binaryFileName]

        if {[isValidFile $path]} then {
          set path [file join [file dirname $path] \
              $indexFileName]

          if {[isValidFile $path]} then {
            return [file dirname $path]
          }
        }
      }
    }

    #
    # NOTE: Check the in-deployment directory for the package being tested.
    #
    set path [file join $directory $nameAndVersion \
        $binaryFileName]

    if {[isValidFile $path]} then {
      set path [file join [file dirname $path] \
          $indexFileName]

      if {[isValidFile $path]} then {
        return [file dirname $path]
      }
    }

    return ""
  }

  proc addToAutoPath { directory } {
    global auto_path

    #
    # NOTE: Attempt to make absolutely sure that the specified directory is
    #       not already present in the auto-path by checking several of the
    #       various forms it may take.
    #
    if {[lsearch -exact $auto_path $directory] == -1 && \
        [lsearch -exact $auto_path [fileNormalize $directory true]] == -1 && \
        [lsearch -exact $auto_path [file nativename $directory]] == -1} then {
      #
      # BUGFIX: Make sure that the specified directory is the *FIRST* one
      #         that gets searched for the package being tested; otherwise,
      #         we may end up loading and testing the wrong package binary.
      #
      set auto_path [linsert $auto_path 0 $directory]
    }
  }

  #############################################################################
  #********************** TEST VARIABLE SETUP PROCEDURES **********************
  #############################################################################

  proc setupTestPackageConfigurations { force } {
    variable testPackageConfigurations; # DEFAULT: {DebugDll ReleaseDll ""}

    if {$force || ![info exists testPackageConfigurations]} then {
      #
      # NOTE: Always start with no configurations.
      #
      set testPackageConfigurations [list]

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
        lappend testPackageConfigurations DebugDll${::test_flags(-suffix)}
        lappend testPackageConfigurations ReleaseDll${::test_flags(-suffix)}
      }

      lappend testPackageConfigurations DebugDll ReleaseDll ""
    }
  }

  proc setupTestVariables {} {
    global tcl_platform

    ###########################################################################
    #*********** NATIVE PACKAGE DIAGNOSTIC CONFIGURATION VARIABLES ************
    ###########################################################################

    #
    # NOTE: Display diagnostic messages while searching for the package being
    #       tested and setting up the tests?  This variable may be shared with
    #       the package being tested; therefore, change it with care.
    #
    variable verbose; # DEFAULT: true

    if {![info exists verbose]} then {
      set verbose true
    }

    #
    # NOTE: The Tcl command used to log warnings, errors, and other messages
    #       generated by the package being tested.  This variable may be shared
    #       with the package being tested; therefore, change it with care.
    #
    variable logCommand; # DEFAULT: tclLog

    if {![info exists logCommand]} then {
      set logCommand tclLog
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
    #********************* NATIVE PACKAGE TEST VARIABLES **********************
    ###########################################################################

    #
    # NOTE: Automatically run all the tests now instead of waiting for the
    #       runPackageTests procedure to be executed?
    #
    variable startTests; # DEFAULT: true

    if {![info exists startTests]} then {
      set startTests true
    }

    #
    # NOTE: The environment variable names to check when attempting to find the
    #       Garuda binary directory.  This list is used during the file search
    #       process from within the [runPackageTests] procedure.
    #
    variable testEnvVars; # DEFAULT: "Garuda_Dll Garuda GarudaLkg Lkg"

    if {![info exists testEnvVars]} then {
      set testEnvVars [list Garuda_Dll Garuda GarudaLkg Lkg]
    }

    #
    # NOTE: The strings to append to the environment variable names listed
    #       above when attempting to find the Garuda binary directory.  This
    #       list is used during the file search process from within the
    #       [runPackageTests] procedure.
    #
    variable testEnvVarSuffixes; # DEFAULT: "_Temp Temp _Build Build"

    if {![info exists testEnvVarSuffixes]} then {
      set testEnvVarSuffixes [list _Temp Temp _Build Build]
    }

    #
    # NOTE: The build platforms for the package being tested that we know about
    #       and support.
    #
    variable testPackagePlatforms; # DEFAULT: "Win32 x64" OR "x64 Win32"

    if {![info exists testPackagePlatforms]} then {
      #
      # NOTE: Attempt to select the appropriate platforms (architectures)
      #       for this machine.
      #
      if {[info exists tcl_platform(machine)] && \
          $tcl_platform(machine) eq "amd64"} then {
        #
        # NOTE: We are running on an x64 machine, prefer it over x86.
        #
        set testPackagePlatforms [list x64 Win32]
      } else {
        #
        # NOTE: We are running on an x86 machine, prefer it over x64.
        #
        set testPackagePlatforms [list Win32 x64]
      }
    }

    #
    # NOTE: The build configurations for the package being tested that we know
    #       about and support.
    #
    setupTestPackageConfigurations false

    #
    # NOTE: The name of the package being tested.
    #
    variable testPackageName; # DEFAULT: Garuda

    if {![info exists testPackageName]} then {
      set testPackageName \
          [lindex [split [string trim [namespace current] :] :] 0]
    }

    #
    # NOTE: The version of the package being tested.
    #
    variable testPackageVersion; # DEFAULT: 1.0

    if {![info exists testPackageVersion]} then {
      set testPackageVersion 1.0
    }

    #
    # NOTE: The name of the dynamic link library file containing the native
    #       code for the package being tested.
    #
    variable testBinaryFileName; # DEFAULT: Garuda.dll

    if {![info exists testBinaryFileName]} then {
      set testBinaryFileName $testPackageName[info sharedlibextension]
    }

    #
    # NOTE: The name of the Tcl package index file.
    #
    variable testPackageIndexFileName; # DEFAULT: pkgIndex.tcl

    if {![info exists testPackageIndexFileName]} then {
      set testPackageIndexFileName pkgIndex.tcl
    }

    #
    # NOTE: The name of the directory where the dynamic link library file
    #       containing the native code for the package being tested resides.
    #
    variable testBinaryPath; # DEFAULT: <unset>

    #
    # NOTE: The names of the Eagle test suite files to run.
    #
    variable testFileNames; # DEFAULT: tcl-load.eagle

    if {![info exists testFileNames]} then {
      set testFileNames [list tcl-load.eagle]
    }

    #
    # NOTE: The name of the main Eagle test suite file.
    #
    variable testSuiteFileName; # DEFAULT: all.eagle

    if {![info exists testSuiteFileName]} then {
      set testSuiteFileName all.eagle
    }
  }

  #############################################################################
  #************************** TEST STARTUP PROCEDURE **************************
  #############################################################################

  proc runPackageTests { directory } {
    global argv
    global auto_path
    variable envVars
    variable envVarSuffixes
    variable logCommand
    variable rootRegistryKeyName
    variable testBinaryFileName
    variable testBinaryPath
    variable testEnvVars
    variable testEnvVarSuffixes
    variable testFileNames
    variable testPackageConfigurations
    variable testPackageIndexFileName
    variable testPackageName
    variable testPackagePlatforms
    variable testPackageVersion
    variable testSuiteFileName
    variable useEnvironment
    variable useLibrary
    variable useRegistry
    variable useRelativePath
    variable verbose

    #
    # HACK: Scan for and then process the "-baseDirectory", "-configuration",
    #       "-suffix", "-preTest", and "-postTest" command line arguments. The
    #       first one may be used to override the base directory that is used
    #       when attempting to locate the package binaries and the Eagle test
    #       suite entry point file (e.g. "all.eagle").  The next two are needed
    #       by the "helper.tcl" script to locate the proper Eagle assembly to
    #       load and use for the tests.  The final two may be needed to support
    #       various tests.
    #
    foreach {name value} $argv {
      switch -exact -- $name {
        -baseDirectory {
          #
          # NOTE: Use the base directory from the command line verbatim.  This
          #       will be picked up and used later in this procedure to help
          #       locate the package binaries as well as the Eagle test suite
          #       entry point file (e.g. "all.eagle").
          #
          set [string trimleft $name -] $value

          #
          # NOTE: Show that we set this option (in the log).
          #
          if {$verbose} then {
            catch {
              set caller [maybeFullName [lindex [info level 0] 0]]

              eval $logCommand [list \
                  "$caller: Set option \"$name\" to value \"$value\"."]
            }
          }
        }
        -configuration -
        -suffix {
          #
          # NOTE: This will be picked up by the "helper.tcl" file.
          #
          set ::test_flags($name) $value

          #
          # NOTE: Show that we set this option (in the log).
          #
          if {$verbose} then {
            catch {
              set caller [maybeFullName [lindex [info level 0] 0]]

              eval $logCommand [list \
                  "$caller: Set option \"$name\" to value \"$value\"."]
            }
          }

          #
          # HACK: If we are changing the suffix, re-check the test package
          #       configurations.
          #
          if {$name eq "-suffix"} then {
            setupTestPackageConfigurations true
          }
        }
        -preTest -
        -postTest {
          #
          # NOTE: Set the local variable (minus leading dashes) to the value,
          #       which is a script to evaluate before/after the test itself.
          #
          set [string trimleft $name -] $value

          #
          # NOTE: Show that we set this option (in the log).
          #
          if {$verbose} then {
            catch {
              set caller [maybeFullName [lindex [info level 0] 0]]

              eval $logCommand [list \
                  "$caller: Set option \"$name\" to value \"$value\"."]
            }
          }
        }
      }
    }

    #
    # NOTE: Skip setting the base directory if it already exists (e.g. it has
    #       been set via the command line).
    #
    if {![info exists baseDirectory]} then {
      #
      # NOTE: When running in development [within the source tree], this should
      #       give us the "Native" directory.  When running in deployment (e.g.
      #       "<Tcl>\lib\Garuda1.0\tests"), this should give us the application
      #       (or Tcl) library directory (i.e. the one containing the various
      #       package sub-directories).
      #
      set baseDirectory [file dirname [file dirname $directory]]

      #
      # NOTE: Attempt to detect if we are running in development [within the
      #       source tree] by checking if the base directory is now "Native".
      #       In that case, we need to go up another level to obtain the root
      #       Eagle source code directory (i.e. the directory with the "bin",
      #       "Library", and "Native" sub-directories).
      #
      if {[file tail $baseDirectory] eq "Native"} then {
        set baseDirectory [file dirname $baseDirectory]
      }
    }

    #
    # NOTE: Show the effective base directory now.
    #
    if {$verbose} then {
      catch {
        set caller [maybeFullName [lindex [info level 0] 0]]

        eval $logCommand [list \
            "$caller: Base directory is \"$baseDirectory\"."]
      }
    }

    #
    # NOTE: Attempt to find binary file for the package being tested using the
    #       configured platforms, configurations, and file name.
    #
    if {[info exists testBinaryPath]} then {
      #
      # NOTE: The path has probably been pre-configured by an external script;
      #       therefore, just use it verbatim.
      #
      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          eval $logCommand [list \
              "$caller: Using existing binary path \"$testBinaryPath\"..."]
        }
      }
    } else {
      set path [findPackagePath $testEnvVars $testEnvVarSuffixes \
          $testPackageName $testPackageVersion $testPackagePlatforms \
          $testPackageConfigurations $baseDirectory $testBinaryFileName \
          $testPackageIndexFileName]

      if {[isValidDirectory $path]} then {
        set testBinaryPath $path
      }
    }

    #
    # NOTE: Double-check that the configured directory is valid.
    #
    if {[info exists testBinaryPath] && \
        [isValidDirectory $testBinaryPath]} then {
      #
      # NOTE: Success, we found the necessary binary file.  Add the directory
      #       containing the file to the Tcl package search path if it is not
      #       already present.
      #
      if {[lsearch -exact $auto_path $testBinaryPath] != -1} then {
        if {$verbose} then {
          catch {
            set caller [maybeFullName [lindex [info level 0] 0]]

            eval $logCommand [list \
                "$caller: Binary path already present in \"auto_path\"."]
          }
        }
      } else {
        addToAutoPath $testBinaryPath
      }

      #
      # NOTE: Evaluate the pre-test script now, if any.  This must be done
      #       prior to loading the actual Tcl package; otherwise, we cannot
      #       impact the (embedded) Eagle interpreter creation process.
      #
      if {[info exists preTest]} then {
        uplevel #0 $preTest
      }

      #
      # NOTE: Attempt to require the package being tested now.  This should
      #       end up sourcing the "helper.tcl" file, which must also provide
      #       us with the "envVars", "rootRegistryKeyName", "useEnvironment",
      #       "useLibrary", "useRegistry", and "useRelativePath" Tcl variables
      #       that we need.
      #
      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          eval $logCommand [list \
              "$caller: Using final binary path \"$testBinaryPath\"..."]
        }
      }

      package require $testPackageName $testPackageVersion

      #
      # NOTE: Configure the Eagle test suite to run only the specified file(s)
      #       unless it has already been configured otherwise.
      #
      if {[lsearch -exact $argv -file] != -1} then {
        if {$verbose} then {
          catch {
            set caller [maybeFullName [lindex [info level 0] 0]]

            eval $logCommand [list \
                "$caller: Option \"-file\" already present in \"argv\"."]
          }
        }
      } else {
        #
        # NOTE: No file option found, add it.
        #
        lappend argv -file $testFileNames

        #
        # NOTE: Show that we set this option (in the log).
        #
        if {$verbose} then {
          catch {
            set caller [maybeFullName [lindex [info level 0] 0]]

            eval $logCommand [list \
                "$caller: Set option \"-file\" to \"$testFileNames\"."]
          }
        }
      }

      #
      # NOTE: Build the list of directories to search for the main Eagle test
      #       suite file.
      #
      set testSuiteDirectories [list]

      eval lappendUnique testSuiteDirectories [list \
          [file join $baseDirectory Library] $baseDirectory]

      if {$useRelativePath} then {
        eval lappendUnique testSuiteDirectories [getRelativePathList \
            [list $directory [file dirname $directory] \
            $baseDirectory [file dirname $baseDirectory] \
            [file dirname [file dirname $baseDirectory]]] \
            $testPackageConfigurations]
      }

      if {$useEnvironment} then {
        eval lappendUnique testSuiteDirectories [getEnvironmentPathList \
            $envVars $envVarSuffixes]
      }

      if {$useRegistry} then {
        eval lappendUnique testSuiteDirectories [getRegistryPathList \
            $rootRegistryKeyName Path]
      }

      if {$useLibrary} then {
        eval lappendUnique testSuiteDirectories [getLibraryPathList]
      }

      if {$verbose} then {
        catch {
          set caller [maybeFullName [lindex [info level 0] 0]]

          eval $logCommand [list \
              "$caller: Final list of directories to search:\
              $testSuiteDirectories"]
        }
      }

      #
      # NOTE: Search for the main Eagle test suite file in all the configured
      #       directories, stopping when found.
      #
      foreach testSuiteDirectory $testSuiteDirectories {
        set testFileName [file join $testSuiteDirectory Tests \
            $testSuiteFileName]

        if {[isValidFile $testFileName]} then {
          break
        }
      }

      #
      # NOTE: Did we find the main Eagle test suite file?
      #
      if {[info exists testFileName] && [isValidFile $testFileName]} then {
        #
        # NOTE: Attempt to run the Eagle test suite now.
        #
        if {$verbose} then {
          catch {
            set caller [maybeFullName [lindex [info level 0] 0]]

            eval $logCommand [list \
                "$caller: Using final test file name \"$testFileName\"..."]
          }
        }

        uplevel #0 [list source $testFileName]

        #
        # NOTE: Evaluate the post-test script now, if any.
        #
        if {[info exists postTest]} then {
          uplevel #0 $postTest
        }
      } else {
        error "cannot locate Eagle test suite file: $testSuiteFileName"
      }
    } else {
      error "cannot locate package binary file: $testBinaryFileName"
    }
  }

  #############################################################################
  #******************************* TEST STARTUP *******************************
  #############################################################################

  #
  # NOTE: First, setup the script variables associated with the package tests.
  #
  setupTestVariables

  #
  # NOTE: Next, save the package test path for later use.
  #
  if {![info exists packageTestPath]} then {
    set packageTestPath [fileNormalize [file dirname [info script]] true]
  }

  #
  # NOTE: Finally, if enabled, start the package tests now.
  #
  if {$startTests} then {
    runPackageTests $packageTestPath
  }
}
