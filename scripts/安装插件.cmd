@echo off
rem ============================================
rem  Install a new dsh plugin (double-click me)
rem  Example package names:
rem    @linxin666/dsh-client-ui-task-board
rem    @linxin666/dsh-ssh
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
set /p PKG=Enter plugin package name: 
if "%PKG%"=="" (echo No package entered. & pause & exit /b 1)
echo Installing %PKG% ...
call dsh plugin --profile web add %PKG% --store-dir "%PNPM_BASE%\pnpm\store"
if errorlevel 1 (
    echo.
    echo [FAILED] Install did not complete. See the errors above.
    echo Common cause: network trouble. Retry later; a mirror can help.
    pause
    exit /b 1
)
echo.
echo DONE. Please exit DSH Desktop from the tray and start it again.
echo (New plugins show up under Settings - Plugin Config.)
pause
