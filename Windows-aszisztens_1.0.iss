[Setup]
AppName=Gemini Search App
AppVersion=1.0
DefaultDirName={autopf}\GeminiSearchApp
DefaultGroupName=Gemini Search App
OutputBaseFilename=GeminiInstaller
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
; Ez engedélyezi, hogy a telepítő elején felugorjon a nyelvválasztó!
ShowLanguageDialog=yes

[Languages]
; Itt definiáljuk a telepítő nyelveit
Name: "hu"; MessagesFile: "compiler:Languages\Hungarian.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; FONTOS: Ezt az útvonalat írd át a te lefordított .exe fájlod helyére!
Source: "bin\Release\net8.0-windows\win-x64\publish\SearchApp.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Gemini Search App"; Filename: "{app}\SearchApp.exe"
Name: "{autodesktop}\Gemini Search App"; Filename: "{app}\SearchApp.exe"; Tasks: desktopicon

[Ini]
; Létrehoz egy config.ini fájlt a GeminiSearchApp mappádban a választott nyelvvel!
Filename: "{userappdata}\GeminiSearchApp\config.ini"; Section: "Settings"; Key: "Language"; String: "{language}"

[Run]
Filename: "{app}\SearchApp.exe"; Description: "{cm:LaunchProgram,Gemini Search App}"; Flags: nowait postinstall skipifsilent 
Filename: "{app}\SearchApp.exe"; Description: "{cm:LaunchProgram,Gemini Search App}"; Flags: nowait postinstall skipifsilent shellexec