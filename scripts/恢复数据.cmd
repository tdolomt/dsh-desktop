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
set "BACKUP_ZIP=%ZIP%"
set "DSH_DATA=%CD%\data"
powershell -NoProfile -Command "Expand-Archive -LiteralPath $env:BACKUP_ZIP -DestinationPath $env:DSH_DATA -Force"
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
rem pnpm cache/store 独立数据目录(默认安装目录 cache\pnpm;pnpmstore.ini 优先)
set "PNPM_BASE=%CD%\cache\pnpm"
if exist "%CD%\pnpmstore.ini" for /f "usebackq eol=# tokens=*" %%L in ("%CD%\pnpmstore.ini") do if not "%%L"=="" set "PNPM_BASE=%%L"
set "PNPM_HOME=%PNPM_BASE%\pnpm"
set "XDG_CACHE_HOME=%PNPM_BASE%\cache"
set "XDG_STATE_HOME=%PNPM_BASE%\state"
set CI=true
cd /d "%CD%\data\profiles\web"
call pnpm install --store-dir "%PNPM_BASE%\pnpm\store"
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
