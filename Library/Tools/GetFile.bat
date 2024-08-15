@ECHO OFF

::
:: GetFile.bat --
::
:: Extensible Adaptable Generalized Logic Engine (Eagle)
:: File Download Tool
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

SET URI=%1

IF DEFINED URI (
  CALL :fn_UnquoteVariable URI
) ELSE (
  GOTO usage
)

%_VECHO% Uri = '%URI%'

SET DUMMY2=%2

IF DEFINED DUMMY2 (
  GOTO usage
)

IF NOT DEFINED windir (
  ECHO The windir environment variable must be set first.
  GOTO errors
)

%_VECHO% WinDir = '%windir%'

IF NOT DEFINED TEMP (
  ECHO The TEMP environment variable must be set first.
  GOTO errors
)

%_VECHO% Temp = '%TEMP%'

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

%_VECHO% Tools = '%TOOLS%'

FOR %%T IN (csc.exe) DO (
  SET %%T_PATH=%%~dp$PATH:T
)

%_VECHO% Csc.exe_PATH = '%csc.exe_PATH%'

IF DEFINED csc.exe_PATH (
  GOTO skip_addToPath
)

CALL :fn_ResetErrorLevel
CALL :fn_SetFrameworkDir

IF ERRORLEVEL 1 (
  GOTO errors
)

CALL :fn_PrependToPath FRAMEWORKDIR

:skip_addToPath

IF NOT EXIST "%TEMP%\GetFile.exe" (
  %_CECHO% csc.exe "/out:%TEMP%\GetFile.exe" /target:exe "%TOOLS%\GetFile.cs"
  %__ECHO% csc.exe "/out:%TEMP%\GetFile.exe" /target:exe "%TOOLS%\GetFile.cs"

  IF ERRORLEVEL 1 (
    ECHO Compilation of "%TOOLS%\GetFile.cs" failed.
    GOTO errors
  )
)

%_CECHO% "%TEMP%\GetFile.exe" "%URI%"
%__ECHO% "%TEMP%\GetFile.exe" "%URI%"

IF ERRORLEVEL 1 (
  ECHO Download from "%URI%" failed.
  GOTO errors
)

GOTO no_errors

:fn_SetFrameworkDir
  IF DEFINED FRAMEWORKDIR (
    REM Use the existing .NET Framework directory...
  ) ELSE IF EXIST "%windir%\Microsoft.NET\Framework64\v2.0.50727" (
    SET FRAMEWORKDIR=%windir%\Microsoft.NET\Framework64\v2.0.50727
  ) ELSE IF EXIST "%windir%\Microsoft.NET\Framework64\v3.5" (
    SET FRAMEWORKDIR=%windir%\Microsoft.NET\Framework64\v3.5
  ) ELSE IF EXIST "%windir%\Microsoft.NET\Framework64\v4.0.30319" (
    SET FRAMEWORKDIR=%windir%\Microsoft.NET\Framework64\v4.0.30319
  ) ELSE IF EXIST "%windir%\Microsoft.NET\Framework\v2.0.50727" (
    SET FRAMEWORKDIR=%windir%\Microsoft.NET\Framework\v2.0.50727
  ) ELSE IF EXIST "%windir%\Microsoft.NET\Framework\v3.5" (
    SET FRAMEWORKDIR=%windir%\Microsoft.NET\Framework\v3.5
  ) ELSE IF EXIST "%windir%\Microsoft.NET\Framework\v4.0.30319" (
    SET FRAMEWORKDIR=%windir%\Microsoft.NET\Framework\v4.0.30319
  ) ELSE (
    ECHO No suitable version of the .NET Framework appears to be installed.
    CALL :fn_SetErrorLevel
    GOTO :EOF
  )
  %_VECHO% FrameworkDir = '%FRAMEWORKDIR%'
  IF NOT EXIST "%FRAMEWORKDIR%\csc.exe" (
    ECHO The .NET Framework file "%FRAMEWORKDIR%\csc.exe" is missing.
    CALL :fn_SetErrorLevel
    GOTO :EOF
  )
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
  ECHO Usage: %~nx0 ^<uri^>
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
