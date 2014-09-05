;
; SQLite.iss --
;
; Written by Joe Mistachkin.
; Released to the public domain, use at your own risk!
;

#define BaseConfiguration StringChange(AppConfiguration, "NativeOnly", "")
#define GacProcessor StringChange(AppProcessor, "x64", "amd64")

#if Pos("NativeOnly", AppConfiguration) == 0
#define InstallerCondition "Application\Core\MSIL and Application\Designer and Application\Designer\Installer"
#define AppVersion GetStringFileInfo("..\..\bin\" + Year + "\" + AppPlatform + "\" + AppConfiguration + "\System.Data.SQLite.dll", PRODUCT_VERSION)
#define OutputConfiguration StringChange(StringChange(AppConfiguration, "Debug", "setup-debug"), "Release", "setup") + "-bundle"
#else
#define InstallerCondition "Application\Core\MSIL and Application\Core\" + AppProcessor + " and Application\Designer and Application\Designer\Installer"
#define AppVersion GetStringFileInfo("..\..\bin\" + Year + "\" + BaseConfiguration + "\bin\System.Data.SQLite.dll", PRODUCT_VERSION)
#define OutputConfiguration StringChange(StringChange(BaseConfiguration, "Debug", "setup-debug"), "Release", "setup")
#endif

[Setup]
AllowNoIcons=true

