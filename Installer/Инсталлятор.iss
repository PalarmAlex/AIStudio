#define MyAppName "AIStudio"
#define MyAppVersion "4.0"
#define MyAppPublisher "МВАП"
#define MyAppURL "https://p-mvap.ru/"
#define MyAppExeName "AIStudio.exe"
#define BinDir "D:\ISIDA\Programms\app\AIStudio\bin\Debug"
#define DocsDir "D:\ISIDA\Programms\app\AIStudio\docs"
#define SymbiontEnvContractDll "D:\ISIDA\Programms\app\SymbiontEnv.Contract\bin\Debug\SymbiontEnv.Contract.dll"
#define SymbiontEnvContractXml "D:\ISIDA\Programms\app\SymbiontEnv.Contract\bin\Debug\SymbiontEnv.Contract.xml"

[Setup]
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
OutputDir=Setup
OutputBaseFilename=AIStudio_v{#MyAppVersion}
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
SetupIconFile=Setup\isida.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
DisableDirPage=no

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Основные файлы (из bin\Debug после сборки проекта)
Source: "{#BinDir}\AIStudio.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinDir}\isida.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinDir}\isida.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinDir}\Ookii.Dialogs.Wpf.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinDir}\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SymbiontEnvContractDll}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#DocsDir}\AdapterAuthorGuide.html"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "{#DocsDir}\AdapterContract.html"; DestDir: "{app}\docs"; Flags: ignoreversion

; Настройки (файл Settings.xml; ранее использовалось имя AIStudio.Settings.xml)
Source: "{#DocsDir}\Settings\Settings.xml"; DestDir: "{commonappdata}\ISIDA\Settings"; Flags: ignoreversion onlyifdoesntexist uninsneveruninstall

; Данные
; Важно: для Psychic нужен recursesubdirs — иначе не копируются Understanding\, Memory\Episodic\ и др.,
; без них циклы мышления получают инфо-функцию №0 и не находят решения в отчётах сценариев.
Source: "{#DocsDir}\Data\Actions\*"; DestDir: "{commonappdata}\ISIDA\Data\Actions"; Flags: recursesubdirs createallsubdirs onlyifdoesntexist uninsneveruninstall
Source: "{#DocsDir}\Data\Gomeostas\*"; DestDir: "{commonappdata}\ISIDA\Data\Gomeostas"; Flags: recursesubdirs createallsubdirs onlyifdoesntexist uninsneveruninstall
Source: "{#DocsDir}\Data\Reflexes\*"; DestDir: "{commonappdata}\ISIDA\Data\Reflexes"; Flags: recursesubdirs createallsubdirs onlyifdoesntexist uninsneveruninstall
Source: "{#DocsDir}\Data\Sensors\*"; DestDir: "{commonappdata}\ISIDA\Data\Sensors"; Flags: recursesubdirs createallsubdirs onlyifdoesntexist uninsneveruninstall
Source: "{#DocsDir}\Data\Psychic\*"; DestDir: "{commonappdata}\ISIDA\Data\Psychic"; Flags: recursesubdirs createallsubdirs onlyifdoesntexist uninsneveruninstall
Source: "{#DocsDir}\Data\Scenarios\*"; DestDir: "{commonappdata}\ISIDA\Scenarios"; Flags: recursesubdirs createallsubdirs onlyifdoesntexist uninsneveruninstall
Source: "{#DocsDir}\BootData\*"; DestDir: "{commonappdata}\ISIDA\BootData"; Flags: recursesubdirs createallsubdirs onlyifdoesntexist uninsneveruninstall

; Каркасы пакетов адаптеров («Создать пакет…» читает %ProgramData%\ISIDA\AdapterPackageTemplates\demo\)
; Исходник: docs\AdapterPackageTemplates\ (manifest, schema 2.0, BootData, README; runtime\ — только README до установки SDK)
Source: "{#DocsDir}\AdapterPackageTemplates\*"; DestDir: "{commonappdata}\ISIDA\AdapterPackageTemplates"; Flags: recursesubdirs createallsubdirs onlyifdoesntexist uninsneveruninstall

; Стартовый SDK в demo\runtime (для «Создать пакет…» и разработки host)
Source: "{#BinDir}\isida.dll"; DestDir: "{commonappdata}\ISIDA\AdapterPackageTemplates\demo\runtime"; Flags: ignoreversion onlyifdoesntexist uninsneveruninstall
Source: "{#BinDir}\isida.dll.config"; DestDir: "{commonappdata}\ISIDA\AdapterPackageTemplates\demo\runtime"; Flags: ignoreversion skipifsourcedoesntexist onlyifdoesntexist uninsneveruninstall
Source: "{#BinDir}\isida.xml"; DestDir: "{commonappdata}\ISIDA\AdapterPackageTemplates\demo\runtime"; Flags: ignoreversion onlyifdoesntexist uninsneveruninstall
Source: "{#SymbiontEnvContractDll}"; DestDir: "{commonappdata}\ISIDA\AdapterPackageTemplates\demo\runtime"; Flags: ignoreversion onlyifdoesntexist uninsneveruninstall
Source: "{#SymbiontEnvContractXml}"; DestDir: "{commonappdata}\ISIDA\AdapterPackageTemplates\demo\runtime"; Flags: ignoreversion onlyifdoesntexist uninsneveruninstall
Source: "{#BinDir}\Newtonsoft.Json.dll"; DestDir: "{commonappdata}\ISIDA\AdapterPackageTemplates\demo\runtime"; Flags: ignoreversion onlyifdoesntexist uninsneveruninstall

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Dirs]
Name: "{commonappdata}\ISIDA"; Permissions: users-modify
Name: "{commonappdata}\ISIDA\Settings"; Permissions: users-modify
Name: "{commonappdata}\ISIDA\Logs"; Permissions: users-modify
Name: "{commonappdata}\ISIDA\BootData"; Permissions: users-modify
Name: "{commonappdata}\ISIDA\Data"; Permissions: users-modify
Name: "{commonappdata}\ISIDA\Data\Actions"; Permissions: users-modify
Name: "{commonappdata}\ISIDA\Data\Gomeostas"; Permissions: users-modify
Name: "{commonappdata}\ISIDA\Data\Reflexes"; Permissions: users-modify
Name: "{commonappdata}\ISIDA\Data\Sensors"; Permissions: users-modify
Name: "{commonappdata}\ISIDA\Data\Psychic\Automatism"; Permissions: users-modify
Name: "{commonappdata}\ISIDA\Projects"; Permissions: users-modify
Name: "{commonappdata}\ISIDA\Scenarios"; Permissions: users-modify
Name: "{commonappdata}\ISIDA\Scenarios\Reports"; Permissions: users-modify
Name: "{commonappdata}\ISIDA\AdapterPackageTemplates"; Permissions: users-modify
Name: "{commonappdata}\ISIDA\Adapters"; Permissions: users-modify

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  NeedDotNet: Boolean;
  DotNetUrl: string;

