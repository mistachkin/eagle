@ECHO OFF

::
:: deploy.bat --
::
:: Extensible Adaptable Generalized Logic Engine (Eagle)
:: Deployment Tool
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

REM SET DFLAGS=/L

%_VECHO% DFlags = '%DFLAGS%'

SET FFLAGS=/V /F /G /H /I /R /Y /Z

%_VECHO% FFlags = '%FFLAGS%'

SET ROOT=%~dp0\..\..
SET ROOT=%ROOT:\\=\%

%_VECHO% Root = '%ROOT%'

SET BINTARGET=%1

IF NOT DEFINED BINTARGET (
  GOTO usage
)

CALL :fn_UnquoteVariable BINTARGET

%_VECHO% BinTarget = '%BINTARGET%'

SET LIBTARGET=%2

IF NOT DEFINED LIBTARGET (
  GOTO usage
)

CALL :fn_UnquoteVariable LIBTARGET

%_VECHO% LibTarget = '%LIBTARGET%'

SET MSBUILDTARGET=%3

IF DEFINED MSBUILDTARGET (
  CALL :fn_UnquoteVariable MSBUILDTARGET
) ELSE (
  %_AECHO% No target directory for MSBuild files, skipping...
)

%_VECHO% MsBuildTarget = '%MSBUILDTARGET%'

SET CONFIGURATION=%4

IF DEFINED CONFIGURATION (
  CALL :fn_UnquoteVariable CONFIGURATION
) ELSE (
  %_AECHO% No configuration specified, using default...
  SET CONFIGURATION=Release
)

%_VECHO% Configuration = '%CONFIGURATION%'

SET SOURCE=%5

IF NOT DEFINED SOURCE (
  %_AECHO% No source directory specified, using default...
  SET SOURCE=%ROOT%
)

CALL :fn_UnquoteVariable SOURCE

%_VECHO% Source = '%SOURCE%'

SET DUMMY2=%6

IF DEFINED DUMMY2 (
  GOTO usage
)

REM ****************************************************************************
REM ************************* Check Source Directories *************************
REM ****************************************************************************

IF NOT EXIST "%SOURCE%" (
  ECHO Cannot copy from "%SOURCE%", it does not exist.
  GOTO errors
)

IF NOT EXIST "%SOURCE%\bin\%CONFIGURATION%\bin" (
  ECHO Cannot copy from "%SOURCE%\bin\%CONFIGURATION%\bin", it does not exist.
  GOTO errors
)

IF NOT EXIST "%SOURCE%\bin\%CONFIGURATION%\bin\x86" (
  ECHO WARNING: Cannot copy from "%SOURCE%\bin\%CONFIGURATION%\bin\x86", it does not exist.
)

IF NOT EXIST "%SOURCE%\bin\%CONFIGURATION%\bin\x64" (
  ECHO WARNING: Cannot copy from "%SOURCE%\bin\%CONFIGURATION%\bin\x64", it does not exist.
)

IF NOT EXIST "%SOURCE%\lib\Eagle1.0" (
  ECHO Cannot copy from "%SOURCE%\lib\Eagle1.0", it does not exist.
  GOTO errors
)

IF NOT EXIST "%SOURCE%\lib\Test1.0" (
  ECHO Cannot copy from "%SOURCE%\lib\Test1.0", it does not exist.
  GOTO errors
)

REM ****************************************************************************
REM *************************** Managed Binary Files ***************************
REM ****************************************************************************

SET BINFILES=Eagle.dll

IF NOT DEFINED NOSYMBOLS (
  SET BINFILES=%BINFILES% Eagle.pdb
)

IF NOT DEFINED NOSHELL (
  SET BINFILES=%BINFILES% EagleShell.exe
)

IF NOT DEFINED NOSHELL32 (
  SET BINFILES=%BINFILES% EagleShell32.exe
)

