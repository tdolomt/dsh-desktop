@echo off
rem ============================================
rem  Uninstall a dsh plugin (double-click me)
rem ============================================
cd /d "%~dp0.."
set PATH=%CD%\node;%CD%\global;%PATH%
set npm_config_prefix=%CD%\global
set npm_config_cache=%CD%\cache
set npm_config_userconfig=%CD%\.npmrc
set DSH_HOME=%CD%\data
set /p PKG=Enter plugin package name to remove: 
if "%PKG%"=="" (echo No package entered. & pause & exit /b 1)
echo Removing %PKG% ...
call dsh plugin --profile web remove %PKG%
if errorlevel 1 (
    echo.
    echo [FAILED] Remove did not complete. See the errors above.
    pause
    exit /b 1
)
rem Also drop it from the bundles list if it is still referenced there
powershell -NoProfile -Command "$env:PKG='%PKG%'; $p='%CD%\data\profiles\web\package.json'; try { $j=Get-Content $p -Raw | ConvertFrom-Json; $b=@($j.dsh.profile.bundles | Where-Object { $_ -ne $env:PKG }); if($b.Count -ne $j.dsh.profile.bundles.Count){ $j.dsh.profile.bundles=$b; $j | ConvertTo-Json -Depth 10 | Set-Content $p -Encoding UTF8; 'bundles updated' } else { 'bundles already clean' } } catch { 'skip bundles cleanup' }"
echo.
echo DONE. Please exit DSH Desktop from the tray and start it again.
pause
