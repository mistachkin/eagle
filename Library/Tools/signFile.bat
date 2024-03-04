@ECHO OFF

::
:: signFile.bat --
::
:: Extensible Adaptable Generalized Logic Engine (Eagle)
:: File Signing Tool
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

SET FLAGS=/V /F /G /H /R /Y /Z

IF NOT DEFINED TIMESTAMP (
  SET TIMESTAMP=-t http://timestamp.verisign.com/scripts/timstamp.dll -tr 10 -tw 60
)

%_VECHO% TimeStamp = '%TIMESTAMP%'

IF NOT DEFINED RFC_TIMESTAMP1 (
  SET RFC_TIMESTAMP1=/tr http://timestamp.globalsign.com/ /td sha512
)

%_VECHO% RfcTimeStamp1 = '%RFC_TIMESTAMP1%'

IF NOT DEFINED RFC_TIMESTAMP2 (
  SET RFC_TIMESTAMP2=/tr http://timestamp.digicert.com/ /td sha1
)

%_VECHO% RfcTimeStamp2 = '%RFC_TIMESTAMP2%'

IF NOT DEFINED RFC_TIMESTAMP3 (
  SET RFC_TIMESTAMP3=/tr http://timestamp.digicert.com/ /td sha512
)

%_VECHO% RfcTimeStamp3 = '%RFC_TIMESTAMP3%'

IF NOT DEFINED PAUSE_MILLISECONDS (
  SET PAUSE_MILLISECONDS=15000
)

%_VECHO% PauseMilliseconds = '%PAUSE_MILLISECONDS%'

SET FILENAME=%1

IF DEFINED FILENAME (
  CALL :fn_UnquoteVariable FILENAME
) ELSE (
  GOTO usage
)

%_VECHO% FileName = '%FILENAME%'

SET PROJECT_NAME=%2

IF DEFINED PROJECT_NAME (
  CALL :fn_UnquoteVariable PROJECT_NAME
) ELSE (
  GOTO usage
)

%_VECHO% ProjectName = '%PROJECT_NAME%'

SET DUMMY2=%3

IF DEFINED DUMMY2 (
  GOTO usage
)

REM
REM HACK: Fixup any spaces and parenthesis in the file name ^(e.g. for
REM       the Inno Setup uninstaller, etc^) for use with the FOR loop.
REM
SET FORFILENAME=%FILENAME%
SET FORFILENAME=%FORFILENAME: =_%
SET FORFILENAME=%FORFILENAME:(=_%
SET FORFILENAME=%FORFILENAME:)=_%

%_VECHO% ForFileName = '%FORFILENAME%'

FOR /F %%E IN ('ECHO %FORFILENAME%') DO (SET FILEEXT=%%~xE)

%_VECHO% FileExt = '%FILEEXT%'

IF /I "%FILEEXT%" == ".rar" (
  REM
  REM NOTE: RAR files cannot be signed using SignCode / SignTool.
  REM
) ELSE IF /I "%FILEEXT%" == ".zip" (
  REM
  REM NOTE: ZIP files cannot be signed using SignCode / SignTool.
  REM
) ELSE IF /I "%FILEEXT%" == ".gz" (
  REM
  REM NOTE: GZIP files cannot be signed using SignCode / SignTool.
  REM
) ELSE (
  REM
  REM NOTE: Assume all other file extensions are executable files.
  REM
  GOTO skip_nonExecutable
)

REM
REM NOTE: For recognized compressed archive file extensions, skip using
REM       the SignCode / SignTool steps.
REM
SET NOSIGNCODE=1
SET NOSIGNTOOL1=1
SET NOSIGNTOOL2=1
SET NOSIGNTOOL3=1

:skip_nonExecutable

%_VECHO% NoSignCode = '%NOSIGNCODE%'
%_VECHO% NoSignTool1 = '%NOSIGNTOOL1%'
%_VECHO% NoSignTool2 = '%NOSIGNTOOL2%'
%_VECHO% NoSignTool3 = '%NOSIGNTOOL3%'

SET TOOLS=%~dp0
SET TOOLS=%TOOLS:~0,-1%

%_VECHO% Tools = '%TOOLS%'

