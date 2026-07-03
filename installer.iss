; Inno Setup Script for TexVibe GST Billing Software
; Fully offline installer configuration

[Setup]
AppId={{5A0D2361-3E8F-4BDB-8F1A-0D1E8E6E440A}}
AppName=TexVibe GST Billing
AppVersion=1.0.0
AppPublisher=Antigravity Textiles ERP
DefaultDirName={autopf}\TexVibe GST Billing
DefaultGroupName=TexVibe GST Billing
AllowNoIcons=yes
OutputDir=InstallerOutput
OutputBaseFilename=TexVibe_GST_Billing_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Source directory points to the published self-contained windows binary folder
Source: "BillingSoftware.UI\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\TexVibe GST Billing"; Filename: "{app}\BillingSoftware.UI.exe"
Name: "{group}\{cm:UninstallProgram,TexVibe GST Billing}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\TexVibe GST Billing"; Filename: "{app}\BillingSoftware.UI.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\BillingSoftware.UI.exe"; Description: "{cm:LaunchProgram,TexVibe GST Billing}"; Flags: nowait postinstall skipifsilent

[Dirs]
; Ensure local AppData directories are configured for Database and Backups at install time
Name: "{userappdata}\BillingSoftware\Database"
Name: "{userappdata}\BillingSoftware\Backups"
Name: "{userappdata}\BillingSoftware\Logs"
Name: "{userappdata}\BillingSoftware\Media"
