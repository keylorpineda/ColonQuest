@echo off
rem Starts the ColonQuest backend on this computer. See RUN_AND_BUILD.md.
cd /d "%~dp0"
python scriptsun_local.py %*
pause
