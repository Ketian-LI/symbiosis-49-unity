@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo [SYMBIOSIS: 49] Board Game camera recognition
echo This tool reads the camera and sends marker positions only to 127.0.0.1:49549.
echo Connect the camera first. For Pocket 3, choose Webcam on the device.
echo.

if not exist ".venv\Scripts\python.exe" (
  echo Creating a local Python environment with py -3...
  where py >nul 2>nul
  if errorlevel 1 goto no_python
  py -3 -m venv ".venv"
  if errorlevel 1 goto setup_failed
)

if not exist ".venv\.requirements-ready" (
  echo Installing OpenCV in the local environment. Internet is needed this first time.
  ".venv\Scripts\python.exe" -m pip install -r "requirements.txt"
  if errorlevel 1 goto setup_failed
  type nul > ".venv\.requirements-ready"
)

echo.
choice /c YN /m "Probe camera indices 0-5 before starting"
if errorlevel 2 goto choose_camera
".venv\Scripts\python.exe" "symbiosis49_camera.py" --probe-cameras
echo.

:choose_camera
echo The integrated camera is often 0. Pocket 3 may be 1 or 2.
echo Use the preview to confirm the image is from the camera above the board.
choice /c 0123456789 /m "Select camera index 0-9"
if errorlevel 1 goto run_camera
goto setup_failed

:run_camera
set /a "CAMERA_INDEX=%errorlevel%-1"
echo Starting index %CAMERA_INDEX%. Press Q or Esc in the preview to stop.
".venv\Scripts\python.exe" "symbiosis49_camera.py" --source %CAMERA_INDEX% --preview
set "CAMERA_EXIT=%errorlevel%"
echo.
echo Camera process exited with code %CAMERA_EXIT%.
pause
exit /b %CAMERA_EXIT%

:no_python
echo Python launcher was not found. Install Python 3 and retry.
pause
exit /b 2

:setup_failed
echo Camera setup or selection failed. Check the error above and retry.
pause
exit /b 1
