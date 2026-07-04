; ACTApi — Inno Setup installer script
; Compatible with InnoSetup 5.x (Unicode) and 6.x
; Generates a single-file installer for the ACTApi Windows Service
;
; Build steps:
;   1. dotnet publish -c Release -o bin\Release\net8.0\publish
;   2. Compile this script with ISCC.exe

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
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
WizardStyle=modern
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Languages\Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main application (published .NET 8 output)
Source: "bin\Release\net8.0\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Settings file — preserve existing on upgrade, never remove during uninstall
Source: "Settings\Settings.json"; DestDir: "{app}\Settings"; Flags: onlyifdoesntexist uninsneveruninstall

; Verify / guard page static assets
Source: "wwwroot\verify\*"; DestDir: "{app}\wwwroot\verify"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Stop + remove existing service (skip if not installed)
Filename: "{sys}\net.exe"; Parameters: "stop {#MyServiceName}"; Flags: runhidden; StatusMsg: "Stopping existing service..."; Check: ServiceExists('{#MyServiceName}')
Filename: "{sys}\sc.exe"; Parameters: "delete {#MyServiceName}"; Flags: runhidden; Check: ServiceExists('{#MyServiceName}')

; Install service (sc.exe create + description + start)
Filename: "{sys}\sc.exe"; Parameters: "create {#MyServiceName} binPath=""{app}\{#MyAppExeName}"" start=auto displayName=""{#MyAppName}"""; Flags: runhidden; StatusMsg: "Installing Windows Service..."
Filename: "{sys}\sc.exe"; Parameters: "description {#MyServiceName} ""RESTful HTTP bridge for ACT Enterprise WCF API"""; Flags: runhidden
Filename: "{sys}\net.exe"; Parameters: "start {#MyServiceName}"; Flags: runhidden; StatusMsg: "Starting service..."

[UninstallRun]
Filename: "{sys}\net.exe"; Parameters: "stop {#MyServiceName}"; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "delete {#MyServiceName}"; Flags: runhidden

[Code]
function ServiceExists(ServiceName: string): Boolean;
var
  ServiceManager, ServiceHandle: Integer;
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

function OpenSCManager(MachineName, DatabaseName: string; DesiredAccess: Integer): Integer;
  external 'OpenSCManagerW@advapi32.dll stdcall';

function OpenService(hSCManager: Integer; ServiceName: string; DesiredAccess: Integer): Integer;
  external 'OpenServiceW@advapi32.dll stdcall';

function CloseServiceHandle(hSCObject: Integer): Boolean;
  external 'CloseServiceHandle@advapi32.dll stdcall';
