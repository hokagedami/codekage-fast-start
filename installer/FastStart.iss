; FastStart Installer Script for Inno Setup
; https://jrsoftware.org/isinfo.php

#define MyAppName "FastStart"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "FastStart"
#define MyAppURL "https://github.com/yourusername/FastStart"
#define MyAppExeName "FastStart.UI.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
AppId={{B4F2D8A1-C5E7-4F9B-A2D3-8E6C1F7B9A5D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Output location
OutputDir=..\dist
OutputBaseFilename=FastStartSetup-{#MyAppVersion}
; Use the generated icon
SetupIconFile=..\assets\FastStart.ico
; Compression settings
Compression=lzma2/ultra64
SolidCompression=yes
; Modern installer appearance
WizardStyle=modern
; Require admin for Program Files installation
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
; Uninstall icon
UninstallDisplayIcon={app}\{#MyAppExeName}
; Minimum Windows version (Windows 11 22H2 = 10.0.22621)
MinVersion=10.0.22000
; Architecture - x64 only
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Start FastStart when Windows starts"; GroupDescription: "Startup Options:"; Flags: unchecked

[Files]
; Main application files from publish output
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Auto-start registry entry (only if task selected)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "FastStart"; ValueData: """{app}\{#MyAppExeName}"" --background"; Flags: uninsdeletevalue; Tasks: autostart

[Run]
; Launch app after installation (optional)
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Kill the app before uninstall
Filename: "taskkill"; Parameters: "/F /IM {#MyAppExeName}"; Flags: runhidden; RunOnceId: "KillFastStart"

[Code]
// Check if .NET 8 Desktop Runtime is installed
function IsDotNet8Installed(): Boolean;
var
  ResultCode: Integer;
begin
  // Check for .NET 8 Desktop Runtime using dotnet --list-runtimes
  Result := Exec('cmd.exe', '/c dotnet --list-runtimes | findstr /C:"Microsoft.WindowsDesktop.App 8."', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;

  // Check for .NET 8 Desktop Runtime
  if not IsDotNet8Installed() then
  begin
    if MsgBox('FastStart requires .NET 8 Desktop Runtime which was not detected on your system.' + #13#10 + #13#10 +
              'Would you like to download it now?' + #13#10 + #13#10 +
              'Click Yes to open the download page, or No to continue anyway.',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
    end;
  end;
end;

var
  ResultCode: Integer;
