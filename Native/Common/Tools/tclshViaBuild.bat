@ECHO OFF

::
:: tclshViaBuild.bat --
::
:: Extensible Adaptable Generalized Logic Engine (Eagle)
:: Tcl Shell Wrapper Tool
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

IF DEFINED TCLSH_CMD GOTO skip_tclshCmd
SET TCLSH_CMD=%TCLSH%

:skip_tclshCmd

IF NOT DEFINED TCLSH_CMD (
  SET TCLSH_CMD=tclsh
)

%_VECHO% TclSh = '%TCLSH%'
%_VECHO% TclShCmd = '%TCLSH_CMD%'

REM
REM NOTE: If an existing Tcl shell executable was already specified, skip
REM       checking along the PATH.
REM
IF EXIST "%TCLSH_CMD%" GOTO skip_PathCheck

REM
REM NOTE: We need to check for the Tcl shell executable along the PATH.
REM       First, extract the file name and extension from the specified
REM       ^(possibly^) fully qualified path and file name.
REM
FOR %%N IN (%TCLSH_CMD%) DO (
  SET TCLSH_CMD=%%~nxN
  SET TCLSH_EXT=%%~xN
)

REM
REM NOTE: Remove any spaces that may be contained in the file name or
REM       extension as they will not work right in the loop below.
REM
SET TCLSH_CMD=%TCLSH_CMD: =%

REM
REM NOTE: If there is no file extension, add an ".exe" one now.  This is
REM       necessary when checking for an executable file along the PATH
REM       because it only checks for the file name and extension exactly
REM       as specified and the Tcl shell executable will always have an
REM       ".exe" file extension on Windows.
REM
IF NOT DEFINED TCLSH_EXT (
  SET TCLSH_CMD=%TCLSH_CMD%.exe
)

%_VECHO% TclShCmd = '%TCLSH_CMD%'
%_VECHO% TclShExt = '%TCLSH_EXT%'

REM
REM NOTE: Check for the Tcl shell executable along the PATH.  If it is not
REM       found, stop without raising an error as this tool is designed to
REM       run during the build process, where such errors would be fatal.
REM
FOR %%T IN (%TCLSH_CMD%) DO (
  SET %%T_PATH=%%~dp$PATH:T

  IF NOT DEFINED %%T_PATH (
    ECHO The Tcl shell executable "%TCLSH_CMD%" was not found in the PATH.
    GOTO no_errors
  )

  CALL :fn_ShowVariable TclShCmdPath %%T_PATH
)

:skip_PathCheck
CALL :fn_ResetErrorLevel

%_CECHO% %TCLSH_CMD% %*
%__ECHO% %TCLSH_CMD% %*

IF ERRORLEVEL 1 (
  ECHO Running Tcl shell via build failed.
  GOTO errors
)

GOTO no_errors

:fn_ShowVariable
  SETLOCAL
  SET __ECHO_CMD=ECHO %%%2%%
  FOR /F "delims=" %%V IN ('%__ECHO_CMD%') DO (
    IF NOT "%%V" == "" (
      IF NOT "%%V" == "%%%2%%" (
        %_VECHO% %1 = '%%V'
      )
    )
  )
  ENDLOCAL
  GOTO :EOF

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
