; 副控管理端 Installer
; Build: "C:\Program Files (x86)\NSIS\makensis.exe" installer-subcontroller.nsi

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "x64.nsh"

Var DotNetFound

;--- Basic Info ---
Name "副控管理端"
OutFile "SubController.Installer.exe"
InstallDir "$LOCALAPPDATA\SubController"
RequestExecutionLevel user
ShowInstDetails show
ShowUninstDetails show

;--- Version Info ---
VIProductVersion "1.0.0.0"
VIAddVersionKey "ProductName" "副控管理端"
VIAddVersionKey "CompanyName" "RemoteControl"
VIAddVersionKey "FileDescription" "副控管理端 installer"
VIAddVersionKey "FileVersion" "1.0.0.0"
VIAddVersionKey "LegalCopyright" "RemoteControl"

;--- UI ---
!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\RemoteControl.SubController.exe"
!define MUI_FINISHPAGE_RUN_TEXT "启动副控管理端"

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
        MessageBox MB_ICONSTOP ".NET Framework 4.x is required. Please install .NET Framework 4.0 or later."
        Abort
    ${EndIf}
FunctionEnd

;--- Main Section ---
Section "Install"
    SetOutPath "$INSTDIR"
    SetOverwrite on

    ; Core executable and DLLs
    File "RemoteControl.SubController\bin\Debug\RemoteControl.SubController.exe"
    File "RemoteControl.SubController\bin\Debug\RemoteControl.Protocals.dll"
    File "RemoteControl.SubController\bin\Debug\log4net.dll"
    File "RemoteControl.SubController\bin\Debug\Newtonsoft.Json.Lite.dll"

    ; Create shortcuts
    CreateDirectory "$SMPROGRAMS\副控管理端"
    CreateShortCut "$SMPROGRAMS\副控管理端\副控管理端.lnk" "$INSTDIR\RemoteControl.SubController.exe"
    CreateShortCut "$SMPROGRAMS\副控管理端\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
    CreateShortCut "$DESKTOP\副控管理端.lnk" "$INSTDIR\RemoteControl.SubController.exe"

    ; Write uninstaller
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ; Register uninstall info
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\SubController" "DisplayName" "副控管理端"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\SubController" "DisplayVersion" "1.0.0.0"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\SubController" "Publisher" "RemoteControl"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\SubController" "InstallLocation" "$INSTDIR"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\SubController" "DisplayIcon" "$INSTDIR\RemoteControl.SubController.exe"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\SubController" "UninstallString" "$INSTDIR\Uninstall.exe"
    WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\SubController" "NoModify" 1
    WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\SubController" "NoRepair" 1
SectionEnd

;--- Uninstall Section ---
Section "Uninstall"
    ; Remove shortcuts
    Delete "$DESKTOP\副控管理端.lnk"
    Delete "$SMPROGRAMS\副控管理端\副控管理端.lnk"
    Delete "$SMPROGRAMS\副控管理端\Uninstall.lnk"
    RMDir "$SMPROGRAMS\副控管理端"

    ; Remove core files
    Delete "$INSTDIR\RemoteControl.SubController.exe"
    Delete "$INSTDIR\RemoteControl.Protocals.dll"
    Delete "$INSTDIR\log4net.dll"
    Delete "$INSTDIR\Newtonsoft.Json.Lite.dll"
    Delete "$INSTDIR\subcontroller-config.json"
    Delete "$INSTDIR\Uninstall.exe"

    ; Remove install directory
    RMDir "$INSTDIR"

    ; Remove registry
    DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\SubController"
SectionEnd
