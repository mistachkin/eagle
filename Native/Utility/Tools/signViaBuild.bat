@ECHO OFF

::
:: signViaBuild.bat --
::
:: Extensible Adaptable Generalized Logic Engine (Eagle)
:: Build Signing Tool
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
IF NOT DEFINED _AECHO (SET _AECHO=ECHO)
IF NOT DEFINED _CECHO (SET _CECHO=ECHO)
IF NOT DEFINED _VECHO (SET _VECHO=REM)

%_AECHO% Running %0 %*

SET LIBRARY_TOOLS=%~dp0\..\..\..\Library\Tools
SET LIBRARY_TOOLS=%LIBRARY_TOOLS:\\=\%

%_VECHO% LibraryTools = '%LIBRARY_TOOLS%'

IF DEFINED NOSIGN (
  ECHO Signing skipped, the NOSIGN environment variable is set.
  GOTO no_errors
)

SET VIA_BUILD=1
CALL "%LIBRARY_TOOLS%\signFile.bat" %*

IF ERRORLEVEL 1 (
  ECHO Signing via build failed.
  GOTO errors
)

GOTO no_errors

:fn_ResetErrorLevel
  VERIFY > NUL
  GOTO :EOF

:fn_SetErrorLevel
  VERIFY MAYBE 2> NUL
  GOTO :EOF

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
