@echo off
title ASSETO — Inventory Management System
chcp 65001 >nul
cd /d "%~dp0asseto"

echo.
echo =============================================
echo   ASSETO — Inventory & Office System
echo =============================================
echo.
echo   Адрес:  http://localhost:5000
echo   Логин:  admin@tracko.uz
echo   Пароль: admin123
echo.
echo   Стоп:   Ctrl+C или закрыть окно
echo =============================================
echo.

python app.py

pause
