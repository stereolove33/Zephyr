@echo off
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ /r:System.dll /r:System.Core.dll /r:System.Windows.Forms.dll /win32icon:"%~dp0ui\zephyr.ico" /out:"%~dp0Zephyr.exe" "%~dp0launcher\ZephyrLauncher.cs"
exit /b %errorlevel%
