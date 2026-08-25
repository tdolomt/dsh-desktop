@echo off
rem ============================================
rem  Update all DSH web plugins (double-click me)
rem ============================================
cd /d "%~dp0.."
set PATH=%CD%\node;%CD%\global;%PATH%
set npm_config_prefix=%CD%\global
set npm_config_cache=%CD%\cache
set npm_config_userconfig=%CD%\.npmrc
set DSH_HOME=%CD%\data
echo Updating plugins...
call dsh plugin --profile web up --latest
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
