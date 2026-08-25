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
if errorlevel 1 (
    echo.
    echo [FAILED] Could not unzip the backup. See the errors above.
    pause
    exit /b 1
)
rem Re-link plugins according to the restored config
set PATH=%CD%\node;%CD%\global;%PATH%
set npm_config_prefix=%CD%\global
set npm_config_cache=%CD%\cache
set CI=true
cd /d "%CD%\data\profiles\web"
call pnpm install
if errorlevel 1 (
    echo.
    echo [FAILED] Plugin re-link failed. See the errors above.
    pause
    exit /b 1
)
cd /d "%~dp0.."
echo.
echo Restored! Please exit DSH Desktop from the tray and start it again.
pause
