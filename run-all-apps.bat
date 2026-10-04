@echo off
setlocal
cd /d "%~dp0"

echo Starting BetPortal, BetPortalAdmin, BetPortalWebApi, and Claude Chat Service...

start "" /D "%~dp0bet-portal" cmd /k "npm start"
start "" /D "%~dp0bet-portal-admin" cmd /k "npm start"
start "" /D "%~dp0BetPortalWebApi" cmd /k "dotnet run"
start "" /D "%~dp0claude-chat-service" cmd /k ".\.venv\Scripts\python.exe -m uvicorn app.main:app --host 0.0.0.0 --port 8000"

echo.
echo All apps started in separate terminals.
echo API: http://localhost:5026
echo bet-portal: http://localhost:4200
echo bet-portal-admin: http://localhost:4210
echo claude-chat-service: http://localhost:8000
pause
