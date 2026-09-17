@echo off
setlocal
set "ROOT=%~dp0.."
cd /d "%ROOT%"
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\arrancar-servidor.ps1"
if errorlevel 1 pause
