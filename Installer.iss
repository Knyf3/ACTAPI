; ACTApi — Inno Setup installer script
; Generates a single-file installer for the ACTApi Windows Service

#define MyAppName "ACT API Bridge"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Entech Security"
#define MyAppURL "https://github.com/Knyf3/ACTAPI"
#define MyAppExeName "ACTApi.exe"

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
WizardStyle=modern
PrivilegesRequired=admin
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.iso"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main application
Source: "bin\Release\net8.0\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Settings file (optional: copy default if not exists)
Source: "Settings\Settings.json"; DestDir: "{app}\Settings"; Flags: onlyifdoesntexist uninsneveruninstall

; Verify page files
Source: "wwwroot\verify\*"; DestDir: "{app}\wwwroot\verify"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Install and start the Windows Service
Filename: "{app}\{#MyAppExeName}"; Parameters: "--install"; Flags: runhidden; StatusMsg: "Installing Windows Service..."
Filename: "net"; Parameters: "start ACTApi"; Flags: runhidden; StatusMsg: "Starting ACT API Bridge service..."; AfterInstall: Sleep(2000)

[UninstallRun]
Filename: "net"; Parameters: "stop ACTApi"; Flags: runhidden
Filename: "{app}\{#MyAppExeName}"; Parameters: "--uninstall"; Flags: runhidden

[Code]
function Sleep(ms: Integer): Boolean;
begin
  Result := True;
end;
