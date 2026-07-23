@echo off
title ASSETO — Inventory Management
chcp 65001 >nul
echo ==========================================
echo    ASSETO — Inventory & Office System
echo ==========================================
echo.

cd /d "%~dp0asseto"

:: ── Docker (recommended) ────────────────────────────────────────────
echo [*] Проверка Docker...
docker info >nul 2>&1
if %errorlevel% equ 0 (
    echo [+] Docker найден — запуск контейнеров...
    docker compose up -d --build
    if %errorlevel% equ 0 (
        echo.
        echo ==========================================
        echo   ASSETO запущен!
        echo   Адрес: http://localhost:8000
        echo   Логин: admin@asseto.uz / admin123
        echo ==========================================
        start "" "http://localhost:8000/login"
        echo.
        echo   Логи:   docker compose logs -f web
        echo   Стоп:   docker compose down
        echo.
        pause
        exit /b
    )
    echo [!] Ошибка Docker Compose.
    pause
    exit /b
)

:: ── Docker не найден ───────────────────────────────────────────────
echo [!] Docker не найден!
echo.
echo ASSETO требует PostgreSQL. Без Docker нужно установить:
echo   1. PostgreSQL 16+
echo   2. Создать БД: CREATE DATABASE asseto
echo   3. Настроить .env: DATABASE_URL=postgresql://...
echo.
echo Самый простой способ — установи Docker Desktop:
echo   https://www.docker.com/products/docker-desktop/
echo.
pause
