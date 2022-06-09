@ECHO OFF

::
:: archive.bat --
::
:: Extensible Adaptable Generalized Logic Engine (Eagle)
:: Source Archiving Tool
::
:: WARNING: Tool assumes directory containing project files is named "Eagle".
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
IF NOT DEFINED _AECHO (SET _AECHO=REM)
IF NOT DEFINED _CECHO (SET _CECHO=REM)
IF NOT DEFINED _VECHO (SET _VECHO=REM)

SET PIPE=^|
IF DEFINED __ECHO SET PIPE=^^^|

%_AECHO% Running %0 %*

SET DUMMY2=%1

IF DEFINED DUMMY2 (
  GOTO usage
)

REM SET DFLAGS=/L

%_VECHO% DFlags = '%DFLAGS%'

SET FFLAGS=/V /F /G /H /R /Y /Z

%_VECHO% FFlags = '%FFLAGS%'

SET ROOT=%~dp0\..\..\..
SET ROOT=%ROOT:\\=\%

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

%_VECHO% Root = '%ROOT%'
%_VECHO% Tools = '%TOOLS%'
%_VECHO% NoSign = '%NOSIGN%'

REM
REM NOTE: Check if we are going to exclude all the external tool executables
REM       and their associated source code.
REM
IF DEFINED NOTOOL (
  SET EXCLUDE_SRC="-x@%ROOT%\Eagle\data\exclude_src.txt"
  SET PREFIX=EagleSourceOnly
) ELSE (
  CALL :fn_UnsetVariable EXCLUDE_SRC
  SET PREFIX=EagleSource
)

%_VECHO% ExcludeSrc = '%EXCLUDE_SRC%'
%_VECHO% Prefix = '%PREFIX%'

REM
REM HACK: Authenticode signing SFX executables breaks the WinRAR
REM       "authenticity verification" algorithm; therefore, disable it if
REM       necessary.
REM
IF DEFINED NOSIGN (
  SET SfxAvOption=-av
)

%_VECHO% SfxAvOption = '%SfxAvOption%'

IF NOT DEFINED SfxSource (
  SET SfxSource=%TOOLS%\data\EagleSource.sfx
)

SET SfxSource=%SfxSource:\\=\%

%_VECHO% SfxSource = '%SfxSource%'

IF NOT DEFINED PATCHLEVEL (
  SET RarNameFormat=-agYYYY-MM-DD-NN
)

%_VECHO% RarNameFormat = '%RarNameFormat%'

CALL :fn_ResetErrorLevel

%__ECHO2% PUSHD "%ROOT%"

IF ERRORLEVEL 1 (
  ECHO Could not change directory to "%ROOT%".
  GOTO errors
)

IF NOT EXIST Eagle\Releases (
  %__ECHO% MKDIR Eagle\Releases

  IF ERRORLEVEL 1 (
    ECHO Could not create directory "Eagle\Releases".
    GOTO errors
  )
)

CALL :fn_PrependToPath TOOLS

IF "%PROCESSOR_ARCHITECTURE%" == "x86" GOTO set_path_x86

SET WINRARPATH=%ProgramFiles(x86)%\WinRAR
GOTO set_path_done

:set_path_x86

SET WINRARPATH=%ProgramFiles%\WinRAR

:set_path_done

CALL :fn_PrependToPath WINRARPATH

%_VECHO% WinRarPath = '%WINRARPATH%'
%_VECHO% Path = '%PATH%'
%_VECHO% Suffix = '%SUFFIX%'
%_VECHO% PatchLevel = '%PATCHLEVEL%'

%_CECHO% RAR.exe a -r -k -ed -av -rr "-z%ROOT%\Eagle\README" "-x@%ROOT%\Eagle\data\exclude_archive.txt" %EXCLUDE_SRC% %RarNameFormat% -- "%ROOT%\Eagle\Releases\%PREFIX%%SUFFIX%%PATCHLEVEL%.rar" "Eagle\*"
%__ECHO% RAR.exe a -r -k -ed -av -rr "-z%ROOT%\Eagle\README" "-x@%ROOT%\Eagle\data\exclude_archive.txt" %EXCLUDE_SRC% %RarNameFormat% -- "%ROOT%\Eagle\Releases\%PREFIX%%SUFFIX%%PATCHLEVEL%.rar" "Eagle\*"

