@echo off
chcp 65001 >nul
title SmartParking Admin Panel

echo ============================================
echo   SmartParking Admin Panel  (port 5174)
echo ============================================
echo.

cd /d "%~dp0admin-panel"

:: Kill any existing Vite process on port 5174
echo [%date% %time%] Checking port 5174...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr /r ":5174.*LISTENING"') do (
    echo Killing PID %%a on port 5174...
    taskkill /PID %%a /F >nul 2>&1
    timeout /t 2 /nobreak >nul
)

:Loop
echo [%date% %time%] Starting Admin Panel...
npx vite --host 0.0.0.0 --port 5174
echo.
echo [%date% %time%] Admin Panel stopped. Restarting in 3 seconds...
timeout /t 3 /nobreak >nul
goto Loop
