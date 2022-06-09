@ECHO OFF

::
:: bake.bat --
::
:: Extensible Adaptable Generalized Logic Engine (Eagle)
:: Setup Preparation & Baking Tool
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
IF NOT DEFINED _AECHO (SET _AECHO=REM)
IF NOT DEFINED _CECHO (SET _CECHO=REM)
IF NOT DEFINED _VECHO (SET _VECHO=REM)

SET PIPE=^|
IF DEFINED __ECHO SET PIPE=^^^|

SET OUTPUT=^>
IF DEFINED __ECHO SET OUTPUT=^^^>

%_AECHO% Running %0 %*

IF NOT DEFINED DEFAULT_PLATFORM (
  SET DEFAULT_PLATFORM=Win32
)

%_VECHO% DefaultPlatform = '%DEFAULT_PLATFORM%'

SET PLATFORM=%1

IF DEFINED PLATFORM (
  CALL :fn_UnquoteVariable PLATFORM
) ELSE (
  %_AECHO% No platform specified, using default...
  SET PLATFORM=%DEFAULT_PLATFORM%
)

%_VECHO% Platform = '%PLATFORM%'

SET CONFIGURATION=%2

IF DEFINED CONFIGURATION (
  CALL :fn_UnquoteVariable CONFIGURATION
) ELSE (
  %_AECHO% No configuration specified, using default...
  SET CONFIGURATION=ReleaseDll
)

%_VECHO% Configuration = '%CONFIGURATION%'

SET DUMMY2=%3

IF DEFINED DUMMY2 (
  GOTO usage
)

IF NOT DEFINED PROCESSOR (
  %_AECHO% No processor specified, using default...
  SET PROCESSOR=x86
)

%_VECHO% Processor = '%PROCESSOR%'

IF NOT DEFINED VCRUNTIME (
  %_AECHO% No VC runtime specified, using default...
  SET VCRUNTIME=2010_SP1_MFC
)

%_VECHO% VcRuntime = '%VCRUNTIME%'

IF NOT DEFINED VCRUNTIMEX86 (
  %_AECHO% No VC runtime for x86 specified, using default...
  SET VCRUNTIMEX86=%VCRUNTIME%
)

%_VECHO% VcRuntimeX86 = '%VCRUNTIMEX86%'

IF NOT DEFINED VCRUNTIMEX64 (
  %_AECHO% No VC runtime for x64 specified, using default...
  SET VCRUNTIMEX64=%VCRUNTIME%
)

%_VECHO% VcRuntimeX64 = '%VCRUNTIMEX64%'

IF NOT DEFINED VCRUNTIMEARM (
  %_AECHO% No VC runtime for ARM specified, using default...
  SET VCRUNTIMEARM=%VCRUNTIME%
)

%_VECHO% VcRuntimeArm = '%VCRUNTIMEARM%'

IF NOT DEFINED NEEDVCRUNTIMES (
  SET NEEDVCRUNTIMES=True
)

%_VECHO% NeedVcRuntimes = '%NEEDVCRUNTIMES%'

IF NOT DEFINED NEEDACTIVETCL (
  SET NEEDACTIVETCL=True
)

%_VECHO% NeedActiveTcl = '%NEEDACTIVETCL%'

IF NOT DEFINED NEEDEAGLE (
  SET NEEDEAGLE=True
)

%_VECHO% NeedEagle = '%NEEDEAGLE%'