IF NOT DEFINED NOSYMBOLS IF NOT DEFINED NOSHELL (
  SET BINFILES=%BINFILES% EagleShell.pdb
)

%_VECHO% BinFiles = '%BINFILES%'

REM ****************************************************************************
REM ***************************** x86 Binary Files *****************************
REM ****************************************************************************

SET X86FILES=x86\Spilornis.dll

IF NOT DEFINED NOSYMBOLS (
  SET X86FILES=%X86FILES% x86\Spilornis.pdb
)

%_VECHO% X86Files = '%X86FILES%'

REM ****************************************************************************
REM ***************************** x64 Binary Files *****************************
REM ****************************************************************************

SET X64FILES=x64\Spilornis.dll

IF NOT DEFINED NOSYMBOLS (
  SET X64FILES=%X64FILES% x64\Spilornis.pdb
)

%_VECHO% X64Files = '%X64FILES%'

REM ****************************************************************************
REM ************************ Core Script Library Files *************************
REM ****************************************************************************

SET LIBFILES=auxiliary.eagle compat.eagle csharp.eagle
SET LIBFILES=%LIBFILES% database.eagle exec.eagle file1.eagle
SET LIBFILES=%LIBFILES% file2.eagle file2u.eagle file3.eagle info.eagle
SET LIBFILES=%LIBFILES% init.eagle list.eagle object.eagle
SET LIBFILES=%LIBFILES% pkgt.eagle platform.eagle process.eagle
SET LIBFILES=%LIBFILES% runopt.eagle safe.eagle shell.eagle
SET LIBFILES=%LIBFILES% shim.eagle test.eagle testlog.eagle
SET LIBFILES=%LIBFILES% unkobj.eagle unzip.eagle update.eagle
SET LIBFILES=%LIBFILES% word.tcl
SET LIBFILES=%LIBFILES% pkgIndex.eagle pkgIndex.tcl

REM
REM NOTE: Since the "embed.eagle" file may be customized by third-parties,
REM       skip copying it if the NOEMBED environment variable is set.
REM
IF NOT DEFINED NOEMBED (
  SET LIBFILES=%LIBFILES% embed.eagle
)

REM
REM NOTE: Since the "vendor.eagle" file may be customized by third-parties,
REM       skip copying it if the NOVENDOR environment variable is set.
REM
IF NOT DEFINED NOVENDOR (
  SET LIBFILES=%LIBFILES% vendor.eagle
)

%_VECHO% LibFiles = '%LIBFILES%'

REM ****************************************************************************
REM ************************ Test Package Library Files ************************
REM ****************************************************************************

SET LIBTESTFILES=all.eagle constraints.eagle epilogue.eagle pkgIndex.eagle
SET LIBTESTFILES=%LIBTESTFILES% pkgIndex.tcl prologue.eagle

%_VECHO% LibTestFiles = '%LIBTESTFILES%'

REM ****************************************************************************
REM ********************** MSBuild Targets / Tasks Files ***********************
REM ****************************************************************************

SET LIBMSBUILDFILES=Eagle.Builds.targets
SET LIBMSBUILDFILES=%LIBMSBUILDFILES% Eagle.Presets.targets
SET LIBMSBUILDFILES=%LIBMSBUILDFILES% Eagle.Sample.targets
SET LIBMSBUILDFILES=%LIBMSBUILDFILES% Eagle.Settings.targets
SET LIBMSBUILDFILES=%LIBMSBUILDFILES% Eagle.targets
SET LIBMSBUILDFILES=%LIBMSBUILDFILES% Eagle.tasks

%_VECHO% LibMsBuildFiles = '%LIBMSBUILDFILES%'

REM ****************************************************************************

CALL :fn_ResetErrorLevel

FOR %%F IN (%BINFILES%) DO (
  %__ECHO% XCOPY "%SOURCE%\bin\%CONFIGURATION%\bin\%%F" "%BINTARGET%\" %FFLAGS% %DFLAGS%

  IF ERRORLEVEL 1 (
    ECHO Failed to copy "%SOURCE%\bin\%CONFIGURATION%\bin\%%F" to "%BINTARGET%\".
    GOTO errors
  )
)

