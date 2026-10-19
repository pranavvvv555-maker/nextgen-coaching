@echo off
title NextGen Coaching Worldwide Public Server (Cloudflare HTTPS Tunnel)
cd /d "%~dp0"

echo ========================================================
echo   NEXTGEN IT COACHING PUBLIC CLOUD DEPLOYMENT
echo   (100%% FREE - NO CREDIT CARD - WORLDWIDE ACCESS)
echo ========================================================
echo.

:: 1. Check if port 5100 is already running
netstat -ano | findstr 127.0.0.1:5100 >nul 2>&1
if %errorlevel% equ 0 goto APP_RUNNING

echo [1/2] Starting NextGen Web Server in Production Mode...
if exist "publish\web\NextGenCoaching.Web.exe" (
    start "NextGen Web Server" /d "%~dp0publish\web" "%~dp0publish\web\NextGenCoaching.Web.exe" --urls "http://0.0.0.0:5100"
) else (
    start "NextGen Web Server" dotnet run --project src/NextGenCoaching.Web -c Release --urls "http://0.0.0.0:5100"
)
timeout /t 5 /nobreak >nul
goto START_TUNNEL

:APP_RUNNING
echo [1/2] NextGen Web Server is already running on port 5100.

:START_TUNNEL
echo.
echo [2/2] Connecting to Cloudflare Global Edge Network...
echo.
echo ========================================================
echo   Look for your worldwide public HTTPS link below:
echo   It will look like:  https://xxxxxx.trycloudflare.com
echo.
echo   Share this link with anyone anywhere on phone or PC!
echo   (Press Ctrl+C to stop the public server)
echo ========================================================
echo.

cloudflared.exe tunnel --url http://127.0.0.1:5100

pause
