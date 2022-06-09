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

IF NOT DEFINED DOTNET (
  SET DOTNET=dotnet.exe
)

%_VECHO% DotNet = '%DOTNET%'

IF NOT DEFINED EXEC_SUBCOMMANDS (
  SET EXEC_SUBCOMMANDS=exec
)

%_VECHO% ExecSubcommands = '%EXEC_SUBCOMMANDS%'

REM SET DFLAGS=/L

%_VECHO% DFlags = '%DFLAGS%'

SET FLAGS=/V /F /G /H /I /R /S /Y /Z

%_VECHO% Flags = '%FLAGS%'

SET FFLAGS=/V /F /G /H /I /R /Y /Z

%_VECHO% FFlags = '%FFLAGS%'

SET CONFIGURATION=%2

IF DEFINED CONFIGURATION (
  CALL :fn_UnquoteVariable CONFIGURATION
) ELSE (
  %_AECHO% No configuration specified, using default...
  SET CONFIGURATION=Release
)

%_VECHO% Configuration = '%CONFIGURATION%'

SET TARGET=%1

IF NOT DEFINED TARGET (
  GOTO usage
)

CALL :fn_UnquoteVariable TARGET

SET DUMMY2=%3

IF DEFINED DUMMY2 (
  GOTO usage
)

IF NOT DEFINED TEMP (
  ECHO The TEMP environment variable must be set first.
  GOTO errors
)

%_VECHO% Temp = '%TEMP%'

IF NOT DEFINED LOGDIR (
  SET LOGDIR=%TEMP%
)

%_VECHO% LogDir = '%LOGDIR%'

SET LOGFILE=%LOGDIR%\Eagle_%CONFIGURATION%_Update_Test.log
SET LOGFILE=%LOGFILE:\\=\%

%_VECHO% LogFile = '%LOGFILE%'

IF EXIST "%LOGFILE%" (
  %__ECHO% DEL "%LOGFILE%"

  IF ERRORLEVEL 1 (
    ECHO Failed to delete "%LOGFILE%".
    GOTO errors
  )
)

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

%_VECHO% Tools = '%TOOLS%'

SET SOURCE=%~dp0\..\..
SET SOURCE=%SOURCE:\\=\%

%_VECHO% Source = '%SOURCE%'
%_VECHO% Target = '%TARGET%'

CALL :fn_ResetErrorLevel

%__ECHO2% PUSHD "%SOURCE%"

IF ERRORLEVEL 1 (
  ECHO Could not change directory to "%SOURCE%".
  GOTO errors
)

REM
REM HACK: Remove the .NET Core output directory names from the source.  This
REM       is necessary to make the release process consistent.
REM
FOR %%D IN (netcoreapp2.0 netcoreapp2.1 netcoreapp2.2 netcoreapp3.0 netcoreapp3.1 netstandard2.0 netstandard2.1) DO (
  IF EXIST "%SOURCE%\bin\%CONFIGURATION%\bin\%%D\*.dll" (
    %__ECHO% MOVE /Y "%SOURCE%\bin\%CONFIGURATION%\bin\%%D\*" "%SOURCE%\bin\%CONFIGURATION%\bin\"

    IF ERRORLEVEL 1 (
      ECHO Failed to move "%SOURCE%\bin\%CONFIGURATION%\bin\%%D\*" to "%SOURCE%\bin\%CONFIGURATION%\bin\".
      GOTO errors
    )
  )

  FOR /F "delims=" %%P IN ('DIR /B "%SOURCE%\bin\%CONFIGURATION%\lib" 2^> NUL') DO (
    IF EXIST "%SOURCE%\bin\%CONFIGURATION%\lib\%%P\%%D\*.dll" (
      %__ECHO% MOVE /Y "%SOURCE%\bin\%CONFIGURATION%\lib\%%P\%%D\*" "%SOURCE%\bin\%CONFIGURATION%\lib\%%P\"

      IF ERRORLEVEL 1 (
        ECHO Failed to move "%SOURCE%\bin\%CONFIGURATION%\lib\%%P\%%D\*" to "%SOURCE%\bin\%CONFIGURATION%\lib\%%P\".
        GOTO errors
      )
    )
  )
)

REM ****************************************************************************
REM **************************** Core Binary Files *****************************
REM ****************************************************************************

%__ECHO% XCOPY "%SOURCE%\bin\%CONFIGURATION%\bin" "%TARGET%\bin" %FLAGS% %DFLAGS%

IF ERRORLEVEL 1 (
  ECHO Failed to copy "%SOURCE%\bin\%CONFIGURATION%\bin" to "%TARGET%\bin".
  GOTO errors
)

REM ****************************************************************************
REM ********************** Package Binary / Library Files **********************
REM ****************************************************************************

%__ECHO% XCOPY "%SOURCE%\bin\%CONFIGURATION%\lib" "%TARGET%\lib" %FLAGS% %DFLAGS% /EXCLUDE:data\exclude_update.txt

IF ERRORLEVEL 1 (
  ECHO Failed to copy "%SOURCE%\bin\%CONFIGURATION%\lib" to "%TARGET%\lib".
  GOTO errors
)

REM ****************************************************************************
REM **************************** Core Library Files ****************************
REM ****************************************************************************

IF NOT DEFINED NOLIB (
  %__ECHO% XCOPY "%SOURCE%\lib" "%TARGET%\lib" %FLAGS% %DFLAGS%

  IF ERRORLEVEL 1 (
    ECHO Failed to copy "%SOURCE%\lib" to "%TARGET%\lib".
    GOTO errors
  )
)

REM ****************************************************************************
REM ***************************** Core Test Files ******************************
REM ****************************************************************************

