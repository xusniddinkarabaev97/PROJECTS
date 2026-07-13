@echo off
chcp 65001 >nul
title GZS Billing

echo ============================================
echo   GZS Billing System
echo ============================================
echo.

echo [1/2] Starting .NET Backend API...
start "GZS Billing API" cmd /c "cd /d %~dp0src\GzsBilling.Api && dotnet run --launch-profile http"

echo [2/2] Starting Admin Panel (Vite)...
start "GZS Admin Panel" cmd /c "cd /d %~dp0admin-panel && npm run dev"

echo.
echo Waiting for services...
timeout /t 8 /nobreak >nul

start https://black.tail3183c8.ts.net

echo.
echo ============================================
echo   https://black.tail3183c8.ts.net
echo ============================================
echo   Admin Panel  +  API  +  Swagger
echo   HAMMASI BITTA URL DA!
echo.
echo   Login: admin / admin123!
echo   URL HECH QACHON O'ZGARMAYDI
echo ============================================
pause >nul
