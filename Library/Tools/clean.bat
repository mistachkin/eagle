@ECHO OFF

::
:: clean.bat --
::
:: Extensible Adaptable Generalized Logic Engine (Eagle)
:: Build Cleaning Tool
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

SET SOURCE=%~dp0\..\..
SET SOURCE=%SOURCE:\\=\%

%_VECHO% Source = '%SOURCE%'

IF NOT DEFINED TEMP (
  ECHO The TEMP environment variable must be set first.
  GOTO usage
)

%_VECHO% Temp = '%TEMP%'

IF NOT EXIST "%TEMP%" (
  ECHO The TEMP directory, "%TEMP%", does not exist.
  GOTO usage
)

IF DEFINED TESTEXEFILES GOTO skip_testExeFiles

SET TESTEXEFILES=EagleShell.dll EagleShell.exe EagleShell32.exe
SET TESTEXEFILES=%TESTEXEFILES% Featherlight.exe
SET TESTEXEFILES=%TESTEXEFILES% dotnet.exe
SET TESTEXEFILES=%TESTEXEFILES% mono.exe
SET TESTEXEFILES=%TESTEXEFILES% tclsh__.exe

:skip_testExeFiles

%_VECHO% TestExeFiles = '%TESTEXEFILES%'

IF DEFINED CLEANDIRS GOTO skip_cleanDirs

SET CLEANDIRS=.vs bin cov-int obj
SET CLEANDIRS=%CLEANDIRS% Build\bin Build\obj
SET CLEANDIRS=%CLEANDIRS% Example\bin Example\obj
SET CLEANDIRS=%CLEANDIRS% Installer\bin Installer\obj
SET CLEANDIRS=%CLEANDIRS% Library\bin Library\obj
SET CLEANDIRS=%CLEANDIRS% Management\bin Management\obj
SET CLEANDIRS=%CLEANDIRS% MonoDevelop\bin MonoDevelop\obj
SET CLEANDIRS=%CLEANDIRS% Native\Package\bin Native\Package\obj
SET CLEANDIRS=%CLEANDIRS% Native\Utility\bin Native\Utility\obj
SET CLEANDIRS=%CLEANDIRS% Sample\bin Sample\obj
SET CLEANDIRS=%CLEANDIRS% Service\bin Service\obj
SET CLEANDIRS=%CLEANDIRS% Shell\bin Shell\obj
SET CLEANDIRS=%CLEANDIRS% Stub\bin Stub\obj
SET CLEANDIRS=%CLEANDIRS% Test\bin Test\obj
SET CLEANDIRS=%CLEANDIRS% Toolkit\bin Toolkit\obj
SET CLEANDIRS=%CLEANDIRS% Update\bin Update\obj

:skip_cleanDirs

%_VECHO% CleanDirs = '%CLEANDIRS%'

IF NOT DEFINED CLEANEXTS (
  SET CLEANEXTS=asc exe htm log nupkg rar txt zip
)

%_VECHO% CleanExts = '%CLEANEXTS%'

IF DEFINED PLUGINDIRS GOTO skip_PluginDirs

SET PLUGINDIRS=Plugins\bin Plugins\obj

:skip_PluginDirs

%_VECHO% PluginDirs = '%PLUGINDIRS%'

IF DEFINED RELEASEDIRS GOTO skip_releaseDirs