IF DEFINED NOSIGNCODE GOTO no_signCode1

IF NOT DEFINED PVK_FILE (
  ECHO The PVK_FILE environment variable must be set first.
  GOTO usage
)

IF NOT EXIST "%PVK_FILE%" (
  ECHO The PVK_FILE file, "%PVK_FILE%", does not exist.

  IF DEFINED VIA_BUILD (
    GOTO usage
  ) ELSE (
    GOTO errors
  )
)

IF NOT DEFINED SPC_FILE (
  ECHO The SPC_FILE environment variable must be set first.
  GOTO usage
)

IF NOT EXIST "%SPC_FILE%" (
  ECHO The SPC_FILE file, "%SPC_FILE%", does not exist.

  IF DEFINED VIA_BUILD (
    GOTO usage
  ) ELSE (
    GOTO errors
  )
)

:no_signCode1

IF DEFINED NOSIGNCODE (
  IF DEFINED NOSIGNTOOL1 (
    IF DEFINED NOSIGNTOOL2 (
      IF DEFINED NOSIGNTOOL3 (
        GOTO skip_requireSignUrl
      )
    )
  )
)

IF NOT DEFINED SIGN_URL (
  ECHO The SIGN_URL environment variable must be set first.
  GOTO usage
)

:skip_requireSignUrl

IF DEFINED NOSIGNTOOL1 GOTO no_signTool1

IF NOT DEFINED PFX_FILE (
  ECHO The PFX_FILE environment variable must be set first.
  GOTO usage
)

IF NOT EXIST "%PFX_FILE%" (
  ECHO The PFX_FILE file, "%PFX_FILE%", does not exist.

  IF DEFINED VIA_BUILD (
    GOTO usage
  ) ELSE (
    GOTO errors
  )
)

IF NOT DEFINED PFX_PASSWORD (
  ECHO The PFX_PASSWORD environment variable must be set first.
  GOTO usage
)

:no_signTool1

IF DEFINED NOSIGNTOOL2 IF DEFINED NOSIGNTOOL3 GOTO no_signTool2

IF NOT DEFINED SUBJECT_NAME (
  ECHO The SUBJECT_NAME environment variable must be set first.
  GOTO usage
)

:no_signTool2

IF NOT DEFINED SIGN_WITH_GPG GOTO no_gpg1

IF NOT DEFINED GPG_PASSPHRASE_FILE (
  ECHO The GPG_PASSPHRASE_FILE environment variable must be set first.
  GOTO usage
)

IF NOT EXIST "%GPG_PASSPHRASE_FILE%" (
  ECHO The GPG_PASSPHRASE_FILE file, "%GPG_PASSPHRASE_FILE%", does not exist.

  IF DEFINED VIA_BUILD (
    GOTO usage
  ) ELSE (
    GOTO errors
  )
)

:no_gpg1

REM
REM HACK: Use GOTO here instead of IF/ELSE due to possible parenthesis
REM       within the PVK and SPC file environment variables.
REM
IF DEFINED NOSIGNCODE GOTO no_signCode2
%_VECHO% PvkFile = '%PVK_FILE%'
%_VECHO% SpcFile = '%SPC_FILE%'
%_VECHO% TimeStamp = '%TIMESTAMP%'
:no_signCode2

IF DEFINED NOSIGNCODE (
  IF DEFINED NOSIGNTOOL1 (
    IF DEFINED NOSIGNTOOL2 (
      IF DEFINED NOSIGNTOOL3 (
        GOTO skip_showCommon
      )
    )
  )
)

%_VECHO% SignUrl = '%SIGN_URL%'
%_VECHO% PauseMilliseconds = '%PAUSE_MILLISECONDS%'

:skip_showCommon

REM
REM HACK: Use GOTO here instead of IF/ELSE due to possible parenthesis
REM       within the PFX password environment variable.
REM
IF DEFINED NOSIGNTOOL1 GOTO no_signTool3

%_VECHO% PfxFile = '%PFX_FILE%'
%_VECHO% PfxPassword = '%PFX_PASSWORD%'
%_VECHO% RfcTimeStamp1 = '%RFC_TIMESTAMP1%'

:no_signTool3

