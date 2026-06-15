Remove-Item 'C:\Users\Administrator\AppData\Local\RemoteControlClient\client.log' -Force -ErrorAction SilentlyContinue
$p = Start-Process -FilePath 'C:\Users\Administrator\AppData\Local\RemoteControlClient\RemoteControlClient.exe' -ArgumentList '/r' -PassThru
Start-Sleep -Seconds 8
Write-Host "--- Client Log ---"
Get-Content 'C:\Users\Administrator\AppData\Local\RemoteControlClient\client.log' -Force -ErrorAction SilentlyContinue
Write-Host "--- Process Status ---"
Write-Host "Process HasExited: $($p.HasExited)"
Write-Host "Process ID: $($p.Id)"
Get-Process -Id $p.Id -ErrorAction SilentlyContinue | Select-Object Id, ProcessName
