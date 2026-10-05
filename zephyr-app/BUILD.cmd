@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0BUILD.ps1" %*
if errorlevel 1 (
    echo.
    echo A compilacao foi interrompida. Copie o erro exibido acima.
    pause
    exit /b 1
)
echo.
echo Concluido. Abra Zephyr.exe nesta pasta.
pause
