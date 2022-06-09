;
; Eagle.iss --
;
; Extensible Adaptable Generalized Logic Engine (Eagle)
; .NET Framework 2.0 (or higher) on Windows XP (or higher) Setup
;
; Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
;
; See the file "license.terms" for information on usage and redistribution of
; this file, and for a DISCLAIMER OF ALL WARRANTIES.
;
; RCS: @(#) $Id: $
;

[Setup]
ArchitecturesAllowed=x86 x64
ArchitecturesInstallIn64BitMode=x64
AllowNoIcons=yes
AlwaysShowComponentsList=no
AppCopyright=Copyright © 2007-2012 by Joe Mistachkin.  All rights reserved.
AppID={#AppId}
AppMutex=Eagle_Setup,Global\Eagle_Setup
AppName=Eagle (beta)
AppPublisher=Eagle Development Team
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
AppVerName=Eagle v{#AppMajorMinorVersion} beta ({#Configuration})
AppVersion={#AppFullVersion}
AppComments=An implementation of the Tcl scripting language for the CLR.
AppReadmeFile={app}\doc\README.TXT
ChangesAssociations=yes
ChangesEnvironment=yes
DefaultDirName={pf}\Eagle
DefaultGroupName=Eagle
LicenseFile=.\data\license.txt
OutputBaseFilename=EagleSetup{#Suffix}{#AppFullVersion}
OutputDir=..\Releases\{#Configuration}
SignedUninstallerDir=.\Output\Eagle
OutputManifestFile=EagleSetup{#Suffix}{#AppFullVersion}-manifest.txt
SetupLogging=yes
SetupIconFile=..\Library\Resources\Eagle.ico
SignedUninstaller={#Signed}
UninstallDisplayIcon={app}\doc\Eagle.ico,0
UninstallFilesDir={app}\uninstall
VersionInfoVersion={#AppFullVersion}
UsedUserAreasWarning=no
ExtraDiskSpaceRequired=5242880

[Code]
#include "Code.pas"

[Components]
Name: Application; Description: Eagle components.; Types: custom compact full
Name: Application\Core; Description: Core components.; Types: custom compact full
Name: Application\Core\Engine; Description: Script engine components.; Types: custom compact full

#if Int(Native)
Name: Application\Core\Engine\Native; Description: Script engine native components.; Types: custom compact full; ExtraDiskSpaceRequired: 10485760
#endif

Name: Application\Core\Library; Description: Script library components.; Types: custom compact full
Name: Application\Core\Update; Description: Script engine update components.; Types: custom compact full

#if Int(Security)
Name: Application\Plugins; Description: Plugin components.; Types: custom compact full
Name: Application\Plugins\Security; Description: Security plugin components.; Types: custom compact full
#endif

Name: Application\Interactive; Description: Interactive components.; Types: custom compact full
Name: Application\Interactive\Shell; Description: Interactive shell components.; Types: custom compact full
Name: Application\Interactive\Association; Description: File association registry components.; Types: custom full
Name: Application\Interactive\Association\User; Description: For the current user only.; Types: custom full; Flags: exclusive
Name: Application\Interactive\Association\Machine; Description: Shared by all users.; Types: custom full; Flags: exclusive
Name: Application\Interactive\Registration; Description: Windows application registration registry components.; Types: custom full
Name: Application\Interactive\Registration\User; Description: For the current user only.; Types: custom full; Flags: exclusive
Name: Application\Interactive\Registration\Machine; Description: Shared by all users.; Types: custom full; Flags: exclusive
Name: Application\Interactive\Path; Description: Executable path registry components.; Types: custom full
Name: Application\Interactive\Path\User; Description: For the current user only.; Types: custom full; Flags: exclusive
Name: Application\Interactive\Path\Machine; Description: Shared by all users.; Types: custom full; Flags: exclusive
Name: Application\Integration; Description: Integration components.; Types: custom full
Name: Application\Integration\Build; Description: MSBuild task components.; Types: custom full
Name: Application\Integration\Management; Description: PowerShell snap-in components.; Types: custom full
Name: Application\Integration\Installer; Description: WiX extension components.; Types: custom full

#if Int(Native) && Int(Tcl)
Name: Application\Integration\Native; Description: Native Tcl/Tk components.; Types: custom full
#endif

Name: Application\Diagnostic; Description: Diagnostic components.; Types: custom full
Name: Application\Diagnostic\Symbols; Description: Debugging symbol components.; Types: custom full
Name: Application\Diagnostic\Tests; Description: Test suite components.; Types: custom full

[Tasks]
Components: Application\Core\Engine; Name: Update; Description: Enable automatic update checks.; Flags: checkedonce
Components: Application\Core\Engine; Name: GAC; Description: Install the assemblies into the global assembly cache.; Flags: unchecked; Check: CheckIsNetFx2Setup() or CheckIsNetFx4Setup()
Components: Application\Core\Engine; Name: NGEN; Description: Generate native images for the assemblies and install the images in the native image cache.; Check: CheckIsNetFx2Setup() or CheckIsNetFx4Setup()
Components: Application\Core\Engine and Application\Integration\Management; Name: SnapIn; Description: Install the snap-in for use with PowerShell.; Check: CheckIsNetFx2Setup() and CheckForPowerShell('')

[Run]
Components: Application\Core\Engine; Tasks: GAC; Filename: {code:GetNetFx2SdkInstallRoot|bin\Gacutil.exe}; Parameters: "/nologo /if ""{app}\bin\Eagle.dll"""; Flags: skipifdoesntexist; Check: CheckIsNetFx2Setup()
Components: Application\Core\Engine; Tasks: NGEN; Filename: {code:GetNetFx2InstallRoot|Ngen.exe}; Parameters: "install ""{app}\bin\Eagle.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx2Setup()
Components: Application\Core\Engine; Tasks: GAC; Filename: {code:GetNetFx4SdkInstallRoot|Gacutil.exe}; Parameters: "/nologo /if ""{app}\bin\Eagle.dll"""; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
Components: Application\Core\Engine; Tasks: NGEN; Filename: {code:GetNetFx4InstallRoot|Ngen.exe}; Parameters: "install ""{app}\bin\Eagle.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
Components: Application\Core\Engine and Application\Integration\Management; Tasks: SnapIn; Filename: {code:GetNetFx2InstallRoot|InstallUtil.exe}; Parameters: "/LogFile="""" ""{app}\bin\EagleCmdlets.dll"""; Flags: skipifdoesntexist; Check: CheckIsNetFx2Setup() and CheckForPowerShell('')

[UninstallRun]
Components: Application\Core\Engine and Application\Integration\Management; Tasks: SnapIn; Filename: {code:GetNetFx2InstallRoot|InstallUtil.exe}; Parameters: "/uninstall /LogFile="""" ""{app}\bin\EagleCmdlets.dll"""; Flags: skipifdoesntexist; Check: CheckIsNetFx2Setup() and CheckForPowerShell('')
Components: Application\Core\Engine; Tasks: NGEN; Filename: {code:GetNetFx4InstallRoot|Ngen.exe}; Parameters: "uninstall ""{app}\bin\Eagle.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
Components: Application\Core\Engine; Tasks: GAC; Filename: {code:GetNetFx4SdkInstallRoot|Gacutil.exe}; Parameters: "/nologo /uf Eagle"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
Components: Application\Core\Engine; Tasks: NGEN; Filename: {code:GetNetFx2InstallRoot|Ngen.exe}; Parameters: "uninstall ""{app}\bin\Eagle.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx2Setup()
Components: Application\Core\Engine; Tasks: GAC; Filename: {code:GetNetFx2SdkInstallRoot|bin\Gacutil.exe}; Parameters: "/nologo /uf Eagle"; Flags: skipifdoesntexist; Check: CheckIsNetFx2Setup()

[Files]
Components: Application; Source: ..\license.terms; DestDir: {app}\doc; Flags: restartreplace uninsrestartdelete
Components: Application; Source: ..\Eagle.url; DestDir: {app}\doc; Flags: restartreplace uninsrestartdelete
Components: Application; Source: ..\Library\Resources\Eagle.ico; DestDir: {app}\doc; Flags: restartreplace uninsrestartdelete
Components: Application; Source: ..\README; DestDir: {app}\doc; DestName: README.TXT; Flags: restartreplace uninsrestartdelete isreadme
Components: Application\Core\Engine; Source: {#SrcBinDir}\Eagle.dll; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete

#if Int(Security)
Components: Application\Plugins\Security; Source: {#BinLibDir}\Harpy.Basic{#AppMajorMinorVersion}\Harpy.Basic.dll; DestDir: {app}\lib\Harpy.Basic{#AppMajorMinorVersion}; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
Components: Application\Plugins\Security; Source: {#BinLibDir}\Harpy.Basic{#AppMajorMinorVersion}\pkgIndex.eagle; DestDir: {app}\lib\Harpy.Basic{#AppMajorMinorVersion}; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
Components: Application\Plugins\Security; Source: {#BinLibDir}\Harpy.Basic{#AppMajorMinorVersion}\pkgIndex.eagle.harpy; DestDir: {app}\lib\Harpy.Basic{#AppMajorMinorVersion}; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
Components: Application\Plugins\Security; Source: {#BinLibDir}\Harpy.Basic{#AppMajorMinorVersion}\keyRing.General.demo.eagle; DestDir: {app}\lib\Harpy.Basic{#AppMajorMinorVersion}; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
Components: Application\Plugins\Security; Source: {#BinLibDir}\Harpy.Basic{#AppMajorMinorVersion}\keyRing.General.demo.eagle.harpy; DestDir: {app}\lib\Harpy.Basic{#AppMajorMinorVersion}; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
Components: Application\Plugins\Security; Source: {#BinLibDir}\Harpy.Basic{#AppMajorMinorVersion}\keyRing.zero.eagle; DestDir: {app}\lib\Harpy.Basic{#AppMajorMinorVersion}; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
Components: Application\Plugins\Security; Source: {#BinLibDir}\Harpy.Basic{#AppMajorMinorVersion}\keyRing.zero.eagle.harpy; DestDir: {app}\lib\Harpy.Basic{#AppMajorMinorVersion}; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
Components: Application\Plugins\Security; Source: {#BinLibDir}\Badge.Basic{#AppMajorMinorVersion}\Badge.Basic.dll; DestDir: {app}\lib\Badge.Basic{#AppMajorMinorVersion}; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
Components: Application\Plugins\Security; Source: {#BinLibDir}\Badge.Basic{#AppMajorMinorVersion}\pkgIndex.eagle; DestDir: {app}\lib\Badge.Basic{#AppMajorMinorVersion}; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
Components: Application\Plugins\Security; Source: {#BinLibDir}\Badge.Basic{#AppMajorMinorVersion}\pkgIndex.eagle.harpy; DestDir: {app}\lib\Badge.Basic{#AppMajorMinorVersion}; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
#endif

#if Int(Native)
Components: Application\Core\Engine\Native; Source: ..\Externals\MSVCPP\vcredist_x86_{#VcRuntimeX86}.exe; DestDir: {tmp}; Flags: dontcopy
Components: Application\Core\Engine\Native; Source: ..\Externals\MSVCPP\vcredist_x64_{#VcRuntimeX64}.exe; DestDir: {tmp}; Flags: dontcopy

#if Int(IncludeArm)
Components: Application\Core\Engine\Native; Source: ..\Externals\MSVCPP\vcredist_ARM_{#VcRuntimeArm}.exe; DestDir: {app}\setup\ARM; Flags: restartreplace uninsrestartdelete
#endif

Components: Application\Core\Engine\Native; Source: {#SrcBinDir}\x86\Spilornis.dll; DestDir: {app}\bin\x86; Flags: restartreplace uninsrestartdelete
Components: Application\Core\Engine\Native and Application\Diagnostic\Symbols; Source: {#SrcBinDir}\x86\Spilornis.pdb; DestDir: {app}\bin\x86; Flags: restartreplace uninsrestartdelete
Components: Application\Core\Engine\Native; Source: {#SrcBinDir}\x64\Spilornis.dll; DestDir: {app}\bin\x64; Flags: restartreplace uninsrestartdelete
Components: Application\Core\Engine\Native and Application\Diagnostic\Symbols; Source: {#SrcBinDir}\x64\Spilornis.pdb; DestDir: {app}\bin\x64; Flags: restartreplace uninsrestartdelete

#if Int(IncludeArm)
Components: Application\Core\Engine\Native; Source: {#SrcBinDir}\ARM\Spilornis.dll; DestDir: {app}\bin\ARM; Flags: restartreplace uninsrestartdelete
Components: Application\Core\Engine\Native and Application\Diagnostic\Symbols; Source: {#SrcBinDir}\ARM\Spilornis.pdb; DestDir: {app}\bin\ARM; Flags: restartreplace uninsrestartdelete
#endif
#endif

Components: Application\Core\Engine and Application\Diagnostic\Symbols; Source: {#SrcBinDir}\Eagle.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete

#if Int(Security)
Components: Application\Plugins\Security and Application\Diagnostic\Symbols; Source: {#BinLibDir}\Harpy.Basic{#AppMajorMinorVersion}\Harpy.Basic.pdb; DestDir: {app}\lib\Harpy.Basic{#AppMajorMinorVersion}; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
Components: Application\Plugins\Security and Application\Diagnostic\Symbols; Source: {#BinLibDir}\Badge.Basic{#AppMajorMinorVersion}\Badge.Basic.pdb; DestDir: {app}\lib\Badge.Basic{#AppMajorMinorVersion}; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
#endif

Components: Application\Core\Update; Source: {#SrcBinDir}\Hippogriff.exe; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Core\Update and Application\Diagnostic\Symbols; Source: {#SrcBinDir}\Hippogriff.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Core\Library; Source: {#SrcLibDir}\Eagle{#AppMajorMinorVersion}\*; DestDir: {app}\lib\Eagle{#AppMajorMinorVersion}; Flags: restartreplace uninsrestartdelete recursesubdirs createallsubdirs
Components: Application\Core\Library; Source: {#SrcLibDir}\Test{#AppMajorMinorVersion}\*; DestDir: {app}\lib\Test{#AppMajorMinorVersion}; Flags: restartreplace uninsrestartdelete recursesubdirs createallsubdirs
Components: Application\Interactive\Shell; Source: {#SrcBinDir}\EagleShell.exe; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Interactive\Shell; Source: {#SrcBinDir}\EagleShell32.exe; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Interactive\Shell and Application\Diagnostic\Symbols; Source: {#SrcBinDir}\EagleShell.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Integration\Build; Source: {#SrcBinDir}\EagleTasks.dll; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Integration\Build and Application\Diagnostic\Symbols; Source: {#SrcBinDir}\EagleTasks.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Integration\Build; Source: {#SrcBinDir}\Eagle.tasks; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Integration\Management; Source: {#SrcBinDir}\EagleCmdlets.dll; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Integration\Management and Application\Diagnostic\Symbols; Source: {#SrcBinDir}\EagleCmdlets.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Integration\Installer; Source: {#SrcBinDir}\EagleExtensions.dll; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Integration\Installer and Application\Diagnostic\Symbols; Source: {#SrcBinDir}\EagleExtensions.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete

#if Int(Native) && Int(Tcl)
Components: Application\Integration\Native; Source: ..\Externals\Tcl\bin\x86\*.exe; DestDir: {app}\lib\Tcl\bin\x86; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
Components: Application\Integration\Native; Source: ..\Externals\Tcl\lib\x86\*.dll; DestDir: {app}\lib\Tcl\lib\x86; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
Components: Application\Integration\Native; Source: ..\Externals\Tcl\bin\x64\*.exe; DestDir: {app}\lib\Tcl\bin\x64; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
Components: Application\Integration\Native; Source: ..\Externals\Tcl\lib\x64\*.dll; DestDir: {app}\lib\Tcl\lib\x64; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete

#if Int(IncludeArm)
Components: Application\Integration\Native; Source: ..\Externals\Tcl\bin\ARM\*.exe; DestDir: {app}\lib\Tcl\bin\ARM; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
Components: Application\Integration\Native; Source: ..\Externals\Tcl\lib\ARM\*.dll; DestDir: {app}\lib\Tcl\lib\ARM; Flags: skipifsourcedoesntexist restartreplace uninsrestartdelete
#endif
#endif

Components: Application\Diagnostic\Tests; Source: {#SrcTestDir}\*; Excludes: *.cs; DestDir: {app}\tests; Flags: restartreplace uninsrestartdelete recursesubdirs createallsubdirs

[Registry]
Components: Application; Root: HKLM32; SubKey: Software\Eagle; Flags: uninsdeletekeyifempty
Components: Application; Root: HKLM32; SubKey: Software\Eagle\{#AppFullVersion}; Flags: uninsdeletekey
Components: Application; Root: HKLM32; SubKey: Software\Eagle\{#AppFullVersion}; ValueType: string; ValueName: AppId; ValueData: {#AppId}
Components: Application; Root: HKLM32; SubKey: Software\Eagle\{#AppFullVersion}; ValueType: string; ValueName: Path; ValueData: {app}
Components: Application; Root: HKLM32; SubKey: Software\Eagle\{#AppFullVersion}; ValueType: string; ValueName: Assembly; ValueData: {#Assembly}
Components: Application; Root: HKLM32; SubKey: Software\Eagle\{#AppFullVersion}; ValueType: string; ValueName: CompileInfo; ValueData: {#CompileInfo}
Components: Application; Tasks: Update; Root: HKLM32; SubKey: Software\Eagle\{#AppMajorMinorVersion}; Flags: uninsdeletekey
Components: Application; Tasks: Update; Root: HKLM32; SubKey: Software\Eagle\{#AppMajorMinorVersion}; ValueName: CheckForUpdates; Flags: deletevalue
Components: Application; Tasks: Update; Root: HKLM32; SubKey: Software\Eagle\{#AppMajorMinorVersion}; ValueType: string; ValueName: CheckCoreUpdates; ValueData: True
Components: Application; Tasks: Update; Root: HKLM32; SubKey: Software\Eagle\{#AppMajorMinorVersion}\Low; ValueName: CheckForUpdates; Flags: deletevalue
Components: Application; Tasks: Update; Root: HKLM32; SubKey: Software\Eagle\{#AppMajorMinorVersion}\Low; ValueType: string; ValueName: CheckCoreUpdates; ValueData: True; Permissions: users-modify
Components: Application\Interactive\Shell and Application\Interactive\Association\User; Root: HKCU32; SubKey: Software\Classes\.eagle; ValueType: string; ValueData: Eagle.Script; Flags: uninsdeletekey
Components: Application\Interactive\Shell and Application\Interactive\Association\User; Root: HKCU32; SubKey: Software\Classes\Eagle.Script; Flags: uninsdeletekey
Components: Application\Interactive\Shell and Application\Interactive\Association\User; Root: HKCU32; SubKey: Software\Classes\Eagle.Script\DefaultIcon; ValueType: string; ValueData: {app}\bin\EagleShell.exe,0
Components: Application\Interactive\Shell and Application\Interactive\Association\User; Root: HKCU32; SubKey: Software\Classes\Eagle.Script\shell\Open\command; ValueType: expandsz; ValueData: """{app}\bin\EagleShell.exe"" -safe -file ""%1"" %*"
Components: Application\Interactive\Shell and Application\Interactive\Association\Machine; Root: HKLM32; SubKey: Software\Classes\.eagle; ValueType: string; ValueData: Eagle.Script; Flags: uninsdeletekey
Components: Application\Interactive\Shell and Application\Interactive\Association\Machine; Root: HKLM32; SubKey: Software\Classes\Eagle.Script; Flags: uninsdeletekey
Components: Application\Interactive\Shell and Application\Interactive\Association\Machine; Root: HKLM32; SubKey: Software\Classes\Eagle.Script\DefaultIcon; ValueType: string; ValueData: {app}\bin\EagleShell.exe,0
Components: Application\Interactive\Shell and Application\Interactive\Association\Machine; Root: HKLM32; SubKey: Software\Classes\Eagle.Script\shell\Open\command; ValueType: expandsz; ValueData: """{app}\bin\EagleShell.exe"" -safe -file ""%1"" %*"
Components: Application\Interactive\Shell and Application\Interactive\Registration\User; Root: HKCU32; SubKey: Software\Microsoft\Windows\CurrentVersion\App Paths\EagleShell.exe; ValueType: expandsz; ValueData: {app}\bin\EagleShell.exe; Flags: uninsdeletekey
Components: Application\Interactive\Shell and Application\Interactive\Registration\User; Root: HKCU32; SubKey: Software\Microsoft\Windows\CurrentVersion\App Paths\EagleShell.exe; ValueType: expandsz; ValueName: Path; ValueData: {app}\bin\; Flags: uninsdeletekey
Components: Application\Interactive\Shell and Application\Interactive\Registration\Machine; Root: HKLM32; SubKey: Software\Microsoft\Windows\CurrentVersion\App Paths\EagleShell.exe; ValueType: expandsz; ValueData: {app}\bin\EagleShell.exe; Flags: uninsdeletekey
Components: Application\Interactive\Shell and Application\Interactive\Registration\Machine; Root: HKLM32; SubKey: Software\Microsoft\Windows\CurrentVersion\App Paths\EagleShell.exe; ValueType: expandsz; ValueName: Path; ValueData: {app}\bin\; Flags: uninsdeletekey
Components: Application; Root: HKLM64; SubKey: Software\Eagle; Flags: uninsdeletekeyifempty; Check: IsWin64()
Components: Application; Root: HKLM64; SubKey: Software\Eagle\{#AppFullVersion}; Flags: uninsdeletekey; Check: IsWin64()
Components: Application; Root: HKLM64; SubKey: Software\Eagle\{#AppFullVersion}; ValueType: string; ValueName: AppId; ValueData: {#AppId}; Check: IsWin64()
Components: Application; Root: HKLM64; SubKey: Software\Eagle\{#AppFullVersion}; ValueType: string; ValueName: Path; ValueData: {app}; Check: IsWin64()
Components: Application; Root: HKLM64; SubKey: Software\Eagle\{#AppFullVersion}; ValueType: string; ValueName: Assembly; ValueData: {#Assembly}; Check: IsWin64()
Components: Application; Root: HKLM64; SubKey: Software\Eagle\{#AppFullVersion}; ValueType: string; ValueName: CompileInfo; ValueData: {#CompileInfo}; Check: IsWin64()
Components: Application; Tasks: Update; Root: HKLM64; SubKey: Software\Eagle\{#AppMajorMinorVersion}; Flags: uninsdeletekey; Check: IsWin64()
Components: Application; Tasks: Update; Root: HKLM64; SubKey: Software\Eagle\{#AppMajorMinorVersion}; ValueName: CheckForUpdates; Check: IsWin64(); Flags: deletevalue
Components: Application; Tasks: Update; Root: HKLM64; SubKey: Software\Eagle\{#AppMajorMinorVersion}; ValueType: string; ValueName: CheckCoreUpdates; ValueData: True; Check: IsWin64()
Components: Application; Tasks: Update; Root: HKLM64; SubKey: Software\Eagle\{#AppMajorMinorVersion}\Low; ValueName: CheckForUpdates; Check: IsWin64(); Flags: deletevalue
Components: Application; Tasks: Update; Root: HKLM64; SubKey: Software\Eagle\{#AppMajorMinorVersion}\Low; ValueType: string; ValueName: CheckCoreUpdates; ValueData: True; Permissions: users-modify; Check: IsWin64()
Components: Application\Interactive\Shell and Application\Interactive\Association\User; Root: HKCU64; SubKey: Software\Classes\.eagle; ValueType: string; ValueData: Eagle.Script; Flags: uninsdeletekey; Check: IsWin64()
Components: Application\Interactive\Shell and Application\Interactive\Association\User; Root: HKCU64; SubKey: Software\Classes\Eagle.Script; Flags: uninsdeletekey; Check: IsWin64()
Components: Application\Interactive\Shell and Application\Interactive\Association\User; Root: HKCU64; SubKey: Software\Classes\Eagle.Script\DefaultIcon; ValueType: string; ValueData: {app}\bin\EagleShell.exe,0; Check: IsWin64()
Components: Application\Interactive\Shell and Application\Interactive\Association\User; Root: HKCU64; SubKey: Software\Classes\Eagle.Script\shell\Open\command; ValueType: expandsz; ValueData: """{app}\bin\EagleShell.exe"" -safe -file ""%1"" %*"; Check: IsWin64()
Components: Application\Interactive\Shell and Application\Interactive\Association\Machine; Root: HKLM64; SubKey: Software\Classes\.eagle; ValueType: string; ValueData: Eagle.Script; Flags: uninsdeletekey; Check: IsWin64()
Components: Application\Interactive\Shell and Application\Interactive\Association\Machine; Root: HKLM64; SubKey: Software\Classes\Eagle.Script; Flags: uninsdeletekey; Check: IsWin64()
Components: Application\Interactive\Shell and Application\Interactive\Association\Machine; Root: HKLM64; SubKey: Software\Classes\Eagle.Script\DefaultIcon; ValueType: string; ValueData: {app}\bin\EagleShell.exe,0; Check: IsWin64()
Components: Application\Interactive\Shell and Application\Interactive\Association\Machine; Root: HKLM64; SubKey: Software\Classes\Eagle.Script\shell\Open\command; ValueType: expandsz; ValueData: """{app}\bin\EagleShell.exe"" -safe -file ""%1"" %*"; Check: IsWin64()
Components: Application\Interactive\Shell and Application\Interactive\Registration\User; Root: HKCU64; SubKey: Software\Microsoft\Windows\CurrentVersion\App Paths\EagleShell.exe; ValueType: expandsz; ValueData: {app}\bin\EagleShell.exe; Flags: uninsdeletekey; Check: IsWin64()
Components: Application\Interactive\Shell and Application\Interactive\Registration\User; Root: HKCU64; SubKey: Software\Microsoft\Windows\CurrentVersion\App Paths\EagleShell.exe; ValueType: expandsz; ValueName: Path; ValueData: {app}\bin\; Flags: uninsdeletekey; Check: IsWin64()
Components: Application\Interactive\Shell and Application\Interactive\Registration\Machine; Root: HKLM64; SubKey: Software\Microsoft\Windows\CurrentVersion\App Paths\EagleShell.exe; ValueType: expandsz; ValueData: {app}\bin\EagleShell.exe; Flags: uninsdeletekey; Check: IsWin64()
Components: Application\Interactive\Shell and Application\Interactive\Registration\Machine; Root: HKLM64; SubKey: Software\Microsoft\Windows\CurrentVersion\App Paths\EagleShell.exe; ValueType: expandsz; ValueName: Path; ValueData: {app}\bin\; Flags: uninsdeletekey; Check: IsWin64()
Components: Application\Interactive\Shell and Application\Interactive\Path\User; Root: HKCU; SubKey: Environment; ValueType: expandsz; ValueName: PATH; ValueData: "{olddata};{app}\bin"; Flags: preservestringtype; Check: not CheckIsAppBinInUserPath()
Components: Application\Interactive\Shell and Application\Interactive\Path\Machine; Root: HKLM; SubKey: System\CurrentControlSet\Control\Session Manager\Environment; ValueType: expandsz; ValueName: PATH; ValueData: "{olddata};{app}\bin"; Flags: preservestringtype; Check: not CheckIsAppBinInSystemPath()

[Icons]
Name: {group}\Eagle Shell; Filename: {app}\bin\EagleShell.exe; WorkingDir: {app}\bin; IconFilename: {app}\bin\EagleShell.exe; Comment: Launch Eagle Shell; IconIndex: 0; Flags: createonlyiffileexists
Name: {group}\Eagle Shell (32-bit); Filename: {app}\bin\EagleShell32.exe; WorkingDir: {app}\bin; IconFilename: {app}\bin\EagleShell32.exe; Comment: Launch Eagle Shell (32-bit); IconIndex: 0; Flags: createonlyiffileexists
Name: {group}\Eagle Updater; Filename: {app}\bin\Hippogriff.exe; WorkingDir: {app}\bin; IconFilename: {app}\bin\Hippogriff.exe; Comment: Launch Eagle Updater; IconIndex: 0; Flags: createonlyiffileexists
Name: {group}\License; Filename: {app}\doc\license.terms; WorkingDir: {app}\doc; Comment: View License; Flags: createonlyiffileexists
Name: {group}\Project Website; Filename: {app}\doc\Eagle.url; WorkingDir: {app}\doc; Comment: View Project Website; Flags: createonlyiffileexists
Name: {group}\README; Filename: {app}\doc\README.TXT; WorkingDir: {app}\doc; Comment: View README.TXT; Flags: createonlyiffileexists
