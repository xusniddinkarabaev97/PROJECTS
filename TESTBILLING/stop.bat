@echo off
chcp 65001 >nul
title GZS Billing - Stop Services

echo ============================================
echo   Stopping GZS Billing Services...
echo ============================================
echo.

:: ── Kill processes by window title ──
echo [1/2] Stopping services...
taskkill /FI "WINDOWTITLE eq GZS Billing API*" /T /F >nul 2>&1
taskkill /FI "WINDOWTITLE eq GZS Admin Panel*" /T /F >nul 2>&1

:: ── Kill by port ──
for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":5036.*LISTENING" 2^>nul') do (
    taskkill /PID %%a /F >nul 2>&1
)
for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":5173.*LISTENING" 2^>nul') do (
    taskkill /PID %%a /F >nul 2>&1
)

:: ── Cleanup ──
echo [2/2] Cleaning up...
del /q "%~dp0lt-api.log" 2>nul
del /q "%~dp0lt-admin.log" 2>nul
del /q "%~dp0cf-tunnel.log" 2>nul

echo.
echo   All services stopped.
echo   Tailscale Funnel stays active (system service)
echo ============================================
pause >nul
