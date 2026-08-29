@echo off
rem ============================================
rem  Update all DSH web plugins (double-click me)
rem ============================================
cd /d "%~dp0.."
set PATH=%CD%\node;%CD%\global;%PATH%
set npm_config_prefix=%CD%\global
set npm_config_cache=%CD%\cache
rem pnpm cache/store 独立数据目录(默认安装目录 cache\pnpm;pnpmstore.ini 优先)
set "PNPM_BASE=%CD%\cache\pnpm"
if exist "%CD%\pnpmstore.ini" for /f "usebackq eol=# tokens=*" %%L in ("%CD%\pnpmstore.ini") do if not "%%L"=="" set "PNPM_BASE=%%L"
set "PNPM_HOME=%PNPM_BASE%\pnpm"
set "XDG_CACHE_HOME=%PNPM_BASE%\cache"
set "XDG_STATE_HOME=%PNPM_BASE%\state"
set npm_config_userconfig=%CD%\.npmrc
set DSH_HOME=%CD%\data
echo Checking plugin updates...
pushd "%CD%\data\profiles\web"
call pnpm outdated
set "OUTDATED=%errorlevel%"
popd
if "%OUTDATED%"=="0" (
    echo.
    echo All plugins are up to date.
    pause
    exit /b 0
)
echo.
echo Updating plugins...
call dsh plugin --profile web up --latest --store-dir "%PNPM_BASE%\pnpm\store"
if errorlevel 1 (
    echo.
    echo [FAILED] Update did not complete. See the errors above.
    echo Common cause: network trouble. Retry later.
    pause
    exit /b 1
)
echo.
echo DONE. Please exit DSH Desktop from the tray and start it again.
pause
