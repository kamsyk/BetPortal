@echo off
setlocal enableextensions

echo Starting installation...
cd /d "%~dp0.."

call npm uninstall ks-ag-controls
if errorlevel 1 (
  echo Version update failed!
  exit /b %errorlevel%
)

echo Installing ks-ag-controls for Development ...
call npm install D:/Develop/Angular/ks-ag-controls/ks-ag-controls-workspace/dist/ks-ag-controls
if errorlevel 1 (
  echo Version update failed!
  exit /b %errorlevel%
)
