#define MyAppName "ACT Pro API Service"
#define MyAppVersion "1.0.2"
#define MyAppPublisher "Total Optima Solusi"
#define MyAppExeName "ACTApi.exe"
#define ServiceName "ACTApiService"

[Setup]
AppId={{044c6f3f-41a7-4c39-8a85-eeb642dc522d}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=ACTApiServiceSetup_{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
WizardStyle=modern
DisableWelcomePage=no
DisableDirPage=no
DisableReadyPage=yes

[Files]
Source: "Files\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Files\Settings\Settings.json"; DestDir: "{app}\Settings"; Flags: ignoreversion

[Dirs]
Name: "{app}\Logs"; Permissions: everyone-full
Name: "{app}\Photos"; Permissions: everyone-full
Name: "{app}\Settings"; Permissions: everyone-full

[Run]
Filename: "sc.exe"; Parameters: "stop {#ServiceName}"; Flags: runhidden waituntilterminated; StatusMsg: "Stopping existing service..."
Filename: "sc.exe"; Parameters: "delete {#ServiceName}"; Flags: runhidden waituntilterminated; StatusMsg: "Removing old service..."

Filename: "icacls.exe"; Parameters: """{app}"" /grant ""{code:GetServiceUser}:(OI)(CI)RX"""; Flags: runhidden waituntilterminated; StatusMsg: "Setting folder permissions..."
Filename: "icacls.exe"; Parameters: """{app}\Settings\Settings.json"" /grant ""{code:GetServiceUser}:(M)"""; Flags: runhidden waituntilterminated; StatusMsg: "Setting settings permissions..."
Filename: "icacls.exe"; Parameters: """{app}\Logs"" /grant ""{code:GetServiceUser}:(OI)(CI)F"""; Flags: runhidden waituntilterminated; StatusMsg: "Setting logs permissions..."

Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""{#MyAppName}"""; Flags: runhidden waituntilterminated
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""{#MyAppName}"" dir=in action=allow protocol=TCP localport={code:GetHttpServerPort}"; Flags: runhidden waituntilterminated; StatusMsg: "Configuring firewall..."
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""{#MyAppName} ACT"" dir=in action=allow protocol=TCP localport={code:GetACTServerPort}"; Flags: runhidden waituntilterminated

Filename: "sc.exe"; Parameters: "{code:GetCreateServiceParams}"; Flags: runhidden waituntilterminated; StatusMsg: "Installing service..."
Filename: "sc.exe"; Parameters: "description {#ServiceName} ""RVMS Visitor Management Service API"""; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "failure {#ServiceName} reset=86400 actions=restart/60000/restart/60000/restart/60000"; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "start {#ServiceName}"; Flags: runhidden waituntilterminated; StatusMsg: "Starting service..."

[UninstallRun]
Filename: "sc.exe"; Parameters: "stop {#ServiceName}"; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "delete {#ServiceName}"; Flags: runhidden waituntilterminated
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""{#MyAppName}"""; Flags: runhidden waituntilterminated
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""{#MyAppName} ACT"""; Flags: runhidden waituntilterminated

[Code]
var
  ServerPage: TInputQueryWizardPage;
  ACTCredPage: TInputQueryWizardPage;
  ServiceAccountPage: TInputQueryWizardPage;
  ShowUsersButton: TNewButton;
  ValidateButton: TNewButton;

// ── Show available local users ──
procedure ShowUsersButtonClick(Sender: TObject);
var
  TempFile: String;
  ResultCode: Integer;
  Lines: TArrayOfString;
  UserList: String;
  I: Integer;
begin
  TempFile := ExpandConstant('{tmp}\localusers.txt');
  Exec(ExpandConstant('{cmd}'), '/C net user > "' + TempFile + '"',
       '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  if LoadStringsFromFile(TempFile, Lines) then
  begin
    UserList := '';
    for I := 0 to GetArrayLength(Lines) - 1 do
      UserList := UserList + Lines[I] + #13#10;
    MsgBox('Available local accounts:' + #13#10 + #13#10 + UserList, mbInformation, MB_OK);
  end
  else
    MsgBox('Could not retrieve local user list.', mbError, MB_OK);

  DeleteFile(TempFile);
end;

// ── Validate the entered account ──
procedure ValidateButtonClick(Sender: TObject);
var
  Username, Domain, UserOnly: String;
  SlashPos, ResultCode: Integer;
begin
  Username := ServiceAccountPage.Values[0];

  if Username = '' then
  begin
    MsgBox('Please enter a username first.', mbError, MB_OK);
    Exit;
  end;

  // Split DOMAIN\User or .\User
  SlashPos := Pos('\', Username);
  if SlashPos > 0 then
  begin
    Domain   := Copy(Username, 1, SlashPos - 1);
    UserOnly := Copy(Username, SlashPos + 1, Length(Username));
  end
  else
  begin
    Domain   := '.';
    UserOnly := Username;
  end;

  // Check local or domain account
  if (Domain = '.') or (CompareText(Domain, GetComputerNameString) = 0) then
    Exec(ExpandConstant('{cmd}'), '/C net user "' + UserOnly + '"',
         '', SW_HIDE, ewWaitUntilTerminated, ResultCode)
  else
    Exec(ExpandConstant('{cmd}'), '/C net user "' + UserOnly + '" /domain',
         '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  if ResultCode = 0 then
    MsgBox('Account "' + Username + '" is valid.', mbInformation, MB_OK)
  else
    MsgBox('Account "' + Username + '" was NOT found.' + #13#10 +
           'Use .\Username for local or DOMAIN\Username for domain accounts.',
           mbError, MB_OK);
end;

// ── Wizard pages ──
procedure InitializeWizard;
begin
  // Page 1: Server settings (4 fields — fits in window)
  ServerPage := CreateInputQueryPage(wpSelectDir,
    'Server Configuration',
    'Enter HTTP and ACT server addresses',
    'Configure the server endpoints for this service.');
  ServerPage.Add('HTTP Server (e.g., localhost):', False);
  ServerPage.Add('HTTP Port:', False);
  ServerPage.Add('ACT Server (e.g., 192.168.2.121):', False);
  ServerPage.Add('ACT Port:', False);
  ServerPage.Values[0] := 'localhost';
  ServerPage.Values[1] := '8021';
  ServerPage.Values[2] := '192.168.2.121';
  ServerPage.Values[3] := '8004';

  // Page 2: ACT credentials (2 fields)
  ACTCredPage := CreateInputQueryPage(ServerPage.ID,
    'ACT Credentials',
    'Enter the ACT application credentials',
    'These are the credentials used to authenticate with the ACT server.');
  ACTCredPage.Add('ACT Username:', False);
  ACTCredPage.Add('ACT Password:', True);
  ACTCredPage.Values[0] := 'fenky';
  ACTCredPage.Values[1] := '';

  // Page 3: Windows service account (2 fields + 2 buttons)
  ServiceAccountPage := CreateInputQueryPage(ACTCredPage.ID,
    'Windows Service Account',
    'Enter the Windows account to run the service',
    'This account''s Windows credentials are used for SecurityMode.Transport.' + #13#10 +
    'Use .\Username for a local account or DOMAIN\Username for a domain account.');
  ServiceAccountPage.Add('Windows Username:', False);
  ServiceAccountPage.Add('Windows Password:', True);
  ServiceAccountPage.Values[0] := '.\Administrator';
  ServiceAccountPage.Values[1] := '';

  // "Show Local Users" button
  ShowUsersButton := TNewButton.Create(ServiceAccountPage);
  ShowUsersButton.Parent   := ServiceAccountPage.Surface;
  ShowUsersButton.Caption  := 'Show Local Users';
  ShowUsersButton.Left     := 0;
  ShowUsersButton.Top      := ServiceAccountPage.Edits[1].Top +
                              ServiceAccountPage.Edits[1].Height + ScaleY(12);
  ShowUsersButton.Width    := ScaleX(130);
  ShowUsersButton.Height   := ScaleY(25);
  ShowUsersButton.OnClick  := @ShowUsersButtonClick;

  // "Validate Account" button
  ValidateButton := TNewButton.Create(ServiceAccountPage);
  ValidateButton.Parent   := ServiceAccountPage.Surface;
  ValidateButton.Caption  := 'Validate Account';
  ValidateButton.Left     := ShowUsersButton.Left + ShowUsersButton.Width + ScaleX(10);
  ValidateButton.Top      := ShowUsersButton.Top;
  ValidateButton.Width    := ScaleX(130);
  ValidateButton.Height   := ScaleY(25);
  ValidateButton.OnClick  := @ValidateButtonClick;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
end;

// ── Validate on Next button ──
function NextButtonClick(CurPageID: Integer): Boolean;
var
  Username, Domain, UserOnly: String;
  SlashPos, ResultCode: Integer;
begin
  Result := True;

  if CurPageID = ServiceAccountPage.ID then
  begin
    Username := ServiceAccountPage.Values[0];

    if Username = '' then
    begin
      MsgBox('Windows Username is required.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if ServiceAccountPage.Values[1] = '' then
    begin
      MsgBox('Windows Password is required.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    // Parse and validate account exists
    SlashPos := Pos('\', Username);
    if SlashPos > 0 then
    begin
      Domain   := Copy(Username, 1, SlashPos - 1);
      UserOnly := Copy(Username, SlashPos + 1, Length(Username));
    end
    else
    begin
      Domain   := '.';
      UserOnly := Username;
    end;

    if (Domain = '.') or (CompareText(Domain, GetComputerNameString) = 0) then
      Exec(ExpandConstant('{cmd}'), '/C net user "' + UserOnly + '"',
           '', SW_HIDE, ewWaitUntilTerminated, ResultCode)
    else
      Exec(ExpandConstant('{cmd}'), '/C net user "' + UserOnly + '" /domain',
           '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

    if ResultCode <> 0 then
    begin
      MsgBox('Account "' + Username + '" was not found.' + #13#10 +
             'Click "Show Local Users" to see available accounts.',
             mbError, MB_OK);
      Result := False;
      Exit;
    end;
  end;
end;

// ── Helper functions for [Run] section ──
function GetHttpServerPort(Param: String): String;
begin
  Result := ServerPage.Values[1];
end;

function GetACTServerPort(Param: String): String;
begin
  Result := ServerPage.Values[3];
end;

function GetServiceUser(Param: String): String;
begin
  Result := ServiceAccountPage.Values[0];
end;

function GetCreateServiceParams(Param: String): String;
begin
  Result := 'create {#ServiceName}' +
            ' binPath= "' + ExpandConstant('{app}') + '\{#MyAppExeName}"' +
            ' start= auto' +
            ' DisplayName= "{#MyAppName}"' +
            ' obj= "' + ServiceAccountPage.Values[0] + '"' +
            ' password= "' + ServiceAccountPage.Values[1] + '"';
end;

procedure UpdateSettingsFile;
var
  SettingsPath: String;
  AnsiContent: AnsiString;
  Content: String;
  ServerUrl, ACTServerUrl: String;
begin
  SettingsPath := ExpandConstant('{app}\Settings\Settings.json');
  if not FileExists(SettingsPath) then Exit;
  if not LoadStringFromFile(SettingsPath, AnsiContent) then Exit;
  Content := String(AnsiContent);

  ServerUrl    := 'http://' + ServerPage.Values[0] + ':' + ServerPage.Values[1];
  ACTServerUrl := ServerPage.Values[2] + ':' + ServerPage.Values[3];

  StringChangeEx(Content, '"Server": "http://localhost:8021"',
    '"Server": "' + ServerUrl + '"', True);
  StringChangeEx(Content, '"ACTServer": "192.168.2.121:8004"',
    '"ACTServer": "' + ACTServerUrl + '"', True);
  StringChangeEx(Content, '"ACTUsername": "fenky"',
    '"ACTUsername": "' + ACTCredPage.Values[0] + '"', True);
  StringChangeEx(Content, '"ACTPassword": "passwordsucks"',
    '"ACTPassword": "' + ACTCredPage.Values[1] + '"', True);

  SaveStringToFile(SettingsPath, AnsiString(Content), False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    UpdateSettingsFile;
end;