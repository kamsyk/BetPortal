@echo off
setlocal enableextensions

echo Building library...
cd /d "D:/Develop/Angular/ks-ag-controls/ks-ag-controls-workspace/
call ng build ks-ag-controls --configuration production
cd /d "D:\Develop\Angular\ks-ag-controls\ks-ag-controls-workspace\dist\ks-ag-controls"
call npm pack

echo Starting installation...
cd /d "D:\Develop\CSharpProjects\BetPortal\bet-portal-admin"

call npm uninstall ks-ag-controls
if errorlevel 1 (
  echo Version update failed!
  exit /b %errorlevel%
)

echo Installing ks-ag-controls for Development ...
call npm install D:\Develop\Angular\ks-ag-controls\ks-ag-controls-workspace\dist\ks-ag-controls\ks-ag-controls-0.0.6.tgz
if errorlevel 1 (
  echo Version update failed!
  exit /b %errorlevel%
)
