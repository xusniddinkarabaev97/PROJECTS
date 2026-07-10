@echo off
chcp 65001 >nul
title SmartParking

echo ============================================
echo   SmartParking
echo   Admin:    http://localhost:5174
echo   Login:    admin@smartparking.uz / Admin123!
echo ============================================
echo.

echo Checking ports...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr /r ":5121.*LISTENING"') do (
    echo Killing PID %%a on port 5121...
    taskkill /PID %%a /F >nul 2>&1
)
for /f "tokens=5" %%a in ('netstat -ano ^| findstr /r ":5174.*LISTENING"') do (
    echo Killing PID %%a on port 5174...
    taskkill /PID %%a /F >nul 2>&1
)
timeout /t 1 /nobreak >nul

echo Starting API...
start "SmartParking API" cmd /k "cd /d %~dp0SmartParking && dotnet run --launch-profile http"

echo Starting Admin Panel...
start "SmartParking Admin" cmd /k "cd /d %~dp0admin-panel && npx vite --host 0.0.0.0 --port 5174"

echo.
echo Waiting for servers... 8..7..6..5..4..3..2..1..
timeout /t 8 /nobreak >nul

echo Opening browser...
start http://localhost:5174

echo.
echo ============================================
echo   READY!  http://localhost:5174
echo ============================================
pause
