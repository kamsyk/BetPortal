@echo off
setlocal enableextensions

echo Starting installation...
cd /d "%~dp0.."

call npm uninstall ks-ag-controls
if errorlevel 1 (
  echo Version update failed!
  exit /b %errorlevel%
)

echo Installing ks-ag-controls for Production ...
call npm i ks-ag-controls
if errorlevel 1 (
  echo Version update failed!
  exit /b %errorlevel%
)