SET RELEASEDIRS=Debug Release
SET RELEASEDIRS=%RELEASEDIRS% DebugNetFx20 ReleaseNetFx20
SET RELEASEDIRS=%RELEASEDIRS% DebugNetFx35 ReleaseNetFx35
SET RELEASEDIRS=%RELEASEDIRS% DebugNetFx40 ReleaseNetFx40
SET RELEASEDIRS=%RELEASEDIRS% DebugNetFx45 ReleaseNetFx45
SET RELEASEDIRS=%RELEASEDIRS% DebugNetFx451 ReleaseNetFx451
SET RELEASEDIRS=%RELEASEDIRS% DebugNetFx452 ReleaseNetFx452
SET RELEASEDIRS=%RELEASEDIRS% DebugNetFx46 ReleaseNetFx46
SET RELEASEDIRS=%RELEASEDIRS% DebugNetFx461 ReleaseNetFx461
SET RELEASEDIRS=%RELEASEDIRS% DebugNetFx462 ReleaseNetFx462
SET RELEASEDIRS=%RELEASEDIRS% DebugNetFx47 ReleaseNetFx47
SET RELEASEDIRS=%RELEASEDIRS% DebugNetFx471 ReleaseNetFx471
SET RELEASEDIRS=%RELEASEDIRS% DebugNetFx472 ReleaseNetFx472
SET RELEASEDIRS=%RELEASEDIRS% DebugNetFx48 ReleaseNetFx48
SET RELEASEDIRS=%RELEASEDIRS% DebugNetStandard20 ReleaseNetStandard20
SET RELEASEDIRS=%RELEASEDIRS% DebugNetStandard21 ReleaseNetStandard21
SET RELEASEDIRS=%RELEASEDIRS% DebugBare ReleaseBare
SET RELEASEDIRS=%RELEASEDIRS% DebugLeanAndMean ReleaseLeanAndMean
SET RELEASEDIRS=%RELEASEDIRS% DebugDatabase ReleaseDatabase
SET RELEASEDIRS=%RELEASEDIRS% DebugMonoOnUnix ReleaseMonoOnUnix
SET RELEASEDIRS=%RELEASEDIRS% DebugDevelopment ReleaseDevelopment
SET RELEASEDIRS=%RELEASEDIRS% DebugCoverage ReleaseCoverage
SET RELEASEDIRS=%RELEASEDIRS% Win32 x64
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDll Win32_ReleaseDll
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDll x64_ReleaseDll
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllNetFx20 Win32_ReleaseDllNetFx20
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllNetFx20 x64_ReleaseDllNetFx20
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllNetFx35 Win32_ReleaseDllNetFx35
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllNetFx35 x64_ReleaseDllNetFx35
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllNetFx40 Win32_ReleaseDllNetFx40
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllNetFx40 x64_ReleaseDllNetFx40
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllNetFx45 Win32_ReleaseDllNetFx45
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllNetFx45 x64_ReleaseDllNetFx45
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllNetFx451 Win32_ReleaseDllNetFx451
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllNetFx451 x64_ReleaseDllNetFx451
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllNetFx452 Win32_ReleaseDllNetFx452
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllNetFx452 x64_ReleaseDllNetFx452
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllNetFx46 Win32_ReleaseDllNetFx46
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllNetFx46 x64_ReleaseDllNetFx46
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllNetFx461 Win32_ReleaseDllNetFx461
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllNetFx461 x64_ReleaseDllNetFx461
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllNetFx462 Win32_ReleaseDllNetFx462
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllNetFx462 x64_ReleaseDllNetFx462
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllNetFx47 Win32_ReleaseDllNetFx47
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllNetFx47 x64_ReleaseDllNetFx47
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllNetFx471 Win32_ReleaseDllNetFx471
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllNetFx471 x64_ReleaseDllNetFx471
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllNetFx472 Win32_ReleaseDllNetFx472
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllNetFx472 x64_ReleaseDllNetFx472
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllNetFx48 Win32_ReleaseDllNetFx48
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllNetFx48 x64_ReleaseDllNetFx48
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllNetStandard20 Win32_ReleaseDllNetStandard20
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllNetStandard20 x64_ReleaseDllNetStandard20
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllNetStandard21 Win32_ReleaseDllNetStandard21
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllNetStandard21 x64_ReleaseDllNetStandard21
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllBare Win32_ReleaseDllBare
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllBare x64_ReleaseDllBare
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllLeanAndMean Win32_ReleaseDllLeanAndMean
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllLeanAndMean x64_ReleaseDllLeanAndMean
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllDatabase Win32_ReleaseDllDatabase
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllDatabase x64_ReleaseDllDatabase
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllMonoOnUnix Win32_ReleaseDllMonoOnUnix
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllMonoOnUnix x64_ReleaseDllMonoOnUnix
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllDevelopment Win32_ReleaseDllDevelopment
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllDevelopment x64_ReleaseDllDevelopment
SET RELEASEDIRS=%RELEASEDIRS% Win32_DebugDllCoverage Win32_ReleaseDllCoverage
SET RELEASEDIRS=%RELEASEDIRS% x64_DebugDllCoverage x64_ReleaseDllCoverage
SET RELEASEDIRS=%RELEASEDIRS% NuGet_Debug NuGet_Release
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugNetFx20 NuGet_ReleaseNetFx20
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugNetFx35 NuGet_ReleaseNetFx35
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugNetFx40 NuGet_ReleaseNetFx40
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugNetFx45 NuGet_ReleaseNetFx45
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugNetFx451 NuGet_ReleaseNetFx451
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugNetFx452 NuGet_ReleaseNetFx452
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugNetFx46 NuGet_ReleaseNetFx46
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugNetFx461 NuGet_ReleaseNetFx461
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugNetFx462 NuGet_ReleaseNetFx462
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugNetFx47 NuGet_ReleaseNetFx47
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugNetFx471 NuGet_ReleaseNetFx471
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugNetFx472 NuGet_ReleaseNetFx472
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugNetFx48 NuGet_ReleaseNetFx48
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugNetStandard20 NuGet_ReleaseNetStandard20
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugNetStandard21 NuGet_ReleaseNetStandard21
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugBare NuGet_ReleaseBare
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugLeanAndMean NuGet_ReleaseLeanAndMean
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugDatabase NuGet_ReleaseDatabase
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugMonoOnUnix NuGet_ReleaseMonoOnUnix
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugDevelopment NuGet_ReleaseDevelopment
SET RELEASEDIRS=%RELEASEDIRS% NuGet_DebugCoverage NuGet_ReleaseCoverage

