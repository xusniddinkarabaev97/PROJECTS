@echo off
title ASSETO - Inventory & Office Management
color 0B
cd /d "%~dp0"

echo.
echo =============================================
echo   ASSETO - Inventory Management System
echo =============================================
echo.

:: Check Python
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Python not found!
    echo Install from https://python.org
    pause
    exit /b 1
)

:: Check PostgreSQL
psql --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [WARN] PostgreSQL not found in PATH
    echo Make sure PostgreSQL is running on localhost:5432
    echo.
)

:: Install dependencies
echo [*] Installing dependencies...
python -m pip install --quiet -r requirements.txt 2>nul

:: Start
echo [*] Starting ASSETO...
echo.
echo Open: http://localhost:5000
echo.
python start.py

pause
