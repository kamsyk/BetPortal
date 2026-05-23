@echo off
setlocal

set "COMPOSE_FILE=%~dp0..\docker-compose.qdrant.yml"

if not exist "%COMPOSE_FILE%" (
  echo [ERROR] Compose file not found: %COMPOSE_FILE%
  exit /b 1
)

if "%~1"=="" goto :help

if /I "%~1"=="up" goto :up
if /I "%~1"=="down" goto :down
if /I "%~1"=="health" goto :health
if /I "%~1"=="restart" goto :restart
if /I "%~1"=="status" goto :status
goto :help

:up
echo Starting Qdrant...
docker compose -f "%COMPOSE_FILE%" up -d || exit /b 1
goto :health

:down
echo Stopping Qdrant...
docker compose -f "%COMPOSE_FILE%" down
exit /b %errorlevel%

:restart
echo Restarting Qdrant...
docker compose -f "%COMPOSE_FILE%" down || exit /b 1
docker compose -f "%COMPOSE_FILE%" up -d || exit /b 1
goto :health

:status
docker compose -f "%COMPOSE_FILE%" ps
exit /b %errorlevel%

:health
echo Checking Qdrant health at http://localhost:6333/readyz ...
powershell -NoProfile -Command "try { $r = Invoke-RestMethod -Method Get -Uri 'http://localhost:6333/readyz' -TimeoutSec 5; Write-Host '[OK]' $r; exit 0 } catch { Write-Host '[ERROR] Qdrant is not reachable on localhost:6333'; exit 1 }"
exit /b %errorlevel%

:help
echo Usage: %~n0 ^<up^|down^|health^|restart^|status^>
echo.
echo   up      - Start Qdrant and run health check
echo   down    - Stop Qdrant
echo   health  - Check readiness endpoint
echo   restart - Restart Qdrant and run health check
echo   status  - Show compose service status
exit /b 1
