@echo off
setlocal

set MSBUILD=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
if not exist "%MSBUILD%" (
    echo ERROR: MSBuild not found.
    echo Expected: %MSBUILD%
    pause
    exit /b 1
)

echo Building video-titles.csproj ...
"%MSBUILD%" "%~dp0video-titles.csproj" /nologo /v:minimal /p:Configuration=Release

if %errorlevel% neq 0 (
    echo.
    echo Build FAILED. See errors above.
    pause
    exit /b 1
)

echo.
echo Build succeeded: %LOCALAPPDATA%\video-titles\video-titles.dll
echo You can now launch video-titles via video-titles.vbs