:skip_releaseDirs

%_VECHO% ReleaseDirs = '%RELEASEDIRS%'

IF DEFINED RELEASESUBDIRS GOTO skip_releaseSubDirs

SET RELEASESUBDIRS=bin NuGet SymbolSource
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDll ReleaseDll
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllNetFx20 ReleaseDllNetFx20
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllNetFx35 ReleaseDllNetFx35
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllNetFx40 ReleaseDllNetFx40
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllNetFx45 ReleaseDllNetFx45
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllNetFx451 ReleaseDllNetFx451
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllNetFx452 ReleaseDllNetFx452
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllNetFx46 ReleaseDllNetFx46
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllNetFx461 ReleaseDllNetFx461
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllNetFx462 ReleaseDllNetFx462
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllNetFx47 ReleaseDllNetFx47
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllNetFx471 ReleaseDllNetFx471
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllNetFx472 ReleaseDllNetFx472
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllNetFx48 ReleaseDllNetFx48
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllNetStandard20 ReleaseDllNetStandard20
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllNetStandard21 ReleaseDllNetStandard21
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllBare ReleaseDllBare
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllLeanAndMean ReleaseDllLeanAndMean
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllDatabase ReleaseDllDatabase
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllMonoOnUnix ReleaseDllMonoOnUnix
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllDevelopment ReleaseDllDevelopment
SET RELEASESUBDIRS=%RELEASESUBDIRS% DebugDllCoverage ReleaseDllCoverage

:skip_releaseSubDirs

%_VECHO% ReleaseSubDirs = '%RELEASESUBDIRS%'

IF DEFINED PACKAGETEADIRS GOTO skip_PackageTeaDirs

REM
REM NOTE: *WARNING* The asterisk character ("*") will be appended to each
REM       of these patterns prior to use.
REM
SET PACKAGETEADIRS=Native\Package\src\win\tea\Debug
SET PACKAGETEADIRS=%PACKAGETEADIRS% Native\Package\src\win\tea\Release

:skip_PackageTeaDirs

%_VECHO% PackageTeaDirs = '%PACKAGETEADIRS%'

IF DEFINED PACKAGETEAFILES GOTO skip_PackageTeaFiles

SET PACKAGETEAFILES=nmakehlp.exe nmakehlp.obj version.ts version.vc
SET PACKAGETEAFILES=%PACKAGETEAFILES% versions.ts versions.vc
SET PACKAGETEAFILES=%PACKAGETEAFILES% trimspace.exe trimspace.obj
SET PACKAGETEAFILES=%PACKAGETEAFILES% vercl.i vercl.ts vercl.vc vercl.x
SET PACKAGETEAFILES=%PACKAGETEAFILES% _junk.out _junk.pch __.idb __.pdb

:skip_PackageTeaFiles

%_VECHO% PackageTeaFiles = '%PACKAGETEAFILES%'

CALL :fn_ResetErrorLevel

%_AECHO%.

