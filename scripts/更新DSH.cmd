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
set npm_config_userconfig=%CD%\.npmrc
set DSH_HOME=%CD%\data
echo Updating DSH engine...
call npm i -g @deepseek-ai/dsh@latest --allow-scripts=@deepseek-ai/dsh-subprocess-local,koffi,node-pty,@google/genai,protobufjs
if errorlevel 1 (
    echo.
    echo [FAILED] Update did not complete. See the errors above.
    echo Common cause: network trouble. Retry later.
    pause
    exit /b 1
)
rem Re-apply the harness-home display patch (engine updates overwrite it):
rem show the real DSH_HOME path in instruction displays instead of the
rem unresolvable literal $DSH_HOME token, so agents can locate AGENTS.md.
powershell -NoProfile -Command "$f='%CD%\global\node_modules\@deepseek-ai\dsh\node_modules\@deepseek-ai\dsh-home-paths\lib\index.js'; if(Test-Path $f){ $c=Get-Content $f -Raw; if($c -notmatch 'resolvedHome;'){ $c=$c.Replace('`$${DSH_HOME_ENV}`','resolvedHome'); Set-Content $f $c -Encoding UTF8 -NoNewline; echo HOME-DISPLAY-PATCHED } else { echo HOME-DISPLAY-ALREADY-PATCHED } } else { echo HOME-DISPLAY-FILE-MISSING }"
echo.
echo DONE. Please exit DSH Desktop from the tray and start it again.
pause