REM
REM HACK: Use GOTO here instead of IF/ELSE due to possible parenthesis
REM       within the subject name environment variable.
REM
IF DEFINED NOSIGNTOOL2 IF DEFINED NOSIGNTOOL3 GOTO no_signTool4

%_VECHO% SubjectName = '%SUBJECT_NAME%'
%_VECHO% RfcTimeStamp2 = '%RFC_TIMESTAMP2%'
%_VECHO% RfcTimeStamp3 = '%RFC_TIMESTAMP3%'

:no_signTool4

REM
REM HACK: Use GOTO here instead of IF/ELSE due to possible parenthesis
REM       within the passphrase file environment variable.
REM
IF NOT DEFINED SIGN_WITH_GPG GOTO no_gpg2
%_VECHO% GpgPassphraseFile = '%GPG_PASSPHRASE_FILE%'
:no_gpg2

%_VECHO% EagleSignCodeDir = '%EAGLESIGNCODEDIR%'

IF DEFINED EAGLESIGNCODEDIR (
  CALL :fn_PrependToPath EAGLESIGNCODEDIR
)

%_VECHO% EagleSignToolDir = '%EAGLESIGNTOOLDIR%'

IF DEFINED EAGLESIGNTOOLDIR (
  CALL :fn_PrependToPath EAGLESIGNTOOLDIR
)

IF DEFINED NOSIGNCODE GOTO skip_signCodePath

FOR %%T IN (SignCode.exe) DO (
  SET %%T_PATH=%%~dp$PATH:T
)

CALL :fn_ResetErrorLevel
CALL :fn_CheckVariable SignCode.exe_PATH

IF ERRORLEVEL 1 (
  ECHO The executable "SignCode.exe" is required to be in the PATH.
  GOTO errors
)

:skip_signCodePath

%_VECHO% SignCodeExePath = '%SignCode.exe_PATH%'

IF DEFINED NOSIGNTOOL1 IF DEFINED NOSIGNTOOL2 (
  GOTO skip_signToolPath
)

FOR %%T IN (SignTool.exe) DO (
  SET %%T_PATH=%%~dp$PATH:T
)

CALL :fn_ResetErrorLevel
CALL :fn_CheckVariable SignTool.exe_PATH

IF ERRORLEVEL 1 (
  ECHO The executable "SignTool.exe" is required to be in the PATH.
  GOTO errors
)

:skip_signToolPath

%_VECHO% SignToolExePath = '%SignTool.exe_PATH%'

IF NOT DEFINED SIGN_WITH_GPG GOTO skip_gpgPath

FOR %%T IN (gpg2.exe) DO (
  SET %%T_PATH=%%~dp$PATH:T
)

CALL :fn_ResetErrorLevel
CALL :fn_CheckVariable gpg2.exe_PATH

IF ERRORLEVEL 1 (
  ECHO The executable "gpg2.exe" is required to be in the PATH.
  GOTO errors
)

:skip_gpgPath

%_VECHO% GpgExePath = '%gpg2.exe_PATH%'

CALL :fn_ResetErrorLevel

IF DEFINED NOSIGNCODE GOTO skip_signCode1

%_AECHO% Signing file "%FILENAME%" with "SignCode.exe"...
%_CECHO% SignCode.exe -a sha1 -n "%PROJECT_NAME%" -i "%SIGN_URL%" -v "%PVK_FILE%" -spc "%SPC_FILE%" %TIMESTAMP% "%FILENAME%"
%__ECHO% SignCode.exe -a sha1 -n "%PROJECT_NAME%" -i "%SIGN_URL%" -v "%PVK_FILE%" -spc "%SPC_FILE%" %TIMESTAMP% "%FILENAME%"

CALL :fn_CheckErrorLevel

IF ERRORLEVEL 1 (
  ECHO Failed to sign file "%FILENAME%" with "SignCode.exe".
  GOTO errors
)

%_CECHO% "%TOOLS%\JustWait.exe" "%PAUSE_MILLISECONDS%"
%__ECHO% "%TOOLS%\JustWait.exe" "%PAUSE_MILLISECONDS%"

