@ECHO OFF

::
:: netFx20Sp.bat --
::
:: Extensible Adaptable Generalized Logic Engine (Eagle)
:: Microsoft.NET Framework v2.0 Service Pack Detection Tool
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

%_AECHO% Running %0 %*

SET DUMMY2=%1

IF DEFINED DUMMY2 (
  GOTO usage
)

SET SETERRORLEVEL=%~dp0\SetErrorLevel.exe
SET SETERRORLEVEL=%SETERRORLEVEL:\\=\%

%_VECHO% SetErrorLevel = '%SETERRORLEVEL%'

REM
REM NOTE: Initially, default to nothing.
REM
SET SP=0x000

IF EXIST "%SETERRORLEVEL%" "%SETERRORLEVEL%" %SP%

REM
REM NOTE: Build the command that we will use to query for v2.0.
REM
SET GET_SP_CMD=reg.exe QUERY "HKLM\Software\Microsoft\NET Framework Setup\NDP\v2.0.50727" /v SP

FOR /F "eol=; tokens=1,2,3*" %%I IN ('%GET_SP_CMD%') DO (
  IF {%%I} == {SP} (
    IF {%%J} == {REG_DWORD} (
      %_AECHO% Found Microsoft.NET Framework v2.0 Service Pack "%%K".
      SET /A SP=0x200 + %%K
      GOTO no_errors
    )
  )
)

GOTO errors

:fn_ResetErrorLevel
  VERIFY > NUL
  GOTO :EOF

:usage
  ECHO.
  ECHO Usage: %~nx0
  ECHO.
  GOTO errors

:errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  GOTO end_of_file

:no_errors
  IF EXIST "%SETERRORLEVEL%" "%SETERRORLEVEL%" %SP%
  ENDLOCAL
  GOTO end_of_file

:end_of_file
EXIT /B %ERRORLEVEL%
