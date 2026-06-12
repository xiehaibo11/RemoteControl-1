; RemoteControl Client Installer - standard visible user install

!include "MUI2.nsh"

;--- Basic Info ---
Name "RemoteControl Client"
OutFile "RemoteControl.Client.Installer.exe"
InstallDir "$LOCALAPPDATA\RemoteControlClient"
RequestExecutionLevel user
ShowInstDetails show
ShowUninstDetails show

;--- Version Info ---
VIProductVersion "1.0.0.0"
VIAddVersionKey "ProductName" "RemoteControl Client"
VIAddVersionKey "CompanyName" "RemoteControl"
VIAddVersionKey "FileDescription" "Remote support client installer"
VIAddVersionKey "FileVersion" "1.0.0.0"
VIAddVersionKey "LegalCopyright" "RemoteControl"

;--- UI ---
!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\RemoteControl.Client.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Start RemoteControl Client"

!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

;--- Main Section ---
Section "Install"
    SetOutPath "$INSTDIR"
    SetOverwrite on

    File "/oname=RemoteControl.Client.exe" "RemoteControl.Client.Generated.exe"
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    CreateDirectory "$SMPROGRAMS\RemoteControl Client"
    CreateShortCut "$SMPROGRAMS\RemoteControl Client\RemoteControl Client.lnk" "$INSTDIR\RemoteControl.Client.exe"
    CreateShortCut "$SMPROGRAMS\RemoteControl Client\Uninstall RemoteControl Client.lnk" "$INSTDIR\Uninstall.exe"
    CreateShortCut "$DESKTOP\RemoteControl Client.lnk" "$INSTDIR\RemoteControl.Client.exe"

    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RemoteControl Client" "DisplayName" "RemoteControl Client"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RemoteControl Client" "DisplayVersion" "1.0.0.0"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RemoteControl Client" "Publisher" "RemoteControl"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RemoteControl Client" "InstallLocation" "$INSTDIR"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RemoteControl Client" "DisplayIcon" "$INSTDIR\RemoteControl.Client.exe"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RemoteControl Client" "UninstallString" "$INSTDIR\Uninstall.exe"
    WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RemoteControl Client" "NoModify" 1
    WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RemoteControl Client" "NoRepair" 1
SectionEnd

Section "Uninstall"
    Delete "$DESKTOP\RemoteControl Client.lnk"
    Delete "$SMPROGRAMS\RemoteControl Client\RemoteControl Client.lnk"
    Delete "$SMPROGRAMS\RemoteControl Client\Uninstall RemoteControl Client.lnk"
    RMDir "$SMPROGRAMS\RemoteControl Client"

    Delete "$INSTDIR\RemoteControl.Client.exe"
    Delete "$INSTDIR\Uninstall.exe"
    RMDir "$INSTDIR"

    DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RemoteControl Client"
SectionEnd
