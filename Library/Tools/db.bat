@ECHO OFF

::
:: db.bat --
::
:: Extensible Adaptable Generalized Logic Engine (Eagle)
:: Database Assembly Setup Tool
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

%_AECHO% Running %0 %*

SET SERVER=%1

IF DEFINED SERVER (
  CALL :fn_UnquoteVariable SERVER
) ELSE (
  %_AECHO% No server specified, using default...
  SET SERVER=.
)

%_VECHO% Server = '%SERVER%'

SET DATABASE=%2

IF DEFINED DATABASE (
  CALL :fn_UnquoteVariable DATABASE
) ELSE (
  %_AECHO% No database specified, using default...
  SET DATABASE=Eagle
)

%_VECHO% Database = '%DATABASE%'

SET ASSEMBLY=%3

IF DEFINED ASSEMBLY (
  CALL :fn_UnquoteVariable ASSEMBLY
) ELSE (
  %_AECHO% No assembly specified, using default...
  SET ASSEMBLY=Eagle
)

%_VECHO% Assembly = '%ASSEMBLY%'

SET CLASS=%4

IF DEFINED CLASS (
  CALL :fn_UnquoteVariable CLASS
) ELSE (
  %_AECHO% No class specified, using default...
  SET CLASS=Eagle._Components.Public.Engine
)

%_VECHO% Class = '%CLASS%'

SET CONFIGURATION=%5

IF DEFINED CONFIGURATION (
  CALL :fn_UnquoteVariable CONFIGURATION
) ELSE (
  %_AECHO% No configuration specified, using default...
  SET CONFIGURATION=Release
)

%_VECHO% Configuration = '%CONFIGURATION%'

SET DUMMY2=%6

IF DEFINED DUMMY2 (
  GOTO usage
)

SET SQLCMD=SqlCmd -E -S "%SERVER%" -h -1 -b

%_VECHO% SqlCmd = '%SQLCMD%'

SET SCRIPT=%~dp0\data\db.sql
SET SCRIPT=%SCRIPT:\\=\%

%_VECHO% Script = '%SCRIPT%'

SET FILENAME=%~dp0\..\..\bin\%CONFIGURATION%\bin\%ASSEMBLY%.dll
SET FILENAME=%FILENAME:\\=\%

%_VECHO% FileName = '%FILENAME%'

IF DEFINED SERVER (
  IF DEFINED DATABASE (
    CALL :fn_ResetErrorLevel
    CALL :fn_SqlCmd
    IF ERRORLEVEL 1 GOTO errors
  ) ELSE (
    GOTO usage
  )
) ELSE (
  GOTO usage
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

:fn_SqlCmd
  %__ECHO% %SQLCMD% -i "%SCRIPT%" -v DatabaseName="%DATABASE%" FileName="%FILENAME%" AssemblyName="%ASSEMBLY%" ClassName="%CLASS%"
  IF ERRORLEVEL 1 (
    ECHO Failed to setup class "%CLASS%" in assembly "%ASSEMBLY%" from file "%FILENAME%" in database "%DATABASE%".
    CALL :fn_SetErrorLevel
    GOTO :EOF
  )
  GOTO :EOF

:usage
  ECHO.
  ECHO Usage: %~nx0 [serverName] [databaseName] [assemblyName] [className] [configuration]
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
