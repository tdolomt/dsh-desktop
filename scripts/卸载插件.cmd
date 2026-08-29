@echo off
rem ============================================
rem  Uninstall a dsh plugin (double-click me)
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
set /p PKG=Enter plugin package name to remove: 
if "%PKG%"=="" (echo No package entered. & pause & exit /b 1)
echo Removing %PKG% ...
call dsh plugin --profile web remove %PKG% --store-dir "%PNPM_BASE%\pnpm\store"
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
