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
REM SET __ECHO2=ECHO
REM SET __ECHO3=ECHO
IF NOT DEFINED _AECHO (SET _AECHO=REM)
IF NOT DEFINED _CECHO (SET _CECHO=REM)
IF NOT DEFINED _CECHO2 (SET _CECHO2=REM)
IF NOT DEFINED _CECHO3 (SET _CECHO3=REM)
IF NOT DEFINED _VECHO (SET _VECHO=REM)

CALL :fn_UnsetVariable BREAK

SET PIPE=^|
SET _CPIPE=^^^|
IF DEFINED __ECHO SET PIPE=^^^|

SET OUTPUT=^>
IF DEFINED __ECHO SET OUTPUT=^^^>

%_AECHO% Running %0 %*

SET CONFIGURATION=%1

IF DEFINED CONFIGURATION (
  CALL :fn_UnquoteVariable CONFIGURATION
) ELSE (
  %_AECHO% No configuration specified, using default...
  SET CONFIGURATION=Release
)

%_VECHO% Configuration = '%CONFIGURATION%'

SET DUMMY2=%2

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
  SET VCRUNTIME=2005_SP1_MFC
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
  IF NOT DEFINED NONATIVE (
    SET NEEDVCRUNTIMES=True
  ) ELSE (
    SET NEEDVCRUNTIMES=False
  )
)

%_VECHO% NeedVcRuntimes = '%NEEDVCRUNTIMES%'

IF NOT DEFINED NEEDACTIVETCL (
  SET NEEDACTIVETCL=False
)

%_VECHO% NeedActiveTcl = '%NEEDACTIVETCL%'

IF NOT DEFINED NEEDEAGLE (
  SET NEEDEAGLE=False
)

%_VECHO% NeedEagle = '%NEEDEAGLE%'