IF NOT DEFINED NONATIVE (
  FOR %%F IN (%X86FILES%) DO (
    REM
    REM NOTE: Technically, these files are optional.  If they are not found,
    REM       just skip over them.
    REM
    IF EXIST "%SOURCE%\bin\%CONFIGURATION%\bin\%%F" (
      %__ECHO% XCOPY "%SOURCE%\bin\%CONFIGURATION%\bin\%%F" "%BINTARGET%\x86\" %FFLAGS% %DFLAGS%

      IF ERRORLEVEL 1 (
        ECHO Failed to copy "%SOURCE%\bin\%CONFIGURATION%\bin\%%F" to "%BINTARGET%\x86\".
        GOTO errors
      )
    ) ELSE (
      ECHO WARNING: Cannot copy "%SOURCE%\bin\%CONFIGURATION%\bin\%%F", it does not exist.
    )
  )

  FOR %%F IN (%X64FILES%) DO (
    REM
    REM NOTE: Technically, these files are optional.  If they are not found,
    REM       just skip over them.
    REM
    IF EXIST "%SOURCE%\bin\%CONFIGURATION%\bin\%%F" (
      %__ECHO% XCOPY "%SOURCE%\bin\%CONFIGURATION%\bin\%%F" "%BINTARGET%\x64\" %FFLAGS% %DFLAGS%

      IF ERRORLEVEL 1 (
        ECHO Failed to copy "%SOURCE%\bin\%CONFIGURATION%\bin\%%F" to "%BINTARGET%\x64\".
        GOTO errors
      )
    ) ELSE (
      ECHO WARNING: Cannot copy "%SOURCE%\bin\%CONFIGURATION%\bin\%%F", it does not exist.
    )
  )
)

IF NOT DEFINED NOLIB (
  FOR %%F IN (%LIBFILES%) DO (
    %__ECHO% XCOPY "%SOURCE%\lib\Eagle1.0\%%F" "%LIBTARGET%\Eagle1.0\" %FFLAGS% %DFLAGS%

    IF ERRORLEVEL 1 (
      ECHO Failed to copy "%SOURCE%\lib\Eagle1.0\%%F" to "%LIBTARGET%\Eagle1.0\".
      GOTO errors
    )
  )

  IF NOT DEFINED NOTESTLIB (
    FOR %%F IN (%LIBTESTFILES%) DO (
      %__ECHO% XCOPY "%SOURCE%\lib\Test1.0\%%F" "%LIBTARGET%\Test1.0\" %FFLAGS% %DFLAGS%

      IF ERRORLEVEL 1 (
        ECHO Failed to copy "%SOURCE%\lib\Test1.0\%%F" to "%LIBTARGET%\Test1.0\".
        GOTO errors
      )
    )
  )
)

IF NOT DEFINED NOMSBUILDLIB (
  IF DEFINED MSBUILDTARGET (
    FOR %%F IN (%LIBMSBUILDFILES%) DO (
      %__ECHO% XCOPY "%SOURCE%\Targets\%%F" "%MSBUILDTARGET%\" %FFLAGS% %DFLAGS%

      IF ERRORLEVEL 1 (
        ECHO Failed to copy "%SOURCE%\Targets\%%F" to "%MSBUILDTARGET%\".
        GOTO errors
      )
    )
  )
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
  ECHO Usage: %~nx0 ^<binTarget^> ^<libTarget^> [msBuildTarget] [configuration] [source]
  GOTO errors

:errors
  CALL :fn_SetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Deploy failure, errors were encountered.
  GOTO end_of_file

:no_errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Deploy success, no errors were encountered.
  GOTO end_of_file

:end_of_file
%__ECHO% EXIT /B %ERRORLEVEL%
