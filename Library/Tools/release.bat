@ECHO OFF

::
:: release.bat --
::
:: Extensible Adaptable Generalized Logic Engine (Eagle)
:: Binary Release Tool
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
REM SET __ECHO3=ECHO
IF NOT DEFINED _AECHO (SET _AECHO=REM)
IF NOT DEFINED _CECHO (SET _CECHO=REM)
IF NOT DEFINED _VECHO (SET _VECHO=REM)

SET PIPE=^|
IF DEFINED __ECHO SET PIPE=^^^|

%_AECHO% Running %0 %*

REM SET DFLAGS=/L

%_VECHO% DFlags = '%DFLAGS%'

SET FFLAGS=/V /F /G /H /R /Y /Z

%_VECHO% FFlags = '%FFLAGS%'

SET CONFIGURATION=%1

IF DEFINED CONFIGURATION (
  CALL :fn_UnquoteVariable CONFIGURATION
) ELSE (
  %_AECHO% No configuration specified, using default...
  SET CONFIGURATION=Release
)

%_VECHO% Configuration = '%CONFIGURATION%'

SET RELEASETYPE=%2

IF DEFINED RELEASETYPE (
  CALL :fn_UnquoteVariable RELEASETYPE
) ELSE (
  %_AECHO% No release type specified, using default...
  SET RELEASETYPE=Binary
)

%_VECHO% ReleaseType = '%RELEASETYPE%'

SET EXCLUDE=%3

IF DEFINED EXCLUDE (
  CALL :fn_UnquoteVariable EXCLUDE
) ELSE (
  %_AECHO% No exclusion list specified, using default...
  SET EXCLUDE=data\exclude_bin.txt
)

%_VECHO% Exclude = '%EXCLUDE%'

SET DUMMY2=%4

IF DEFINED DUMMY2 (
  GOTO usage
)

SET ROOT=%~dp0\..\..\..
SET ROOT=%ROOT:\\=\%

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

%_VECHO% Root = '%ROOT%'
%_VECHO% Tools = '%TOOLS%'
%_VECHO% NoSign = '%NOSIGN%'

REM
REM HACK: Authenticode signing SFX executables breaks the WinRAR
REM       "authenticity verification" algorithm; therefore, disable
REM       it if necessary.
REM
IF DEFINED NOSIGN (
  SET SfxAvOption=-av
)

%_VECHO% SfxAvOption = '%SfxAvOption%'

IF NOT DEFINED SfxBinary (
  SET SfxBinary=%TOOLS%\data\EagleBinary.sfx
)

SET SfxBinary=%SfxBinary:\\=\%

%_VECHO% SfxBinary = '%SfxBinary%'

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

