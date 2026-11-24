[Setup]
AppId={{8F9B8A7E-1E4D-4C6C-9C36-6C8E6CB5E8B4}
AppName=Email Webhooks
AppVersion=1.0.0
AppPublisher=Email Webhooks
AppComments=Installs the Email Webhooks service and the required ASP.NET Core 8 runtime
DefaultDirName={autopf}\EmailWebhooks
DefaultGroupName=Email Webhooks
OutputDir=..\publish
OutputBaseFilename=EmailWebhooks-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName=Email Webhooks

[Files]
Source: "..\bin\ReleasePackage\*"; DestDir: "{app}"; Excludes: "EmailWebhooks-Setup.exe"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{commonappdata}\EmailWebhooks"

[Icons]
Name: "{group}\Uninstall Email Webhooks"; Filename: "{uninstallexe}"

[Code]
const
  ServiceName = 'EmailWebhooksService';
  ServiceDisplayName = 'Email Webhooks';
  ServiceDescription = 'Email Webhooks ASP.NET Core API';
  AspNetCoreRuntimeUrl = 'https://aka.ms/dotnet/8.0/aspnetcore-runtime-win-x64.exe';

var
  RuntimeInstalled: Boolean;

function IsAspNetCore8RuntimeInstalled: Boolean;
var
  FindRec: TFindRec;
begin
  Result := FindFirst(
    ExpandConstant('{autopf}\dotnet\shared\Microsoft.AspNetCore.App\8.*'),
    FindRec);
  if Result then
    FindClose(FindRec);
end;

procedure InitializeWizard;
begin
  RuntimeInstalled := IsAspNetCore8RuntimeInstalled;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpReady then
  begin
    if RuntimeInstalled then
      WizardForm.ReadyMemo.Lines.Add('ASP.NET Core 8 runtime: already installed')
    else
      WizardForm.ReadyMemo.Lines.Add('ASP.NET Core 8 runtime: will be downloaded and installed');
  end;
end;

function InstallAspNetCoreRuntime: Boolean;
var
  RuntimeInstaller: String;
  ResultCode: Integer;
begin
  Result := False;
  if RuntimeInstalled then
  begin
    Result := True;
    Exit;
  end;

  WizardForm.StatusLabel.Caption := 'Downloading ASP.NET Core 8 runtime...';
  RuntimeInstaller := ExpandConstant('{tmp}\aspnetcore-runtime-win-x64.exe');
  try
    DownloadTemporaryFile(AspNetCoreRuntimeUrl,
      'aspnetcore-runtime-win-x64.exe', '', nil);
  except
    MsgBox('The ASP.NET Core 8 runtime could not be downloaded. Email Webhooks cannot be installed without it.',
      mbError, MB_OK);
    Exit;
  end;

  WizardForm.StatusLabel.Caption := 'Installing ASP.NET Core 8 runtime...';
  if not Exec(RuntimeInstaller, '/install /quiet /norestart', '', SW_SHOW,
    ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then
  begin
    MsgBox('The ASP.NET Core 8 runtime installation failed. Error code: ' +
      IntToStr(ResultCode), mbError, MB_OK);
    Exit;
  end;

  RuntimeInstalled := IsAspNetCore8RuntimeInstalled;
  Result := RuntimeInstalled;
  if not Result then
    MsgBox('The ASP.NET Core 8 runtime installer completed, but the runtime was not detected.',
      mbError, MB_OK);
end;

function RunServiceCommand(const CommandLine: string): Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec(ExpandConstant('{sys}\sc.exe'), CommandLine, '',
    SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssInstall then
  begin
    if not InstallAspNetCoreRuntime then
      Abort;
  end;

  if CurStep = ssInstall then
  begin
    RunServiceCommand('stop ' + ServiceName);
    RunServiceCommand('delete ' + ServiceName);
  end;

  if CurStep = ssPostInstall then
  begin
    RunServiceCommand('create ' + ServiceName + ' binPath= "' +
      ExpandConstant('{app}\EmailWebhooks.exe') + '" start= auto DisplayName= "' +
      ServiceDisplayName + '"');
    RunServiceCommand('description ' + ServiceName + ' "' + ServiceDescription + '"');
    Exec(ExpandConstant('{sys}\reg.exe'),
      'add "HKLM\SYSTEM\CurrentControlSet\Services\' + ServiceName +
      '" /v Environment /t REG_MULTI_SZ /d "SQLITE_PATH=' +
      ExpandConstant('{commonappdata}\EmailWebhooks\emailwebhooks.db') + '" /f',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    RunServiceCommand('start ' + ServiceName);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    RunServiceCommand('stop ' + ServiceName);
    RunServiceCommand('delete ' + ServiceName);
  end;
end;