#if AppProcessor != "x86"
ArchitecturesAllowed={#AppProcessor}
ArchitecturesInstallIn64BitMode={#AppProcessor}
#endif

AlwaysShowComponentsList=false
AppCopyright=Public Domain
AppID={#AppId}
AppName=System.Data.SQLite
AppPublisher=System.Data.SQLite Team
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
AppVerName=System.Data.SQLite v{#AppVersion} ({#AppConfiguration})
AppVersion={#AppVersion}
AppComments=The ADO.NET adapter for the SQLite database engine.
AppReadmeFile={app}\readme.htm
DefaultDirName={pf}\System.Data.SQLite\{#Year}
DefaultGroupName=System.Data.SQLite\{#Year}
OutputDir=..\Output
OutputBaseFilename=sqlite-{#Framework}-{#OutputConfiguration}-{#AppProcessor}-{#Year}-{#AppVersion}
OutputManifestFile=sqlite-{#Framework}-{#OutputConfiguration}-{#AppProcessor}-{#Year}-{#AppVersion}-manifest.txt
SetupLogging=true
UninstallFilesDir={app}\uninstall
VersionInfoVersion={#AppVersion}
ExtraDiskSpaceRequired=2097152
ChangesEnvironment=true

[Code]
#include "CheckForNetFx.pas"
#include "InitializeSetup.pas"

[Components]
Name: Application; Description: System.Data.SQLite components.; Types: custom compact full
Name: Application\Core; Description: Core components.; Types: custom compact full
Name: Application\Core\MSIL; Description: Core managed components.; Types: custom compact full
Name: Application\Core\{#AppProcessor}; Description: Core native {#AppProcessor} components.; Types: custom compact full

#if Year != "2005"
Name: Application\LINQ; Description: LINQ support components.; Types: custom compact full
#endif

#if Year != "2005" && Year != "2008"
Name: Application\EF6; Description: Entity Framework 6 support components.; Types: custom compact full
#endif

Name: Application\Designer; Description: Visual Studio designer components.; Types: custom full
Name: Application\Designer\Installer; Description: Visual Studio designer installer components.; Types: custom full
Name: Application\Symbols; Description: Debugging symbol components.; Types: custom compact full
Name: Application\Documentation; Description: Documentation components.; Types: custom compact full
Name: Application\Test; Description: Test components.; Types: custom compact full

[Tasks]
#if Year == "2005"
Components: Application\Core\MSIL; Name: ngen; Description: Generate native images for the assemblies and install them into the native image cache.; Check: CheckIsNetFx2Setup() or CheckIsNetFx4Setup()
#elif Year == "2008"
Components: Application\Core\MSIL Or Application\LINQ; Name: ngen; Description: Generate native images for the assemblies and install them into the native image cache.; Check: CheckIsNetFx2Setup() or CheckIsNetFx4Setup()
#else
Components: Application\Core\MSIL Or Application\LINQ Or Application\EF6; Name: ngen; Description: Generate native images for the assemblies and install them into the native image cache.; Check: CheckIsNetFx4Setup()
#endif

#if Pos("NativeOnly", AppConfiguration) == 0
#if Year == "2005"
Components: Application\Core\MSIL; Name: gac; Description: Install the assemblies into the global assembly cache.; Flags: unchecked; Check: CheckIsNetFx2Setup() or CheckIsNetFx4Setup()
#elif Year == "2008"
Components: Application\Core\MSIL Or Application\LINQ; Name: gac; Description: Install the assemblies into the global assembly cache.; Flags: unchecked; Check: CheckIsNetFx2Setup() or CheckIsNetFx4Setup()
#else
Components: Application\Core\MSIL Or Application\LINQ Or Application\EF6; Name: gac; Description: Install the assemblies into the global assembly cache.; Flags: unchecked; Check: CheckIsNetFx4Setup()
#endif

#if AppProcessor == "x86"
#if Year == "2005"
Components: {#InstallerCondition}; Name: gac\vs2005; Description: Install the designer components for Visual Studio 2005.; Flags: unchecked; Check: CheckIsNetFx2Setup()
#endif
#if Year == "2008"
Components: {#InstallerCondition}; Name: gac\vs2008; Description: Install the designer components for Visual Studio 2008.; Flags: unchecked; Check: CheckIsNetFx2Setup()
#endif
#if Year == "2010"
Components: {#InstallerCondition}; Name: gac\vs2010; Description: Install the designer components for Visual Studio 2010.; Flags: unchecked; Check: CheckIsNetFx4Setup()
#endif
#if Year == "2012"
Components: {#InstallerCondition}; Name: gac\vs2012; Description: Install the designer components for Visual Studio 2012.; Flags: unchecked; Check: CheckIsNetFx4Setup()
#endif
#if Year == "2013"
Components: {#InstallerCondition}; Name: gac\vs2013; Description: Install the designer components for Visual Studio 2013.; Flags: unchecked; Check: CheckIsNetFx4Setup()
#endif
#endif
#endif

[Run]
Components: Application\Core\MSIL; Tasks: ngen; Filename: {code:GetNetFx2InstallRoot|Ngen.exe}; Parameters: "install ""{app}\bin\System.Data.SQLite.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx2Setup()
Components: Application\Core\MSIL; Tasks: ngen; Filename: {code:GetNetFx4InstallRoot|Ngen.exe}; Parameters: "install ""{app}\bin\System.Data.SQLite.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()

#if Year != "2005"
Components: Application\LINQ; Tasks: ngen; Filename: {code:GetNetFx2InstallRoot|Ngen.exe}; Parameters: "install ""{app}\bin\System.Data.SQLite.Linq.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx2Setup() and CheckForNetFx35(1)
Components: Application\LINQ; Tasks: ngen; Filename: {code:GetNetFx4InstallRoot|Ngen.exe}; Parameters: "install ""{app}\bin\System.Data.SQLite.Linq.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
#endif

#if Year != "2005" && Year != "2008"
Components: Application\EF6; Tasks: ngen; Filename: {code:GetNetFx4InstallRoot|Ngen.exe}; Parameters: "install ""{app}\bin\System.Data.SQLite.EF6.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
#endif

#if Pos("NativeOnly", AppConfiguration) == 0 && AppProcessor == "x86"
#if Year == "2005"
Components: {#InstallerCondition}; Tasks: gac\vs2005; Filename: {app}\bin\Installer.exe; Parameters: "-install true -wow64 true -installFlags AllExceptGlobalAssemblyCache -tracePriority Lowest -verbose true -noCompact true -noNetFx35 true -noNetFx40 true -noNetFx45 true -noNetFx451 true -noVs2008 true -noVs2010 true -noVs2012 true -noVs2013 true -whatIf false -confirm true"; Flags: skipifdoesntexist; Check: CheckIsNetFx2Setup()
#endif
#if Year == "2008"
Components: {#InstallerCondition}; Tasks: gac\vs2008; Filename: {app}\bin\Installer.exe; Parameters: "-install true -wow64 true -installFlags AllExceptGlobalAssemblyCache -tracePriority Lowest -verbose true -noCompact true -noNetFx40 true -noNetFx45 true -noNetFx451 true -noVs2005 true -noVs2010 true -noVs2012 true -noVs2013 true -whatIf false -confirm true"; Flags: skipifdoesntexist; Check: CheckIsNetFx2Setup()
#endif
#if Year == "2010"
Components: {#InstallerCondition}; Tasks: gac\vs2010; Filename: {app}\bin\Installer.exe; Parameters: "-install true -wow64 true -installFlags AllExceptGlobalAssemblyCache -tracePriority Lowest -verbose true -noCompact true -noNetFx20 true -noNetFx45 true -noNetFx451 true -noVs2005 true -noVs2008 true -noVs2012 true -noVs2013 true -whatIf false -confirm true"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
#endif
#if Year == "2012"
Components: {#InstallerCondition}; Tasks: gac\vs2012; Filename: {app}\bin\Installer.exe; Parameters: "-install true -wow64 true -installFlags AllExceptGlobalAssemblyCache -tracePriority Lowest -verbose true -noCompact true -noNetFx20 true -noNetFx40 true -noNetFx451 true -noVs2005 true -noVs2008 true -noVs2010 true -noVs2013 true -configVersion 4.0.30319 -whatIf false -confirm true"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
Components: {#InstallerCondition}; Tasks: gac\vs2012; Filename: {app}\bin\Installer.exe; Parameters: "-perUser true -install true -wow64 true -installFlags AllExceptGlobalAssemblyCache -tracePriority Lowest -verbose true -noCompact true -noNetFx20 true -noNetFx40 true -noNetFx451 true -noVs2005 true -noVs2008 true -noVs2010 true -noVs2013 true -configVersion 4.0.30319 -vsVersionSuffix _Config -whatIf false -confirm true"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
#endif
#if Year == "2013"
Components: {#InstallerCondition}; Tasks: gac\vs2013; Filename: {app}\bin\Installer.exe; Parameters: "-install true -wow64 true -installFlags AllExceptGlobalAssemblyCache -tracePriority Lowest -verbose true -noCompact true -noNetFx20 true -noNetFx40 true -noNetFx45 true -noVs2005 true -noVs2008 true -noVs2010 true -noVs2012 true -configVersion 4.0.30319 -whatIf false -confirm true"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
#endif
#endif

[UninstallRun]
#if Pos("NativeOnly", AppConfiguration) == 0 && AppProcessor == "x86"
#if Year == "2013"
Components: {#InstallerCondition}; Tasks: gac\vs2013; Filename: {app}\bin\Installer.exe; Parameters: "-install false -wow64 true -installFlags AllExceptGlobalAssemblyCache -tracePriority Lowest -verbose true -noCompact true -noNetFx20 true -noNetFx40 true -noNetFx45 true -noVs2005 true -noVs2008 true -noVs2010 true -noVs2012 true -configVersion 4.0.30319 -whatIf false -confirm true"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
#endif
#if Year == "2012"
Components: {#InstallerCondition}; Tasks: gac\vs2012; Filename: {app}\bin\Installer.exe; Parameters: "-perUser true -install false -wow64 true -installFlags AllExceptGlobalAssemblyCache -tracePriority Lowest -verbose true -noCompact true -noNetFx20 true -noNetFx40 true -noNetFx451 true -noVs2005 true -noVs2008 true -noVs2010 true -noVs2013 true -configVersion 4.0.30319 -vsVersionSuffix _Config -whatIf false -confirm true"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
Components: {#InstallerCondition}; Tasks: gac\vs2012; Filename: {app}\bin\Installer.exe; Parameters: "-install false -wow64 true -installFlags AllExceptGlobalAssemblyCache -tracePriority Lowest -verbose true -noCompact true -noNetFx20 true -noNetFx40 true -noNetFx451 true -noVs2005 true -noVs2008 true -noVs2010 true -noVs2013 true -configVersion 4.0.30319 -whatIf false -confirm true"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
#endif
#if Year == "2010"
Components: {#InstallerCondition}; Tasks: gac\vs2010; Filename: {app}\bin\Installer.exe; Parameters: "-install false -wow64 true -installFlags AllExceptGlobalAssemblyCache -tracePriority Lowest -verbose true -noCompact true -noNetFx20 true -noNetFx45 true -noNetFx451 true -noVs2005 true -noVs2008 true -noVs2012 true -noVs2013 true -whatIf false -confirm true"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
#endif
#if Year == "2008"
Components: {#InstallerCondition}; Tasks: gac\vs2008; Filename: {app}\bin\Installer.exe; Parameters: "-install false -wow64 true -installFlags AllExceptGlobalAssemblyCache -tracePriority Lowest -verbose true -noCompact true -noNetFx40 true -noNetFx45 true -noNetFx451 true -noVs2005 true -noVs2010 true -noVs2012 true -noVs2013 true -whatIf false -confirm true"; Flags: skipifdoesntexist; Check: CheckIsNetFx2Setup()
#endif
#if Year == "2005"
Components: {#InstallerCondition}; Tasks: gac\vs2005; Filename: {app}\bin\Installer.exe; Parameters: "-install false -wow64 true -installFlags AllExceptGlobalAssemblyCache -tracePriority Lowest -verbose true -noCompact true -noNetFx35 true -noNetFx40 true -noNetFx45 true -noNetFx451 true -noVs2008 true -noVs2010 true -noVs2012 true -noVs2013 true -whatIf false -confirm true"; Flags: skipifdoesntexist; Check: CheckIsNetFx2Setup()
#endif
#endif

#if Year != "2005" && Year != "2008"
Components: Application\LINQ; Tasks: ngen; Filename: {code:GetNetFx4InstallRoot|Ngen.exe}; Parameters: "uninstall ""{app}\bin\System.Data.SQLite.EF6.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
#endif

#if Year != "2005"
Components: Application\LINQ; Tasks: ngen; Filename: {code:GetNetFx4InstallRoot|Ngen.exe}; Parameters: "uninstall ""{app}\bin\System.Data.SQLite.Linq.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
Components: Application\LINQ; Tasks: ngen; Filename: {code:GetNetFx2InstallRoot|Ngen.exe}; Parameters: "uninstall ""{app}\bin\System.Data.SQLite.Linq.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx2Setup() and CheckForNetFx35(1)
#endif

Components: Application\Core\MSIL; Tasks: ngen; Filename: {code:GetNetFx4InstallRoot|Ngen.exe}; Parameters: "uninstall ""{app}\bin\System.Data.SQLite.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx4Setup()
Components: Application\Core\MSIL; Tasks: ngen; Filename: {code:GetNetFx2InstallRoot|Ngen.exe}; Parameters: "uninstall ""{app}\bin\System.Data.SQLite.dll"" /nologo"; Flags: skipifdoesntexist; Check: CheckIsNetFx2Setup()

[Dirs]
Name: {app}\bin
Name: {app}\doc

#if Pos("NativeOnly", AppConfiguration) == 0
Name: {app}\GAC; Tasks: gac
#endif

[Files]
Components: Application\Core\{#AppProcessor}; Source: ..\..\Externals\MSVCPP\vcredist_{#AppProcessor}_{#VcRuntime}.exe; DestDir: {tmp}; Flags: dontcopy
Components: Application; Source: ..\..\System.Data.SQLite.url; DestDir: {app}; Flags: restartreplace uninsrestartdelete
Components: Application; Source: ..\..\readme.htm; DestDir: {app}; Flags: restartreplace uninsrestartdelete isreadme

#if Pos("NativeOnly", AppConfiguration) == 0
Components: Application\Core\MSIL; Tasks: gac; Source: ..\..\bin\{#Year}\{#AppPlatform}\{#AppConfiguration}\System.Data.SQLite.dll; DestDir: {app}\GAC; StrongAssemblyName: "System.Data.SQLite, Version={#AppVersion}, Culture=neutral, PublicKeyToken={#AppPublicKey}, ProcessorArchitecture={#GacProcessor}"; Flags: restartreplace uninsrestartdelete uninsnosharedfileprompt sharedfile gacinstall
Components: Application\Core\MSIL; Source: ..\..\bin\{#Year}\{#AppPlatform}\{#AppConfiguration}\System.Data.SQLite.dll; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Core\MSIL and Application\Symbols; Source: ..\..\bin\{#Year}\{#AppPlatform}\{#AppConfiguration}\System.Data.SQLite.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
#else
Components: Application\Core\MSIL; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\System.Data.SQLite.dll; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Core\MSIL and Application\Symbols; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\System.Data.SQLite.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
#endif

Components: Application\Core\MSIL; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\System.Data.SQLite.dll.config; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete

#if Year != "2005"
#if Pos("NativeOnly", AppConfiguration) == 0
Components: Application\LINQ; Tasks: gac; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\System.Data.SQLite.Linq.dll; DestDir: {app}\GAC; StrongAssemblyName: "System.Data.SQLite.Linq, Version={#AppVersion}, Culture=neutral, PublicKeyToken={#AppPublicKey}, ProcessorArchitecture=MSIL"; Flags: restartreplace uninsrestartdelete uninsnosharedfileprompt sharedfile gacinstall
#endif

Components: Application\LINQ; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\System.Data.SQLite.Linq.dll; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\LINQ and Application\Symbols; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\System.Data.SQLite.Linq.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
#endif

#if Year != "2005" && Year != "2008"
#if Year == "2010"
Components: Application\EF6; Source: ..\..\Externals\EntityFramework\lib\net40\EntityFramework.dll; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
#elif Year == "2012" || Year == "2013"
Components: Application\EF6; Source: ..\..\Externals\EntityFramework\lib\net45\EntityFramework.dll; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
#endif

#if Pos("NativeOnly", AppConfiguration) == 0
Components: Application\EF6; Tasks: gac; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\System.Data.SQLite.EF6.dll; DestDir: {app}\GAC; StrongAssemblyName: "System.Data.SQLite.EF6, Version={#AppVersion}, Culture=neutral, PublicKeyToken={#AppPublicKey}, ProcessorArchitecture=MSIL"; Flags: restartreplace uninsrestartdelete uninsnosharedfileprompt sharedfile gacinstall
#endif

Components: Application\EF6; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\System.Data.SQLite.EF6.dll; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\EF6 and Application\Symbols; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\System.Data.SQLite.EF6.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
#endif

#if Pos("NativeOnly", AppConfiguration) != 0
Components: Application\Core\{#AppProcessor}; Source: ..\..\bin\{#Year}\{#AppPlatform}\{#AppConfiguration}\SQLite.Interop.dll; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Core\{#AppProcessor} and Application\Symbols; Source: ..\..\bin\{#Year}\{#AppPlatform}\{#AppConfiguration}\SQLite.Interop.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
#endif

Components: Application\Documentation; Source: ..\..\doc\SQLite.NET.chm; DestDir: {app}\doc; Flags: restartreplace uninsrestartdelete
Components: Application\Designer; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\SQLite.Designer.dll; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Designer and Application\Symbols; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\SQLite.Designer.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Designer\Installer; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\Installer.exe; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Designer\Installer and Application\Symbols; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\Installer.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete

#if AppProcessor == "x86"
Components: Application\Test; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\test32.exe; DestDir: {app}\bin; DestName: test.exe; Flags: restartreplace uninsrestartdelete
#else
Components: Application\Test; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\test.exe; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
#endif

Components: Application\Test and Application\Symbols; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\test.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Test; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\test.exe.config; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete

#if Year != "2005"
#if AppProcessor == "x86"
Components: Application\Test and Application\LINQ; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\testlinq32.exe; DestDir: {app}\bin; DestName: testlinq.exe; Flags: restartreplace uninsrestartdelete
#else
Components: Application\Test and Application\LINQ; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\testlinq.exe; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
#endif
Components: Application\Test and Application\LINQ and Application\Symbols; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\testlinq.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Test and Application\LINQ; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\testlinq.exe.config; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
#endif

#if Year != "2005" && Year != "2008"
Components: Application\Test and (Application\LINQ or Application\EF6); Source: ..\..\testlinq\northwindEF.db; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
#if AppProcessor == "x86"
Components: Application\Test and (Application\LINQ or Application\EF6); Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\testef632.exe; DestDir: {app}\bin; DestName: testef6.exe; Flags: restartreplace uninsrestartdelete
#else
Components: Application\Test and (Application\LINQ or Application\EF6); Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\testef6.exe; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
#endif
Components: Application\Test and (Application\LINQ or Application\EF6) and Application\Symbols; Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\testef6.pdb; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
Components: Application\Test and (Application\LINQ or Application\EF6); Source: ..\..\bin\{#Year}\{#BaseConfiguration}\bin\testef6.exe.config; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
#elif Year != "2005"
Components: Application\Test and Application\LINQ; Source: ..\..\testlinq\northwindEF.db; DestDir: {app}\bin; Flags: restartreplace uninsrestartdelete
#endif

[Icons]
Name: {group}\Test Application; Filename: {app}\bin\test.exe; WorkingDir: {app}\bin; IconFilename: {app}\bin\test.exe; Comment: Launch Test Application; IconIndex: 0; Flags: createonlyiffileexists
Name: {group}\Class Library Documentation; Filename: {app}\doc\SQLite.NET.chm; WorkingDir: {app}\doc; Comment: Launch Class Library Documentation; Flags: createonlyiffileexists
Name: {group}\README File; Filename: {app}\readme.htm; WorkingDir: {app}; Comment: View README File; Flags: createonlyiffileexists
