Remove-Item 'C:\Users\Administrator\AppData\Local\RemoteControlClient\client.log' -Force -ErrorAction SilentlyContinue
Start-Process -FilePath 'C:\Users\Administrator\AppData\Local\RemoteControlClient\RemoteControlClient.exe' -ArgumentList '/r'
Start-Sleep -Seconds 8
Write-Host "--- Client Log ---"
Get-Content 'C:\Users\Administrator\AppData\Local\RemoteControlClient\client.log' -Force -ErrorAction SilentlyContinue
Write-Host "--- Processes ---"
Get-Process -Name "RemoteControl*" -ErrorAction SilentlyContinue | Select-Object Id, ProcessName | Format-Table -AutoSize