IF ERRORLEVEL 1 (
  ECHO Failed to wait "%PAUSE_MILLISECONDS%" milliseconds.
  GOTO errors
)

:skip_signCode1

IF DEFINED NOSIGNTOOL1 GOTO skip_signTool1

%_AECHO% Signing file "%FILENAME%" with "SignTool.exe"...
%_CECHO% SignTool.exe sign /as /v /d "%PROJECT_NAME%" /du "%SIGN_URL%" /f "%PFX_FILE%" /p "%PFX_PASSWORD%" /fd sha512 %RFC_TIMESTAMP1% "%FILENAME%"
%__ECHO% SignTool.exe sign /as /v /d "%PROJECT_NAME%" /du "%SIGN_URL%" /f "%PFX_FILE%" /p "%PFX_PASSWORD%" /fd sha512 %RFC_TIMESTAMP1% "%FILENAME%"

CALL :fn_CheckErrorLevel

IF ERRORLEVEL 1 (
  ECHO Failed to sign file "%FILENAME%" with "SignTool.exe" and primary parameters.
  GOTO errors
)

%_CECHO% "%TOOLS%\JustWait.exe" "%PAUSE_MILLISECONDS%"
%__ECHO% "%TOOLS%\JustWait.exe" "%PAUSE_MILLISECONDS%"

IF ERRORLEVEL 1 (
  ECHO Failed to wait "%PAUSE_MILLISECONDS%" milliseconds.
  GOTO errors
)

:skip_signTool1

IF DEFINED NOSIGNTOOL2 IF DEFINED NOSIGNTOOL3 GOTO skip_signTool2

IF NOT DEFINED NOSIGNTOOL2 (
  %_AECHO% Signing file "%FILENAME%" with "SignTool.exe" using SHA1...
  %_CECHO% SignTool.exe sign /as /v /d "%PROJECT_NAME%" /du "%SIGN_URL%" /n "%SUBJECT_NAME%" /fd sha1 %RFC_TIMESTAMP2% "%FILENAME%"
  %__ECHO% SignTool.exe sign /as /v /d "%PROJECT_NAME%" /du "%SIGN_URL%" /n "%SUBJECT_NAME%" /fd sha1 %RFC_TIMESTAMP2% "%FILENAME%"

  CALL :fn_CheckErrorLevel

  IF ERRORLEVEL 1 (
    ECHO Failed to sign file "%FILENAME%" with "SignTool.exe" and secondary parameters.
    GOTO errors
  )

  %_CECHO% "%TOOLS%\JustWait.exe" "%PAUSE_MILLISECONDS%"
  %__ECHO% "%TOOLS%\JustWait.exe" "%PAUSE_MILLISECONDS%"

  IF ERRORLEVEL 1 (
    ECHO Failed to wait "%PAUSE_MILLISECONDS%" milliseconds.
    GOTO errors
  )
)

IF NOT DEFINED NOSIGNTOOL3 (
  %_AECHO% Signing file "%FILENAME%" with "SignTool.exe" using SHA512...
  %_CECHO% SignTool.exe sign /as /v /d "%PROJECT_NAME%" /du "%SIGN_URL%" /n "%SUBJECT_NAME%" /fd sha512 %RFC_TIMESTAMP3% "%FILENAME%"
  %__ECHO% SignTool.exe sign /as /v /d "%PROJECT_NAME%" /du "%SIGN_URL%" /n "%SUBJECT_NAME%" /fd sha512 %RFC_TIMESTAMP3% "%FILENAME%"

  CALL :fn_CheckErrorLevel

  IF ERRORLEVEL 1 (
    ECHO Failed to sign file "%FILENAME%" with "SignTool.exe" and tertiary parameters.
    GOTO errors
  )

  %_CECHO% "%TOOLS%\JustWait.exe" "%PAUSE_MILLISECONDS%"
  %__ECHO% "%TOOLS%\JustWait.exe" "%PAUSE_MILLISECONDS%"

  IF ERRORLEVEL 1 (
    ECHO Failed to wait "%PAUSE_MILLISECONDS%" milliseconds.
    GOTO errors
  )
)

:skip_signTool2

IF DEFINED NOSIGNTOOL1 IF DEFINED NOSIGNTOOL2 IF DEFINED NOSIGNTOOL3 (
  GOTO skip_signToolVerify
)

