@echo off
chcp 65001 >nul
title BillingSCHR API

echo ============================================
echo   BillingSCHR .NET API  (port 5122)
echo   Мобилизационный призывной резерв
echo ============================================
echo.

cd /d "%~dp0"

:Loop
echo [%date% %time%] Starting API...
dotnet run --launch-profile http
echo.
echo [%date% %time%] API stopped. Restarting in 3 seconds...
timeout /t 3 /nobreak >nul
goto Loop
