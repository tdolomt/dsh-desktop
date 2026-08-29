@echo off
rem ============================================
rem  DSH web portable - one-click rebuild
rem  Requirements: Windows 10/11 x64, 7-Zip,
rem                .NET Framework 4.x (built-in)
rem  Input:  DSH-Portable\  (the payload kit)
rem  Output: DSH-Desktop-2.1.0.zip (distributable)
rem ============================================
setlocal
cd /d "%~dp0"
set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

rem locate 7-Zip
set "SZ="
if exist "D:\Program Files\7-Zip-Zstandard\7z.exe" set "SZ=D:\Program Files\7-Zip-Zstandard\7z.exe"
if not defined SZ if exist "C:\Program Files\7-Zip\7z.exe" set "SZ=C:\Program Files\7-Zip\7z.exe"
if not defined SZ if exist "C:\Program Files (x86)\7-Zip\7z.exe" set "SZ=C:\Program Files (x86)\7-Zip\7z.exe"
if not defined SZ (echo 7-Zip not found - install 7-Zip first. & pause & exit /b 1)

echo [0/3] Compiling tools...
"%CSC%" /nologo /target:exe /out:"%~dp0packer.exe" /codepage:65001 /r:System.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll "%~dp0src\Packer.cs"
if errorlevel 1 (echo COMPILE PACKER FAILED & pause & exit /b 1)
"%CSC%" /nologo /target:winexe /out:"%~dp0DSH-Installer-Stub.exe" /codepage:65001 /win32icon:"%~dp0DSH-Portable\app\DSH.ico" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll "%~dp0src\Installer.cs"
if errorlevel 1 (echo COMPILE STUB FAILED & pause & exit /b 1)
"%CSC%" /nologo /target:winexe /out:"%~dp0DSH-Portable\uninstall.exe" /codepage:65001 /win32icon:"%~dp0DSH-Portable\app\DSH.ico" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "%~dp0src\Uninstaller.cs"
if errorlevel 1 (echo COMPILE UNINSTALLER FAILED & pause & exit /b 1)

echo [1/3] Packing and encrypting payload...
rem zip with 7-Zip (multithreaded; the old single-threaded .NET ZipArchive
rem took minutes over ~36k files), then AES-encrypt into payload.dat
del /q "%TEMP%\dsh_payload_tmp.zip" 2>nul
"%SZ%" a -tzip -mx=5 -mmt=on "%TEMP%\dsh_payload_tmp.zip" "%~dp0DSH-Portable\*" >nul
if errorlevel 1 (echo PACK ZIP FAILED & pause & exit /b 1)
"%~dp0packer.exe" "%TEMP%\dsh_payload_tmp.zip" "%~dp0payload.dat"
if errorlevel 1 (echo ENCRYPT FAILED & pause & exit /b 1)
del /q "%TEMP%\dsh_payload_tmp.zip" 2>nul

echo [2/3] Assembling dist zip...
del /q "%~dp0DSH-Desktop-2.1.0.zip" 2>nul
"%SZ%" a -tzip -mx=5 "%~dp0DSH-Desktop-2.1.0.zip" "%~dp0DSH-Installer-Stub.exe" "%~dp0payload.dat" "%~dp0docs\*.txt" >nul
if errorlevel 1 (echo ZIP FAILED & pause & exit /b 1)

echo [3/3] Done!
echo Output: %~dp0DSH-Desktop-2.1.0.zip
pause
