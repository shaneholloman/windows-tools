@echo off
setlocal

if "%~1"=="" (
    echo Usage: img-to-svg ^<image_file^> [output.svg] [options]
    echo.
    echo Engines  ^(--engine^):
    echo   vtracer       ^(default^) fast, works on any image
    echo   starvector-1b AI model for icons/logos/diagrams  ~3 GB VRAM
    echo   starvector-8b AI model, higher quality           ~16 GB VRAM
    echo.
    echo Vtracer presets  ^(--preset^):
    echo   poster  ^(default^) - logos, icons, flat colour
    echo   photo             - photographs and gradients
    echo   bw                - black and white line art
    echo.
    echo Examples:
    echo   img-to-svg logo.png
    echo   img-to-svg photo.webp --preset photo
    echo   img-to-svg icon.png --engine starvector-8b
    exit /b 1
)

python "%~dp0img-to-svg.py" %*
