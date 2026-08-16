@echo off
rem ============================================
rem  Install a new dsh plugin (double-click me)
rem  Example package names:
rem    @linxin666/dsh-client-ui-task-board
rem    @linxin666/dsh-ssh
rem ============================================
cd /d "%~dp0"
set PATH=%CD%\node;%CD%\global;%PATH%
set npm_config_prefix=%CD%\global
set npm_config_cache=%CD%\cache
set npm_config_userconfig=%CD%\.npmrc
set DSH_HOME=%CD%\data
set /p PKG=Enter plugin package name: 
if "%PKG%"=="" (echo No package entered. & pause & exit /b 1)
echo Installing %PKG% ...
dsh plugin --profile web add %PKG%
echo.
echo Done. Please exit DSH Web from the tray and start it again.
echo (New plugins show up under Settings - Plugin Config.)
pause
