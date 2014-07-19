{
  CheckForNetFx.iss --

  Written by Joe Mistachkin.
  Released to the public domain, use at your own risk!
}

var
  IsNetFx2Setup : Boolean;
  IsNetFx4Setup : Boolean;

  NetFxSubKeyName: String;
  NetFxInstallRoot: String;
  NetFxSetupSubKeyName: String;
  NetFxIsInstalled: String;

  NetFx2Version: String;
  NetFx2SetupVersion: String;
  NetFx2HasServicePack: String;
  NetFx2ServicePack: Integer;
  NetFx2ErrorMessage: String;

  NetFx35SetupVersion: String;
  NetFx35HasServicePack: String;
  NetFx35ServicePack: Integer;
  NetFx35ErrorMessage: String;

  NetFx4Version: String;
  NetFx4SetupVersion: String;
  NetFx4HasServicePack: String;
  NetFx4ServicePack: Integer;
  NetFx4ErrorMessage: String;

  VcRuntimeRedistributable: String;

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

function CheckForNetFx2(const NeedServicePack: Integer): Boolean;
var
  SubKeyName: String;
  IsInstalled: Cardinal;
  HasServicePack: Cardinal;
begin
  Result := False;

  SubKeyName := NetFxSetupSubKeyName + '\' + NetFx2SetupVersion;

  if RegQueryDWordValue(HKEY_LOCAL_MACHINE, SubKeyName, NetFxIsInstalled,
      IsInstalled) then
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

function CheckForNetFx35(const NeedServicePack: Integer): Boolean;
var
  SubKeyName: String;
  IsInstalled: Cardinal;
  HasServicePack: Cardinal;
begin
  Result := False;

  SubKeyName := NetFxSetupSubKeyName + '\' + NetFx35SetupVersion;

  if RegQueryDWordValue(HKEY_LOCAL_MACHINE, SubKeyName, NetFxIsInstalled,
      IsInstalled) then
  begin
    if IsInstalled <> 0 then
    begin
      if RegQueryDWordValue(HKEY_LOCAL_MACHINE, SubKeyName,
          NetFx35HasServicePack, HasServicePack) then
      begin
        if HasServicePack >= NeedServicePack then
        begin
          Result := True;
        end;
      end;
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

  if RegQueryDWordValue(HKEY_LOCAL_MACHINE, SubKeyName, NetFxIsInstalled,
      IsInstalled) then
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

function GetNetFx2InstallRoot(const FileName: String): String;
var
  InstallRoot: String;
begin
  Result := '';

  if RegQueryStringValue(HKEY_LOCAL_MACHINE, NetFxSubKeyName,
      NetFxInstallRoot, InstallRoot) then
  begin
    if InstallRoot <> '' then
    begin
      Result := TrimSlash(InstallRoot) + '\' + NetFx2Version;

      if FileName <> '' then
      begin
        Result := TrimSlash(Result) + '\' + FileName;
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
    if InstallRoot <> '' then
    begin
      Result := TrimSlash(InstallRoot) + '\' + NetFx4Version;

      if FileName <> '' then
      begin
        Result := TrimSlash(Result) + '\' + FileName;
      end;
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

function ExtractAndInstallVcRuntime(var ResultCode: Integer): Boolean;
begin
  ExtractTemporaryFile(VcRuntimeRedistributable);

  if Exec(ExpandConstant(
      '{tmp}\' + VcRuntimeRedistributable),
      '/q', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
  begin
    Result := True;
  end
  else begin
    Result := False;
  end;
end;

function NetFxInitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  IsNetFx2Setup := {#IsNetFx2};
  IsNetFx4Setup := not IsNetFx2Setup;

  NetFxSubKeyName := 'Software\Microsoft\.NETFramework';
  NetFxInstallRoot := 'InstallRoot';
  NetFxSetupSubKeyName := 'Software\Microsoft\NET Framework Setup\NDP';
  NetFxIsInstalled := 'Install';

  NetFx2Version := 'v2.0.50727';
  NetFx2SetupVersion := 'v2.0.50727';
  NetFx2HasServicePack := 'SP';
  NetFx2ServicePack := 2;
  NetFx2ErrorMessage := 'The Microsoft .NET Framework v2.0 with Service Pack '
      + IntToStr(NetFx2ServicePack) + ' or higher is required.';

  NetFx35SetupVersion := 'v3.5';
  NetFx35HasServicePack := 'SP';
  NetFx35ServicePack := 1;
  NetFx35ErrorMessage := 'The Microsoft .NET Framework v3.5 with Service Pack '
      + IntToStr(NetFx35ServicePack) + ' or higher is required for LINQ support.';

  NetFx4Version := 'v4.0.30319';
  NetFx4SetupVersion := 'v4\Full';
  NetFx4HasServicePack := 'Servicing';
  NetFx4ServicePack := 0;
  NetFx4ErrorMessage := 'The Microsoft .NET Framework v4.0 with Service Pack '
      + IntToStr(NetFx4ServicePack) + ' or higher is required.';

  VcRuntimeRedistributable := 'vcredist_{#AppProcessor}_{#VcRuntime}.exe';

  if IsNetFx2Setup then
  begin
    Result := CheckForNetFx2(NetFx2ServicePack);

    if not Result then
    begin
      MsgBox(NetFx2ErrorMessage, mbError, MB_OK);
    end;

    if Result and not CheckForNetFx35(NetFx35ServicePack) then
    begin
      MsgBox(NetFx35ErrorMessage, mbInformation, MB_OK);
    end;
  end;

  if IsNetFx4Setup then
  begin
    Result := CheckForNetFx4(NetFx4ServicePack);

    if not Result then
    begin
      MsgBox(NetFx4ErrorMessage, mbError, MB_OK);
    end;
  end;

  if Result then
  begin
    Result := ExtractAndInstallVcRuntime(ResultCode);

    if not Result or (ResultCode <> 0) then
    begin
      MsgBox('Failed to install Microsoft Visual C++ Runtime: ' +
          VcRuntimeRedistributable + ', ' + SysErrorMessage(ResultCode),
          mbError, MB_OK);

      if Result then
      begin
        Result := False;
      end;
    end;
  end;
end;
