[Setup]
AllowNoIcons=yes
ArchitecturesInstallIn64BitMode=x64
AlwaysShowComponentsList=no
AppCopyright=Public Domain
AppID={{02E43EC2-6B1C-45B5-9E48-941C3E1B204A}
AppName=System.Data.SQLite
AppPublisher=System.Data.SQLite Team
AppPublisherURL=http://system.data.sqlite.org/
AppSupportURL=http://system.data.sqlite.org/
AppUpdatesURL=http://system.data.sqlite.org/
AppVerName=System.Data.SQLite v1.0.67
AppVersion=1.0.67.0
AppComments=The ADO.NET adapter for the SQLite database engine.
AppReadmeFile={app}\README.HTM
DefaultDirName={pf}\System.Data.SQLite
DefaultGroupName=System.Data.SQLite
OutputBaseFilename=System.Data.SQLite.Setup
OutputManifestFile=System.Data.SQLite.Setup-manifest.txt
SetupLogging=yes
UninstallFilesDir={app}\uninstall
ExtraDiskSpaceRequired=2097152

[Code]
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
  NetFx2ServicePack: Cardinal;
  NetFx2ErrorMessage: String;

  NetFx4Version: String;
  NetFx4SetupVersion: String;
  NetFx4HasServicePack: String;
  NetFx4ServicePack: Cardinal;
  NetFx4ErrorMessage: String;

function CheckForNetFx2(NeedServicePack: Cardinal): Boolean;
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

function CheckForNetFx4(NeedServicePack: Cardinal): Boolean;
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

function GetNetFx2InstallRoot(FileName: String): String;
var
  InstallRoot: String;
begin
  Result := '';

  if RegQueryStringValue(HKEY_LOCAL_MACHINE, NetFxSubKeyName,
      NetFxInstallRoot, InstallRoot) then
  begin
    Result := InstallRoot + '\' + NetFx2Version;

    if FileName <> '' then
    begin
      Result := Result + '\' + FileName;
    end;
  end;
end;

function GetNetFx4InstallRoot(FileName: String): String;
var
  InstallRoot: String;
begin
  Result := '';

  if RegQueryStringValue(HKEY_LOCAL_MACHINE, NetFxSubKeyName,
      NetFxInstallRoot, InstallRoot) then
  begin
    Result := InstallRoot + '\' + NetFx4Version;

    if FileName <> '' then
    begin
      Result := Result + '\' + FileName;
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
  ExtractTemporaryFile('vcredist_x86_2008_SP1.exe');

  if Exec(ExpandConstant('{tmp}\vcredist_x86_2008_SP1.exe'), '/q', '',
      SW_SHOW, ewWaitUntilTerminated, ResultCode) then
  begin
    Result := True;
  end
  else begin
    Result := False;
  end;
end;

function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  IsNetFx2Setup := True;
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

  NetFx4Version := 'v4.0.30319';
  NetFx4SetupVersion := 'v4\Full';
  NetFx4HasServicePack := 'Servicing';
  NetFx4ServicePack := 0;
  NetFx4ErrorMessage := 'The Microsoft .NET Framework v4.0 with Service Pack '
      + IntToStr(NetFx4ServicePack) + ' or higher is required.';

  if IsNetFx2Setup then
  begin
    Result := CheckForNetFx2(NetFx2ServicePack);

    if not Result then
    begin
      MsgBox(NetFx2ErrorMessage, mbError, MB_OK);
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

    if not Result then
    begin
      MsgBox('Failed to install Microsoft Visual C++ Runtime: ' +
          SysErrorMessage(ResultCode), mbError, MB_OK);
    end;
  end;
end;

[Components]
Name: Application; Description: System.Data.SQLite components.; Types: custom compact full
Name: Application\Core; Description: Core components.; Types: custom compact full
Name: Application\Core\MSIL; Description: Core managed components.; Types: custom compact full
Name: Application\Core\x86; Description: Core native components.; Types: custom compact full
Name: Application\Symbols; Description: Debugging symbol components.; Types: custom compact full
Name: Application\Documentation; Description: Documentation components.; Types: custom compact full
Name: Application\Test; Description: Test components.; Types: custom compact full

[Tasks]
Components: Application\Core\MSIL; Name: GAC; Description: Install the assemblies into the global assembly cache.; Flags: unchecked; Check: CheckIsNetFx2Setup() or CheckIsNetFx4Setup()
Components: Application\Core\MSIL; Name: NGEN; Description: Generate native images for the assemblies and install the images in the native image cache.; Check: CheckIsNetFx2Setup() or CheckIsNetFx4Setup()

[Run]
Components: Application\Core\MSIL; Tasks: NGEN; Filename: {code:GetNetFx2InstallRoot|Ngen.exe}; Parameters: "install ""{app}\bin\System.Data.SQLite.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx2Setup()
Components: Application\Core\MSIL; Tasks: NGEN; Filename: {code:GetNetFx4InstallRoot|Ngen.exe}; Parameters: "install ""{app}\bin\System.Data.SQLite.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()

[UninstallRun]
Components: Application\Core\MSIL; Tasks: NGEN; Filename: {code:GetNetFx4InstallRoot|Ngen.exe}; Parameters: "uninstall ""{app}\bin\System.Data.SQLite.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
Components: Application\Core\MSIL; Tasks: NGEN; Filename: {code:GetNetFx2InstallRoot|Ngen.exe}; Parameters: "uninstall ""{app}\bin\System.Data.SQLite.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx2Setup()

[Dirs]
Name: {app}\bin
Name: {app}\doc
Name: {app}\GAC

[Files]
Components: Application\Core\x86; Source: ..\Externals\MSVCPP\vcredist_x86_2008_SP1.exe; DestDir: {tmp}; Flags: dontcopy
Components: Application; Source: ..\readme.htm; DestDir: {app}; Flags: restartreplace uninsrestartdelete isreadme
Components: Application\Core\MSIL; Tasks: GAC; Source: ..\bin\Release\bin\System.Data.SQLite.dll; DestDir: {app}\GAC; StrongAssemblyName: "System.Data.SQLite, Version=1.0.67.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139, ProcessorArchitecture=MSIL"; Flags: restartreplace uninsrestartdelete uninsnosharedfileprompt sharedfile gacinstall
Components: Application\Core\MSIL; Source: ..\bin\Release\bin\System.Data.SQLite.dll; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Core\MSIL and Application\Symbols; Source: ..\bin\Release\bin\System.Data.SQLite.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Core\x86; Source: ..\bin\Win32\ReleaseNativeOnly\SQLite.Interop.dll; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Core\x86 and Application\Symbols; Source: ..\bin\Win32\ReleaseNativeOnly\SQLite.Interop.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Documentation; Source: ..\doc\SQLite.NET.chm; DestDir: {app}\doc; Flags: restartreplace uninsrestartdelete
Components: Application\Test; Source: ..\bin\Release\bin\test.exe; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Test and Application\Symbols; Source: ..\bin\Release\bin\test.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Test; Source: ..\bin\Release\bin\test.exe.config; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete

[Icons]
Name: {group}\Test Suite; Filename: {app}\bin\test.exe; WorkingDir: {app}\bin; IconFilename: {app}\bin\test.exe; Comment: Launch Test Suite; IconIndex: 0; Flags: createonlyiffileexists
Name: {group}\Class Library Documentation; Filename: {app}\doc\SQLite.NET.chm; WorkingDir: {app}\doc; Comment: Launch Class Library Documentation; Flags: createonlyiffileexists
Name: {group}\README File; Filename: {app}\readme.htm; WorkingDir: {app}; Comment: View README File; Flags: createonlyiffileexists
