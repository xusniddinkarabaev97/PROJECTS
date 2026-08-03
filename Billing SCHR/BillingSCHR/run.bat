@echo off
title BillingSCHR — МПР

echo ============================================
echo   BillingSCHR — Мобилизационный призывной резерв
echo   Admin:    http://localhost:5174
echo   Login:    admin / admin123
echo ============================================
echo.

set "ROOT=%~dp0"

echo Starting API...
start "BillingSCHR API" cmd /k "cd /d "%ROOT%" && dotnet run --launch-profile http"

echo Starting Admin Panel...
start "BillingSCHR Admin" cmd /k "cd /d "%ROOT%admin-panel" && npx vite --host 0.0.0.0 --port 5174"

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
