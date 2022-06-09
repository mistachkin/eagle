@ECHO OFF

::
:: preFlight.bat --
::
:: Extensible Adaptable Generalized Logic Engine (Eagle)
:: Release Preparation Testing Tool
::
:: Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
::
:: See the file "license.terms" for information on usage and redistribution of
:: this file, and for a DISCLAIMER OF ALL WARRANTIES.
::
:: RCS: @(#) $Id: $
::

SETLOCAL

REM SET __ECHO=ECHO
REM SET __ECHO3=ECHO
IF NOT DEFINED _AECHO (SET _AECHO=REM)
IF NOT DEFINED _CECHO (SET _CECHO=REM)
IF NOT DEFINED _VECHO (SET _VECHO=REM)

SET PIPE=^|
IF DEFINED __ECHO SET PIPE=^^^|

%_AECHO% Running %0 %*

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

%_VECHO% Tools = '%TOOLS%'

IF NOT DEFINED TEMP (
  ECHO The TEMP environment variable must be set first.
  GOTO usage
)

%_VECHO% Temp = '%TEMP%'

IF NOT EXIST "%TEMP%" (
  ECHO The TEMP directory, "%TEMP%", does not exist.
  GOTO usage
)

CALL :fn_ResetErrorLevel

%__ECHO% mtee.exe /? > NUL 2>&1

IF ERRORLEVEL 1 (
  ECHO The "mtee.exe" tool appears to be missing.
  GOTO usage
)

IF EXIST "%CD%\mtee.exe" (
  %_AECHO% Using "mtee.exe" tool from the current directory...
) ELSE (
  %_AECHO% Using "mtee.exe" tool from the PATH...
)

%_VECHO% EagleJunctionDir = '%EagleJunctionDir%'
%_VECHO% EagleSnDir = '%EagleSnDir%'
%_VECHO% EagleSigCheckDir = '%EagleSigCheckDir%'
%_VECHO% EagleFossilDir = '%EagleFossilDir%'
%_VECHO% EagleNuGetDir = '%EagleNuGetDir%'

IF NOT DEFINED EagleJunctionDir (
  SET JUNCTION_EXE=junction.exe
  GOTO no_eagleJunctionDir
)

SET JUNCTION_EXE=%EagleJunctionDir%\junction.exe
SET JUNCTION_EXE=%JUNCTION_EXE:\\=\%

:no_eagleJunctionDir

IF NOT DEFINED EagleSnDir (
  SET SN_EXE=sn.exe
  GOTO no_eagleSnDir
)

SET SN_EXE=%EagleSnDir%\sn.exe
SET SN_EXE=%SN_EXE:\\=\%

:no_eagleSnDir

IF NOT DEFINED EagleSigCheckDir (
  SET SIGCHECK_EXE=SigCheck.exe
  GOTO no_eagleSigCheckDir
)

SET SIGCHECK_EXE=%EagleSigCheckDir%\SigCheck.exe
SET SIGCHECK_EXE=%SIGCHECK_EXE:\\=\%

:no_eagleSigCheckDir

IF NOT DEFINED EagleFossilDir (
  SET FOSSIL_EXE=fossil.exe
  GOTO no_eagleFossilDir
)

SET FOSSIL_EXE=%EagleFossilDir%\fossil.exe
SET FOSSIL_EXE=%FOSSIL_EXE:\\=\%

:no_eagleFossilDir

IF NOT DEFINED EagleNuGetDir (
  SET NUGET_EXE=NuGet4.exe
  GOTO no_eagleNuGetDir
)

SET NUGET_EXE=%EagleNuGetDir%\NuGet4.exe
SET NUGET_EXE=%NUGET_EXE:\\=\%

:no_eagleNuGetDir

%_VECHO% JunctionExe = '%JUNCTION_EXE%'
%_VECHO% SnExe = '%SN_EXE%'
%_VECHO% SigCheckExe = '%SIGCHECK_EXE%'
%_VECHO% FossilExe = '%FOSSIL_EXE%'
%_VECHO% NuGetExe = '%NUGET_EXE%'

IF DEFINED EagleJunctionDir (
  IF EXIST "%JUNCTION_EXE%" (
    %_AECHO% The file "%JUNCTION_EXE%" does exist.
  ) ELSE (
    ECHO The file "%JUNCTION_EXE%" does not exist.
    GOTO usage
  )
) ELSE (
  CALL :fn_VerifyFileAlongPath %JUNCTION_EXE%

  IF ERRORLEVEL 1 (
    GOTO usage
  )
)

IF DEFINED EagleSnDir (
  IF EXIST "%SN_EXE%" (
    %_AECHO% The file "%SN_EXE%" does exist.
  ) ELSE (
    ECHO The file "%SN_EXE%" does not exist.
    GOTO usage
  )
) ELSE (
  CALL :fn_VerifyFileAlongPath %SN_EXE%

  IF ERRORLEVEL 1 (
    GOTO usage
  )
)

IF DEFINED EagleSigCheckDir (
  IF EXIST "%SIGCHECK_EXE%" (
    %_AECHO% The file "%SIGCHECK_EXE%" does exist.
  ) ELSE (
    ECHO The file "%SIGCHECK_EXE%" does not exist.
    GOTO usage
  )
) ELSE (
  CALL :fn_VerifyFileAlongPath %SIGCHECK_EXE%

  IF ERRORLEVEL 1 (
    GOTO usage
  )
)

IF DEFINED EagleFossilDir (
  IF EXIST "%FOSSIL_EXE%" (
    %_AECHO% The file "%FOSSIL_EXE%" does exist.
  ) ELSE (
    ECHO The file "%FOSSIL_EXE%" does not exist.
    GOTO usage
  )
) ELSE (
  CALL :fn_VerifyFileAlongPath %FOSSIL_EXE%

  IF ERRORLEVEL 1 (
    GOTO usage
  )
)

IF DEFINED EagleNuGetDir (
  IF EXIST "%NUGET_EXE%" (
    %_AECHO% The file "%NUGET_EXE%" does exist.
  ) ELSE (
    ECHO The file "%NUGET_EXE%" does not exist.
    GOTO usage
  )
) ELSE (
  CALL :fn_VerifyFileAlongPath %NUGET_EXE%

  IF ERRORLEVEL 1 (
    GOTO usage
  )
)

IF DEFINED NOLKG GOTO skip_lastKnownGood
IF NOT DEFINED LKG GOTO skip_lastKnownGood

SET SRCLKGDIR=%LKG%\Eagle\bin
SET SRCLKGDIR=%SRCLKGDIR:\\=\%

%_VECHO% SrcLkgDir = '%SRCLKGDIR%'

IF NOT EXIST "%SRCLKGDIR%\EagleShell.exe" (
  ECHO The file "%SRCLKGDIR%\EagleShell.exe" does not exist.
  GOTO usage
)

%__ECHO% "%SRCLKGDIR%\EagleShell.exe" /? > NUL 2>&1

IF ERRORLEVEL 1 (
  ECHO The "%SRCLKGDIR%\EagleShell.exe" tool appears to be missing.
  GOTO usage
)

%_AECHO% Using LKG "EagleShell.exe" tool from "%SRCLKGDIR%"...

REM
REM NOTE: Skip checking for the "EagleShell.exe" tool along the PATH because we
REM       just found it in the LKG location.
REM
GOTO skip_eagleShell

:skip_lastKnownGood

%__ECHO% EagleShell.exe /? > NUL 2>&1

IF ERRORLEVEL 1 (
  ECHO The "EagleShell.exe" tool appears to be missing.
  GOTO usage
)

IF EXIST "%CD%\EagleShell.exe" (
  %_AECHO% Using "EagleShell.exe" tool from the current directory...
) ELSE (
  %_AECHO% Using "EagleShell.exe" tool from the PATH...
)

:skip_eagleShell

REM ****************************************************************************
REM
REM NOTE: Attempt to detect if Inno Setup is installed.  If it is not, disable
REM       all release processing steps that require it.
REM
REM ****************************************************************************

IF DEFINED NOBAKE GOTO skip_innoSetup

IF EXIST "%ProgramFiles%\Inno Setup 5" (
  %_AECHO% Found Inno Setup in Program Files directory.
  GOTO skip_innoSetup
)

IF EXIST "%ProgramFiles(x86)%\Inno Setup 5" (
  %_AECHO% Found Inno Setup in x86-specific Program Files directory.
  GOTO skip_innoSetup
)

SET NOBAKE=1
%_AECHO% Skipping all setup creation steps...

:skip_innoSetup

REM ****************************************************************************
REM
REM NOTE: Attempt to detect if WinRAR is installed.  If it is not, disable all
REM       release processing steps that require it.
REM
REM ****************************************************************************

IF DEFINED NOARCHIVE (
  IF DEFINED NORELEASE (
    GOTO skip_winRar
  )
)

IF EXIST "%ProgramFiles%\WinRAR" (
  %_AECHO% Found WinRAR in Program Files directory.
  GOTO skip_winRar
)

IF EXIST "%ProgramFiles(x86)%\WinRAR" (
  %_AECHO% Found WinRAR in x86-specific Program Files directory.
  GOTO skip_winRar
)

IF NOT DEFINED NOARCHIVE (
  SET NOARCHIVE=1
  %_AECHO% Skipping all source code archiving steps...
)

IF NOT DEFINED NORELEASE (
  SET NORELEASE=1
  %_AECHO% Skipping all binary archiving steps...
)

:skip_winRar

IF NOT DEFINED NOARCHIVE (
  IF EXIST "%TOOLS%\data\EagleSource.sfx" (
    %_AECHO% Found source code archive base SFX...
  ) ELSE (
    SET NOARCHIVE=1
    %_AECHO% Skipping all source code archiving steps...
  )
)

IF NOT DEFINED NORELEASE (
  IF EXIST "%TOOLS%\data\EagleBinary.sfx" (
    %_AECHO% Found binary archive base SFX...
  ) ELSE (
    SET NORELEASE=1
    %_AECHO% Skipping all binary archiving steps...
  )
)

REM ****************************************************************************
REM
REM NOTE: Attempt to detect if an Authenticode certificate and the associated
REM       private key are available.  If not, disable all release processing
REM       steps that require them.
REM
REM ****************************************************************************

IF NOT DEFINED NOSIGN (
  IF NOT DEFINED PVK_FILE (
    SET NOSIGN=1
    %_AECHO% The PVK_FILE environment variable must be set first.
    %_AECHO% Skipping all steps that require signing...
    GOTO skip_sign
  )

  IF NOT EXIST "%PVK_FILE%" (
    SET NOSIGN=1
    %_AECHO% The PVK_FILE file, "%PVK_FILE%", does not exist.
    %_AECHO% Skipping all steps that require signing...
    GOTO skip_sign
  )

  IF NOT DEFINED SPC_FILE (
    SET NOSIGN=1
    %_AECHO% The SPC_FILE environment variable must be set first.
    %_AECHO% Skipping all steps that require signing...
    GOTO skip_sign
  )

  IF NOT EXIST "%SPC_FILE%" (
    SET NOSIGN=1
    %_AECHO% The SPC_FILE file, "%SPC_FILE%", does not exist.
    %_AECHO% Skipping all steps that require signing...
    GOTO skip_sign
  )

  IF NOT DEFINED SIGN_URL (
    SET NOSIGN=1
    %_AECHO% The SIGN_URL environment variable must be set first.
    %_AECHO% Skipping all steps that require signing...
    GOTO skip_sign
  )

  IF NOT DEFINED PFX_FILE (
    SET NOSIGN=1
    ECHO The PFX_FILE environment variable must be set first.
    %_AECHO% Skipping all steps that require signing...
    GOTO skip_sign
  )

  IF NOT EXIST "%PFX_FILE%" (
    SET NOSIGN=1
    ECHO The PFX_FILE file, "%PFX_FILE%", does not exist.
    %_AECHO% Skipping all steps that require signing...
    GOTO skip_sign
  )

  IF NOT DEFINED PFX_PASSWORD (
    SET NOSIGN=1
    ECHO The PFX_PASSWORD environment variable must be set first.
    %_AECHO% Skipping all steps that require signing...
    GOTO skip_sign
  )

  IF NOT DEFINED SUBJECT_NAME (
    SET NOSIGN=1
    ECHO The SUBJECT_NAME environment variable must be set first.
    %_AECHO% Skipping all steps that require signing...
    GOTO skip_sign
  )

  IF NOT DEFINED TIMESTAMP_URL (
    SET NOSIGN=1
    ECHO The TIMESTAMP_URL environment variable must be set first.
    %_AECHO% Skipping all steps that require signing...
    GOTO skip_sign
  )
)

:skip_sign

REM ****************************************************************************
REM
REM NOTE: The following environment variables control the directories where the
REM       external utilities required by the Eagle release process reside.  If
REM       one of them is not set, it is assumed the associated utility resides
REM       along the PATH.
REM
REM ****************************************************************************

REM SET EagleJunctionDir=C:\some\directory
REM SET EagleSnDir=C:\some\directory
REM SET EagleSigCheckDir=C:\some\directory
REM SET EagleFossilDir=C:\some\directory
REM SET EagleNuGetDir=C:\some\directory

REM ****************************************************************************
REM
REM NOTE: The following environment variables enable building an extra solution
REM       file that refers to the freshly built core library assembly.
REM
REM ****************************************************************************

REM SET EXTRA_SOLUTION=%EAGLE%\Stub\EagleEye%YEAR%.sln
REM SET EagleOutputPathReference=true

REM ****************************************************************************
REM
REM NOTE: The following environment variables control settings used during the
REM       Eagle release process.  This tool uses these variables to help quality
REM       check the release process itself.  It is primarily intended to be used
REM       by members of the Eagle Development Team; however, anybody can use it
REM       if they so desire.  If you are a member of the Eagle Development Team
REM       and believe that different settings may be necessary for your specific
REM       environment, please coordinate the necessary changes with the team.
REM
REM ****************************************************************************

REM SET COMMERCIAL=1
REM SET COREONLY=1
REM SET ENTERPRISE=1
REM SET MINBUILD=1
REM SET MINTEST=1
REM SET NETCORE20ONLY=1
REM SET NETCORE20YEAR=NetStandard20
REM SET NETCORE30YEAR=NetStandard21
REM SET NETFX20ONLY=1
REM SET NETFX20YEAR=2005
REM SET NETFX35ONLY=1
REM SET NETFX35YEAR=2008
REM SET NETFX40ONLY=1
REM SET NETFX40YEAR=2010
REM SET NETFX45ONLY=1
REM SET NETFX45YEAR=2012
REM SET NETFX451ONLY=1
REM SET NETFX451YEAR=2013
REM SET NETFX452ONLY=1
REM SET NETFX452YEAR=2013
REM SET NETFX46ONLY=1
REM SET NETFX46YEAR=2015
REM SET NETFX461ONLY=1
REM SET NETFX461YEAR=2015
REM SET NETFX462ONLY=1
REM SET NETFX462YEAR=2015
REM SET NETFX47ONLY=1
REM SET NETFX47YEAR=2017
REM SET NETFX471ONLY=1
REM SET NETFX471YEAR=2017
REM SET NETFX472ONLY=1
REM SET NETFX472YEAR=2017
REM SET NETFX48ONLY=1
REM SET NETFX48YEAR=2019
REM SET NOADMINISTRATOR=1
REM SET NOARCHIVE=1
REM SET NOATTRIBUTE=1
REM SET NOBAKE=1
REM SET NOBINARY=1
REM SET NOBUILD=1
REM SET NOBUILDINFO=1
REM SET NOBUILDLOGS=1
REM SET NOCLEAN=1
REM SET NOCORE=1
REM SET NOCOVERITY=1
REM SET NOCSHARPFILES=1
REM SET NODATETIME=1
REM SET NOEMBED=1
REM SET NOEXTRA=1
REM SET NOFRAMEWORK=1
REM SET NOFRAMEWORK64=1
REM SET NOFRAMEWORKDIR=1
REM SET NOHASH=1
REM SET NOLIB=1
REM SET NOLKG=1
REM SET NOLOGS=1
REM SET NOMSBUILD64=1
REM SET NOMSBUILDDIR=1
REM SET NOMSBUILDLIB=1
REM SET NONATIVE=1
REM SET NONUGET=1
REM SET NONUGETPACK=1
REM SET NONUGETPUSH=1
REM SET NONUGETSIGN=1
REM SET NONUGETTAG=1
REM SET NOPACKAGE=1
REM SET NOPATCHLEVEL=1
REM SET NORELEASE=1
REM SET NORUNTIME=1
REM SET NOSECURITY=1
REM SET NOSHELL=1
REM SET NOSHELL32=1
REM SET NOSIGCHECK=1
REM SET NOSIGN=1
REM SET NOSIGNTOOL1=1
REM SET NOSIGNTOOL2=1
REM SET NOSOURCE=1
REM SET NOSOURCEID=1
REM SET NOSOURCEONLY=1
REM SET NOSTRONGNAME=1
REM SET NOSYMBOLS=1
REM SET NOTAG=1
REM SET NOTCL=1
REM SET NOTEST=1
REM SET NOTESTALL=1
REM SET NOTESTLOGS=1
REM SET NOTESTLIB=1
REM SET NOTESTS=1
REM SET NOTOOL=1
REM SET NOTRACELOGS=1
REM SET NOUPDATETEST=1
REM SET NOUSEPACKAGERESTORE=1
REM SET NOVENDOR=1
REM SET NOVERIFY=1
REM SET NOVERIFYHASH=1
REM SET OFFICIAL=1
REM SET PACKAGE_PATCHLEVEL=1.0.X.X
REM SET PATCHLEVEL=1.0.X.X
REM SET SHELLONLY=1
REM SET SIGN_WITH_GPG=1

REM ****************************************************************************
REM
REM NOTE: The following environment variables allow the release process to skip
REM       to the specified build configuraiton within the predefined sequence.
REM
REM ****************************************************************************

REM SET BUILD_RELEASE=1
REM SET TAG_RELEASE=1
REM SET BUILD_NUGET=1
REM SET VERIFY_RELEASE=1
REM SET PUSH_NUGET=1
REM SET BUILD_SOURCE_ONLY=1
REM SET BUILD_NETSTANDARD20=1
REM SET BUILD_NETSTANDARD21=1
REM SET BUILD_NETFX20=1
REM SET BUILD_NETFX35=1
REM SET BUILD_NETFX40=1
REM SET BUILD_NETFX45=1
REM SET BUILD_NETFX451=1
REM SET BUILD_NETFX452=1
REM SET BUILD_NETFX46=1
REM SET BUILD_NETFX461=1
REM SET BUILD_NETFX462=1
REM SET BUILD_NETFX47=1
REM SET BUILD_NETFX471=1
REM SET BUILD_NETFX472=1
REM SET BUILD_NETFX48=1
REM SET BUILD_BARE=1
REM SET BUILD_LEAN=1
REM SET BUILD_DATABASE=1
REM SET BUILD_UNIX=1
REM SET BUILD_DEVELOPMENT=1

REM ****************************************************************************
REM
REM NOTE: The following environment variables control the version of MSVCRT to
REM       bundle with native binaries built targeting the specified version of
REM       the .NET Framework.
REM
REM ****************************************************************************

SET VCRUNTIME_NETFX20=2005_SP1_MFC
SET VCRUNTIME_NETFX35=2008_SP1_MFC
SET VCRUNTIME_NETFX40=2010_SP1_MFC
SET VCRUNTIME_NETFX45=2012_VSU4
SET VCRUNTIMEARM_NETFX45=2012_VSU4
SET VCRUNTIME_NETFX451=2013_VSU2
SET VCRUNTIMEARM_NETFX451=2013_VSU2
SET VCRUNTIME_NETFX452=2013_VSU2
SET VCRUNTIMEARM_NETFX452=2013_VSU2
SET VCRUNTIME_NETFX46=2015_VSU3
SET VCRUNTIMEARM_NETFX46=2015_VSU3
SET VCRUNTIME_NETFX461=2015_VSU3
SET VCRUNTIMEARM_NETFX461=2015_VSU3
SET VCRUNTIME_NETFX462=2015_VSU3
SET VCRUNTIMEARM_NETFX462=2015_VSU3
SET VCRUNTIME_NETFX47=2017_VCU8
SET VCRUNTIMEARM_NETFX47=2017_VCU5
SET VCRUNTIME_NETFX471=2017_VCU8
SET VCRUNTIMEARM_NETFX471=2017_VCU5
SET VCRUNTIME_NETFX472=2017_VCU8
SET VCRUNTIMEARM_NETFX472=2017_VCU5
SET VCRUNTIME_NETFX48=2019_VCU1
SET VCRUNTIMEARM_NETFX48=2019_VCU1

REM ****************************************************************************
REM
REM NOTE: The following environment variables control the build configurations
REM       that will be skipped during the release process.  By default, most of
REM       them are disabled to speed up the release testing process.
REM
REM ****************************************************************************

SET NONETSTANDARD20=1
SET NONETSTANDARD21=1
SET NONETFX20=1
SET NONETFX35=1
SET NONETFX40=1
SET NONETFX45=1
SET NONETFX451=1
SET NONETFX452=1
SET NONETFX46=1
SET NONETFX461=1
SET NONETFX462=1
SET NONETFX47=1
SET NONETFX471=1
SET NONETFX472=1
SET NONETFX48=1
SET NOBARE=1
SET NOLEAN=1
SET NODATABASE=1
SET NOUNIX=1
SET NODEVELOPMENT=1

REM ****************************************************************************
REM
REM NOTE: The following environment variables control the build configurations
REM       that will be skipped during the release testing process.  By default,
REM       most of them are disabled to speed up the release testing process.
REM
REM ****************************************************************************

SET NETSTANDARD20_NOTEST=1
SET NETSTANDARD21_NOTEST=1
SET NETFX20_NOTEST=1
SET NETFX35_NOTEST=1
SET NETFX40_NOTEST=1
SET NETFX45_NOTEST=1
SET NETFX451_NOTEST=1
SET NETFX452_NOTEST=1
SET NETFX46_NOTEST=1
SET NETFX461_NOTEST=1
SET NETFX462_NOTEST=1
SET NETFX47_NOTEST=1
SET NETFX471_NOTEST=1
SET NETFX472_NOTEST=1
SET NETFX48_NOTEST=1
SET BARE_NOTEST=1
SET LEAN_NOTEST=1
SET DATABASE_NOTEST=1
SET UNIX_NOTEST=1
SET DEVELOPMENT_NOTEST=1

REM ****************************************************************************
REM
REM NOTE: Now we should be ready to run the release process.
REM
REM ****************************************************************************

%__ECHO3% CALL "%TOOLS%\flight.bat" %* %PIPE% mtee.exe "%TEMP%\EagleFlight.log"

IF ERRORLEVEL 1 (
  ECHO Release testing failed.
  GOTO errors
)

GOTO no_errors

:fn_VerifyFileAlongPath
  SET VALUE=%1
  IF DEFINED VALUE (
    FOR %%T IN (%VALUE%) DO (
      SET %%T_PATH=%%~dp$PATH:T
    )
    IF DEFINED %VALUE%_PATH (
      %_AECHO% The file "%VALUE%" does exist along the PATH.
    ) ELSE (
      ECHO The file "%VALUE%" does not exist along the PATH.
      CALL :fn_SetErrorLevel
    )
  )
  GOTO :EOF

:fn_ResetErrorLevel
  VERIFY > NUL
  GOTO :EOF

:fn_SetErrorLevel
  VERIFY MAYBE 2> NUL
  GOTO :EOF

:usage
  ECHO.
  ECHO Usage: %~nx0 [...]
  ECHO.
  ECHO The TEMP environment variable must be set to an appropriate directory.
  ECHO.
  ECHO The "mtee.exe" tool is required to exist in the current directory or somewhere
  ECHO along your PATH.  It can be downloaded for free from:
  ECHO.
  ECHO                          http://www.commandline.co.uk/
  ECHO.
  ECHO The "junction.exe" tool is required to be present along the PATH or in the
  ECHO directory specified by the EagleJunctionDir environment variable.  It can be
  ECHO downloaded for free from:
  ECHO.
  ECHO                          https://www.sysinternals.com/
  ECHO.
  ECHO The "sn.exe" tool is required to be present along the PATH or in the directory
  ECHO specified by the EagleSnDir environment variable.  It is distributed with the
  ECHO .NET Framework SDK.
  ECHO.
  ECHO The "SigCheck.exe" tool is required to be present along the PATH or in the
  ECHO directory specified by the EagleSigCheckDir environment variable.  It can be
  ECHO downloaded for free from:
  ECHO.
  ECHO                          https://www.sysinternals.com/
  ECHO.
  ECHO The "NuGet4.exe" tool is required to be present along the PATH or in the
  ECHO directory specified by the EagleNuGetDir environment variable.  It can be
  ECHO downloaded for free from:
  ECHO.
  ECHO                          https://www.nuget.org/
  ECHO.
  ECHO The latest Eagle binaries ^(including the "EagleShell.exe" tool^) are required to
  ECHO exist in the "%%LKG%%\Eagle\bin" directory or somewhere along your PATH.  They
  ECHO can be downloaded for free from:
  ECHO.
  ECHO                          https://eagle.to/
  ECHO.
  GOTO errors

:errors
  CALL :fn_SetErrorLevel
  ENDLOCAL
  GOTO end_of_file

:no_errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  GOTO end_of_file

:end_of_file
%__ECHO% EXIT /B %ERRORLEVEL%
