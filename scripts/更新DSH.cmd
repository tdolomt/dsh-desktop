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
npm i -g @deepseek-ai/dsh@latest --allow-scripts=@deepseek-ai/dsh-subprocess-local,koffi,node-pty,@google/genai,protobufjs
echo.
echo Done. Please exit DSH Desktop from the tray and start it again.
pause