%_AECHO% Verifying file "%FILENAME%" with "SignTool.exe"...
%_CECHO% SignTool.exe verify /pa /all /v "%FILENAME%"
%__ECHO% SignTool.exe verify /pa /all /v "%FILENAME%"

CALL :fn_CheckErrorLevel

IF ERRORLEVEL 1 (
  ECHO Failed to verify file "%FILENAME%" with "SignTool.exe".
  GOTO errors
)

:skip_signToolVerify

IF NOT DEFINED SIGN_WITH_GPG GOTO skip_gpg1

%_AECHO% Signing file "%FILENAME%" with "gpg2.exe"...
%_CECHO% gpg2.exe --batch --yes --detach-sign --comment "%PROJECT_NAME%" --armor --passphrase-file "%GPG_PASSPHRASE_FILE%" "%FILENAME%"
%__ECHO% gpg2.exe --batch --yes --detach-sign --comment "%PROJECT_NAME%" --armor --passphrase-file "%GPG_PASSPHRASE_FILE%" "%FILENAME%"

CALL :fn_CheckErrorLevel

IF ERRORLEVEL 1 (
  ECHO Failed to sign file "%FILENAME%" with "gpg2.exe".
  GOTO errors
)

%_AECHO% Verifying file "%FILENAME%" with "gpg2.exe"...
%_CECHO% gpg2.exe --verify "%FILENAME%.asc"
%__ECHO% gpg2.exe --verify "%FILENAME%.asc"

CALL :fn_CheckErrorLevel

IF ERRORLEVEL 1 (
  ECHO Failed to verify file "%FILENAME%" with "gpg2.exe".
  GOTO errors
)

:skip_gpg1

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

:fn_CheckVariable
  IF NOT DEFINED %1 (
    CALL :fn_SetErrorLevel
  )
  GOTO :EOF

:fn_CheckErrorLevel
  IF %ERRORLEVEL% NEQ 0 (
    CALL :fn_SetErrorLevel
  )
  GOTO :EOF

:usage
  IF DEFINED VIA_BUILD (
    GOTO no_errors
  ) ELSE (
    ECHO.
    ECHO Usage: %~nx0 ^<fileName^> ^<projectName^>
    ECHO.
    ECHO The SIGN_URL environment variable should be set to the URL containing further
    ECHO information about the file to be signed ^(e.g. the URL for the company web site^).
    ECHO.
    ECHO When using the "SignCode.exe" tool, the following applies as well:
    ECHO.
    ECHO The PVK_FILE environment variable must be set to the full path and file name of
    ECHO an existing private key file.
    ECHO.
    ECHO The SPC_FILE environment variable must be set to the full path and file name of
    ECHO an existing certificate file.
    ECHO.
    ECHO When using the "SignTool.exe" tool, the following applies as well:
    ECHO.
    ECHO The PFX_FILE environment variable must be set to the full path and file name of
    ECHO an existing personal information exchange file, if applicable.
    ECHO.
    ECHO The PFX_PASSWORD environment variable must be set to the password to be used
    ECHO with the file specified by the PFX_FILE environment variable, if any.
    ECHO.
    ECHO The SUBJECT_NAME environment variable must be set to the subject name of the
    ECHO secondary certificate, if applicable.
    ECHO.
    ECHO When using the "gpg2.exe" tool, the following applies as well:
    ECHO.
    ECHO The GPG_PASSPHRASE_FILE environment variable must be set to the full path and
    ECHO file name of the GPG passphrase file, if applicable.
    GOTO errors
  )

:errors
  CALL :fn_SetErrorLevel
  ENDLOCAL
  IF NOT DEFINED VIA_BUILD (
    ECHO.
    ECHO File signing failure, errors were encountered.
  )
  GOTO end_of_file

:no_errors
  CALL :fn_ResetErrorLevel
  ENDLOCAL
  IF NOT DEFINED VIA_BUILD (
    ECHO.
    ECHO File signing success, no errors were encountered.
  )
  GOTO end_of_file

:end_of_file
%__ECHO% EXIT /B %ERRORLEVEL%
