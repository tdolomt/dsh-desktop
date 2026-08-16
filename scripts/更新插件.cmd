@echo off
rem ============================================
rem  Update all DSH web plugins (double-click me)
rem ============================================
cd /d "%~dp0"
set PATH=%CD%\node;%CD%\global;%PATH%
set npm_config_prefix=%CD%\global
set npm_config_cache=%CD%\cache
set npm_config_userconfig=%CD%\.npmrc
set DSH_HOME=%CD%\data
echo Updating plugins...
dsh plugin --profile web update
echo.
echo Done. Please exit DSH Web from the tray and start it again.
pause
