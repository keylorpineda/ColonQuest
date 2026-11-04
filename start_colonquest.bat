@echo off
rem ColonQuest: starts the local backend with your own keys and opens the simulator. See RUN_AND_BUILD.md.
cd /d "%~dp0"
set "UV=uv"
where uv >nul 2>nul && goto run
set "UV=%USERPROFILE%\.local\bin\uv.exe"
if exist "%UV%" goto run
echo Installing uv, which runs the local backend and brings its own Python (one time only)...
powershell -NoProfile -ExecutionPolicy ByPass -Command "irm https://astral.sh/uv/install.ps1 | iex" >nul
if exist "%UV%" goto run
echo uv could not be installed. Install it from https://docs.astral.sh/uv/ and run this file again.
pause
exit /b 1
:run
"%UV%" run --no-project --python 3.12 python scripts\run_local.py %*
if errorlevel 1 pause
