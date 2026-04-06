@echo off
setlocal enabledelayedexpansion

REM ============================================================================
REM BetPortal Database Update Tool - Development Environment
REM ============================================================================
REM This script runs EfDbTool to update database schema
REM for the BetPortal database in the Development environment.
REM
REM Prerequisites:
REM   - EfDbTool.exe must be built and available
REM   - BetPortal.MsSql.Development connection string must be configured
REM ============================================================================

REM Configuration
set "TOOL_PATH=<your path to folder with EfDbTool.exe>"
set "TOOL_EXE=EfDbTool.exe"
set "CONNECTION_NAME=BetPortal.MsSql.Development"
set "UPDATE_MODE=Update"

REM Display banner
echo.
echo ========================================
echo BetPortal Update Tool
echo ========================================
echo Connection: %CONNECTION_NAME%
echo Mode: %UPDATE_MODE%
echo.

REM Check if tool directory exists
if not exist "%TOOL_PATH%" (
    echo ERROR: Tool path not found: %TOOL_PATH%
    echo Please build the EfDbTool project first.
    exit /b 1
)

REM Check if tool executable exists
if not exist "%TOOL_PATH%\%TOOL_EXE%" (
    echo ERROR: Tool executable not found: %TOOL_EXE%
    echo Please ensure EfDbTool is built successfully.
    exit /b 1
)

REM Navigate to tool directory
echo Navigating to: %TOOL_PATH%
cd /d "%TOOL_PATH%"

REM Verify we're in the correct directory
if not exist "%TOOL_EXE%" (
    echo ERROR: Failed to navigate to tool directory.
    exit /b 1
)

REM Execute update tool
echo.
echo Running: %TOOL_EXE% %CONNECTION_NAME% %UPATE_MODE%
echo.

"%TOOL_EXE%" %CONNECTION_NAME% %UPDATE_MODE%

REM Capture exit code
set EXIT_CODE=%ERRORLEVEL%

REM Display result
echo.
if %EXIT_CODE% equ 0 (
    echo ========================================
    echo Update completed successfully!
    echo ========================================
) else (
    echo ========================================
    echo Update failed with exit code: %EXIT_CODE%
    echo ========================================
)

REM Return to original directory (optional)
cd /d "%~dp0"

echo.
pause
exit /b %EXIT_CODE%