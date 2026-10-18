@echo off
title NextGen Launcher
echo ========================================================
echo   Starting NextGen Coaching Web Platform
echo ========================================================
echo.

echo 1. Launching Web Frontend (http://localhost:5100)...
start "NextGen Web Frontend" cmd /k "cd /d %~dp0src\NextGenCoaching.Web && dotnet run --no-build --launch-profile http"

echo 2. Launching Backend API (http://localhost:5101)...
start "NextGen Backend API" cmd /k "cd /d %~dp0src\NextGenCoaching.Api && dotnet run --no-build --launch-profile http"

echo.
echo Waiting 3 seconds for servers to initialize...
timeout /t 3 /nobreak > nul

echo Opening http://localhost:5100 in your browser...
start http://localhost:5100

echo.
echo ========================================================
echo   Both services are running!
echo   Frontend: http://localhost:5100
echo   Backend API: http://localhost:5101
echo ========================================================
echo.
