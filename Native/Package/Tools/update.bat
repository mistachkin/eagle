@ECHO OFF

::
:: update.bat --
::
:: Extensible Adaptable Generalized Logic Engine (Eagle)
:: Update Tool
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

REM SET DFLAGS=/L

%_VECHO% DFlags = '%DFLAGS%'

SET FLAGS=/V /F /G /H /I /R /S /Y /Z

%_VECHO% Flags = '%FLAGS%'

SET FFLAGS=/V /F /G /H /I /R /Y /Z

%_VECHO% FFlags = '%FFLAGS%'

IF NOT DEFINED DEFAULT_PLATFORM (
  SET DEFAULT_PLATFORM=Win32
)

%_VECHO% DefaultPlatform = '%DEFAULT_PLATFORM%'

IF NOT DEFINED OTHER_PLATFORMS (
  SET OTHER_PLATFORMS=x64 ARM
)

%_VECHO% OtherPlatforms = '%OTHER_PLATFORMS%'

SET PLATFORM=%2

IF DEFINED PLATFORM (
  CALL :fn_UnquoteVariable PLATFORM
) ELSE (
  %_AECHO% No platform specified, using default...
  SET PLATFORM=%DEFAULT_PLATFORM%
)

%_VECHO% Platform = '%PLATFORM%'

SET CONFIGURATION=%3

IF DEFINED CONFIGURATION (
  CALL :fn_UnquoteVariable CONFIGURATION
) ELSE (
  %_AECHO% No configuration specified, using default...
  SET CONFIGURATION=ReleaseDll
)

%_VECHO% Configuration = '%CONFIGURATION%'

SET DUMMY2=%4

IF DEFINED DUMMY2 (
  GOTO usage
)

REM
REM TODO: If the package name need to be updated, the following line must also
REM       be changed.
REM
IF NOT DEFINED PACKAGE_NAME (
  SET PACKAGE_NAME=Garuda
)

%_VECHO% PackageName = '%PACKAGE_NAME%'

SET SOURCE=%~dp0\..\..\..
SET SOURCE=%SOURCE:\\=\%

SET TARGET=%1

IF NOT DEFINED TARGET (
  GOTO usage
)

CALL :fn_UnquoteVariable TARGET

%_VECHO% Source = '%SOURCE%'
%_VECHO% Target = '%TARGET%'

IF DEFINED TARGET (
  CALL :fn_ResetErrorLevel

  %__ECHO% XCOPY "%SOURCE%\bin\%PLATFORM%\%CONFIGURATION%\%PACKAGE_NAME%.*" "%TARGET%" %FFLAGS% %DFLAGS%

  IF ERRORLEVEL 1 (
    ECHO Failed to copy "%SOURCE%\bin\%PLATFORM%\%CONFIGURATION%\%PACKAGE_NAME%.*" to "%TARGET%".
    GOTO errors
  )

  REM
  REM NOTE: Are we handling the default platform?
  REM
  IF /I "%PLATFORM%" == "%DEFAULT_PLATFORM%" (
    REM
    REM NOTE: Check all the non-default platforms.  When available, copy them
    REM       to the target directory as well.
    REM
    FOR %%P IN (%OTHER_PLATFORMS%) DO (
      IF EXIST "%SOURCE%\bin\%%P\%CONFIGURATION%" (
        %__ECHO% XCOPY "%SOURCE%\bin\%%P\%CONFIGURATION%\%PACKAGE_NAME%.*" "%TARGET%\%%P\" %FFLAGS% %DFLAGS%

        IF ERRORLEVEL 1 (
          ECHO Failed to copy "%SOURCE%\bin\%%P\%CONFIGURATION%\%PACKAGE_NAME%.*" to "%TARGET%\%%P\".
          GOTO errors
        )
      )
    )
  )

  %__ECHO% XCOPY "%SOURCE%\bin\%PLATFORM%\%CONFIGURATION%\dotnet.tcl" "%TARGET%" %FFLAGS% %DFLAGS%

  IF ERRORLEVEL 1 (
    ECHO Failed to copy "%SOURCE%\bin\%PLATFORM%\%CONFIGURATION%\dotnet.tcl" to "%TARGET%".
    GOTO errors
  )

  %__ECHO% XCOPY "%SOURCE%\bin\%PLATFORM%\%CONFIGURATION%\helper.tcl" "%TARGET%" %FFLAGS% %DFLAGS%

  IF ERRORLEVEL 1 (
    ECHO Failed to copy "%SOURCE%\bin\%PLATFORM%\%CONFIGURATION%\helper.tcl" to "%TARGET%".
    GOTO errors
  )

  %__ECHO% XCOPY "%SOURCE%\bin\%PLATFORM%\%CONFIGURATION%\pkgIndex.tcl" "%TARGET%" %FFLAGS% %DFLAGS%

  IF ERRORLEVEL 1 (
    ECHO Failed to copy "%SOURCE%\bin\%PLATFORM%\%CONFIGURATION%\pkgIndex.tcl" to "%TARGET%".
    GOTO errors
  )

  IF NOT DEFINED NOSCRIPTS (
    %__ECHO% XCOPY "%SOURCE%\Native\Package\Scripts" "%TARGET%\Scripts" %FLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SOURCE%\Native\Package\Scripts" to "%TARGET%\Scripts".
      GOTO errors
    )
  )

  IF NOT DEFINED NOTESTS (
    %__ECHO% XCOPY "%SOURCE%\Native\Package\Tests" "%TARGET%\Tests" %FLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SOURCE%\Native\Package\Tests" to "%TARGET%\Tests".
      GOTO errors
    )
  )

  IF NOT DEFINED NORELEASE (
    %__ECHO% XCOPY "%SOURCE%\Native\Package\README" "%TARGET%" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SOURCE%\Native\Package\README" to "%TARGET%".
      GOTO errors
    )

    %__ECHO% XCOPY "%SOURCE%\license.terms" "%TARGET%" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SOURCE%\license.terms" to "%TARGET%".
      GOTO errors
    )

    %__ECHO% XCOPY "%SOURCE%\*.url" "%TARGET%" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SOURCE%\*.url" to "%TARGET%".
      GOTO errors
    )
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

:usage
  ECHO.
  ECHO Usage: %~nx0 ^<target^> [platform] [configuration]
  ECHO.
  GOTO errors

:errors
  CALL :fn_SetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Update failure, errors were encountered.
  GOTO end_of_file

:no_errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Update success, no errors were encountered.
  GOTO end_of_file

:end_of_file
%__ECHO% EXIT /B %ERRORLEVEL%
