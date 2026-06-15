@echo off
C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe C:\RemoteControl-1\RemoteControl.sln /p:Configuration=Debug /p:Platform="Mixed Platforms" /v:minimal > C:\RemoteControl-1\build_output.txt 2>&1
echo %ERRORLEVEL% > C:\RemoteControl-1\build_exitcode.txt
