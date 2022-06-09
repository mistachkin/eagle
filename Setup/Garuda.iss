;
; Garuda.iss --
;
; Extensible Adaptable Generalized Logic Engine (Eagle)
; Windows XP (or higher) Setup
;
; Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
;
; See the file "license.terms" for information on usage and redistribution of
; this file, and for a DISCLAIMER OF ALL WARRANTIES.
;
; RCS: @(#) $Id: $
;

[Setup]

#if Processor != "x86"
ArchitecturesAllowed={#Processor}
ArchitecturesInstallIn64BitMode={#Processor}
#endif

AllowNoIcons=yes
AlwaysShowComponentsList=no
AppCopyright=Copyright © 2007-2012 by Joe Mistachkin.  All rights reserved.
AppID={#AppId}
AppMutex=Garuda_Setup,Global\Garuda_Setup
AppName=Garuda (beta)
AppPublisher=Eagle Development Team
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
AppVerName=Garuda v{#AppMajorMinorVersion} beta ({#Platform} {#Configuration})
AppVersion={#AppFullVersion}
AppComments=Eagle Package for Tcl 8.4 or higher.
AppReadmeFile={app}\README.TXT
ChangesEnvironment=yes
DefaultDirName={code:GetActiveTclDir|lib\Garuda{#AppMajorMinorVersion}}
DefaultGroupName=Eagle\Garuda
LicenseFile=.\data\license.txt
OutputBaseFilename=GarudaSetup{#Suffix}{#AppFullVersion}
OutputDir=..\Releases\{#Platform}_{#Configuration}
SignedUninstallerDir=.\Output\Garuda
OutputManifestFile=GarudaSetup{#Suffix}{#AppFullVersion}-manifest.txt
SetupLogging=yes
SetupIconFile=..\Library\Resources\Eagle.ico
SignedUninstaller={#Signed}
UninstallDisplayIcon={app}\Garuda.dll,0
UninstallFilesDir={app}\uninstall
VersionInfoVersion={#AppFullVersion}
ExtraDiskSpaceRequired=5242880

[Code]
#include "Code.pas"

[Components]
Name: Application; Description: Garuda components.; Types: custom compact full
Name: Application\Core; Description: Core components.; Types: custom compact full
Name: Application\Core\{#Processor}; Description: Core {#Processor} components.; Types: custom compact full; ExtraDiskSpaceRequired: 27262976
Name: Application\Core\Library; Description: Core library components.; Types: custom compact full
Name: Application\Example; Description: Example components.; Types: custom full
Name: Application\Example\Scripts; Description: Example script components.; Types: custom full
Name: Application\Diagnostic; Description: Diagnostic components.; Types: custom full
Name: Application\Diagnostic\Symbols; Description: Debugging symbol components.; Types: custom full
Name: Application\Diagnostic\Tests; Description: Test suite components.; Types: custom full

[Files]
#if Processor == "x86"
Components: Application\Core\x86; Source: ..\Externals\MSVCPP\vcredist_x86_{#VcRuntimeX86}.exe; DestDir: {tmp}; Flags: dontcopy
#elif Processor == "x64"
Components: Application\Core\x64; Source: ..\Externals\MSVCPP\vcredist_x64_{#VcRuntimeX64}.exe; DestDir: {tmp}; Flags: dontcopy
#endif

Components: Application\Core\{#Processor}; Source: {#SrcBinDir}\Garuda.dll; DestDir: {app}; Flags: restartreplace uninsrestartdelete
Components: Application\Core\{#Processor} and Application\Diagnostic\Symbols; Source: {#SrcBinDir}\Garuda.pdb; DestDir: {app}; Flags: restartreplace uninsrestartdelete
Components: Application\Core\Library; Source: {#SrcLibDir}\*; DestDir: {app}; Flags: restartreplace uninsrestartdelete recursesubdirs createallsubdirs
Components: Application; Source: ..\license.terms; DestDir: {app}\doc; Flags: restartreplace uninsrestartdelete
Components: Application; Source: ..\Eagle.url; DestDir: {app}\doc; Flags: restartreplace uninsrestartdelete
Components: Application; Source: ..\Native\Package\README; DestDir: {app}\doc; DestName: README.TXT; Flags: restartreplace uninsrestartdelete isreadme
Components: Application\Example\Scripts; Source: ..\Native\Package\Scripts\*; DestDir: {app}\scripts; Flags: restartreplace uninsrestartdelete recursesubdirs createallsubdirs
Components: Application\Diagnostic\Tests; Source: ..\Native\Package\Tests\*; DestDir: {app}\tests; Flags: restartreplace uninsrestartdelete recursesubdirs createallsubdirs

[Registry]
Components: Application; Root: HKLM; SubKey: Software\Eagle; Flags: uninsdeletekeyifempty
Components: Application; Root: HKLM; SubKey: Software\Eagle\Garuda; Flags: uninsdeletekeyifempty
Components: Application; Root: HKLM; SubKey: Software\Eagle\Garuda\{#AppFullVersion}; Flags: uninsdeletekey
Components: Application; Root: HKLM; SubKey: Software\Eagle\Garuda\{#AppFullVersion}; ValueType: string; ValueName: AppId; ValueData: {#AppId}
Components: Application; Root: HKLM; SubKey: Software\Eagle\Garuda\{#AppFullVersion}; ValueType: string; ValueName: Path; ValueData: {app}

[Icons]
Name: {group}\License; Filename: {app}\doc\license.terms; WorkingDir: {app}\doc; Comment: View License; Flags: createonlyiffileexists
Name: {group}\Project Website; Filename: {app}\doc\Eagle.url; WorkingDir: {app}\doc; Comment: View Project Website; Flags: createonlyiffileexists
Name: {group}\README; Filename: {app}\doc\README.TXT; WorkingDir: {app}\doc; Comment: View README.TXT; Flags: createonlyiffileexists