SET APPID={{23ECDA23-B62D-454C-B45E-ABCB5A00E64A}

SET EAGLEBINDIR=%~dp0\..\..\..\bin\%CONFIGURATION%\bin

REM
REM HACK: We know the "All" and "Dll" suffixes on the configuration will not
REM       work for the purpose of finding the Eagle binaries; therefore, we
REM       simply remove them.  This could break if the configuration names
REM       ever change.
REM
SET EAGLEBINDIR=%EAGLEBINDIR:All=%
SET EAGLEBINDIR=%EAGLEBINDIR:Dll=%

SET SRCBINDIR=%~dp0\..\..\..\bin\%PLATFORM%\%CONFIGURATION%
SET SRCLIBDIR=%~dp0\..\lib
SET SRCTESTDIR=%~dp0\..\Tests
SET SRCISSFILE=%~dp0\..\..\..\Setup\Garuda.iss

REM
REM HACK: Required to make Visual Studio happy.
REM
SET EAGLEBINDIR=%EAGLEBINDIR:\\=\%
SET SRCBINDIR=%SRCBINDIR:\\=\%
SET SRCLIBDIR=%SRCLIBDIR:\\=\%
SET SRCTESTDIR=%SRCTESTDIR:\\=\%
SET SRCISSFILE=%SRCISSFILE:\\=\%

%_VECHO% AppId = '%APPID%'
%_VECHO% EagleBinDir = '%EAGLEBINDIR%'
%_VECHO% SrcBinDir = '%SRCBINDIR%'
%_VECHO% SrcLibDir = '%SRCLIBDIR%'
%_VECHO% SrcTestDir = '%SRCTESTDIR%'
%_VECHO% SrcIssFile = '%SRCISSFILE%'

SET LIBRARY_TOOLS=%~dp0\..\..\..\Library\Tools
SET LIBRARY_TOOLS=%LIBRARY_TOOLS:\\=\%

%_VECHO% LibraryTools = '%LIBRARY_TOOLS%'
%_VECHO% NoSign = '%NOSIGN%'

SET PATH=%EAGLEBINDIR%;%LIBRARY_TOOLS%;%PATH%

IF "%PROCESSOR_ARCHITECTURE%" == "x86" GOTO set_path_x86

SET PATH=%ProgramFiles(x86)%\Inno Setup 5;%PATH%
GOTO set_path_done

:set_path_x86

SET PATH=%ProgramFiles%\Inno Setup 5;%PATH%

:set_path_done

%_VECHO% Path = '%PATH%'
%_VECHO% Suffix = '%SUFFIX%'

IF NOT DEFINED ISNETFX2 (
  SET ISNETFX2=False
)

IF NOT DEFINED ISNETFX4 (
  SET ISNETFX4=False
)

%_VECHO% IsNetFx2 = '%ISNETFX2%'
%_VECHO% IsNetFx4 = '%ISNETFX4%'

IF DEFINED PACKAGE_PATCHLEVEL GOTO skip_patchLevel1

SET GET_PATCHLEVEL_CMD=EagleShell.exe -eval "puts stdout [file version {%SRCBINDIR%\Garuda.dll}]"

%_VECHO% GetPatchLevelCmd = '%GET_PATCHLEVEL_CMD%'

IF DEFINED __ECHO (
  %__ECHO% %GET_PATCHLEVEL_CMD%
  SET PACKAGE_PATCHLEVEL=1.0.X.X
) ELSE (
  FOR /F %%T IN ('%GET_PATCHLEVEL_CMD%') DO (SET PACKAGE_PATCHLEVEL=%%T)
)

:skip_patchLevel1

IF NOT DEFINED PACKAGE_PATCHLEVEL (
  ECHO The PACKAGE_PATCHLEVEL environment variable could not be set.
  GOTO errors
)

%_VECHO% PackagePatchLevel = '%PACKAGE_PATCHLEVEL%'

SET RELEASES=%~dp0\..\..\..\Releases
SET RELEASES=%RELEASES:\\=\%

%_VECHO% Releases = '%RELEASES%'

IF NOT EXIST "%RELEASES%" (
  %__ECHO% MKDIR "%RELEASES%"

  IF ERRORLEVEL 1 (
    ECHO Could not create directory "%RELEASES%".
    GOTO errors
  )
)

REM
REM NOTE: Output the patch level we just fetched to a file to be
REM       picked up by the rest of the build process.
REM
%__ECHO% ECHO %PACKAGE_PATCHLEVEL% %OUTPUT% "%RELEASES%\Garuda_patchLevel.txt"

SET SETUP=%~dp0\..\..\..\Releases\%PLATFORM%_%CONFIGURATION%\GarudaSetup%SUFFIX%%PACKAGE_PATCHLEVEL%.exe
SET SETUP=%SETUP:\\=\%

SET UNINSTALL=%~dp0\..\..\..\Setup\Output\Garuda\uninst-*.e32
SET UNINSTALL=%UNINSTALL:\\=\%

%_VECHO% Setup = '%SETUP%'
%_VECHO% Uninstall = '%UNINSTALL%'

SET GET_MAJORMINOR_CMD=EagleShell.exe -eval "puts stdout [join [lrange [split %PACKAGE_PATCHLEVEL% .] 0 1] .]"

%_VECHO% GetMajorMinorCmd = '%GET_MAJORMINOR_CMD%'

IF NOT DEFINED MAJORMINOR (
  IF DEFINED __ECHO (
    %__ECHO% %GET_MAJORMINOR_CMD%
    SET MAJORMINOR=1.0
  ) ELSE (
    FOR /F %%T IN ('%GET_MAJORMINOR_CMD%') DO (SET MAJORMINOR=%%T)
  )
)

IF NOT DEFINED MAJORMINOR (
  ECHO The MAJORMINOR environment variable could not be set.
  GOTO usage
)

%_VECHO% MajorMinor = '%MAJORMINOR%'

CALL :fn_ResetErrorLevel

IF NOT DEFINED SKIP_SIGN_UNINSTALLER (
  SET SIGNED=no

  IF NOT DEFINED NOSIGN (
    FOR %%F IN (%UNINSTALL%) DO (
      %__ECHO3% CALL "%LIBRARY_TOOLS%\signFile.bat" "%%F" "Garuda Uninstaller"
      IF ERRORLEVEL 1 GOTO errors

      IF NOT DEFINED NOSIGCHECK (
        %_AECHO% Checking signatures on file "%%F"...
        %__ECHO% SigCheck.exe "%%F" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

        IF ERRORLEVEL 1 (
          ECHO Checking signatures on "%%F" failed.
          GOTO errors
        )
      )

      SET SIGNED=yes
    )
  )
) ELSE (
  REM
  REM NOTE: Assume the Inno Setup uninstaller has been pre-signed.
  REM
  SET SIGNED=yes
)

%_VECHO% Signed = '%SIGNED%'

IF NOT DEFINED URL (
  IF DEFINED SIGN_URL (
    SET URL=%SIGN_URL%
  ) ELSE (
    SET URL=https://eagle.to/
  )
)

%_VECHO% Url = '%URL%'

%_CECHO% ISCC.exe "%SRCISSFILE%" "/dAppId=%APPID%" "/dPlatform=%PLATFORM%" "/dConfiguration=%CONFIGURATION%" "/dProcessor=%PROCESSOR%" "/dVcRuntimeX86=%VCRUNTIMEX86%" "/dVcRuntimeX64=%VCRUNTIMEX64%" "/dVcRuntimeArm=%VCRUNTIMEARM%" "/dSuffix=%SUFFIX%" "/dSigned=%SIGNED%" "/dSrcBinDir=%SRCBINDIR%" "/dSrcLibDir=%SRCLIBDIR%" "/dSrcTestDir=%SRCTESTDIR%" "/dIsNetFx2=%ISNETFX2%" "/dIsNetFx4=%ISNETFX4%" "/dNeedActiveTcl=%NEEDACTIVETCL%" "/dNeedEagle=%NEEDEAGLE%" "/dNeedVcRuntimes=%NEEDVCRUNTIMES%" "/dAppFullVersion=%PACKAGE_PATCHLEVEL%" "/dAppMajorMinorVersion=%MAJORMINOR%" "/dAppURL=%URL%"
%__ECHO% ISCC.exe "%SRCISSFILE%" "/dAppId=%APPID%" "/dPlatform=%PLATFORM%" "/dConfiguration=%CONFIGURATION%" "/dProcessor=%PROCESSOR%" "/dVcRuntimeX86=%VCRUNTIMEX86%" "/dVcRuntimeX64=%VCRUNTIMEX64%" "/dVcRuntimeArm=%VCRUNTIMEARM%" "/dSuffix=%SUFFIX%" "/dSigned=%SIGNED%" "/dSrcBinDir=%SRCBINDIR%" "/dSrcLibDir=%SRCLIBDIR%" "/dSrcTestDir=%SRCTESTDIR%" "/dIsNetFx2=%ISNETFX2%" "/dIsNetFx4=%ISNETFX4%" "/dNeedActiveTcl=%NEEDACTIVETCL%" "/dNeedEagle=%NEEDEAGLE%" "/dNeedVcRuntimes=%NEEDVCRUNTIMES%" "/dAppFullVersion=%PACKAGE_PATCHLEVEL%" "/dAppMajorMinorVersion=%MAJORMINOR%" "/dAppURL=%URL%"

IF %ERRORLEVEL% NEQ 0 (
  ECHO Failed to compile setup.
  GOTO errors
)

IF NOT DEFINED NOSIGN (
  %__ECHO3% CALL "%LIBRARY_TOOLS%\signFile.bat" "%SETUP%" "Garuda Setup"
  IF ERRORLEVEL 1 GOTO errors

  IF NOT DEFINED NOSIGCHECK (
    %_AECHO% Checking signatures on file "%SETUP%"...
    %__ECHO% SigCheck.exe "%SETUP%" %PIPE% FINDSTR "/G:%LIBRARY_TOOLS%\data\SigCheck.txt"

    IF ERRORLEVEL 1 (
      ECHO Checking signatures on "%SETUP%" failed.
      GOTO errors
    )
  )
)

GOTO no_errors

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

:fn_ResetErrorLevel
  VERIFY > NUL
  GOTO :EOF

:fn_SetErrorLevel
  VERIFY MAYBE 2> NUL
  GOTO :EOF

:usage
  ECHO.
  ECHO Usage: %~nx0 [platform] [configuration]
  GOTO errors

:errors
  CALL :fn_SetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Failure, errors were encountered.
  GOTO end_of_file

:no_errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Success, no errors were encountered.
  GOTO end_of_file

:end_of_file
%__ECHO% EXIT /B %ERRORLEVEL%