function IsDotNet472OrHigherInstalled(): Boolean;
var
  Release: Cardinal;
begin
  Result := False;
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) then
  begin
    Result := Release >= 461310; // .NET 4.7.2 или выше
  end;
end;

function OnDownloadProgress(const Url, FileName: string; const Progress, ProgressMax: Int64): Boolean;
begin
  if Progress = ProgressMax then
    Log(Format('Successfully downloaded file to {tmp}: %s', [FileName]));
  Result := True;
end;

function InitializeSetup(): Boolean;
var
  Dummy: Integer;
begin
  NeedDotNet := not IsDotNet472OrHigherInstalled();
  
  if NeedDotNet then
  begin
    DotNetUrl := 'https://go.microsoft.com/fwlink/?linkid=2088631'; // Официальная ссылка на .NET 4.8
    
    if MsgBox('Для работы программы требуется .NET Framework 4.8.' + #13#10 + #13#10 +
              'У вас не установлена необходимая версия .NET Framework.' + #13#10 +
              'Хотите установить его сейчас?' + #13#10 + #13#10 +
              'Да - будет открыт браузер для скачивания с официального сайта Microsoft' + #13#10 +
              'Нет - продолжить установку программы без .NET Framework (она может не работать)',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      // Открываем ссылку в браузере
      ShellExec('open', DotNetUrl, '', '', SW_SHOW, ewNoWait, Dummy);
      MsgBox('Пожалуйста, дождитесь завершения установки .NET Framework и перезапустите установщик.', 
             mbInformation, MB_OK);
      Result := False; // Прерываем установку
    end
    else
    begin
      if MsgBox('Программа может не работать без .NET Framework 4.8.' + #13#10 +
                'Продолжить установку?',
                mbConfirmation, MB_YESNO) = IDYES then
      begin
        Result := True;
      end
      else
      begin
        Result := False;
      end;
    end;
  end
  else
  begin
    Result := True;
  end;
end;

procedure InitializeUninstallProgressForm();
var
  DeleteData: Boolean;
begin
  DeleteData := MsgBox('Удалить файлы данных и настроек программы?' + #13#10 + #13#10 +
    'Да - удалить ВСЕ данные, настройки и шаблоны' + #13#10 +
    'Нет - оставить данные для возможной переустановки' + #13#10 + #13#10 +
    'Рекомендуется выбрать "Нет" если планируете переустановить программу позже.',
    mbConfirmation, MB_YESNO) = IDYES;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataPath: string;
  DeleteData: Boolean;
begin
  if CurUninstallStep = usUninstall then
  begin
    DeleteData := MsgBox('Удалить файлы данных и настроек программы?' + #13#10 + #13#10 +
      'Да - удалить ВСЕ данные, настройки и шаблоны' + #13#10 +
      'Нет - оставить данные для возможной переустановки',
      mbConfirmation, MB_YESNO) = IDYES;
    
    if DeleteData then
    begin
      DataPath := ExpandConstant('{commonappdata}\ISIDA');
      if DirExists(DataPath) then
      begin
        DelTree(DataPath, True, True, True);
      end;
    end;
  end;
end;