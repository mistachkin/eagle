@ECHO OFF

::
:: build.bat --
::
:: Extensible Adaptable Generalized Logic Engine (Eagle)
:: Build Tool
::
:: Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
::
:: See the file "license.terms" for information on usage and redistribution of
:: this file, and for a DISCLAIMER OF ALL WARRANTIES.
::
:: RCS: @(#) $Id: $
::

REM ****************************************************************************
REM ******************** Prologue / Command Line Processing ********************
REM ****************************************************************************

SETLOCAL
ECHO BUILD STARTED ON %DATE% AT %TIME% BY %USERDOMAIN%\%USERNAME%

REM SET __ECHO=ECHO
REM SET __ECHO2=ECHO
REM SET __ECHO3=ECHO
IF NOT DEFINED _AECHO (SET _AECHO=REM)
IF NOT DEFINED _CECHO (SET _CECHO=REM)
IF NOT DEFINED _CECHO2 (SET _CECHO2=REM)
IF NOT DEFINED _CECHO3 (SET _CECHO3=REM)
IF NOT DEFINED _VECHO (SET _VECHO=REM)

%_AECHO% Running %0 %*

REM ****************************************************************************
REM ********************** Set Miscellaneous Environment ***********************
REM ****************************************************************************

IF NOT DEFINED MSBUILD (
  SET MSBUILD=MSBuild.exe
)

%_VECHO% MsBuild = '%MSBUILD%'

IF NOT DEFINED DOTNET (
  SET DOTNET=dotnet.exe
)

%_VECHO% DotNet = '%DOTNET%'

IF NOT DEFINED CSC (
  SET CSC=csc.exe
)

%_VECHO% Csc = '%CSC%'

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

%_VECHO% Tools = '%TOOLS%'

SET ROOT=%~dp0\..\..
SET ROOT=%ROOT:\\=\%

%_VECHO% Root = '%ROOT%'

SET ARGS=%*

%_VECHO% Args = '%ARGS%'

IF NOT DEFINED VSWHERE_EXE (
  SET VSWHERE_EXE=%TOOLS%\vswhere.exe
)

SET VSWHERE_EXE=%VSWHERE_EXE:\\=\%

%_VECHO% VsWhereExe = '%VSWHERE_EXE%'

REM ****************************************************************************
REM ********************* .NET Framework Version Overrides *********************
REM ****************************************************************************

REM
REM TODO: When the next version of Visual Studio is released, this section may
REM       need updating.
REM
IF DEFINED NETCORE20ONLY (
  %_AECHO% Forcing the use of the .NET Core 2.0...
  IF NOT DEFINED YEAR (
    IF DEFINED NETCORE20YEAR (
      SET YEAR=%NETCORE20YEAR%
    ) ELSE (
      SET YEAR=NetStandard2X
    )
  )
  IF NOT DEFINED VCPRJEXT (
    SET VCPRJEXT=.vcxproj
  )
  CALL :fn_VerifyDotNetCore
  IF ERRORLEVEL 1 GOTO errors
  SET NOBUILDTOOLDIR=1
  SET USEDOTNET=1
  GOTO setup_buildToolDir
)

IF DEFINED NETCORE30ONLY (
  %_AECHO% Forcing the use of the .NET Core 3.0...
  IF NOT DEFINED YEAR (
    IF DEFINED NETCORE30YEAR (
      SET YEAR=%NETCORE30YEAR%
    ) ELSE (
      SET YEAR=NetStandard2X
    )
  )
  IF NOT DEFINED VCPRJEXT (
    SET VCPRJEXT=.vcxproj
  )
  CALL :fn_VerifyDotNetCore
  IF ERRORLEVEL 1 GOTO errors
  SET NOBUILDTOOLDIR=1
  SET USEDOTNET=1
  GOTO setup_buildToolDir
)

IF DEFINED NETFX20ONLY (
  %_AECHO% Forcing the use of the .NET Framework 2.0...
  IF NOT DEFINED YEAR (
    IF DEFINED NETFX20YEAR (
      SET YEAR=%NETFX20YEAR%
    ) ELSE (
      SET YEAR=2005
    )
  )
  IF NOT DEFINED VCPRJEXT (
    SET VCPRJEXT=.vcproj
  )
  CALL :fn_CheckFrameworkDir v2.0.50727
  GOTO setup_buildToolDir
)

IF DEFINED NETFX35ONLY (
  %_AECHO% Forcing the use of the .NET Framework 3.5...
  IF NOT DEFINED YEAR (
    IF DEFINED NETFX35YEAR (
      SET YEAR=%NETFX35YEAR%
    ) ELSE (
      SET YEAR=2008
    )
  )
  IF NOT DEFINED VCPRJEXT (
    SET VCPRJEXT=.vcproj
  )
  CALL :fn_CheckFrameworkDir v3.5
  GOTO setup_buildToolDir
)

IF DEFINED NETFX40ONLY (
  %_AECHO% Forcing the use of the .NET Framework 4.0...
  IF NOT DEFINED YEAR (
    IF DEFINED NETFX40YEAR (
      SET YEAR=%NETFX40YEAR%
    ) ELSE (
      SET YEAR=2010
    )
  )
  IF NOT DEFINED VCPRJEXT (
    SET VCPRJEXT=.vcxproj
  )
  CALL :fn_CheckFrameworkDir v4.0.30319
  GOTO setup_buildToolDir
)

IF DEFINED NETFX45ONLY (
  %_AECHO% Forcing the use of the .NET Framework 4.5...
  IF NOT DEFINED YEAR (
    IF DEFINED NETFX45YEAR (
      SET YEAR=%NETFX45YEAR%
    ) ELSE (
      SET YEAR=2012
    )
  )
  IF NOT DEFINED VCPRJEXT (
    SET VCPRJEXT=.vcxproj
  )
  CALL :fn_CheckFrameworkDir v4.0.30319
  GOTO setup_buildToolDir
)

IF DEFINED NETFX451ONLY (
  %_AECHO% Forcing the use of the .NET Framework 4.5.1...
  IF NOT DEFINED YEAR (
    IF DEFINED NETFX451YEAR (
      SET YEAR=%NETFX451YEAR%
    ) ELSE (
      SET YEAR=2013
    )
  )
  IF NOT DEFINED VCPRJEXT (
    SET VCPRJEXT=.vcxproj
  )
  CALL :fn_CheckFrameworkDir v4.0.30319
  CALL :fn_CheckMsBuildDir 12.0
  GOTO setup_buildToolDir
)

IF DEFINED NETFX452ONLY (
  %_AECHO% Forcing the use of the .NET Framework 4.5.2...
  IF NOT DEFINED YEAR (
    IF DEFINED NETFX452YEAR (
      SET YEAR=%NETFX452YEAR%
    ) ELSE (
      SET YEAR=2013
    )
  )
  IF NOT DEFINED VCPRJEXT (
    SET VCPRJEXT=.vcxproj
  )
  CALL :fn_CheckFrameworkDir v4.0.30319
  CALL :fn_CheckMsBuildDir 12.0
  GOTO setup_buildToolDir
)

IF DEFINED NETFX46ONLY (
  %_AECHO% Forcing the use of the .NET Framework 4.6...
  IF NOT DEFINED YEAR (
    IF DEFINED NETFX46YEAR (
      SET YEAR=%NETFX46YEAR%
    ) ELSE (
      SET YEAR=2015
    )
  )
  IF NOT DEFINED VCPRJEXT (
    SET VCPRJEXT=.vcxproj
  )
  CALL :fn_CheckFrameworkDir v4.0.30319
  CALL :fn_CheckMsBuildDir 14.0
  GOTO setup_buildToolDir
)

IF DEFINED NETFX461ONLY (
  %_AECHO% Forcing the use of the .NET Framework 4.6.1...
  IF NOT DEFINED YEAR (
    IF DEFINED NETFX461YEAR (
      SET YEAR=%NETFX461YEAR%
    ) ELSE (
      SET YEAR=2015
    )
  )
  IF NOT DEFINED VCPRJEXT (
    SET VCPRJEXT=.vcxproj
  )
  CALL :fn_CheckFrameworkDir v4.0.30319
  CALL :fn_CheckMsBuildDir 14.0
  GOTO setup_buildToolDir
)

IF DEFINED NETFX462ONLY (
  %_AECHO% Forcing the use of the .NET Framework 4.6.2...
  IF NOT DEFINED YEAR (
    IF DEFINED NETFX462YEAR (
      SET YEAR=%NETFX462YEAR%
    ) ELSE (
      SET YEAR=2015
    )
  )
  IF NOT DEFINED VCPRJEXT (
    SET VCPRJEXT=.vcxproj
  )
  CALL :fn_CheckFrameworkDir v4.0.30319
  CALL :fn_CheckMsBuildDir 14.0
  GOTO setup_buildToolDir
)

IF DEFINED NETFX47ONLY (
  %_AECHO% Forcing the use of the .NET Framework 4.7...
  IF NOT DEFINED YEAR (
    IF DEFINED NETFX47YEAR (
      SET YEAR=%NETFX47YEAR%
    ) ELSE (
      SET YEAR=2017
    )
  )
  IF NOT DEFINED VCPRJEXT (
    SET VCPRJEXT=.vcxproj
  )
  IF NOT DEFINED NOUSEPACKAGERESTORE (
    IF NOT DEFINED USEPACKAGERESTORE (
      SET USEPACKAGERESTORE=1
    )
  )
  CALL :fn_CheckFrameworkDir v4.0.30319
  CALL :fn_CheckMsBuildDir 14.0
  CALL :fn_CheckVisualStudioMsBuildDir 15.0 15.0
  GOTO setup_buildToolDir
)

IF DEFINED NETFX471ONLY (
  %_AECHO% Forcing the use of the .NET Framework 4.7.1...
  IF NOT DEFINED YEAR (
    IF DEFINED NETFX471YEAR (
      SET YEAR=%NETFX471YEAR%
    ) ELSE (
      SET YEAR=2017
    )
  )
  IF NOT DEFINED VCPRJEXT (
    SET VCPRJEXT=.vcxproj
  )
  IF NOT DEFINED NOUSEPACKAGERESTORE (
    IF NOT DEFINED USEPACKAGERESTORE (
      SET USEPACKAGERESTORE=1
    )
  )
  CALL :fn_CheckFrameworkDir v4.0.30319
  CALL :fn_CheckMsBuildDir 14.0
  CALL :fn_CheckVisualStudioMsBuildDir 15.0 15.0
  GOTO setup_buildToolDir
)

IF DEFINED NETFX472ONLY (
  %_AECHO% Forcing the use of the .NET Framework 4.7.2...
  IF NOT DEFINED YEAR (
    IF DEFINED NETFX472YEAR (
      SET YEAR=%NETFX472YEAR%
    ) ELSE (
      SET YEAR=2017
    )
  )
  IF NOT DEFINED VCPRJEXT (
    SET VCPRJEXT=.vcxproj
  )
  IF NOT DEFINED NOUSEPACKAGERESTORE (
    IF NOT DEFINED USEPACKAGERESTORE (
      SET USEPACKAGERESTORE=1
    )
  )
  CALL :fn_CheckFrameworkDir v4.0.30319
  CALL :fn_CheckMsBuildDir 14.0
  CALL :fn_CheckVisualStudioMsBuildDir 15.0 15.0
  GOTO setup_buildToolDir
)

IF DEFINED NETFX48ONLY (
  %_AECHO% Forcing the use of the .NET Framework 4.8...
  IF NOT DEFINED YEAR (
    IF DEFINED NETFX48YEAR (
      SET YEAR=%NETFX48YEAR%
    ) ELSE (
      SET YEAR=2019
    )
  )
  IF NOT DEFINED VCPRJEXT (
    SET VCPRJEXT=.vcxproj
  )
  IF NOT DEFINED NOUSEPACKAGERESTORE (
    IF NOT DEFINED USEPACKAGERESTORE (
      SET USEPACKAGERESTORE=1
    )
  )
  CALL :fn_CheckFrameworkDir v4.0.30319
  CALL :fn_CheckMsBuildDir 14.0
  CALL :fn_CheckVisualStudioMsBuildDir Current 16.0
  GOTO setup_buildToolDir
)

IF DEFINED NETFX481ONLY (
  %_AECHO% Forcing the use of the .NET Framework 4.8.1...
  IF NOT DEFINED YEAR (
    IF DEFINED NETFX481YEAR (
      SET YEAR=%NETFX481YEAR%
    ) ELSE (
      SET YEAR=2022
    )
  )
  IF NOT DEFINED VCPRJEXT (
    SET VCPRJEXT=.vcxproj
  )
  IF NOT DEFINED NOUSEPACKAGERESTORE (
    IF NOT DEFINED USEPACKAGERESTORE (
      SET USEPACKAGERESTORE=1
    )
  )
  CALL :fn_CheckFrameworkDir v4.0.30319
  CALL :fn_CheckMsBuildDir 14.0
  CALL :fn_CheckVisualStudioMsBuildDir Current 17.0
  GOTO setup_buildToolDir
)

REM ****************************************************************************
REM ********************* Visual Studio Version Detection **********************
REM ****************************************************************************

REM
REM TODO: When the next version of Visual Studio and/or MSBuild is released,
REM       this section may need updating.
REM
IF NOT DEFINED VISUALSTUDIOMSBUILDDIR (
  CALL :fn_CheckVisualStudioMsBuildDir Current 17.0
  IF DEFINED VISUALSTUDIOMSBUILDDIR (
    IF NOT DEFINED YEAR (
      SET YEAR=2022
    )
    IF NOT DEFINED VCPRJEXT (
      SET VCPRJEXT=.vcxproj
    )
    IF NOT DEFINED NOUSEPACKAGERESTORE (
      IF NOT DEFINED USEPACKAGERESTORE (
        SET USEPACKAGERESTORE=1
      )
    )
  )
)

IF NOT DEFINED VISUALSTUDIOMSBUILDDIR (
  CALL :fn_CheckVisualStudioMsBuildDir Current 16.0
  IF DEFINED VISUALSTUDIOMSBUILDDIR (
    IF NOT DEFINED YEAR (
      SET YEAR=2019
    )
    IF NOT DEFINED VCPRJEXT (
      SET VCPRJEXT=.vcxproj
    )
    IF NOT DEFINED NOUSEPACKAGERESTORE (
      IF NOT DEFINED USEPACKAGERESTORE (
        SET USEPACKAGERESTORE=1
      )
    )
  )
)

IF NOT DEFINED VISUALSTUDIOMSBUILDDIR (
  CALL :fn_CheckVisualStudioMsBuildDir 15.0 15.0
  IF DEFINED VISUALSTUDIOMSBUILDDIR (
    IF NOT DEFINED YEAR (
      SET YEAR=2017
    )
    IF NOT DEFINED VCPRJEXT (
      SET VCPRJEXT=.vcxproj
    )
    IF NOT DEFINED NOUSEPACKAGERESTORE (
      IF NOT DEFINED USEPACKAGERESTORE (
        SET USEPACKAGERESTORE=1
      )
    )
  )
)

REM ****************************************************************************
REM ************************ MSBuild Version Detection *************************
REM ****************************************************************************

REM
REM TODO: When the next version of MSBuild is released, this section may need
REM       updating.
REM
IF NOT DEFINED MSBUILDDIR (
  CALL :fn_CheckMsBuildDir 14.0
  IF DEFINED MSBUILDDIR (
    IF NOT DEFINED YEAR (
      SET YEAR=2015
    )
    IF NOT DEFINED VCPRJEXT (
      SET VCPRJEXT=.vcxproj
    )
  )
)

IF NOT DEFINED MSBUILDDIR (
  CALL :fn_CheckMsBuildDir 12.0
  IF DEFINED MSBUILDDIR (
    IF NOT DEFINED YEAR (
      SET YEAR=2013
    )
    IF NOT DEFINED VCPRJEXT (
      SET VCPRJEXT=.vcxproj
    )
  )
)

REM ****************************************************************************
REM ********************* .NET Framework Version Detection *********************
REM ****************************************************************************

REM
REM TODO: When the next version of Visual Studio is released, this section may
REM       need updating.
REM
IF NOT DEFINED FRAMEWORKDIR1 (
  CALL :fn_CheckFrameworkDir v4.0.30319
  IF DEFINED FRAMEWORKDIR1 (
    IF NOT DEFINED YEAR (
      SET YEAR=2010
    )
    IF NOT DEFINED VCPRJEXT (
      SET VCPRJEXT=.vcxproj
    )
  )
)

IF NOT DEFINED FRAMEWORKDIR1 (
  CALL :fn_CheckFrameworkDir v3.5
  IF DEFINED FRAMEWORKDIR1 (
    IF NOT DEFINED YEAR (
      SET YEAR=2008
    )
    IF NOT DEFINED VCPRJEXT (
      SET VCPRJEXT=.vcproj
    )
  )
)

IF NOT DEFINED FRAMEWORKDIR1 (
  CALL :fn_CheckFrameworkDir v2.0.50727
  IF DEFINED FRAMEWORKDIR1 (
    IF NOT DEFINED YEAR (
      SET YEAR=2005
    )
    IF NOT DEFINED VCPRJEXT (
      SET VCPRJEXT=.vcproj
    )
  )
)

REM ****************************************************************************
REM *************************** Build Tool Detection ***************************
REM ****************************************************************************

:setup_buildToolDir

%_VECHO% NoBuildToolDir = '%NOBUILDTOOLDIR%'
%_VECHO% UseDotNet = '%USEDOTNET%'
%_VECHO% UsePackageRestore = '%USEPACKAGERESTORE%'

IF NOT DEFINED NOBUILDTOOLDIR (
  IF DEFINED BUILDTOOLDIR (
    %_AECHO% Forcing the use of build tool directory "%BUILDTOOLDIR%"...
  ) ELSE (
    CALL :fn_CheckBuildToolDir
    CALL :fn_VerifyBuildToolDir
  )
)

%_VECHO% Year = '%YEAR%'
%_VECHO% VcPrjExt = '%VCPRJEXT%'
%_VECHO% FrameworkDir1 = '%FRAMEWORKDIR1%'
%_VECHO% MsBuildDir = '%MSBUILDDIR%'
%_VECHO% VisualStudioMsBuildDir = '%VISUALSTUDIOMSBUILDDIR%'
%_VECHO% BuildToolDir = '%BUILDTOOLDIR%'

IF NOT DEFINED NOBUILDTOOLDIR (
  IF NOT DEFINED BUILDTOOLDIR (
    ECHO.
    ECHO No directory containing MSBuild could be found.
    ECHO.
    ECHO Please install the .NET Framework or set the "FRAMEWORKDIR1"
    ECHO environment variable to the location where it is installed.
    ECHO.
    GOTO errors
  )
)

REM ****************************************************************************
REM ****************************** Save Directory ******************************
REM ****************************************************************************

CALL :fn_ResetErrorLevel

%_CECHO2% PUSHD "%ROOT%"
%__ECHO2% PUSHD "%ROOT%"

IF ERRORLEVEL 1 (
  ECHO Could not change directory to "%ROOT%".
  GOTO errors
)

REM ****************************************************************************
REM ************************* Augment Executable Path **************************
REM ****************************************************************************

IF NOT DEFINED NOBUILDTOOLDIR (
  CALL :fn_PrependToPath BUILDTOOLDIR
)

%_VECHO% Path = '%PATH%'

REM ****************************************************************************
REM *************************** Tcl Library Handling ***************************
REM ****************************************************************************

REM
REM NOTE: Make sure Eagle can find the script library even when being built
REM       with an arbitrary configuration suffix.
REM
SET TCL_LIBRARY=%ROOT%\lib\Eagle1.0
SET TCL_LIBRARY=%TCL_LIBRARY:\\=\%
SET TCL_LIBRARY=%TCL_LIBRARY:\=/%

%_VECHO% TclLibrary = '%TCL_LIBRARY%'

REM ****************************************************************************
REM ************************** Configuration Handling **************************
REM ****************************************************************************

SET CONFIGURATION=%1

IF DEFINED CONFIGURATION (
  CALL :fn_UnquoteVariable CONFIGURATION
) ELSE (
  %_AECHO% No configuration specified, using default...
  SET CONFIGURATION=Release
)

CALL :fn_CopyVariable CONFIGURATION BASE_CONFIGURATION
CALL :fn_CopyVariable CONFIGURATION EXTRA_CONFIGURATION

SET BASE_CONFIGURATION=%BASE_CONFIGURATION:All=%
SET BASE_CONFIGURATION=%BASE_CONFIGURATION:Dll=%

%_VECHO% Configuration = '%CONFIGURATION%'
%_VECHO% BaseConfiguration = '%BASE_CONFIGURATION%'
%_VECHO% ExtraConfiguration = '%EXTRA_CONFIGURATION%'

REM ****************************************************************************
REM **************************** Solution Handling *****************************
REM ****************************************************************************

REM
REM NOTE: Unless we are prevented from doing so, try for the "commercial" and
REM       "enterprise" solution files first.  Failing that, fall back to the
REM       standard solution files.
REM
CALL :fn_SetupSolution

IF DEFINED EXTRA_SOLUTION (
  CALL :fn_ReplaceAndUnquoteVariable EXTRA_SOLUTION
)

%_VECHO% Solution = '%SOLUTION%'
%_VECHO% ExtraSolution = '%EXTRA_SOLUTION%'

FOR /F %%E IN ('ECHO %SOLUTION%') DO (SET SOLUTIONEXT=%%~xE)
CALL :fn_ResetErrorLevel

FOR /F %%E IN ('ECHO %EXTRA_SOLUTION%') DO (SET EXTRA_SOLUTIONEXT=%%~xE)
CALL :fn_ResetErrorLevel

%_VECHO% SolutionExt = '%SOLUTIONEXT%'
%_VECHO% ExtraSolutionExt = '%EXTRA_SOLUTIONEXT%'

REM ****************************************************************************
REM ********************* Solution Configuration Handling **********************
REM ****************************************************************************

REM
REM NOTE: When building a C++ project, automatically change the "Configuration"
REM       property to be "DebugDll" or "ReleaseDll", if necessary.  This allows
REM       this tool to be called for a C++ project with no build configuration
REM       specified on the command line.  It also allows the build configuration
REM       specified on the command line to be "DebugAll" or "ReleaseAll", which
REM       are the ones used by the solution files when building all the managed
REM       and native code projects.
REM
CALL :fn_SetupConfiguration
CALL :fn_SetupExtraConfiguration

%_VECHO% Configuration = '%CONFIGURATION%'

REM ****************************************************************************
REM **************************** Platform Handling *****************************
REM ****************************************************************************

CALL :fn_SetupPlatform
CALL :fn_SetupExtraPlatform

%_VECHO% DefaultPlatform = '%DEFAULT_PLATFORM%'
%_VECHO% Platform = '%PLATFORM%'

REM ****************************************************************************
REM ***************************** Target Handling ******************************
REM ****************************************************************************

CALL :fn_SetupTarget

%_VECHO% Target = '%TARGET%'

REM ****************************************************************************
REM ******************************* Log Handling *******************************
REM ****************************************************************************

CALL :fn_SetupLogging
IF ERRORLEVEL 1 GOTO errors

%_VECHO% Temp = '%TEMP%'
%_VECHO% LogAsm = '%LOGASM%'
%_VECHO% LogDir = '%LOGDIR%'
%_VECHO% LogPrefix = '%LOGPREFIX%'
%_VECHO% LogSuffix = '%LOGSUFFIX%'
%_VECHO% Logging = '%LOGGING%'

REM ****************************************************************************
REM ****************************** Build Solution ******************************
REM ****************************************************************************

IF DEFINED USEDOTNET (
  SET MSBUILD=%DOTNET%
  SET BUILD_SUBCOMMANDS=build
) ELSE (
  CALL :fn_UnsetVariable BUILD_SUBCOMMANDS
)

IF NOT DEFINED RESTORE_SUBCOMMANDS (
  SET RESTORE_SUBCOMMANDS=restore
)

%_VECHO% MsBuild = '%MSBUILD%'
%_VECHO% BuildSubCommands = '%BUILD_SUBCOMMANDS%'
%_VECHO% RestoreSubCommands = '%RESTORE_SUBCOMMANDS%'

IF DEFINED USEPACKAGERESTORE (
  %_CECHO% "%DOTNET%" %RESTORE_SUBCOMMANDS% "%SOLUTION%"
  %__ECHO% "%DOTNET%" %RESTORE_SUBCOMMANDS% "%SOLUTION%"

  IF ERRORLEVEL 1 (
    ECHO Restore failed.
    GOTO errors
  )
)

%_CECHO% "%MSBUILD%" %BUILD_SUBCOMMANDS% "%SOLUTION%" "/target:%TARGET%" "/property:Configuration=%CONFIGURATION%" "/property:Platform=%PLATFORM%" %LOGGING% /property:BuildType=%ARGS%
%__ECHO% "%MSBUILD%" %BUILD_SUBCOMMANDS% "%SOLUTION%" "/target:%TARGET%" "/property:Configuration=%CONFIGURATION%" "/property:Platform=%PLATFORM%" %LOGGING% /property:BuildType=%ARGS%

IF ERRORLEVEL 1 (
  ECHO Build failed.
  GOTO errors
)

IF DEFINED EXTRA_SOLUTION (
  %_CECHO% "%MSBUILD%" %BUILD_SUBCOMMANDS% "%EXTRA_SOLUTION%" "/target:%EXTRA_TARGET%" "/property:Configuration=%EXTRA_CONFIGURATION%" "/property:Platform=%EXTRA_PLATFORM%" %LOGGING% /property:BuildType=%ARGS%
  %__ECHO% "%MSBUILD%" %BUILD_SUBCOMMANDS% "%EXTRA_SOLUTION%" "/target:%EXTRA_TARGET%" "/property:Configuration=%EXTRA_CONFIGURATION%" "/property:Platform=%EXTRA_PLATFORM%" %LOGGING% /property:BuildType=%ARGS%

  IF ERRORLEVEL 1 (
    ECHO Extra build failed.
    GOTO errors
  )
)

REM ****************************************************************************
REM **************************** Restore Directory *****************************
REM ****************************************************************************

%_CECHO2% POPD
%__ECHO2% POPD

IF ERRORLEVEL 1 (
  ECHO Could not restore directory.
  GOTO errors
)

REM ****************************************************************************
REM *********************************** Done ***********************************
REM ****************************************************************************

GOTO no_errors

REM ****************************************************************************
REM ****************** Epilogue / Functions / Return Handling ******************
REM ****************************************************************************

:fn_CheckFrameworkDir
  IF DEFINED NOFRAMEWORKDIR GOTO :EOF
  SET FRAMEWORKVER=%1
  %_AECHO% Checking for .NET Framework "%FRAMEWORKVER%"...
  IF NOT DEFINED FRAMEWORKVER GOTO :EOF
  IF DEFINED NOFRAMEWORK64 (
    %_AECHO% Forced into using 32-bit version of MSBuild from Microsoft.NET...
    SET FRAMEWORKDIR1=%windir%\Microsoft.NET\Framework\%FRAMEWORKVER%
    CALL :fn_VerifyFrameworkDir
    GOTO :EOF
  )
  IF NOT "%PROCESSOR_ARCHITECTURE%" == "x86" (
    %_AECHO% The operating system appears to be 64-bit.
    IF EXIST "%windir%\Microsoft.NET\Framework64\%FRAMEWORKVER%" (
      IF EXIST "%windir%\Microsoft.NET\Framework64\%FRAMEWORKVER%\%MSBUILD%" (
        IF EXIST "%windir%\Microsoft.NET\Framework64\%FRAMEWORKVER%\%CSC%" (
          %_AECHO% Using 64-bit version of MSBuild from Microsoft.NET...
          SET FRAMEWORKDIR1=%windir%\Microsoft.NET\Framework64\%FRAMEWORKVER%
          CALL :fn_VerifyFrameworkDir
          GOTO :EOF
        ) ELSE (
          %_AECHO% Missing 64-bit version of "%CSC%".
        )
      ) ELSE (
        %_AECHO% Missing 64-bit version of "%MSBUILD%".
      )
    ) ELSE (
      %_AECHO% Missing 64-bit version of .NET Framework "%FRAMEWORKVER%".
    )
  ) ELSE (
    %_AECHO% The operating system appears to be 32-bit.
  )
  %_AECHO% Using 32-bit version of MSBuild from Microsoft.NET...
  SET FRAMEWORKDIR1=%windir%\Microsoft.NET\Framework\%FRAMEWORKVER%
  CALL :fn_VerifyFrameworkDir
  GOTO :EOF

:fn_VerifyFrameworkDir
  IF DEFINED NOFRAMEWORKDIR GOTO :EOF
  IF NOT DEFINED FRAMEWORKDIR1 (
    %_AECHO% .NET Framework directory is not defined.
    GOTO :EOF
  )
  IF DEFINED FRAMEWORKDIR1 IF NOT EXIST "%FRAMEWORKDIR1%" (
    %_AECHO% .NET Framework directory does not exist, unsetting...
    CALL :fn_UnsetVariable FRAMEWORKDIR1
    GOTO :EOF
  )
  IF DEFINED FRAMEWORKDIR1 IF NOT EXIST "%FRAMEWORKDIR1%\%MSBUILD%" (
    %_AECHO% File "%MSBUILD%" not in .NET Framework directory, unsetting...
    CALL :fn_UnsetVariable FRAMEWORKDIR1
    GOTO :EOF
  )
  IF DEFINED FRAMEWORKDIR1 IF NOT EXIST "%FRAMEWORKDIR1%\%CSC%" (
    %_AECHO% File "%CSC%" not in .NET Framework directory, unsetting...
    CALL :fn_UnsetVariable FRAMEWORKDIR1
    GOTO :EOF
  )
  %_AECHO% .NET Framework directory "%FRAMEWORKDIR1%" verified.
  GOTO :EOF

:fn_CheckMsBuildDir
  IF DEFINED NOMSBUILDDIR GOTO :EOF
  SET MSBUILDVER=%1
  %_AECHO% Checking for MSBuild "%MSBUILDVER%"...
  IF NOT DEFINED MSBUILDVER GOTO :EOF
  IF DEFINED NOMSBUILD64 (
    %_AECHO% Forced into using 32-bit version of MSBuild from Program Files...
    GOTO set_msbuild_x86
  )
  IF "%PROCESSOR_ARCHITECTURE%" == "x86" GOTO set_msbuild_x86
  %_AECHO% The operating system appears to be 64-bit.
  %_AECHO% Using 32-bit version of MSBuild from Program Files...
  SET MSBUILDDIR=%ProgramFiles(x86)%\MSBuild\%MSBUILDVER%\bin
  GOTO set_msbuild_done
  :set_msbuild_x86
  %_AECHO% The operating system appears to be 32-bit.
  %_AECHO% Using native version of MSBuild from Program Files...
  SET MSBUILDDIR=%ProgramFiles%\MSBuild\%MSBUILDVER%\bin
  :set_msbuild_done
  CALL :fn_VerifyMsBuildDir
  GOTO :EOF

:fn_VerifyMsBuildDir
  IF DEFINED NOMSBUILDDIR GOTO :EOF
  IF NOT DEFINED MSBUILDDIR (
    %_AECHO% MSBuild directory is not defined.
    GOTO :EOF
  )
  IF DEFINED MSBUILDDIR IF NOT EXIST "%MSBUILDDIR%" (
    %_AECHO% MSBuild directory does not exist, unsetting...
    CALL :fn_UnsetVariable MSBUILDDIR
    GOTO :EOF
  )
  IF DEFINED MSBUILDDIR IF NOT EXIST "%MSBUILDDIR%\%MSBUILD%" (
    %_AECHO% File "%MSBUILD%" not in MSBuild directory, unsetting...
    CALL :fn_UnsetVariable MSBUILDDIR
    GOTO :EOF
  )
  IF DEFINED MSBUILDDIR IF NOT EXIST "%MSBUILDDIR%\%CSC%" (
    %_AECHO% File "%CSC%" not in MSBuild directory, unsetting...
    CALL :fn_UnsetVariable MSBUILDDIR
    GOTO :EOF
  )
  %_AECHO% MSBuild directory "%MSBUILDDIR%" verified.
  GOTO :EOF

:fn_CheckVisualStudioMsBuildDir
  IF DEFINED NOVISUALSTUDIOMSBUILDDIR GOTO :EOF
  SET MSBUILDVER=%1
  SET VISUALSTUDIOVER=%2
  %_AECHO% Checking for MSBuild "%MSBUILDVER%" within Visual Studio "%VISUALSTUDIOVER%"...
  IF NOT DEFINED MSBUILDVER GOTO :EOF
  IF NOT DEFINED VISUALSTUDIOVER GOTO :EOF
  IF NOT DEFINED VSWHERE_EXE GOTO :EOF
  IF NOT EXIST "%VSWHERE_EXE%" GOTO :EOF
  SET VS_WHEREIS_CMD="%VSWHERE_EXE%" -version %VISUALSTUDIOVER% -products * -requires Microsoft.Component.MSBuild -property installationPath -latest
  IF DEFINED __ECHO (
    %__ECHO% %VS_WHEREIS_CMD%
    SET VISUALSTUDIOINSTALLDIR=C:\Program Files\Microsoft Visual Studio\2017\Community
    GOTO skip_visualStudioInstallDir
  )
  FOR /F "delims=" %%D IN ('%VS_WHEREIS_CMD%') DO (SET VISUALSTUDIOINSTALLDIR=%%D)
  :skip_visualStudioInstallDir
  IF NOT DEFINED VISUALSTUDIOINSTALLDIR (
    %_AECHO% Visual Studio "%VISUALSTUDIOVER%" is not installed.
    GOTO :EOF
  )
  %_AECHO% Visual Studio "%VISUALSTUDIOVER%" is installed.
  SET VISUALSTUDIOMSBUILDDIR=%VISUALSTUDIOINSTALLDIR%\MSBuild\%MSBUILDVER%\bin
  SET VISUALSTUDIOMSBUILDDIR=%VISUALSTUDIOMSBUILDDIR:\\=\%
  CALL :fn_VerifyVisualStudioMsBuildDir
  GOTO :EOF

:fn_VerifyVisualStudioMsBuildDir
  IF DEFINED NOVISUALSTUDIOMSBUILDDIR GOTO :EOF
  IF NOT DEFINED VISUALSTUDIOMSBUILDDIR (
    %_AECHO% Visual Studio directory is not defined.
    GOTO :EOF
  )
  IF DEFINED VISUALSTUDIOMSBUILDDIR IF NOT EXIST "%VISUALSTUDIOMSBUILDDIR%" (
    %_AECHO% Visual Studio directory does not exist, unsetting...
    CALL :fn_UnsetVariable VISUALSTUDIOMSBUILDDIR
    GOTO :EOF
  )
  IF DEFINED VISUALSTUDIOMSBUILDDIR IF NOT EXIST "%VISUALSTUDIOMSBUILDDIR%\%MSBUILD%" (
    %_AECHO% File "%MSBUILD%" not in Visual Studio directory, unsetting...
    CALL :fn_UnsetVariable VISUALSTUDIOMSBUILDDIR
    GOTO :EOF
  )
  IF DEFINED VISUALSTUDIOMSBUILDDIR IF NOT EXIST "%VISUALSTUDIOMSBUILDDIR%\Roslyn\%CSC%" (
    %_AECHO% File "%CSC%" not in Visual Studio directory, unsetting...
    CALL :fn_UnsetVariable VISUALSTUDIOMSBUILDDIR
    GOTO :EOF
  )
  %_AECHO% Visual Studio directory "%VISUALSTUDIOMSBUILDDIR%" verified.
  GOTO :EOF

:fn_CheckBuildToolDir
  %_AECHO% Checking for build tool directories...
  IF DEFINED VISUALSTUDIOMSBUILDDIR GOTO set_visualstudio_msbuild_tools
  IF DEFINED MSBUILDDIR GOTO set_msbuild_tools
  IF DEFINED FRAMEWORKDIR1 GOTO set_framework_tools
  %_AECHO% No build tool directories found.
  GOTO :EOF
  :set_visualstudio_msbuild_tools
  %_AECHO% Using Visual Studio MSBuild directory "%VISUALSTUDIOMSBUILDDIR%"...
  CALL :fn_CopyVariable VISUALSTUDIOMSBUILDDIR BUILDTOOLDIR
  GOTO :EOF
  :set_msbuild_tools
  %_AECHO% Using MSBuild directory "%MSBUILDDIR%"...
  CALL :fn_CopyVariable MSBUILDDIR BUILDTOOLDIR
  GOTO :EOF
  :set_framework_tools
  %_AECHO% Using .NET Framework directory "%FRAMEWORKDIR1%"...
  CALL :fn_CopyVariable FRAMEWORKDIR1 BUILDTOOLDIR
  GOTO :EOF

:fn_VerifyBuildToolDir
  IF NOT DEFINED BUILDTOOLDIR (
    %_AECHO% Build tool directory is not defined.
    GOTO :EOF
  )
  IF DEFINED BUILDTOOLDIR IF NOT EXIST "%BUILDTOOLDIR%" (
    %_AECHO% Build tool directory does not exist, unsetting...
    CALL :fn_UnsetVariable BUILDTOOLDIR
    GOTO :EOF
  )
  IF DEFINED BUILDTOOLDIR IF NOT EXIST "%BUILDTOOLDIR%\%MSBUILD%" (
    %_AECHO% File "%MSBUILD%" not in build tool directory, unsetting...
    CALL :fn_UnsetVariable BUILDTOOLDIR
    GOTO :EOF
  )
  IF DEFINED BUILDTOOLDIR IF NOT EXIST "%BUILDTOOLDIR%\%CSC%" IF NOT EXIST "%BUILDTOOLDIR%\Roslyn\%CSC%" (
    %_AECHO% File "%CSC%" not in build tool directory, unsetting...
    CALL :fn_UnsetVariable BUILDTOOLDIR
    GOTO :EOF
  )
  %_AECHO% Build tool directory "%BUILDTOOLDIR%" verified.
  GOTO :EOF

:fn_VerifyDotNetCore
  FOR %%T IN (%DOTNET%) DO (
    SET %%T_PATH=%%~dp$PATH:T
  )
  IF NOT DEFINED %DOTNET%_PATH (
    ECHO The .NET Core executable "%DOTNET%" is required to be in the PATH.
    CALL :fn_SetErrorLevel
    GOTO :EOF
  )
  GOTO :EOF

:fn_SetupSolution
  IF DEFINED SOLUTION (
    %_AECHO% Building the specified project/solution only...
    GOTO :EOF
  )
  IF DEFINED COREONLY (
    %_AECHO% Building core library project only...
    SET SOLUTION=.\Library\Eagle%YEAR%.csproj
    CALL :fn_CopyVariable BASE_CONFIGURATION CONFIGURATION
    GOTO :EOF
  )
  IF DEFINED SHELLONLY (
    %_AECHO% Building core library and shell projects only...
    SET SOLUTION=.\EagleCore%YEAR%.sln
    CALL :fn_CopyVariable BASE_CONFIGURATION CONFIGURATION
    GOTO :EOF
  )
  IF DEFINED PACKAGEONLY (
    %_AECHO% Building native package only...
    SET SOLUTION=.\Native\Package\Garuda%YEAR%%VCPRJEXT%
    GOTO :EOF
  )
  IF DEFINED UTILITYONLY (
    %_AECHO% Building native utility library only...
    SET SOLUTION=.\Native\Utility\Spilornis%YEAR%%VCPRJEXT%
    GOTO :EOF
  )
  IF DEFINED NOCOMMERCIAL (
    %_AECHO% Building non-commercial projects only...
    SET SOLUTION=.\EagleNonCommercial%YEAR%.sln
  ) ELSE (
    %_AECHO% Building all commercial projects...
    SET SOLUTION=.\EagleCommercial%YEAR%.sln
  )
  IF DEFINED SOLUTION IF EXIST "%SOLUTION%" GOTO :EOF
  IF DEFINED NOENTERPRISE (
    %_AECHO% Building non-enterprise projects only...
    SET SOLUTION=.\EagleNonEnterprise%YEAR%.sln
  ) ELSE (
    %_AECHO% Building all enterprise projects...
    SET SOLUTION=.\EagleEnterprise%YEAR%.sln
  )
  IF DEFINED SOLUTION IF EXIST "%SOLUTION%" GOTO :EOF
  IF DEFINED NOEXTRA (
    %_AECHO% Building non-extra projects only...
    SET SOLUTION=.\EagleNonExtra%YEAR%.sln
  ) ELSE (
    %_AECHO% Building all extra projects...
    SET SOLUTION=.\EagleExtra%YEAR%.sln
  )
  IF DEFINED SOLUTION IF EXIST "%SOLUTION%" GOTO :EOF
  %_AECHO% Building all core projects...
  SET SOLUTION=.\Eagle%YEAR%.sln
  GOTO :EOF

:fn_SetupConfiguration
  IF /I "%SOLUTIONEXT%" == ".vcproj" (
    REM
    REM NOTE: Visual C++ 200X project, allow the build configuration to be
    REM       adjusted, if necessary.
    REM
  ) ELSE IF /I "%SOLUTIONEXT%" == ".vcxproj" (
    REM
    REM NOTE: Visual C++ 20XX project, allow the build configuration to be
    REM       adjusted, if necessary.
    REM
  ) ELSE (
    REM
    REM NOTE: Adjusting the configuration should not be necessary.
    REM
    GOTO :EOF
  )
  IF /I "%CONFIGURATION%" == "Debug" (
    SET CONFIGURATION=DebugDll
  ) ELSE IF /I "%CONFIGURATION%" == "DebugAll" (
    SET CONFIGURATION=DebugDll
  ) ELSE IF /I "%CONFIGURATION%" == "Release" (
    SET CONFIGURATION=ReleaseDll
  ) ELSE IF /I "%CONFIGURATION%" == "ReleaseAll" (
    SET CONFIGURATION=ReleaseDll
  )
  GOTO :EOF

:fn_SetupExtraConfiguration
  IF /I "%EXTRA_SOLUTIONEXT%" == ".vcproj" (
    REM
    REM NOTE: Visual C++ 200X project, allow the build configuration to be
    REM       adjusted, if necessary.
    REM
  ) ELSE IF /I "%EXTRA_SOLUTIONEXT%" == ".vcxproj" (
    REM
    REM NOTE: Visual C++ 201X project, allow the build configuration to be
    REM       adjusted, if necessary.
    REM
  ) ELSE (
    REM
    REM NOTE: Adjusting the configuration should not be necessary.
    REM
    GOTO :EOF
  )
  IF /I "%EXTRA_CONFIGURATION%" == "Debug" (
    SET EXTRA_CONFIGURATION=DebugDll
  ) ELSE IF /I "%EXTRA_CONFIGURATION%" == "DebugAll" (
    SET EXTRA_CONFIGURATION=DebugDll
  ) ELSE IF /I "%EXTRA_CONFIGURATION%" == "Release" (
    SET EXTRA_CONFIGURATION=ReleaseDll
  ) ELSE IF /I "%EXTRA_CONFIGURATION%" == "ReleaseAll" (
    SET EXTRA_CONFIGURATION=ReleaseDll
  )
  GOTO :EOF

:fn_SetupPlatform
  IF NOT DEFINED DEFAULT_PLATFORM (
    SET DEFAULT_PLATFORM=Win32
  )
  IF DEFINED PLATFORM (
    CALL :fn_UnquoteVariable PLATFORM
  ) ELSE (
    REM
    REM NOTE: It seems that MSBuild is very picky about the precise value of
    REM       the "Platform" property.  When building a solution file, using
    REM       a value of "Any CPU" is required.  When building a C# project,
    REM       a value of "AnyCPU" is required.  When building a C++ project,
    REM       a value of either "Win32" or "x64" is required.  Other values
    REM       will most likely cause the build to fail.
    REM
    %_AECHO% No platform specified, using default...
    IF /I "%SOLUTIONEXT%" == ".csproj" (
      SET PLATFORM=AnyCPU
    ) ELSE IF /I "%SOLUTIONEXT%" == ".vcproj" (
      CALL :fn_CopyVariable DEFAULT_PLATFORM PLATFORM
    ) ELSE IF /I "%SOLUTIONEXT%" == ".vcxproj" (
      CALL :fn_CopyVariable DEFAULT_PLATFORM PLATFORM
    ) ELSE (
      SET PLATFORM=Any CPU
    )
  )
  GOTO :EOF

:fn_SetupExtraPlatform
  IF NOT DEFINED DEFAULT_PLATFORM (
    SET DEFAULT_PLATFORM=Win32
  )
  IF DEFINED EXTRA_PLATFORM (
    CALL :fn_UnquoteVariable EXTRA_PLATFORM
  ) ELSE (
    REM
    REM NOTE: It seems that MSBuild is very picky about the precise value of
    REM       the "Platform" property.  When building a solution file, using
    REM       a value of "Any CPU" is required.  When building a C# project,
    REM       a value of "AnyCPU" is required.  When building a C++ project,
    REM       a value of either "Win32" or "x64" is required.  Other values
    REM       will most likely cause the build to fail.
    REM
    %_AECHO% No extra platform specified, using default...
    IF /I "%EXTRA_SOLUTIONEXT%" == ".csproj" (
      SET EXTRA_PLATFORM=AnyCPU
    ) ELSE IF /I "%EXTRA_SOLUTIONEXT%" == ".vcproj" (
      CALL :fn_CopyVariable DEFAULT_PLATFORM EXTRA_PLATFORM
    ) ELSE IF /I "%EXTRA_SOLUTIONEXT%" == ".vcxproj" (
      CALL :fn_CopyVariable DEFAULT_PLATFORM EXTRA_PLATFORM
    ) ELSE (
      SET EXTRA_PLATFORM=Any CPU
    )
  )
  GOTO :EOF

:fn_SetupTarget
  IF NOT DEFINED TARGET (
    SET TARGET=Rebuild
  )
  IF NOT DEFINED EXTRA_TARGET (
    SET EXTRA_TARGET=Build
  )
  GOTO :EOF

:fn_SetupLogging
  IF NOT DEFINED TEMP (
    ECHO The TEMP environment variable must be set first.
    CALL :fn_SetErrorLevel
    GOTO :EOF
  )
  IF NOT DEFINED LOGASM (
    IF DEFINED USEDOTNET (
      SET LOGASM=Microsoft.Build
    ) ELSE (
      SET LOGASM=Microsoft.Build.Engine
    )
  )
  IF NOT DEFINED LOGDIR (
    SET LOGDIR=%TEMP%
  )
  IF NOT DEFINED LOGPREFIX (
    SET LOGPREFIX=EagleBuild
  )
  IF NOT DEFINED LOGSUFFIX (
    SET LOGSUFFIX=Unknown
  )
  IF DEFINED LOGGING GOTO :EOF
  IF DEFINED NOLOG GOTO :EOF
  SET LOGGING="/logger:FileLogger,%LOGASM%;Logfile=%LOGDIR%\%LOGPREFIX%%CONFIGURATION%%LOGSUFFIX%.log;Append=true;Verbosity=diagnostic"
  GOTO :EOF

:fn_ReplaceAndUnquoteVariable
  IF NOT DEFINED ComSpec GOTO :EOF
  IF NOT DEFINED %1 GOTO :EOF
  SETLOCAL
  SET __ECHO_CMD=ECHO %%%1%%
  FOR /F "delims=" %%V IN ('"%ComSpec%" /C %__ECHO_CMD%') DO (
    SET VALUE=%%V
  )
  SET VALUE=%VALUE:"=%
  REM "
  ENDLOCAL && SET %1=%VALUE%
  GOTO :EOF

:fn_UnquoteVariable
  IF NOT DEFINED %1 GOTO :EOF
  SETLOCAL
  SET __ECHO_CMD=ECHO %%%1%%
  FOR /F "delims=" %%V IN ('%__ECHO_CMD%') DO (
    SET VALUE=%%V
  )
  SET VALUE=%VALUE:"=%
  REM "
  ENDLOCAL && SET %1=%VALUE%
  GOTO :EOF

:fn_PrependToPath
  IF NOT DEFINED %1 GOTO :EOF
  SETLOCAL
  SET __ECHO_CMD=ECHO %%%1%%
  FOR /F "delims=" %%V IN ('%__ECHO_CMD%') DO (
    SET VALUE=%%V
  )
  SET VALUE=%VALUE:"=%
  REM "
  ENDLOCAL && SET PATH=%VALUE%;%PATH%
  GOTO :EOF

:fn_CopyVariable
  IF NOT DEFINED %1 GOTO :EOF
  IF "%2" == "" GOTO :EOF
  SETLOCAL
  SET __ECHO_CMD=ECHO %%%1%%
  FOR /F "delims=" %%V IN ('%__ECHO_CMD%') DO (
    SET VALUE=%%V
  )
  ENDLOCAL && SET %2=%VALUE%
  GOTO :EOF

:fn_UnsetVariable
  SETLOCAL
  SET VALUE=%1
  IF DEFINED VALUE (
    SET VALUE=
    ENDLOCAL
    SET %VALUE%=
  ) ELSE (
    ENDLOCAL
  )
  CALL :fn_ResetErrorLevel
  GOTO :EOF

:fn_ResetErrorLevel
  VERIFY > NUL
  GOTO :EOF

:fn_SetErrorLevel
  VERIFY MAYBE 2> NUL
  GOTO :EOF

:usage
  ECHO.
  ECHO Usage: %~nx0 [configuration] [...]
  GOTO errors

:errors
  CALL :fn_SetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Build failure, errors were encountered.
  GOTO end_of_file

:no_errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Build success, no errors were encountered.
  GOTO end_of_file

:end_of_file
ECHO BUILD STOPPED ON %DATE% AT %TIME% BY %USERDOMAIN%\%USERNAME%
%__ECHO% EXIT /B %ERRORLEVEL%
