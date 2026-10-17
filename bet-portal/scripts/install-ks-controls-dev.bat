@echo off
setlocal enableextensions

set "PORTAL_DIR=%~dp0.."
set "LIBRARY_DIR=%~dp0..\..\..\..\Angular\ks-ag-controls\ks-ag-controls-workspace"

echo Building library...
cd /d "%LIBRARY_DIR%"
if errorlevel 1 exit /b %errorlevel%

call npm run build:lib:styles
if errorlevel 1 (
  echo Library styles build failed!
  exit /b %errorlevel%
)

call npm exec -- ng build ks-ag-controls --configuration production
if errorlevel 1 (
  echo Library build failed!
  exit /b %errorlevel%
)

cd /d "%LIBRARY_DIR%\dist\ks-ag-controls"
if errorlevel 1 exit /b %errorlevel%

call npm pack
if errorlevel 1 (
  echo Library packaging failed!
  exit /b %errorlevel%
)

echo Installing ks-ag-controls for Development ...
cd /d "%PORTAL_DIR%"
if errorlevel 1 exit /b %errorlevel%

call npm install "%LIBRARY_DIR%\dist\ks-ag-controls\ks-ag-controls-0.0.8.tgz"
if errorlevel 1 (
  echo Version update failed!
  exit /b %errorlevel%
)

echo ks-ag-controls installed successfully.
