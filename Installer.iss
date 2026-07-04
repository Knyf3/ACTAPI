; ACTApi — Inno Setup 6 installer script
; Generates a single-file installer for the ACTApi Windows Service

#define MyAppName "ACT API Bridge"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Entech Security"
#define MyAppURL "https://github.com/Knyf3/ACTAPI"
#define MyAppExeName "ACTApi.exe"
#define MyServiceName "ACTApi"

[Setup]
AppId={{B4F1A2D3-5E6F-7A8B-9C0D-1E2F3A4B5C6D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf64}\Entech Security\ACT API Bridge
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=.\Installer
OutputBaseFilename=ACTAPI_Setup_{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
WizardStyle=modern
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main binaries — excludes Settings.json so the explicit rule below takes precedence
Source: "bin\Release\net8.0\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "Settings.json,Settings\Settings.json"

; Settings file — preserved on upgrade, kept during uninstall
Source: "Settings\Settings.json"; DestDir: "{app}\Settings"; Flags: onlyifdoesntexist uninsneveruninstall

; Verify page static assets
Source: "wwwroot\verify\*"; DestDir: "{app}\wwwroot\verify"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Stop existing service if running (skip during fresh install)
Filename: "{sys}\sc.exe"; Parameters: "stop {#MyServiceName}"; Flags: runhidden; StatusMsg: "Stopping ACT API Service..."; Check: ServiceExists('{#MyServiceName}')

; Wait for file locks to release
Filename: "{sys}\timeout.exe"; Parameters: "/t 3 /nobreak"; Flags: runhidden; Check: ServiceExists('{#MyServiceName}')

; Create service only on fresh install (not upgrade)
Filename: "{sys}\sc.exe"; Parameters: "create {#MyServiceName} binPath=""{app}\{#MyAppExeName}"" start=auto displayName=""{#MyAppName}"""; Flags: runhidden; StatusMsg: "Registering Windows Service..."; Check: not ServiceExists('{#MyServiceName}')
Filename: "{sys}\sc.exe"; Parameters: "description {#MyServiceName} ""RESTful HTTP bridge for ACT Enterprise WCF API"""; Flags: runhidden; Check: not ServiceExists('{#MyServiceName}')

; Always start the service after install/upgrade
Filename: "{sys}\sc.exe"; Parameters: "start {#MyServiceName}"; Flags: runhidden; StatusMsg: "Starting ACT API Service..."

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop {#MyServiceName}"; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "delete {#MyServiceName}"; Flags: runhidden

[Code]
// External API declarations MUST be at the top of [Code]
function OpenSCManager(MachineName, DatabaseName: string; DesiredAccess: LongWord): THandle;
  external 'OpenSCManagerW@advapi32.dll stdcall';

function OpenService(hSCManager: THandle; ServiceName: string; DesiredAccess: LongWord): THandle;
  external 'OpenServiceW@advapi32.dll stdcall';

function CloseServiceHandle(hSCObject: THandle): Boolean;
  external 'CloseServiceHandle@advapi32.dll stdcall';

function ServiceExists(ServiceName: string): Boolean;
var
  ServiceManager, ServiceHandle: THandle;
begin
  Result := False;
  ServiceManager := OpenSCManager('', '', 4);
  if ServiceManager <> 0 then
  begin
    ServiceHandle := OpenService(ServiceManager, ServiceName, 4);
    if ServiceHandle <> 0 then
    begin
      Result := True;
      CloseServiceHandle(ServiceHandle);
    end;
    CloseServiceHandle(ServiceManager);
  end;
end;
