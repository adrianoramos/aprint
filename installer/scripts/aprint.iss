#ifndef MyAppName
  #define MyAppName "Aprint"
#endif
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#ifndef MyFileVersion
	#define MyFileVersion "1.0.0.0"
#endif
#ifndef MyAppPublisher
  #define MyAppPublisher "Aprint"
#endif
#ifndef MyAppURL
  #define MyAppURL "https://github.com/adrianoramos/aprint"
#endif
#ifndef MyAppExeName
  #define MyAppExeName "Aprint.exe"
#endif
#ifndef MyPublishDir
	#define MyPublishDir "..\..\artifacts\publish"
#endif
#ifndef MyLicenseFile
	#define MyLicenseFile "..\..\LICENSE"
#endif
#ifndef MyDotNetDesktopRuntimeUrl
  #define MyDotNetDesktopRuntimeUrl "https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime"
#endif
#ifndef SignToolCmd
  #define SignToolCmd ""
#endif

[Setup]
AppId={{0EB8385F-7616-43AC-A7E8-7CA5B2F0436A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
LicenseFile={#MyLicenseFile}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
DisableDirPage=yes
PrivilegesRequired=admin
OutputDir=..
OutputBaseFilename={#MyAppName}-{#MyAppVersion}-setup
SetupIconFile=..\..\src\Aprint\Resources\AppIcon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Another screenshot tool for Windows
VersionInfoVersion={#MyFileVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
CloseApplications=force
CloseApplicationsFilter={#MyAppExeName}
RestartApplications=no
#if SignToolCmd != ""
SignTool=byparam powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "{#SignToolCmd} $f"
SignedUninstaller=yes
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "startup"; Description: "Run {#MyAppName} at system startup"; GroupDescription: "Startup Options:"

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "{#MyLicenseFile}"; DestDir: "{app}"; DestName: "LICENSE"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "{#MyAppName}"; ValueType: string; ValueData: "{app}\{#MyAppExeName}"; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
	ResultCode: Integer;
begin
	if CurUninstallStep = usUninstall then
	begin
		RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', '{#MyAppName}');
		Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM "{#MyAppExeName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
	end;
end;

function HasWindowsDesktopRuntimeMinVersion(MinVersion: Integer): Boolean;
var
  BasePath: string;
  FindRec: TFindRec;
  VersionStr: string;
  DotIndex: Integer;
  MajorVersionStr: string;
  MajorVersion: Integer;
begin
  Result := False;

  // Check x64 installation
  BasePath := 'C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App';
  if DirExists(BasePath) then
  begin
	if FindFirst(BasePath + '\*', FindRec) then
	begin
	  try
		repeat
		  // Check if it's a directory and not a hidden file
		  if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0) and (FindRec.Name <> '.') and (FindRec.Name <> '..') then
		  begin
			VersionStr := FindRec.Name;
			// Extract major version number from string like "8.0.28"
			DotIndex := Pos('.', VersionStr);
			if DotIndex > 0 then
			begin
			  MajorVersionStr := Copy(VersionStr, 1, DotIndex - 1);
			  try
				MajorVersion := StrToInt(MajorVersionStr);
				if MajorVersion >= MinVersion then
				begin
				  Result := True;
				  Break;
				end;
			  except
				// If conversion fails, skip this version
			  end;
			end;
		  end;
		until not FindNext(FindRec);
	  finally
		FindClose(FindRec);
	  end;
	end;
  end;

  // If not found in x64, check x86 installation
  if not Result then
  begin
	BasePath := 'C:\Program Files (x86)\dotnet\shared\Microsoft.WindowsDesktop.App';
	if DirExists(BasePath) then
	begin
	  if FindFirst(BasePath + '\*', FindRec) then
	  begin
		try
		  repeat
			if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0) and (FindRec.Name <> '.') and (FindRec.Name <> '..') then
			begin
			  VersionStr := FindRec.Name;
			  DotIndex := Pos('.', VersionStr);
			  if DotIndex > 0 then
			  begin
				MajorVersionStr := Copy(VersionStr, 1, DotIndex - 1);
				try
				  MajorVersion := StrToInt(MajorVersionStr);
				  if MajorVersion >= MinVersion then
				  begin
					Result := True;
					Break;
				  end;
				except
				  // If conversion fails, skip this version
				end;
			  end;
			end;
		  until not FindNext(FindRec);
		finally
		  FindClose(FindRec);
		end;
	  end;
	end;
  end;
end;

function InitializeSetup(): Boolean;
var
  OpenRuntimePage: Integer;
  ErrorCode: Integer;
begin
  Result := True;

  if not HasWindowsDesktopRuntimeMinVersion(8) then
  begin
	OpenRuntimePage := MsgBox(
	  'This installer requires .NET 8 Desktop Runtime (WindowsDesktop) or higher.'+#13#10#13#10+
	  'Click Yes to open the official download page, then run the installer again.',
	  mbConfirmation,
	  MB_YESNO
	);

	if OpenRuntimePage = IDYES then
	  ShellExec('open', '{#MyDotNetDesktopRuntimeUrl}', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);

	Result := False;
  end;
end;

