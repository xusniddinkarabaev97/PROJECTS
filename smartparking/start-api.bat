@echo off
chcp 65001 >nul
title SmartParking API

echo ============================================
echo   SmartParking .NET API  (port 5121)
echo ============================================
echo.

cd /d "%~dp0SmartParking"

:: Kill any existing process on port 5121
echo [%date% %time%] Checking port 5121...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr /r ":5121.*LISTENING"') do (
    echo Killing PID %%a on port 5121...
    taskkill /PID %%a /F >nul 2>&1
    timeout /t 2 /nobreak >nul
)

:Loop
echo [%date% %time%] Starting API...
dotnet run --launch-profile http
echo.
echo [%date% %time%] API stopped. Restarting in 3 seconds...
timeout /t 3 /nobreak >nul
goto Loop
