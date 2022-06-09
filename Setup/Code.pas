{
  Code.pas --

  Extensible Adaptable Generalized Logic Engine (Eagle)
  Common Setup Functions

  Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.

  See the file "license.terms" for information on usage and redistribution of
  this file, and for a DISCLAIMER OF ALL WARRANTIES.

  RCS: @(#) $Id: $
}

var
  IsNetFx2Setup: Boolean;
  IsNetFx4Setup: Boolean;
  NeedVcRuntimes: Boolean;
  NeedActiveTcl: Boolean;
  NeedEagle: Boolean;
  NeedEagleFail: Boolean;

  UserEnvironmentSubKeyName: String;
  SystemEnvironmentSubKeyName: String;

  NetFxSubKeyName: String;
  NetFxInstallRoot: String;
  NetFxSetupSubKeyName: String;
  NetFxIsInstalled: String;

  NetFx2Version: String;
  NetFx2SetupVersion: String;
  NetFx2HasServicePack: String;
  NetFx2ServicePack: Integer;
  NetFx2SdkSubKeyName: String;
  NetFx2SdkInstallRoot: String;
  NetFx2ErrorMessage: String;

  NetFx4Version: String;
  NetFx4SetupVersion: String;
  NetFx4HasServicePack: String;
  NetFx4ServicePack: Integer;
  NetFx4SdkSubKeyName: String;
  NetFx4SdkInstallRoot: String;
  NetFx4ErrorMessage: String;

  PowerShellSubKeyName: String;
  PowerShellIsInstalled: String;
  PowerShellHasProductId: String;

  PowerShellInstance: String;
  PowerShellProductId: String;

  VcRuntimeRedistributableX86: String;
  VcRuntimeRedistributableX64: String;
  VcRuntimeRedistributableARM: String;

  ActiveTclSubKeyName: String;
  ActiveTclInstallRoot: String;
  ActiveTclErrorMessage: String;

  EagleSubKeyName: String;
  EagleInstallRoot: String;
  EagleErrorMessage: String;

function TrimSlash(const Path: String): String;
var
  LastCharacter: String;
begin
  Result := Path;

  if Result <> '' then
  begin
    LastCharacter := Copy(Result, Length(Result), 1);

    if (LastCharacter = '\') or (LastCharacter = '/') then
    begin
      Result := Copy(Result, 1, Length(Result) - 1);
    end;
  end;
end;

function CheckIsNetFx2Setup(): Boolean;
begin
  Result := IsNetFx2Setup;
end;

function CheckIsNetFx4Setup(): Boolean;
begin
  Result := IsNetFx4Setup;
end;

function CheckIsAppBinInUserPath(): Boolean;
var
  Path: String;
  AppBinDir: String;
begin
  Result := False;

  if RegQueryStringValue(HKEY_CURRENT_USER, UserEnvironmentSubKeyName,
      'PATH', Path) then
  begin
    Path := Lowercase(Path);
    AppBinDir := Lowercase(ExpandConstant('{app}\bin'));

    if Pos(';' + AppBinDir + ';', ';' + Path + ';') <> 0 then
    begin
      Result := True;
    end;
  end;
end;

function CheckIsAppBinInSystemPath(): Boolean;
var
  Path: String;
  AppBinDir: String;
begin
  Result := False;

  if RegQueryStringValue(HKEY_LOCAL_MACHINE, SystemEnvironmentSubKeyName,
      'PATH', Path) then
  begin
    Path := Lowercase(Path);
    AppBinDir := Lowercase(ExpandConstant('{app}\bin'));

    if Pos(';' + AppBinDir + ';', ';' + Path + ';') <> 0 then
    begin
      Result := True;
    end;
  end;
end;

function CheckNeedVcRuntimes(): Boolean;
begin
  Result := NeedVcRuntimes;
end;

function CheckNeedActiveTcl(): Boolean;
begin
  Result := NeedActiveTcl;
end;

function CheckNeedEagle(): Boolean;
begin
  Result := NeedEagle;
end;

function CheckNeedEagleFail(): Boolean;
begin
  Result := NeedEagleFail;
end;

function GetNetFx2InstallRoot(const FileName: String): String;
var
  InstallRoot: String;
begin
  Result := '';

  if RegQueryStringValue(HKEY_LOCAL_MACHINE, NetFxSubKeyName,
      NetFxInstallRoot, InstallRoot) then
  begin
    Result := TrimSlash(InstallRoot) + '\' + NetFx2Version;

    if FileName <> '' then
    begin
      Result := TrimSlash(Result) + '\' + FileName;
    end;
  end;
end;

function GetNetFx2SdkInstallRoot(const FileName: String): String;
var
  InstallRoot: String;
begin
  Result := '';

  if RegQueryStringValue(HKEY_LOCAL_MACHINE, NetFx2SdkSubKeyName,
      NetFx2SdkInstallRoot, InstallRoot) then
  begin
    Result := InstallRoot;

    if FileName <> '' then
    begin
      Result := TrimSlash(Result) + '\' + FileName;
    end;
  end;
end;

function CheckForNetFx2(const NeedServicePack: Integer): Boolean;
var
  SubKeyName: String;
  IsInstalled: Cardinal;
  HasServicePack: Cardinal;
begin
  Result := False;

  SubKeyName := NetFxSetupSubKeyName + '\' + NetFx2SetupVersion;

  if RegQueryDWordValue(HKEY_LOCAL_MACHINE, SubKeyName,
      NetFxIsInstalled, IsInstalled) then
  begin
    if IsInstalled <> 0 then
    begin
      if RegQueryDWordValue(HKEY_LOCAL_MACHINE, SubKeyName,
          NetFx2HasServicePack, HasServicePack) then
      begin
        if HasServicePack >= NeedServicePack then
        begin
          Result := True;
        end;
      end;
    end;
  end;
end;

function GetNetFx4InstallRoot(const FileName: String): String;
var
  InstallRoot: String;
begin
  Result := '';

  if RegQueryStringValue(HKEY_LOCAL_MACHINE, NetFxSubKeyName,
      NetFxInstallRoot, InstallRoot) then
  begin
    Result := TrimSlash(InstallRoot) + '\' + NetFx4Version;

    if FileName <> '' then
    begin
      Result := TrimSlash(Result) + '\' + FileName;
    end;
  end;
end;

function GetNetFx4SdkInstallRoot(const FileName: String): String;
var
  InstallRoot: String;
begin
  Result := '';

  if RegQueryStringValue(HKEY_LOCAL_MACHINE, NetFx4SdkSubKeyName,
      NetFx4SdkInstallRoot, InstallRoot) then
  begin
    Result := InstallRoot;

    if FileName <> '' then
    begin
      Result := TrimSlash(Result) + '\' + FileName;
    end;
  end;
end;

function CheckForNetFx4(const NeedServicePack: Integer): Boolean;
var
  SubKeyName: String;
  IsInstalled: Cardinal;
  HasServicePack: Cardinal;
begin
  Result := False;

  SubKeyName := NetFxSetupSubKeyName + '\' + NetFx4SetupVersion;

  if RegQueryDWordValue(HKEY_LOCAL_MACHINE, SubKeyName,
      NetFxIsInstalled, IsInstalled) then
  begin
    if IsInstalled <> 0 then
    begin
      if RegQueryDWordValue(HKEY_LOCAL_MACHINE, SubKeyName,
          NetFx4HasServicePack, HasServicePack) then
      begin
        if HasServicePack >= NeedServicePack then
        begin
          Result := True;
        end;
      end;
    end;
  end;
end;

function GetActiveTclDir(const FileName: String): String;
var
  SubKeyNames: TArrayOfString;
  Index: Integer;
  InstallRoot: String;
begin
  Result := '';

  if RegGetSubkeyNames(HKEY_LOCAL_MACHINE, ActiveTclSubKeyName,
      SubKeyNames) then
  begin
    for Index := GetArrayLength(SubKeyNames) - 1 downto 0 do begin
      if RegQueryStringValue(HKEY_LOCAL_MACHINE, ActiveTclSubKeyName + '\' +
          SubKeyNames[Index], '', InstallRoot) then
      begin
        if InstallRoot <> '' then
        begin
          if DirExists(InstallRoot) then
          begin
            Result := InstallRoot;

            if FileName <> '' then
            begin
              Result := TrimSlash(Result) + '\' + FileName;
            end;

            exit;
          end;
        end;
      end;
    end;
  end;
end;

function CheckForActiveTcl(var InstallRoot: String): Boolean;
begin
  InstallRoot := GetActiveTclDir('');

  if InstallRoot <> '' then
  begin
    if DirExists(InstallRoot) then
    begin
      Result := True;
    end
    else begin
      Result := False;
    end;
  end
  else begin
    Result := False;
  end;
end;

function GetEagleDir(const FileName: String): String;
var
  SubKeyNames: TArrayOfString;
  Index: Integer;
  InstallRoot: String;
begin
  Result := '';

  if RegGetSubkeyNames(HKEY_LOCAL_MACHINE, EagleSubKeyName,
      SubKeyNames) then
  begin
    for Index := GetArrayLength(SubKeyNames) - 1 downto 0 do begin
      if RegQueryStringValue(HKEY_LOCAL_MACHINE, EagleSubKeyName + '\' +
          SubKeyNames[Index], '', InstallRoot) then
      begin
        if InstallRoot <> '' then
        begin
          if DirExists(InstallRoot) then
          begin
            Result := InstallRoot;

            if FileName <> '' then
            begin
              Result := TrimSlash(Result) + '\' + FileName;
            end;

            exit;
          end;
        end;
      end;
    end;
  end;
end;

function CheckForEagle(var InstallRoot: String): Boolean;
begin
  InstallRoot := GetEagleDir('');

  if InstallRoot <> '' then
  begin
    if DirExists(InstallRoot) then
    begin
      Result := True;
    end
    else begin
      Result := False;
    end;
  end
  else begin
    Result := False;
  end;
end;

function CheckForPowerShell(const NeedProductId: String): Boolean;
var
  SubKeyName: String;
  IsInstalled: Cardinal;
  ProductId: String;
begin
  Result := False;

  SubKeyName := PowerShellSubKeyName + '\' + PowerShellInstance;

  if RegQueryDWordValue(HKEY_LOCAL_MACHINE, SubKeyName,
      PowerShellIsInstalled, IsInstalled) then
  begin
    if IsInstalled <> 0 then
    begin
      if RegQueryStringValue(HKEY_LOCAL_MACHINE, SubKeyName,
          PowerShellHasProductId, ProductId) then
      begin
        if NeedProductId = '' then
        begin
          NeedProductId := PowerShellProductId;
        end;

        if ProductId = NeedProductId then
        begin
          Result := True;
        end;
      end;
    end;
  end;
end;

function ExtractAndInstallVcRuntimes(
    var VcRuntime: String;
    var ResultCode: Integer): Boolean;
begin
  ExtractTemporaryFile(VcRuntimeRedistributableX86);

  if Exec(ExpandConstant(
      '{tmp}\' + VcRuntimeRedistributableX86),
      '/q', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
  begin
    Result := True;
  end
  else begin
    VcRuntime := VcRuntimeRedistributableX86;
    Result := False;
  end;

  if Result and Is64BitInstallMode then
  begin
    ExtractTemporaryFile(VcRuntimeRedistributableX64);

    if Exec(ExpandConstant(
        '{tmp}\' + VcRuntimeRedistributableX64),
        '/q', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
    begin
      Result := True;
    end
    else begin
      VcRuntime := VcRuntimeRedistributableX64;
      Result := False;
    end;
  end;
end;

function RemoveAppBinFromUserPath(): Boolean;
var
  Path: String;
  LowerPath: String;
  AppBinDir: String;
  Index: Integer;
begin
  Result := False;

  if RegQueryStringValue(HKEY_CURRENT_USER, UserEnvironmentSubKeyName,
      'PATH', Path) then
  begin
    LowerPath := Lowercase(Path);
    AppBinDir := Lowercase(ExpandConstant('{app}\bin'));
    Index := Pos(';' + AppBinDir + ';', ';' + LowerPath + ';');

    if Index <> 0 then
    begin
      if Index > 1 then Index := Index - 1;
      Delete(Path, Index, Length(AppBinDir) + 1);

      if RegWriteStringValue(HKEY_CURRENT_USER, UserEnvironmentSubKeyName,
          'PATH', Path) then
      begin
        Result := True;
      end;
    end;
  end;
end;

function RemoveAppBinFromSystemPath(): Boolean;
var
  Path: String;
  LowerPath: String;
  AppBinDir: String;
  Index: Integer;
begin
  Result := False;

  if RegQueryStringValue(HKEY_LOCAL_MACHINE, SystemEnvironmentSubKeyName,
      'PATH', Path) then
  begin
    LowerPath := Lowercase(Path);
    AppBinDir := Lowercase(ExpandConstant('{app}\bin'));
    Index := Pos(';' + AppBinDir + ';', ';' + LowerPath + ';');

    if Index <> 0 then
    begin
      if Index > 1 then Index := Index - 1;
      Delete(Path, Index, Length(AppBinDir) + 1);

      if RegWriteStringValue(HKEY_LOCAL_MACHINE, SystemEnvironmentSubKeyName,
          'PATH', Path) then
      begin
        Result := True;
      end;
    end;
  end;
end;

procedure InitializeConstants();
begin
  IsNetFx2Setup := {#IsNetFx2};
  IsNetFx4Setup := {#IsNetFx4};
  NeedVcRuntimes := {#NeedVcRuntimes};
  NeedActiveTcl := {#NeedActiveTcl};
  NeedEagle := {#NeedEagle};
  NeedEagleFail := False;

  UserEnvironmentSubKeyName := 'Environment';
  SystemEnvironmentSubKeyName :=
      'System\CurrentControlSet\Control\Session Manager\Environment';

  NetFxSubKeyName := 'Software\Microsoft\.NETFramework';
  NetFxInstallRoot := 'InstallRoot';
  NetFxSetupSubKeyName := 'Software\Microsoft\NET Framework Setup\NDP';
  NetFxIsInstalled := 'Install';

  NetFx2Version := 'v2.0.50727';
  NetFx2SetupVersion := 'v2.0.50727';
  NetFx2HasServicePack := 'SP';
  NetFx2ServicePack := 2;
  NetFx2SdkSubKeyName := NetFxSubKeyName
  NetFx2SdkInstallRoot := 'sdkInstallRootv2.0';
  NetFx2ErrorMessage := 'The Microsoft .NET Framework v2.0 with Service Pack '
      + IntToStr(NetFx2ServicePack) + ' or higher is required.';

  NetFx4Version := 'v4.0.30319';
  NetFx4SetupVersion := 'v4\Full';
  NetFx4HasServicePack := 'Servicing';
  NetFx4ServicePack := 0;
  NetFx4SdkSubKeyName :=
      'Software\Microsoft\Microsoft SDKs\Windows\v7.0A\WinSDK-NetFx40Tools';

  NetFx4SdkInstallRoot := 'InstallationFolder';
  NetFx4ErrorMessage := 'The Microsoft .NET Framework v4.0 with Service Pack '
      + IntToStr(NetFx4ServicePack) + ' or higher is required.';

  PowerShellSubKeyName := 'Software\Microsoft\PowerShell';
  PowerShellIsInstalled := 'Install';
  PowerShellHasProductId := 'PID';

  PowerShellInstance := '1';
  PowerShellProductId := '89383-100-0001260-04309';

  VcRuntimeRedistributableX86 := 'vcredist_x86_{#VcRuntimeX86}.exe';
  VcRuntimeRedistributableX64 := 'vcredist_x64_{#VcRuntimeX64}.exe';
  VcRuntimeRedistributableARM := 'vcredist_ARM_{#VcRuntimeArm}.exe';

  ActiveTclSubKeyName := 'Software\ActiveState\ActiveTcl';
  ActiveTclErrorMessage :=
      'The ActiveTcl distribution of Tcl/Tk v8.4 or higher is required.';

  EagleSubKeyName := 'Software\Eagle';
  EagleErrorMessage := 'Eagle v1.0 or higher is required.';
end;

function InitializeSetup(): Boolean;
var
  VcRuntime: String;
  ResultCode: Integer;
begin
  InitializeConstants();

  Result := True;

  if Result and CheckIsNetFx2Setup() then
  begin
    Result := CheckForNetFx2(NetFx2ServicePack);

    if not Result then
    begin
      MsgBox(NetFx2ErrorMessage, mbError, MB_OK);
    end;
  end;

  if Result and CheckIsNetFx4Setup() then
  begin
    Result := CheckForNetFx4(NetFx4ServicePack);

    if not Result then
    begin
      MsgBox(NetFx4ErrorMessage, mbError, MB_OK);
    end;
  end;

  if Result and CheckNeedActiveTcl() then
  begin
    Result := CheckForActiveTcl(ActiveTclInstallRoot);

    if not Result then
    begin
      MsgBox(ActiveTclErrorMessage, mbError, MB_OK);
    end;
  end;

  if Result and CheckNeedEagle() then
  begin
    Result := CheckForEagle(EagleInstallRoot);

    if not Result then
    begin
      if not CheckNeedEagleFail() then
      begin
        Result := True;
      end;

      MsgBox(EagleErrorMessage, mbError, MB_OK);
    end;
  end;

  if Result and CheckNeedVcRuntimes() then
  begin
    Result := ExtractAndInstallVcRuntimes(VcRuntime, ResultCode);

    if not Result or ((ResultCode <> 0) and (ResultCode <> 1638)
        and (ResultCode <> 3010) and (ResultCode <> 5100)) then
    begin
      MsgBox('Failed to install Microsoft Visual C++ Runtime: ' +
          VcRuntime + ', ' + SysErrorMessage(ResultCode), mbError,
          MB_OK);

      if Result then
      begin
        Result := False;
      end;
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    InitializeConstants();
    RemoveAppBinFromUserPath();
    RemoveAppBinFromSystemPath();
  end;
end;
