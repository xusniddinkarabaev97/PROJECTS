"""
ASSETO — быстрый запуск для локального использования (PostgreSQL).
Требуется: локальный PostgreSQL или Docker.
Запуск: python start.py
"""
import os, sys, secrets, getpass, subprocess

print("""
╔═══════════════════════════════════════════╗
║          ASSETO  — Local Setup (PG)       ║
╚═══════════════════════════════════════════╝
""")

ENV_FILE = os.path.join(os.path.dirname(__file__), ".env")

# ── 1. Создаём .env если нет ──────────────────────────────────────────────────
if not os.path.exists(ENV_FILE):
    print("📝 Создаём .env файл...")
    secret = secrets.token_hex(32)
    with open(ENV_FILE, "w", encoding="utf-8") as f:
        f.write(f"SECRET_KEY={secret}\n")
        f.write("SECURE_COOKIES=False\n")
        f.write("HOST=0.0.0.0\n")
        f.write("PORT=5000\n")
        f.write("DATABASE_URL=postgresql://asseto:asseto@localhost:5432/asseto\n")
    print(f"   ✅ .env создан (SECRET_KEY: {secret[:8]}...)\n")
else:
    print("   ✅ .env уже существует\n")

# ── 2. Устанавливаем зависимости ──────────────────────────────────────────────
print("📦 Проверяем зависимости...")
try:
    import flask, openpyxl, qrcode, bcrypt, jwt, pyotp
    print("   ✅ Основные пакеты установлены\n")
except ImportError as e:
    print(f"   ⚠️  Не хватает: {e.name}")
    print("   Устанавливаем...")
    subprocess.run([sys.executable, "-m", "pip", "install", "-r", "requirements.txt", "-q"])
    print("   ✅ Установлено\n")

# ── 3. Инициализируем БД ──────────────────────────────────────────────────────
print("🗄️  Инициализируем базу данных...")
sys.path.insert(0, os.path.dirname(__file__))
from app import app, init_db, migrate_db, get_db, ROLES
with app.app_context():
    init_db()
    migrate_db()
print("   ✅ База данных готова\n")

# ── 4. Создаём admin если нет пользователей ──────────────────────────────────
with app.app_context():
    with get_db() as db:
        user_count = db.execute("SELECT COUNT(*) FROM users").fetchone()[0]

if user_count == 0:
    print("👤 Создаём первого администратора...\n")

    name = input("   Имя администратора [Администратор]: ").strip() or "Администратор"
    email = input("   Email [admin@asseto.local]: ").strip() or "admin@asseto.local"

    while True:
        pw = getpass.getpass("   Пароль (мин. 8 символов): ")
        if len(pw) >= 8:
            break
        print("   ❌ Пароль слишком короткий")

    company = input("   Название компании [Моя компания]: ").strip() or "Моя компания"

    import bcrypt as _bc
    pw_hash = _bc.hashpw(pw.encode(), _bc.gensalt()).decode()

    with app.app_context():
        with get_db() as db:
            db.execute(
                """UPDATE companies SET name=? WHERE id=1""",
                (company,)
            )
            db.execute(
                """INSERT INTO users
                   (name, email, password_hash, role, active, onboarding_done, company_id)
                   VALUES (?, ?, ?, 'superadmin', 1, 0, 1)""",
                (name, email, pw_hash)
            )

    print(f"\n   ✅ Администратор создан:")
    print(f"      Email:    {email}")
    print(f"      Пароль:   {'*' * len(pw)}")
    print(f"      Компания: {company}\n")
else:
    print(f"   ✅ База уже содержит {user_count} пользователей\n")

# ── 5. Показываем инструкцию ──────────────────────────────────────────────────
print("═" * 50)
print("""
🚀 ЗАПУСК:

   Откройте браузер: http://localhost:5000

   Или доступ по сети:
   http://ВАШ_IP:5000 (для коллег в одной сети)
""")

import socket
try:
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    s.connect(("8.8.8.8", 80))
    local_ip = s.getsockname()[0]
    s.close()
    print(f"   Ваш IP: http://{local_ip}:5000")
except:
    pass

print("""
   Для остановки: Ctrl+C

═══════════════════════════════════════════════
""")

# ── 6. Запускаем сервер ───────────────────────────────────────────────────────
os.environ["FLASK_ENV"] = "development"
from app import app, init_db, migrate_db
app.run(host="0.0.0.0", port=5000, debug=False, threaded=True)
