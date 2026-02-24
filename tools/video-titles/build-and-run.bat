@echo off
setlocal

echo Stopping any running video-titles instance...
call "%~dp0kill.bat"

call "%~dp0build.bat"
if %errorlevel% neq 0 exit /b 1

echo.
echo Launching video-titles...
wscript.exe "%~dp0video-titles.vbs"
