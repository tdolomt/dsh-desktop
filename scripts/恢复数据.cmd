@echo off
rem ============================================
rem  Restore user data from an exported zip.
rem  Use after (re)installing: run this in the
rem  new install directory, pick the backup zip.
rem ============================================
cd /d "%~dp0.."
set /p ZIP=Enter the backup zip path (or drag it here): 
if not exist "%ZIP%" (echo File not found. & pause & exit /b 1)
echo Restoring user data...
powershell -NoProfile -Command "Expand-Archive -Path '%ZIP%' -DestinationPath '%CD%\data' -Force"
rem Re-link plugins according to the restored config
set PATH=%CD%\node;%CD%\global;%PATH%
set npm_config_prefix=%CD%\global
set npm_config_cache=%CD%\cache
set CI=true
cd /d "%CD%\data\profiles\web"
pnpm install >nul 2>&1
cd /d "%~dp0.."
echo.
echo Restored! Please exit DSH Desktop from the tray and start it again.
pause
