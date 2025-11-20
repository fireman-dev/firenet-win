[Setup]
AppName=FireNet VPN
AppVersion=1.0.0
DefaultDirName={pf}\FireNet
DefaultGroupName=FireNet
OutputBaseFilename=FireNetInstaller
Compression=lzma
SolidCompression=yes

[Files]
Source: "..\release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\xray-core\*"; DestDir: "{app}\xray-core"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\FireNet"; Filename: "{app}\FireNet.UI.exe"
Name: "{commondesktop}\FireNet"; Filename: "{app}\FireNet.UI.exe"

[Run]
Filename: "{app}\FireNet.UI.exe"; Description: "Run FireNet"; Flags: nowait postinstall skipifsilent