REM
REM NOTE: The following three parameters are for use with the Inno Setup
REM       Preprocessor in #if directives and [apparently] cannot simply
REM       be True or False.  This fact is poorly documented in the Inno
REM       Setup Preprocessor documentation (i.e. bare True and False are
REM       not seen as "boolean", nor "integer", and cannot be converted
REM       to an integer for use in an #if expression).
REM
IF NOT DEFINED NONATIVE (
  SET NATIVE=1
) ELSE (
  SET NATIVE=0
)

%_VECHO% Native = '%NATIVE%'

REM
REM HACK: Cannot use the environment variable "SECURITY" here because
REM       that forces the shell to load Harpy, et al, which we do not
REM       want/need here.
REM
IF NOT DEFINED NOSECURITY (
  SET SECURITY_VALUE=1
) ELSE (
  SET SECURITY_VALUE=0
)

%_VECHO% SecurityValue = '%SECURITY_VALUE%'

IF NOT DEFINED NOTCL (
  SET TCL=1
) ELSE (
  SET TCL=0
)

%_VECHO% Tcl = '%TCL%'

IF NOT DEFINED INCLUDEARM (
  %_AECHO% Include ARM flag not specified, using default...
  SET INCLUDEARM=0
)

%_VECHO% IncludeArm = '%INCLUDEARM%'

SET APPID={{4E5890E6-BC71-4C9E-AEE8-9F4742044BE2}
SET EAGLEBINDIR=%~dp0\..\..\bin\%CONFIGURATION%\bin
SET SRCBINDIR=%~dp0\..\..\bin\%CONFIGURATION%\bin
SET BINLIBDIR=%~dp0\..\..\bin\%CONFIGURATION%\lib
SET SRCLIBDIR=%~dp0\..\..\lib
SET SRCTESTDIR=%~dp0\..\Tests
SET SRCISSFILE=%~dp0\..\..\Setup\Eagle.iss

REM
REM HACK: Required to make Visual Studio happy.
REM
SET EAGLEBINDIR=%EAGLEBINDIR:\\=\%
SET SRCBINDIR=%SRCBINDIR:\\=\%
SET BINLIBDIR=%BINLIBDIR:\\=\%
SET SRCLIBDIR=%SRCLIBDIR:\\=\%
SET SRCTESTDIR=%SRCTESTDIR:\\=\%
SET SRCISSFILE=%SRCISSFILE:\\=\%

%_VECHO% AppId = '%APPID%'
%_VECHO% EagleBinDir = '%EAGLEBINDIR%'
%_VECHO% SrcBinDir = '%SRCBINDIR%'
%_VECHO% BinLibDir = '%BINLIBDIR%'
%_VECHO% SrcLibDir = '%SRCLIBDIR%'
%_VECHO% SrcTestDir = '%SRCTESTDIR%'
%_VECHO% SrcIssFile = '%SRCISSFILE%'

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

%_VECHO% Tools = '%TOOLS%'
%_VECHO% NoSign = '%NOSIGN%'

CALL :fn_PrependToPath EAGLEBINDIR

IF "%PROCESSOR_ARCHITECTURE%" == "x86" GOTO set_path_x86

SET INNOSETUPPATH=%ProgramFiles(x86)%\Inno Setup 5
GOTO set_path_done

:set_path_x86

SET INNOSETUPPATH=%ProgramFiles%\Inno Setup 5

:set_path_done

CALL :fn_PrependToPath INNOSETUPPATH

%_VECHO% InnoSetupPath = '%INNOSETUPPATH%'
%_VECHO% Path = '%PATH%'
%_VECHO% Suffix = '%SUFFIX%'

SET GET_ISNETFX2_CMD=EagleShell.exe -evaluate "puts stdout [expr {[string match 2.* [info runtimeversion]] ? {True} : {False}}]"

%_VECHO% GetIsNetFx2Cmd = '%GET_ISNETFX2_CMD%'

IF NOT DEFINED ISNETFX2 (
  IF DEFINED __ECHO (
    %__ECHO% %GET_ISNETFX2_CMD%
    SET ISNETFX2=True
  ) ELSE (
    FOR /F %%T IN ('%GET_ISNETFX2_CMD%') DO (SET ISNETFX2=%%T)
  )
)

IF NOT DEFINED ISNETFX2 (
  ECHO The ISNETFX2 environment variable could not be set.
  GOTO errors
)

IF NOT DEFINED ISNETFX4 (
  SET ISNETFX4=not IsNetFx2Setup
)

%_VECHO% IsNetFx2 = '%ISNETFX2%'
%_VECHO% IsNetFx4 = '%ISNETFX4%'

IF DEFINED PATCHLEVEL GOTO skip_patchLevel1

IF NOT DEFINED NONETFX20 (
  SET GET_PATCHLEVEL_CMD=EagleShell.exe -evaluate "puts stdout [info engine PatchLevel]"
) ELSE (
  SET GET_PATCHLEVEL_CMD=EagleShell.exe -evaluate "puts stdout [appendArgs [info engine Version] . [join [clock build] .]]"
)

%_VECHO% GetPatchlevelCmd = '%GET_PATCHLEVEL_CMD%'

IF DEFINED __ECHO (
  %__ECHO% %GET_PATCHLEVEL_CMD%
  SET PATCHLEVEL=1.0.X.X
) ELSE (
  FOR /F %%T IN ('%GET_PATCHLEVEL_CMD%') DO (SET PATCHLEVEL=%%T)
)

:skip_patchLevel1

IF NOT DEFINED PATCHLEVEL (
  ECHO The PATCHLEVEL environment variable could not be set.
  GOTO errors
)

%_VECHO% PatchLevel = '%PATCHLEVEL%'

SET RELEASES=%~dp0\..\..\Releases
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
%__ECHO% ECHO %PATCHLEVEL% %OUTPUT% "%RELEASES%\Eagle_patchLevel.txt"

SET SETUP=%~dp0\..\..\Releases\%CONFIGURATION%\EagleSetup%SUFFIX%%PATCHLEVEL%.exe
SET SETUP=%SETUP:\\=\%

SET UNINSTALL=%~dp0\..\..\Setup\Output\Eagle\uninst-*.e32
SET UNINSTALL=%UNINSTALL:\\=\%

%_VECHO% Setup = '%SETUP%'
%_VECHO% Uninstall = '%UNINSTALL%'

SET GET_MAJORMINOR_CMD=EagleShell.exe -evaluate "puts stdout [join [lrange [split %PATCHLEVEL% .] 0 1] .]"

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

SET GET_ASSEMBLY_CMD=EagleShell.exe -evaluate "puts stdout [lindex [info assembly] 0]"

%_VECHO% GetAssemblyCmd = '%GET_ASSEMBLY_CMD%'

IF NOT DEFINED ASSEMBLY (
  IF DEFINED __ECHO (
    %__ECHO% %GET_ASSEMBLY_CMD%
    SET ASSEMBLY=Eagle, Version=1.0.X.X, Culture=neutral, PublicKeyToken=YYYYYYYYYYYYYYYY
  ) ELSE (
    FOR /F "delims=" %%T IN ('%GET_ASSEMBLY_CMD%') DO (SET ASSEMBLY=%%T)
  )
)

IF NOT DEFINED ASSEMBLY (
  ECHO The ASSEMBLY environment variable could not be set.
  GOTO usage
)

%_VECHO% Assembly = '%ASSEMBLY%'

SET GET_COMPILEINFO_CMD=EagleShell.exe -evaluate "puts stdout [getCompileInfo]"

%_VECHO% GetCompileInfoCmd = '%GET_COMPILEINFO_CMD%'

IF NOT DEFINED COMPILEINFO (
  IF DEFINED __ECHO (
    %__ECHO% %GET_COMPILEINFO_CMD%
    SET COMPILEINFO=TimeStamp {2007.10.01T00:00:00.000 +0000} Runtime Microsoft.NET RuntimeVersion 2.0.50727.42 ModuleVersionId 00000000-0000-0000-0000-000000000000 CompileOptions {NONE}
  ) ELSE (
    FOR /F "delims=" %%T IN ('%GET_COMPILEINFO_CMD%') DO (SET COMPILEINFO=%%T)
  )
)

IF NOT DEFINED COMPILEINFO (
  ECHO The COMPILEINFO environment variable could not be set.
  GOTO usage
)

REM
REM HACK: Fixup opening curly braces for use with Inno Setup.
REM
SET COMPILEINFO=%COMPILEINFO:{={{%

%_VECHO% CompileInfo = '%COMPILEINFO%'

CALL :fn_ResetErrorLevel

IF NOT DEFINED SKIP_SIGN_UNINSTALLER (
  SET SIGNED=no

  IF NOT DEFINED NOSIGN (
    FOR %%F IN (%UNINSTALL%) DO (
      %_CECHO3% CALL "%TOOLS%\signFile.bat" "%%F" "Eagle Uninstaller"
      %__ECHO3% CALL "%TOOLS%\signFile.bat" "%%F" "Eagle Uninstaller"
      IF ERRORLEVEL 1 GOTO errors

      IF NOT DEFINED NOSIGCHECK (
        %_AECHO% Checking signatures on file "%%F"...
        %_CECHO% SigCheck.exe "%%F" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
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

%_CECHO% ISCC.exe "%SRCISSFILE%" "/dAppId=%APPID%" "/dConfiguration=%CONFIGURATION%" "/dProcessor=%PROCESSOR%" "/dVcRuntimeX86=%VCRUNTIMEX86%" "/dVcRuntimeX64=%VCRUNTIMEX64%" "/dVcRuntimeArm=%VCRUNTIMEARM%" "/dSuffix=%SUFFIX%" "/dSigned=%SIGNED%" "/dSrcBinDir=%SRCBINDIR%" "/dBinLibDir=%BINLIBDIR%" "/dSrcLibDir=%SRCLIBDIR%" "/dSrcTestDir=%SRCTESTDIR%" "/dIsNetFx2=%ISNETFX2%" "/dIsNetFx4=%ISNETFX4%" "/dNeedActiveTcl=%NEEDACTIVETCL%" "/dNeedEagle=%NEEDEAGLE%" "/dNeedVcRuntimes=%NEEDVCRUNTIMES%" "/dAppFullVersion=%PATCHLEVEL%" "/dAppMajorMinorVersion=%MAJORMINOR%" "/dAppURL=%URL%" "/dNative=%NATIVE%" "/dSecurity=%SECURITY_VALUE%" "/dTcl=%TCL%" "/dIncludeArm=%INCLUDEARM%" "/dAssembly=%ASSEMBLY%" "/dCompileInfo=%COMPILEINFO%"
%__ECHO% ISCC.exe "%SRCISSFILE%" "/dAppId=%APPID%" "/dConfiguration=%CONFIGURATION%" "/dProcessor=%PROCESSOR%" "/dVcRuntimeX86=%VCRUNTIMEX86%" "/dVcRuntimeX64=%VCRUNTIMEX64%" "/dVcRuntimeArm=%VCRUNTIMEARM%" "/dSuffix=%SUFFIX%" "/dSigned=%SIGNED%" "/dSrcBinDir=%SRCBINDIR%" "/dBinLibDir=%BINLIBDIR%" "/dSrcLibDir=%SRCLIBDIR%" "/dSrcTestDir=%SRCTESTDIR%" "/dIsNetFx2=%ISNETFX2%" "/dIsNetFx4=%ISNETFX4%" "/dNeedActiveTcl=%NEEDACTIVETCL%" "/dNeedEagle=%NEEDEAGLE%" "/dNeedVcRuntimes=%NEEDVCRUNTIMES%" "/dAppFullVersion=%PATCHLEVEL%" "/dAppMajorMinorVersion=%MAJORMINOR%" "/dAppURL=%URL%" "/dNative=%NATIVE%" "/dSecurity=%SECURITY_VALUE%" "/dTcl=%TCL%" "/dIncludeArm=%INCLUDEARM%" "/dAssembly=%ASSEMBLY%" "/dCompileInfo=%COMPILEINFO%"

IF %ERRORLEVEL% NEQ 0 (
  ECHO Failed to compile setup.
  GOTO errors
)

IF NOT DEFINED NOSIGN (
  %_CECHO3% CALL "%TOOLS%\signFile.bat" "%SETUP%" "Eagle Setup"
  %__ECHO3% CALL "%TOOLS%\signFile.bat" "%SETUP%" "Eagle Setup"
  IF ERRORLEVEL 1 GOTO errors

  IF NOT DEFINED NOSIGCHECK (
    %_AECHO% Checking signatures on file "%SETUP%"...
    %_CECHO% SigCheck.exe "%SETUP%" %_CPIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"
    %__ECHO% SigCheck.exe "%SETUP%" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

    IF ERRORLEVEL 1 (
      ECHO Checking signatures on "%SETUP%" failed.
      GOTO errors
    )
  )
)

GOTO no_errors

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

:fn_ResetErrorLevel
  VERIFY > NUL
  GOTO :EOF

:fn_SetErrorLevel
  VERIFY MAYBE 2> NUL
  GOTO :EOF

:usage
  ECHO.
  ECHO Usage: %~nx0 [configuration]
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
