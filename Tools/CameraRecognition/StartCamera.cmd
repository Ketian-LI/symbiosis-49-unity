@echo off
setlocal EnableExtensions
cd /d "%~dp0"
if not exist "%~dp0SYMBIOSIS49-ArtCamera\SYMBIOSIS49-ArtCamera.exe" (
  echo The original-art camera detector is missing.
  exit /b 2
)
call "%~dp0SYMBIOSIS49-ArtCamera\StartArtCamera.cmd"
exit /b %errorlevel%
