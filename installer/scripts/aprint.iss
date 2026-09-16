#ifndef MyAppName
  #define MyAppName "Aprint"
#endif
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
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
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=..
OutputBaseFilename={#MyAppName}-{#MyAppVersion}-setup
SetupIconFile=..\..\src\Aprint\Resources\AppIcon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
CloseApplications=yes
RestartApplications=no
#if SignToolCmd != ""
SignTool=byparam powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "{#SignToolCmd} $f"
SignedUninstaller=yes
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
function HasWindowsDesktopRuntime8Installed: Boolean;
var
  Versions: TArrayOfString;
  I: Integer;
begin
  Result := False;

  if RegGetSubkeyNames(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App', Versions) then
  begin
	for I := 0 to GetArrayLength(Versions) - 1 do
	begin
	  if Pos('8.', Versions[I]) = 1 then
	  begin
		Result := True;
		Exit;
	  end;
	end;
  end;

  if RegGetSubkeyNames(HKLM32, 'SOFTWARE\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.WindowsDesktop.App', Versions) then
  begin
	for I := 0 to GetArrayLength(Versions) - 1 do
	begin
	  if Pos('8.', Versions[I]) = 1 then
	  begin
		Result := True;
		Exit;
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

  if not HasWindowsDesktopRuntime8Installed then
  begin
	OpenRuntimePage := MsgBox(
	  'This installer requires .NET 8 Desktop Runtime (WindowsDesktop).'+#13#10#13#10+
	  'Click Yes to open the official download page, then run the installer again.',
	  mbConfirmation,
	  MB_YESNO
	);

	if OpenRuntimePage = IDYES then
	  ShellExec('open', '{#MyDotNetDesktopRuntimeUrl}', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);

	Result := False;
  end;
end;