IF NOT EXIST "Eagle\Releases\%CONFIGURATION%" (
  %__ECHO% MKDIR "Eagle\Releases\%CONFIGURATION%"

  IF ERRORLEVEL 1 (
    ECHO Could not create directory "Eagle\Releases\%CONFIGURATION%".
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

IF NOT DEFINED TEMP (
  ECHO The TEMP environment variable must be set first.
  GOTO errors
)

%_VECHO% Temp = '%TEMP%'

IF EXIST "%TEMP%\Eagle_%CONFIGURATION%" (
  %__ECHO% RMDIR /S /Q "%TEMP%\Eagle_%CONFIGURATION%"

  IF ERRORLEVEL 1 (
    ECHO Could not remove release staging directory.
    GOTO errors
  )
)

%__ECHO3% CALL "%TOOLS%\update.bat" "%TEMP%\Eagle_%CONFIGURATION%\Eagle" "%CONFIGURATION%"

IF ERRORLEVEL 1 (
  ECHO Could not update binary files.
  GOTO errors
)

%__ECHO2% PUSHD "%TEMP%\Eagle_%CONFIGURATION%"

IF ERRORLEVEL 1 (
  ECHO Could not change directory to "%TEMP%\Eagle_%CONFIGURATION%".
  GOTO errors
)

%_VECHO% Suffix = '%SUFFIX%'
%_VECHO% PatchLevel = '%PATCHLEVEL%'

%_CECHO% RAR.exe a -r -k -ed -av -rr "-z%ROOT%\Eagle\README" "-x@%ROOT%\Eagle\%EXCLUDE%" %RarNameFormat% -- "%ROOT%\Eagle\Releases\%CONFIGURATION%\Eagle%RELEASETYPE%%SUFFIX%%PATCHLEVEL%.rar" "Eagle\*"
%__ECHO% RAR.exe a -r -k -ed -av -rr "-z%ROOT%\Eagle\README" "-x@%ROOT%\Eagle\%EXCLUDE%" %RarNameFormat% -- "%ROOT%\Eagle\Releases\%CONFIGURATION%\Eagle%RELEASETYPE%%SUFFIX%%PATCHLEVEL%.rar" "Eagle\*"

IF ERRORLEVEL 1 (
  ECHO Failed to archive binary files.
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

%__ECHO% FOR /F "delims=" %%F IN ('DIR /B /OD "%ROOT%\Eagle\Releases\%CONFIGURATION%\Eagle%RELEASETYPE%*.rar"') DO (SET BinRarFile=%%~nF)

IF DEFINED __ECHO (
  SET BinRarFile=EagleFakeBinaryArchive
)

IF DEFINED BinRarFile (
  SET BinRarFile=%ROOT%\Eagle\Releases\%CONFIGURATION%\%BinRarFile%
)

IF NOT DEFINED NOSIGN (
  IF DEFINED BinRarFile (
    %__ECHO3% CALL "%TOOLS%\signFile.bat" "%BinRarFile%.rar" "Eagle Binary Distribution"
    IF ERRORLEVEL 1 GOTO errors
  )
)

IF DEFINED NOSFX (
  %_AECHO% Skipping creation of SFX...
  GOTO skip_sfx
)

IF DEFINED BinRarFile (
  %_VECHO% BinRarFile = '%BinRarFile%'

  IF EXIST "%BinRarFile%.exe" (
    %__ECHO% DEL "%BinRarFile%.exe"

    IF ERRORLEVEL 1 (
      ECHO Could not delete file "%BinRarFile%.exe".
      GOTO errors
    )
  )

  IF EXIST "%SfxBinary%" (
    %__ECHO% ECHO F %PIPE% XCOPY "%SfxBinary%" "%ProgramFiles%\WinRAR\Default.SFX" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SfxBinary%" to "%ProgramFiles%\WinRAR\Default.SFX".
      GOTO errors
    )
  )

  %_CECHO% WinRAR.exe a -r -k -ed -ibck %SfxAvOption% -rr "-z%ROOT%\Eagle\README" "-x@%ROOT%\Eagle\%EXCLUDE%" -sfx "-iicon%ROOT%\Eagle\Library\Resources\Eagle.ico" "-iimg%ROOT%\Eagle\images\logoSfx.bmp" -- "%BinRarFile%.exe" "Eagle\*"
  %__ECHO% WinRAR.exe a -r -k -ed -ibck %SfxAvOption% -rr "-z%ROOT%\Eagle\README" "-x@%ROOT%\Eagle\%EXCLUDE%" -sfx "-iicon%ROOT%\Eagle\Library\Resources\Eagle.ico" "-iimg%ROOT%\Eagle\images\logoSfx.bmp" -- "%BinRarFile%.exe" "Eagle\*"

  IF ERRORLEVEL 1 (
    ECHO Failed to create binary SFX.
    GOTO errors
  )

  IF NOT DEFINED NOSIGN (
    %__ECHO3% CALL "%TOOLS%\signFile.bat" "%BinRarFile%.exe" "Eagle Binary Distribution"
    IF ERRORLEVEL 1 GOTO errors

    IF NOT DEFINED NOSIGCHECK (
      %_AECHO% Checking signatures on file "%BinRarFile%.exe"...
      %__ECHO% SigCheck.exe "%BinRarFile%.exe" %PIPE% FINDSTR "/G:%TOOLS%\data\SigCheck.txt"

      IF ERRORLEVEL 1 (
        ECHO Checking signatures on "%BinRarFile%.exe" failed.
        GOTO errors
      )
    )
  )
) ELSE (
  ECHO Failed to find archived binary files.
  GOTO errors
)

:skip_sfx

%__ECHO2% POPD

IF ERRORLEVEL 1 (
  ECHO Could not restore directory.
  GOTO errors
)

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

:fn_ResetErrorLevel
  VERIFY > NUL
  GOTO :EOF

:fn_SetErrorLevel
  VERIFY MAYBE 2> NUL
  GOTO :EOF

:usage
  ECHO.
  ECHO Usage: %~nx0 [configuration] [releaseType] [exclude]
  GOTO errors

:errors
  CALL :fn_SetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Release failure, errors were encountered.
  GOTO end_of_file

:no_errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Release success, no errors were encountered.
  GOTO end_of_file

:end_of_file
%__ECHO% EXIT /B %ERRORLEVEL%
