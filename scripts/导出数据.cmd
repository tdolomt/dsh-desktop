@echo off
rem ============================================
rem  Export user data (credentials, sessions,
rem  settings, plugin config) to a zip on the
rem  desktop. Regenerable parts (node_modules,
rem  logs) are excluded.
rem ============================================
cd /d "%~dp0.."
for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmm"') do set "STAMP=%%i"
set "STAGE=%TEMP%\dsh_export_%RANDOM%"
for /f %%D in ('powershell -NoProfile -Command "[Environment]::GetFolderPath('Desktop')"') do set "DESKTOP=%%D"
set "OUT=%DESKTOP%\DSH-Data-%STAMP%.zip"
mkdir "%STAGE%" 2>nul
echo Exporting user data...
robocopy "%CD%\data" "%STAGE%" /e /xd node_modules logs /nfl /ndl /njh /njs >nul
if errorlevel 8 (
    echo Export failed: cannot copy data.
    rmdir /s /q "%STAGE%" 2>nul
    pause
    exit /b 1
)
powershell -NoProfile -Command "Compress-Archive -Path '%STAGE%\*' -DestinationPath '%OUT%' -Force"
rmdir /s /q "%STAGE%" 2>nul
if exist "%OUT%" (echo.
echo Done! Backup saved to:
echo   %OUT%
echo.
echo Keep this file safe - it contains your API credentials.
) else (echo Export failed.)
pause