IF ERRORLEVEL 1 (
  ECHO Failed to archive source files.
  GOTO errors
)

REM
REM HACK: The 4NT ^(TCC^) command processor does not appear to support some of
REM       things we really need to create the SFX; therefore, just skip it.
REM       For reasons unknown, this cannot simply use "IF DEFINED _4VER".
REM
IF NOT "%_4VER%" == "" (
  %_AECHO% 4NT command processor detected, skipping creation of SFX...
  GOTO skip_sfx
)

%__ECHO% FOR /F "delims=" %%F IN ('DIR /B /OD "%ROOT%\Eagle\Releases\%PREFIX%*.rar"') DO (SET SrcRarFile=%%~nF)

IF DEFINED __ECHO (
  SET SrcRarFile=EagleFakeSourceArchive
)

IF DEFINED SrcRarFile (
  SET SrcRarFile=%ROOT%\Eagle\Releases\%SrcRarFile%
)

IF NOT DEFINED NOSIGN (
  IF DEFINED SrcRarFile (
    %__ECHO3% CALL "%TOOLS%\signFile.bat" "%SrcRarFile%.rar" "Eagle Source Distribution"
    IF ERRORLEVEL 1 GOTO errors
  )
)

IF DEFINED NOSFX (
  %_AECHO% Skipping creation of SFX...
  GOTO skip_sfx
)

IF DEFINED SrcRarFile (
  %_VECHO% SrcRarFile = '%SrcRarFile%'

  IF EXIST "%SrcRarFile%.exe" (
    %__ECHO% DEL "%SrcRarFile%.exe"

    IF ERRORLEVEL 1 (
      ECHO Could not delete file "%SrcRarFile%.exe".
      GOTO errors
    )
  )

  IF EXIST "%SfxSource%" (
    %__ECHO% ECHO F %PIPE% XCOPY "%SfxSource%" "%ProgramFiles%\WinRAR\Default.SFX" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SfxSource%" to "%ProgramFiles%\WinRAR\Default.SFX".
      GOTO errors
    )
  )

  %_CECHO% WinRAR.exe a -r -k -ed -ibck %SfxAvOption% -rr "-z%ROOT%\Eagle\README" "-x@%ROOT%\Eagle\data\exclude_archive.txt" %EXCLUDE_SRC% -sfx "-iicon%ROOT%\Eagle\Library\Resources\Eagle.ico" "-iimg%ROOT%\Eagle\images\logoSfx.bmp" -- "%SrcRarFile%.exe" "Eagle\*"
  %__ECHO% WinRAR.exe a -r -k -ed -ibck %SfxAvOption% -rr "-z%ROOT%\Eagle\README" "-x@%ROOT%\Eagle\data\exclude_archive.txt" %EXCLUDE_SRC% -sfx "-iicon%ROOT%\Eagle\Library\Resources\Eagle.ico" "-iimg%ROOT%\Eagle\images\logoSfx.bmp" -- "%SrcRarFile%.exe" "Eagle\*"

  IF ERRORLEVEL 1 (
    ECHO Failed to create source SFX.
    GOTO errors
  )

  IF NOT DEFINED NOSIGN (
    %__ECHO3% CALL "%TOOLS%\signFile.bat" "%SrcRarFile%.exe" "Eagle Source Distribution"
    IF ERRORLEVEL 1 GOTO errors

    IF NOT DEFINED NOSIGCHECK (
      %_AECHO% Checking signatures on file "%SrcRarFile%.exe"...
      %__ECHO% SigCheck.exe "%SrcRarFile%.exe" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

      IF ERRORLEVEL 1 (
        ECHO Checking signatures on "%SrcRarFile%.exe" failed.
        GOTO errors
      )
    )
  )
) ELSE (
  ECHO Failed to find archived source files.
  GOTO errors
)

:skip_sfx

%__ECHO2% POPD

IF ERRORLEVEL 1 (
  ECHO Could not restore directory.
  GOTO errors
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
  ECHO Usage: %~nx0
  GOTO errors

:errors
  CALL :fn_SetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Archive failure, errors were encountered.
  GOTO end_of_file

:no_errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Archive success, no errors were encountered.
  GOTO end_of_file

:end_of_file
%__ECHO% EXIT /B %ERRORLEVEL%
