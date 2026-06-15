; 魔法师 Installer - packages all dependencies
; Build: "C:\Program Files (x86)\NSIS\makensis.exe" installer-server.nsi

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "x64.nsh"

Var DotNetFound

;--- Basic Info ---
Name "魔法师"
OutFile "魔法师.Installer.exe"
InstallDir "$LOCALAPPDATA\魔法师"
RequestExecutionLevel user
ShowInstDetails show
ShowUninstDetails show

;--- Version Info ---
VIProductVersion "1.0.0.0"
VIAddVersionKey "ProductName" "魔法师"
VIAddVersionKey "CompanyName" "魔法师"
VIAddVersionKey "FileDescription" "魔法师 installer"
VIAddVersionKey "FileVersion" "1.0.0.0"
VIAddVersionKey "LegalCopyright" "魔法师"

;--- UI ---
!define MUI_ABORTWARNING
!define MUI_ICON "RemoteControl.Server\app.ico"
!define MUI_FINISHPAGE_RUN "$INSTDIR\RemoteControl.Server.exe"
!define MUI_FINISHPAGE_RUN_TEXT "启动魔法师"

!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "SimpChinese"

Function CheckDotNet4CurrentRegView
    ClearErrors
    ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Install"
    ${IfNot} ${Errors}
    ${AndIf} $0 == 1
        StrCpy $DotNetFound 1
    ${EndIf}
FunctionEnd

Function .onInit
    StrCpy $DotNetFound 0

    SetRegView 32
    Call CheckDotNet4CurrentRegView

    ${If} ${RunningX64}
        SetRegView 64
        Call CheckDotNet4CurrentRegView
        SetRegView 32
    ${EndIf}

    ${If} $DotNetFound != 1
        MessageBox MB_ICONSTOP ".NET Framework 4.x Full is required. Install .NET Framework 4.0 or later before installing 魔法师."
        Abort
    ${EndIf}
FunctionEnd

;--- Main Section ---
Section "Install"
    SetOutPath "$INSTDIR"
    SetOverwrite on
    CreateDirectory "$INSTDIR\Log"

    ; Core executable and DLLs
    File "RemoteControl.Server\bin\Debug\RemoteControl.Server.exe"
    File "RemoteControl.Server\bin\Debug\RemoteControl.Audio.dll"
    File "RemoteControl.Server\bin\Debug\RemoteControl.Protocals.dll"
    File "RemoteControl.Server\bin\Debug\log4net.dll"
    File "RemoteControl.Server\bin\Debug\IrisSkin2.dll"
    File "RemoteControl.Server\bin\Debug\Newtonsoft.Json.Lite.dll"

    ; Config files
    File "RemoteControl.Server\bin\Debug\RemoteControl.Server.exe.config"
    File "/oname=config.json" "installer-server-config.json"

    ; Client template for generating clients
    File "RemoteControl.Server\bin\Debug\RemoteControl.Client.dat"

    ; Client default icon
    File "Resources\yuan.ico"

    ; Avatars directory
    SetOutPath "$INSTDIR\Avatars"
    File "RemoteControl.Server\bin\Debug\Avatars\*.*"

    ; Skins directory (recursive)
    SetOutPath "$INSTDIR\Skins"
    File /r "RemoteControl.Server\bin\Debug\Skins\*.*"

    ; Tools directory
    SetOutPath "$INSTDIR\Tools"
    File "RemoteControl.Server\bin\Debug\Tools\*.*"

    ; Reset output path
    SetOutPath "$INSTDIR"

    ; Create shortcuts
    CreateDirectory "$SMPROGRAMS\魔法师"
    CreateShortCut "$SMPROGRAMS\魔法师\魔法师.lnk" "$INSTDIR\RemoteControl.Server.exe"
    CreateShortCut "$SMPROGRAMS\魔法师\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
    CreateShortCut "$DESKTOP\魔法师.lnk" "$INSTDIR\RemoteControl.Server.exe"

    ; Write uninstaller
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ; Register uninstall info
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\魔法师" "DisplayName" "魔法师"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\魔法师" "DisplayVersion" "1.0.0.0"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\魔法师" "Publisher" "魔法师"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\魔法师" "InstallLocation" "$INSTDIR"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\魔法师" "DisplayIcon" "$INSTDIR\RemoteControl.Server.exe"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\魔法师" "UninstallString" "$INSTDIR\Uninstall.exe"
    WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\魔法师" "NoModify" 1
    WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\魔法师" "NoRepair" 1
SectionEnd

;--- Uninstall Section ---
Section "Uninstall"
    ; Remove shortcuts
    Delete "$DESKTOP\魔法师.lnk"
    Delete "$SMPROGRAMS\魔法师\魔法师.lnk"
    Delete "$SMPROGRAMS\魔法师\Uninstall.lnk"
    RMDir "$SMPROGRAMS\魔法师"

    ; Remove core files
    Delete "$INSTDIR\RemoteControl.Server.exe"
    Delete "$INSTDIR\RemoteControl.Audio.dll"
    Delete "$INSTDIR\RemoteControl.Protocals.dll"
    Delete "$INSTDIR\log4net.dll"
    Delete "$INSTDIR\IrisSkin2.dll"
    Delete "$INSTDIR\Newtonsoft.Json.Lite.dll"
    Delete "$INSTDIR\RemoteControl.Server.exe.config"
    Delete "$INSTDIR\config.json"
    Delete "$INSTDIR\RemoteControl.Client.dat"
    Delete "$INSTDIR\yuan.ico"
    Delete "$INSTDIR\Uninstall.exe"

    ; Remove Avatars
    RMDir /r "$INSTDIR\Avatars"

    ; Remove Skins
    RMDir /r "$INSTDIR\Skins"

    ; Remove Tools
    RMDir /r "$INSTDIR\Tools"

    ; Remove Log directory
    RMDir /r "$INSTDIR\Log"

    ; Remove install directory
    RMDir "$INSTDIR"

    ; Remove registry
    DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\魔法师"
SectionEnd
