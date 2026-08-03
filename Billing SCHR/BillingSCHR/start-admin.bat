@echo off
chcp 65001 >nul
title BillingSCHR Admin Panel

echo ============================================
echo   BillingSCHR Admin Panel  (port 5174)
echo   Мобилизационный призывной резерв МО РУз
echo ============================================
echo.

cd /d "%~dp0admin-panel"

:Loop
echo [%date% %time%] Starting Admin Panel...
npx vite --host 0.0.0.0 --port 5174
echo.
echo [%date% %time%] Admin Panel stopped. Restarting in 3 seconds...
timeout /t 3 /nobreak >nul
goto Loop
