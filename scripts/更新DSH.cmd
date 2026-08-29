@echo off
rem ============================================
rem  Update the DSH engine (double-click me)
rem  Your data (sessions/config/credentials) is
rem  NOT touched by this update.
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
echo Checking current DSH engine version...
for /f "tokens=3 delims=@" %%V in ('npm list -g @deepseek-ai/dsh --depth=0 2^>nul ^| findstr /R "dsh@"') do set "CUR=%%V"
for /f %%V in ('npm view @deepseek-ai/dsh version 2^>nul') do set "LATEST=%%V"
if not defined CUR set "CUR=unknown"
if not defined LATEST set "LATEST=unknown"
if "%CUR%"=="%LATEST%" (
    echo Already up to date: @deepseek-ai/dsh@%CUR%
    echo.
    echo Please exit DSH Desktop from the tray and start it again.
    pause
    exit /b 0
)
echo Current: %CUR%
echo Latest:  %LATEST%
echo.
echo Updating DSH engine...
call npm i -g @deepseek-ai/dsh@latest --allow-scripts=@deepseek-ai/dsh-subprocess-local,koffi,node-pty,@google/genai,protobufjs
if errorlevel 1 (
    echo.
    echo [FAILED] Update did not complete. See the errors above.
    echo Common cause: network trouble. Retry later.
    pause
    exit /b 1
)
for /f "tokens=3 delims=@" %%V in ('npm list -g @deepseek-ai/dsh --depth=0 2^>nul ^| findstr /R "dsh@"') do set "NEW=%%V"
if defined NEW echo Updated to: @deepseek-ai/dsh@%NEW%
rem Re-apply the harness-home display patch (engine updates overwrite it):
rem show the real DSH_HOME path in instruction displays instead of the
rem unresolvable literal $DSH_HOME token, so agents can locate AGENTS.md.
powershell -NoProfile -Command "$f='%CD%\global\node_modules\@deepseek-ai\dsh\node_modules\@deepseek-ai\dsh-home-paths\lib\index.js'; if(Test-Path $f){ $c=Get-Content $f -Raw; if($c -notmatch 'resolvedHome;'){ $c=$c.Replace('`$${DSH_HOME_ENV}`','resolvedHome'); Set-Content $f $c -Encoding UTF8 -NoNewline; echo HOME-DISPLAY-PATCHED } else { echo HOME-DISPLAY-ALREADY-PATCHED } } else { echo HOME-DISPLAY-FILE-MISSING }"
echo.
echo DONE. Please exit DSH Desktop from the tray and start it again.
pause