IF NOT DEFINED NOTESTS (
  %__ECHO% XCOPY "%SOURCE%\Library\Tests" "%TARGET%\Tests" %FLAGS% %DFLAGS%

  IF ERRORLEVEL 1 (
    ECHO Failed to copy "%SOURCE%\Library\Tests" to "%TARGET%\Tests".
    GOTO errors
  )
)

REM ****************************************************************************
REM ************************** Standard Release Files **************************
REM ****************************************************************************

IF NOT DEFINED NORELEASE (
  %__ECHO% XCOPY "%SOURCE%\README" "%TARGET%" %FFLAGS% %DFLAGS%

  IF ERRORLEVEL 1 (
    ECHO Failed to copy "%SOURCE%\README" to "%TARGET%".
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

REM ****************************************************************************
REM *************************** Deployment Self-Test ***************************
REM ****************************************************************************

IF DEFINED NOUPDATETEST (
  %__ECHO2% POPD

  IF ERRORLEVEL 1 (
    ECHO Could not restore directory.
    GOTO errors
  )

  GOTO no_errors
)

CALL :fn_UnsetVariable EXTRA_PREARGS
CALL :fn_UnsetVariable EXTRA_POSTARGS

IF NOT DEFINED NOEXTRA (
  IF NOT DEFINED NOSIGN (
    SET EXTRA_PREARGS=-security true
    SET EXTRA_POSTARGS=-preTest "eval [string map [list \r\n \n] [readFile {%TOOLS%\data\security.eagle}]]"
  )
)

%_VECHO% ExtraPreArgs = '%EXTRA_PREARGS%'
%_VECHO% ExtraPostArgs = '%EXTRA_POSTARGS%'

SET TESTS_PASSED=NONE
SET TESTS_TOTAL=NONE

IF NOT DEFINED NOLIB (
  IF NOT DEFINED NOTESTS (
    REM
    REM NOTE: Run the copied Eagle shell, in place, with a "stub" test file
    REM       that should have 1 passed test and 1 skipped test and verify
    REM       that everything works as expected.
    REM
    CALL :fn_UnsetVariable TESTS_WERE_RUN

    IF EXIST "%TARGET%\bin\EagleShell.exe" (
      %_CECHO% "%TARGET%\bin\EagleShell.exe" %EXTRA_PREARGS% -file "%TARGET%\Tests\all.eagle" -logFile "%LOGFILE%" -file empty.eagle %EXTRA_POSTARGS%
      %__ECHO% "%TARGET%\bin\EagleShell.exe" %EXTRA_PREARGS% -file "%TARGET%\Tests\all.eagle" -logFile "%LOGFILE%" -file empty.eagle %EXTRA_POSTARGS%

      SET TESTS_WERE_RUN=1
    ) ELSE IF EXIST "%TARGET%\bin\EagleShell.dll" (
      %_CECHO% "%DOTNET%" %EXEC_SUBCOMMANDS% "%TARGET%\bin\EagleShell.dll" %EXTRA_PREARGS% -file "%TARGET%\Tests\all.eagle" -logFile "%LOGFILE%" -file empty.eagle %EXTRA_POSTARGS%
      %__ECHO% "%DOTNET%" %EXEC_SUBCOMMANDS% "%TARGET%\bin\EagleShell.dll" %EXTRA_PREARGS% -file "%TARGET%\Tests\all.eagle" -logFile "%LOGFILE%" -file empty.eagle %EXTRA_POSTARGS%

      SET TESTS_WERE_RUN=1
    ) ELSE (
      %_AECHO% Shell executable not available, update test skipped.
    )

    REM
    REM NOTE: If the tests were actually run, verify the results now.
    REM
    IF DEFINED TESTS_WERE_RUN (
      IF ERRORLEVEL 1 (
        ECHO Failed update test in "%TARGET%", bad exit code.
        GOTO errors
      ) ELSE IF EXIST "%LOGFILE%" (
        CALL :fn_UnsetVariable TESTS_PASSED

        FOR /F "delims=" %%L IN ('TYPE "%LOGFILE%" ^| find.exe "PASSED: "') DO (
          SET TESTS_PASSED=%%L
        )

        CALL :fn_UnsetVariable TESTS_TOTAL

        FOR /F "delims=" %%L IN ('TYPE "%LOGFILE%" ^| find.exe "TOTAL: "') DO (
          SET TESTS_TOTAL=%%L
        )
      ) ELSE (
        ECHO Failed update test in "%TARGET%", no log file.
        GOTO errors
      )
    )
  ) ELSE (
    %_AECHO% Test files not available, update test skipped.
  )
) ELSE (
  %_AECHO% Library files not available, update test skipped.
)

IF NOT DEFINED TESTS_PASSED (
  ECHO Failed update test in "%TARGET%", no passed tests count.
  GOTO errors
) ELSE IF /I NOT "%TESTS_PASSED%" == "NONE" (
  IF /I NOT "%TESTS_PASSED%" == "PASSED: 1" (
    ECHO Failed update test in "%TARGET%", wrong passed tests count "%TESTS_PASSED%".
    GOTO errors
  )
)

IF NOT DEFINED TESTS_TOTAL (
  ECHO Failed update test in "%TARGET%", no total tests count.
  GOTO errors
) ELSE IF /I NOT "%TESTS_TOTAL%" == "NONE" (
  IF /I NOT "%TESTS_TOTAL%" == "TOTAL: 2" (
    ECHO Failed update test in "%TARGET%", wrong total tests count "%TESTS_TOTAL%".
    GOTO errors
  )
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
  ECHO Usage: %~nx0 ^<target^> [configuration]
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
