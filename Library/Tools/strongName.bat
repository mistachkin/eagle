@ECHO OFF

::
:: strongName.bat --
::
:: Extensible Adaptable Generalized Logic Engine (Eagle)
:: Strong Name Verification Skipping Tool
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

SET ARGUMENT=%1

IF DEFINED ARGUMENT (
  SET ARGUMENT=-Vu
  SET ACTION=unregister
) ELSE (
  SET ARGUMENT=-Vr
  SET ACTION=register
)

SET DUMMY2=%2

IF DEFINED DUMMY2 (
  GOTO usage
)

CALL :fn_ResetErrorLevel

%_CECHO% sn.exe %ARGUMENT% *,29c6297630be05eb
%__ECHO% sn.exe %ARGUMENT% *,29c6297630be05eb

IF %ERRORLEVEL% NEQ 0 (
  ECHO Failed to %ACTION% debug assembly for verification skipping.
  GOTO errors
)

%_CECHO% sn.exe %ARGUMENT% *,1e22ec67879739a2
%__ECHO% sn.exe %ARGUMENT% *,1e22ec67879739a2

IF %ERRORLEVEL% NEQ 0 (
  ECHO Failed to %ACTION% release assembly for verification skipping.
  GOTO errors
)

%_CECHO% sn.exe %ARGUMENT% *,358030063a832bc3
%__ECHO% sn.exe %ARGUMENT% *,358030063a832bc3

IF %ERRORLEVEL% NEQ 0 (
  ECHO Failed to %ACTION% release assembly for verification skipping.
  GOTO errors
)

GOTO no_errors

:fn_ResetErrorLevel
  VERIFY > NUL
  GOTO :EOF

:fn_SetErrorLevel
  VERIFY MAYBE 2> NUL
  GOTO :EOF

:usage
  ECHO.
  ECHO Usage: %~nx0 [argument]
  ECHO.
  ECHO If an argument is supplied, strong name verification skipping entries for Eagle
  ECHO will be added; otherwise, they will be removed.
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