FOR %%D IN (%CLEANDIRS%) DO (
  IF EXIST "%SOURCE%\%%D" (
    %__ECHO% RMDIR /S /Q "%SOURCE%\%%D"

    IF ERRORLEVEL 1 (
      ECHO Could not remove directory "%SOURCE%\%%D".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Removed directory "%SOURCE%\%%D".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% Directory "%SOURCE%\%%D" does not exist.
    %_AECHO%.
  )
)

FOR %%E IN (%CLEANEXTS%) DO (
  IF EXIST "%SOURCE%\Releases\*.%%E" (
    %__ECHO% DEL /Q "%SOURCE%\Releases\*.%%E"

    IF ERRORLEVEL 1 (
      ECHO Could not delete "%SOURCE%\Releases\*.%%E".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Deleted "%SOURCE%\Releases\*.%%E".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% No files matching "%SOURCE%\Releases\*.%%E" exist.
    %_AECHO%.
  )
)

FOR %%C IN (%PLUGINDIRS%) DO (
  %_AECHO% Checking for plugin directories matching "%SOURCE%\%%C"...
  %_AECHO%.

  FOR /F "delims=" %%D IN ('DIR /B /S /AD "%SOURCE%\%%C" 2^> NUL') DO (
    %__ECHO% RMDIR /S /Q "%%D"

    IF ERRORLEVEL 1 (
      ECHO Could not remove directory "%%D".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Removed directory "%%D".
      %_AECHO%.
    )
  )
)

FOR %%C IN (%RELEASEDIRS%) DO (
  FOR %%E IN (%CLEANEXTS%) DO (
    IF EXIST "%SOURCE%\Releases\%%C\*.%%E" (
      %__ECHO% DEL /Q "%SOURCE%\Releases\%%C\*.%%E"

      IF ERRORLEVEL 1 (
        ECHO Could not delete "%SOURCE%\Releases\%%C\*.%%E".
        ECHO.
        GOTO errors
      ) ELSE (
        %_AECHO% Deleted "%SOURCE%\Releases\%%C\*.%%E".
        %_AECHO%.
      )
    ) ELSE (
      %_AECHO% No files matching "%SOURCE%\Releases\%%C\*.%%E" exist.
      %_AECHO%.
    )

    FOR %%D IN (%RELEASESUBDIRS%) DO (
      IF EXIST "%SOURCE%\Releases\%%C\%%D\*.%%E" (
        %__ECHO% DEL /Q "%SOURCE%\Releases\%%C\%%D\*.%%E"

        IF ERRORLEVEL 1 (
          ECHO Could not delete "%SOURCE%\Releases\%%C\%%D\*.%%E".
          ECHO.
          GOTO errors
        ) ELSE (
          %_AECHO% Deleted "%SOURCE%\Releases\%%C\%%D\*.%%E".
          %_AECHO%.
        )
      ) ELSE (
        %_AECHO% No files matching "%SOURCE%\Releases\%%C\%%D\*.%%E" exist.
        %_AECHO%.
      )
    )
  )

  IF EXIST "%TEMP%\Eagle_%%C" (
    %__ECHO% RMDIR /S /Q "%TEMP%\Eagle_%%C"

    IF ERRORLEVEL 1 (
      ECHO Could not remove directory "%TEMP%\Eagle_%%C".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Removed directory "%TEMP%\Eagle_%%C".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% Directory "%TEMP%\Eagle_%%C" does not exist.
    %_AECHO%.
  )
)

FOR %%C IN (%PACKAGETEADIRS%) DO (
  %_AECHO% Checking for package TEA directories matching "%SOURCE%\%%C*"...
  %_AECHO%.

  FOR /F "delims=" %%D IN ('DIR /B /S /AD "%SOURCE%\%%C*" 2^> NUL') DO (
    %__ECHO% RMDIR /S /Q "%%D"

    IF ERRORLEVEL 1 (
      ECHO Could not remove directory "%%D".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Removed directory "%%D".
      %_AECHO%.
    )
  )
)

SETLOCAL EnableDelayedExpansion
FOR %%F IN (%PACKAGETEAFILES%) DO (
  SET PATTERN=%%F
  SET PATTERN=!PATTERN:__=*!

  %_AECHO% Checking for package TEA files matching "%SOURCE%\Native\Package\src\win\tea\!PATTERN!"...
  %_AECHO%.

  IF EXIST "%SOURCE%\Native\Package\src\win\tea\!PATTERN!" (
    %__ECHO% DEL /Q "%SOURCE%\Native\Package\src\win\tea\!PATTERN!"

    IF ERRORLEVEL 1 (
      ECHO Could not delete "%SOURCE%\Native\Package\src\win\tea\!PATTERN!".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Deleted "%SOURCE%\Native\Package\src\win\tea\!PATTERN!".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% No files matching "%SOURCE%\Native\Package\src\win\tea\!PATTERN!" exist.
    %_AECHO%.
  )
)
ENDLOCAL

IF EXIST "%SOURCE%\*.VC.db" (
  REM
  REM NOTE: *WARNING* Deleting from the entire source tree.
  REM
  %__ECHO% DEL /S /Q "%SOURCE%\*.VC.db"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.VC.db".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.VC.db".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.VC.db" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\*.VC.opendb" (
  REM
  REM NOTE: *WARNING* Unhiding in the entire source tree.
  REM
  %__ECHO% ATTRIB -H "%SOURCE%\*.VC.opendb" /S

  IF ERRORLEVEL 1 (
    ECHO Could not make "%SOURCE%\*.VC.opendb" visible.
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Made "%SOURCE%\*.VC.opendb" visible.
    %_AECHO%.
  )

  REM
  REM NOTE: *WARNING* Deleting from the entire source tree.
  REM
  %__ECHO% DEL /S /Q "%SOURCE%\*.VC.opendb"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.VC.opendb".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.VC.opendb".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.VC.opendb" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\*.cache" (
  REM
  REM NOTE: *WARNING* Deleting from the entire source tree.
  REM
  %__ECHO% DEL /S /Q "%SOURCE%\*.cache"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.cache".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.cache".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.cache" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\*.csv" (
  %__ECHO% DEL /Q "%SOURCE%\*.csv"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.csv".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.csv".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.csv" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\*.ncb" (
  REM
  REM NOTE: *WARNING* Deleting from the entire source tree.
  REM
  %__ECHO% DEL /S /Q "%SOURCE%\*.ncb"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.ncb".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.ncb".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.ncb" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\*.psess" (
  %__ECHO% DEL /Q "%SOURCE%\*.psess"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.psess".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.psess".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.psess" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\*.sdf" (
  %__ECHO% DEL /Q "%SOURCE%\*.sdf"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.sdf".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.sdf".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.sdf" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\*.suo" (
  REM
  REM NOTE: *WARNING* Unhiding in the entire source tree.
  REM
  %__ECHO% ATTRIB -H "%SOURCE%\*.suo" /S

  IF ERRORLEVEL 1 (
    ECHO Could not make "%SOURCE%\*.suo" visible.
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Made "%SOURCE%\*.suo" visible.
    %_AECHO%.
  )

  REM
  REM NOTE: *WARNING* Deleting from the entire source tree.
  REM
  %__ECHO% DEL /S /Q "%SOURCE%\*.suo"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.suo".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.suo".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.suo" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\*.vsp" (
  %__ECHO% DEL /Q "%SOURCE%\*.vsp"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.vsp".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.vsp".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.vsp" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\*.vsps" (
  %__ECHO% DEL /Q "%SOURCE%\*.vsps"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\*.vsps".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\*.vsps".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\*.vsps" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\Installer\Tests\*.msi" (
  %__ECHO% DEL /Q "%SOURCE%\Installer\Tests\*.msi"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Installer\Tests\*.msi".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Installer\Tests\*.msi".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Installer\Tests\*.msi" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\Installer\Tests\*.wixobj" (
  %__ECHO% DEL /Q "%SOURCE%\Installer\Tests\*.wixobj"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Installer\Tests\*.wixobj".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Installer\Tests\*.wixobj".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Installer\Tests\*.wixobj" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\Installer\Tests\*.wixpdb" (
  %__ECHO% DEL /Q "%SOURCE%\Installer\Tests\*.wixpdb"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Installer\Tests\*.wixpdb".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Installer\Tests\*.wixpdb".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Installer\Tests\*.wixpdb" exist.
  %_AECHO%.
)

IF EXIST "%SOURCE%\Service\Web.config" (
  %__ECHO% DEL /Q "%SOURCE%\Service\Web.config"

  IF ERRORLEVEL 1 (
    ECHO Could not delete "%SOURCE%\Service\Web.config".
    ECHO.
    GOTO errors
  ) ELSE (
    %_AECHO% Deleted "%SOURCE%\Service\Web.config".
    %_AECHO%.
  )
) ELSE (
  %_AECHO% No files matching "%SOURCE%\Service\Web.config" exist.
  %_AECHO%.
)

IF DEFINED NOLKG GOTO clean_allTestLogs
IF NOT DEFINED LKG GOTO clean_allTestLogs

SET SRCLKGDIR=%LKG%\Eagle\bin
SET SRCLKGDIR=%SRCLKGDIR:\\=\%

%_VECHO% SrcLkgDir = '%SRCLKGDIR%'

IF NOT EXIST "%SRCLKGDIR%\EagleShell.exe" (
  ECHO The file "%SRCLKGDIR%\EagleShell.exe" does not exist.
  GOTO usage
)

%__ECHO% "%SRCLKGDIR%\EagleShell.exe" /? > NUL 2>&1

IF ERRORLEVEL 1 (
  ECHO The "%SRCLKGDIR%\EagleShell.exe" tool appears to be missing.
  GOTO usage
)

%_AECHO% Using LKG "EagleShell.exe" tool from "%SRCLKGDIR%"...

CALL :fn_AppendToPath SRCLKGDIR

%_VECHO% Path = '%PATH%'

REM
REM BUGFIX: Do not attempt to delete a log file that is currently in use by
REM         a running test suite as that can cause spurious failures of the
REM         test suite due to a missing test log start sentry.
REM
SETLOCAL EnableDelayedExpansion
FOR %%E IN (%TESTEXEFILES%) DO (
  SET PATTERN=%%E
  SET PATTERN=!PATTERN:__=*!

  FOR /F "delims=" %%F IN ('DIR /B /S "%TEMP%\!PATTERN!.test.*.log" 2^> NUL') DO (
    CALL :fn_UnsetVariable SHOULD_DELETE_FILE

    CALL :fn_CheckTestLogFile "!PATTERN!" "%%F"
    IF ERRORLEVEL 1 GOTO errors

    IF DEFINED SHOULD_DELETE_FILE (
      %__ECHO% DEL /Q "%%F"

      IF ERRORLEVEL 1 (
        ECHO Could not delete "%%F".
        ECHO.
        GOTO errors
      ) ELSE (
        %_AECHO% Deleted "%%F".
        %_AECHO%.
      )
    ) ELSE (
      %_AECHO% Skipping "%%F", in use by active test suite.
      %_AECHO%.
    )
  )
)
ENDLOCAL

GOTO skip_allTestLogs

:clean_allTestLogs

IF NOT DEFINED NOTESTLOGS (
  IF EXIST "%TEMP%\dotnet.exe.test.*.log" (
    %__ECHO% DEL /Q "%TEMP%\dotnet.exe.test.*.log"

    IF ERRORLEVEL 1 (
      ECHO Could not delete "%TEMP%\dotnet.exe.test.*.log".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Deleted "%TEMP%\dotnet.exe.test.*.log".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% No files matching "%TEMP%\dotnet.exe.test.*.log" exist.
    %_AECHO%.
  )
) ELSE (
  %_AECHO% Skipped deleting "%TEMP%\dotnet.exe.test.*.log".
  %_AECHO%.
)

IF NOT DEFINED NOTESTLOGS (
  IF EXIST "%TEMP%\EagleShell.dll.test.*.log" (
    %__ECHO% DEL /Q "%TEMP%\EagleShell.dll.test.*.log"

    IF ERRORLEVEL 1 (
      ECHO Could not delete "%TEMP%\EagleShell.dll.test.*.log".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Deleted "%TEMP%\EagleShell.dll.test.*.log".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% No files matching "%TEMP%\EagleShell.dll.test.*.log" exist.
    %_AECHO%.
  )
) ELSE (
  %_AECHO% Skipped deleting "%TEMP%\EagleShell.dll.test.*.log".
  %_AECHO%.
)

IF NOT DEFINED NOTESTLOGS (
  IF EXIST "%TEMP%\EagleShell.exe.test.*.log" (
    %__ECHO% DEL /Q "%TEMP%\EagleShell.exe.test.*.log"

    IF ERRORLEVEL 1 (
      ECHO Could not delete "%TEMP%\EagleShell.exe.test.*.log".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Deleted "%TEMP%\EagleShell.exe.test.*.log".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% No files matching "%TEMP%\EagleShell.exe.test.*.log" exist.
    %_AECHO%.
  )
) ELSE (
  %_AECHO% Skipped deleting "%TEMP%\EagleShell.exe.test.*.log".
  %_AECHO%.
)

IF NOT DEFINED NOTESTLOGS (
  IF EXIST "%TEMP%\EagleShell32.exe.test.*.log" (
    %__ECHO% DEL /Q "%TEMP%\EagleShell32.exe.test.*.log"

    IF ERRORLEVEL 1 (
      ECHO Could not delete "%TEMP%\EagleShell32.exe.test.*.log".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Deleted "%TEMP%\EagleShell32.exe.test.*.log".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% No files matching "%TEMP%\EagleShell32.exe.test.*.log" exist.
    %_AECHO%.
  )
) ELSE (
  %_AECHO% Skipped deleting "%TEMP%\EagleShell32.exe.test.*.log".
  %_AECHO%.
)

IF NOT DEFINED NOTESTLOGS (
  IF EXIST "%TEMP%\Featherlight.exe.test.*.log" (
    %__ECHO% DEL /Q "%TEMP%\Featherlight.exe.test.*.log"

    IF ERRORLEVEL 1 (
      ECHO Could not delete "%TEMP%\Featherlight.exe.test.*.log".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Deleted "%TEMP%\Featherlight.exe.test.*.log".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% No files matching "%TEMP%\Featherlight.exe.test.*.log" exist.
    %_AECHO%.
  )
) ELSE (
  %_AECHO% Skipped deleting "%TEMP%\Featherlight.exe.test.*.log".
  %_AECHO%.
)

IF NOT DEFINED NOTESTLOGS (
  IF EXIST "%TEMP%\mono.exe.test.*.log" (
    %__ECHO% DEL /Q "%TEMP%\mono.exe.test.*.log"

    IF ERRORLEVEL 1 (
      ECHO Could not delete "%TEMP%\mono.exe.test.*.log".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Deleted "%TEMP%\mono.exe.test.*.log".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% No files matching "%TEMP%\mono.exe.test.*.log" exist.
    %_AECHO%.
  )
) ELSE (
  %_AECHO% Skipped deleting "%TEMP%\mono.exe.test.*.log".
  %_AECHO%.
)

IF NOT DEFINED NOTESTLOGS (
  IF EXIST "%TEMP%\tclsh*.exe.test.*.log" (
    %__ECHO% DEL /Q "%TEMP%\tclsh*.exe.test.*.log"

    IF ERRORLEVEL 1 (
      ECHO Could not delete "%TEMP%\tclsh*.exe.test.*.log".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Deleted "%TEMP%\tclsh*.exe.test.*.log".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% No files matching "%TEMP%\tclsh*.exe.test.*.log" exist.
    %_AECHO%.
  )
) ELSE (
  %_AECHO% Skipped deleting "%TEMP%\tclsh*.exe.test.*.log".
  %_AECHO%.
)

:skip_allTestLogs

IF NOT DEFINED NOTRACELOGS (
  IF EXIST "%TEMP%\harpy-auto-trace-*.log" (
    %__ECHO% DEL /Q "%TEMP%\harpy-auto-trace-*.log"

    IF ERRORLEVEL 1 (
      ECHO Could not delete "%TEMP%\harpy-auto-trace-*.log".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Deleted "%TEMP%\harpy-auto-trace-*.log".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% No files matching "%TEMP%\harpy-auto-trace-*.log" exist.
    %_AECHO%.
  )
) ELSE (
  %_AECHO% Skipped deleting "%TEMP%\harpy-auto-trace-*.log".
  %_AECHO%.
)

IF NOT DEFINED NOBUILDLOGS (
  IF EXIST "%TEMP%\EagleBuild*.log" (
    %__ECHO% DEL /Q "%TEMP%\EagleBuild*.log"

    IF ERRORLEVEL 1 (
      ECHO Could not delete "%TEMP%\EagleBuild*.log".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Deleted "%TEMP%\EagleBuild*.log".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% No files matching "%TEMP%\EagleBuild*.log" exist.
    %_AECHO%.
  )
) ELSE (
  %_AECHO% Skipped deleting "%TEMP%\EagleBuild*.log".
  %_AECHO%.
)

REM
REM NOTE: If we are being called via the release preparation tool, we must skip
REM       deleting the log file associated with it because it will be locked.
REM
IF NOT DEFINED NOFLIGHT (
  IF EXIST "%TEMP%\EagleFlight*.log" (
    %__ECHO% DEL /Q "%TEMP%\EagleFlight*.log"

    IF ERRORLEVEL 1 (
      ECHO Could not delete "%TEMP%\EagleFlight*.log".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Deleted "%TEMP%\EagleFlight*.log".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% No files matching "%TEMP%\EagleFlight*.log" exist.
    %_AECHO%.
  )
) ELSE (
  %_AECHO% Skipped deleting "%TEMP%\EagleFlight*.log".
  %_AECHO%.
)

IF NOT DEFINED NOBUILDLOGS (
  IF EXIST "%TEMP%\Garuda*Build.*" (
    %__ECHO% DEL /Q "%TEMP%\Garuda*Build.*"

    IF ERRORLEVEL 1 (
      ECHO Could not delete "%TEMP%\Garuda*Build.*".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Deleted "%TEMP%\Garuda*Build.*".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% No files matching "%TEMP%\Garuda*Build.*" exist.
    %_AECHO%.
  )
) ELSE (
  %_AECHO% Skipped deleting "%TEMP%\Garuda*Build.*".
  %_AECHO%.
)

IF NOT DEFINED NOBUILDLOGS (
  IF EXIST "%TEMP%\Spilornis*Build.*" (
    %__ECHO% DEL /Q "%TEMP%\Spilornis*Build.*"

    IF ERRORLEVEL 1 (
      ECHO Could not delete "%TEMP%\Spilornis*Build.*".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Deleted "%TEMP%\Spilornis*Build.*".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% No files matching "%TEMP%\Spilornis*Build.*" exist.
    %_AECHO%.
  )
) ELSE (
  %_AECHO% Skipped deleting "%TEMP%\Spilornis*Build.*".
  %_AECHO%.
)

IF NOT DEFINED NOCSHARPFILES (
  IF EXIST "%TEMP%\*.cs" (
    %__ECHO% DEL /Q "%TEMP%\*.cs"

    IF ERRORLEVEL 1 (
      ECHO Could not delete "%TEMP%\*.cs".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Deleted "%TEMP%\*.cs".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% No files matching "%TEMP%\*.cs" exist.
    %_AECHO%.
  )
) ELSE (
  %_AECHO% Skipped deleting "%TEMP%\*.cs".
  %_AECHO%.
)

IF NOT DEFINED NONUGET (
  IF EXIST "%TEMP%\NuGetScratch" (
    %__ECHO% RMDIR /S /Q "%TEMP%\NuGetScratch"

    IF ERRORLEVEL 1 (
      ECHO Could not remove directory "%TEMP%\NuGetScratch".
      ECHO.
      GOTO errors
    ) ELSE (
      %_AECHO% Removed directory "%TEMP%\NuGetScratch".
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% Directory "%TEMP%\NuGetScratch" does not exist.
    %_AECHO%.
  )
) ELSE (
  %_AECHO% Skipped deleting "%TEMP%\NuGetScratch".
  %_AECHO%.
)

IF DEFINED USERNAME (
  IF NOT DEFINED NOCOVERITY (
    IF EXIST "%TEMP%\cov-%USERNAME%" (
      %__ECHO% RMDIR /S /Q "%TEMP%\cov-%USERNAME%"

      IF ERRORLEVEL 1 (
        ECHO Could not remove directory "%TEMP%\cov-%USERNAME%".
        ECHO.
        GOTO errors
      ) ELSE (
        %_AECHO% Removed directory "%TEMP%\cov-%USERNAME%".
        %_AECHO%.
      )
    ) ELSE (
      %_AECHO% Directory "%TEMP%\cov-%USERNAME%" does not exist.
      %_AECHO%.
    )
  ) ELSE (
    %_AECHO% Skipped deleting "%TEMP%\cov-%USERNAME%".
    %_AECHO%.
  )
)

GOTO no_errors

:fn_ResetErrorLevel
  VERIFY > NUL
  GOTO :EOF

:fn_SetErrorLevel
  VERIFY MAYBE 2> NUL
  GOTO :EOF

:fn_AppendToPath
  IF NOT DEFINED %1 GOTO :EOF
  SETLOCAL
  SET __ECHO_CMD=ECHO %%%1%%
  FOR /F "delims=" %%V IN ('%__ECHO_CMD%') DO (
    SET VALUE=%%V
  )
  SET VALUE=%VALUE:"=%
  REM "
  ENDLOCAL && SET PATH=%PATH%;%VALUE%
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

:fn_CheckTestLogFile
  SET EXEFILENAME=%1
  IF NOT DEFINED EXEFILENAME (
    ECHO Cannot check log file, missing EXE file name.
    CALL :fn_SetErrorLevel
    GOTO :EOF
  )
  SET LOGFILENAME=%2
  IF NOT DEFINED LOGFILENAME (
    ECHO Cannot check log file, missing LOG file name.
    CALL :fn_SetErrorLevel
    GOTO :EOF
  )
  CALL :fn_UnquoteVariable EXEFILENAME
  CALL :fn_UnquoteVariable LOGFILENAME
  REM
  REM HACK: Must use the "ne" operator here in the expression because of our
  REM       use of delayed expansion around the calling FOR loop.
  REM
  SET CHECK_TESTLOGFILE_CMD=EagleShell.exe -evaluate "if {[regexp -skip 1 -- [appendArgs {[/\\]} [string map [list . \\. * (?:.*?)?] {%EXEFILENAME%}] {\.test\.(\d+)\.log$}] {%LOGFILENAME%} pid] && [string is integer -strict $pid] && ([catch {object invoke -alias System.Diagnostics.Process GetProcessById $pid} process] ne 0 || [$process HasExited])} then {puts stdout 1}"
  IF NOT DEFINED __ECHO GOTO exec_checkTestLogFileCmd
  %__ECHO% %CHECK_TESTLOGFILE_CMD%
  GOTO :EOF
  :exec_checkTestLogFileCmd
  IF EXIST fn_CheckTestLogFile.err DEL /Q fn_CheckTestLogFile.err
  %_CECHO% %CHECK_TESTLOGFILE_CMD%
  FOR /F %%T IN ('%CHECK_TESTLOGFILE_CMD% ^|^| ECHO 1 ^>^> fn_CheckTestLogFile.err') DO (
    SET SHOULD_DELETE_FILE=%%T
  )
  IF EXIST fn_CheckTestLogFile.err (
    DEL /Q fn_CheckTestLogFile.err
    ECHO Failed to check log file.
    CALL :fn_SetErrorLevel
    GOTO :EOF
  )
  GOTO :EOF

:usage
  ECHO.
  ECHO Usage: %~nx0
  ECHO.
  ECHO The TEMP environment variable must be set to the full path of the existing
  ECHO directory used to store temporary files.
  GOTO errors

:errors
  CALL :fn_SetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Clean failure, errors were encountered.
  GOTO end_of_file

:no_errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  ECHO.
  ECHO Clean success, no errors were encountered.
  GOTO end_of_file

:end_of_file
%__ECHO% EXIT /B %ERRORLEVEL%
