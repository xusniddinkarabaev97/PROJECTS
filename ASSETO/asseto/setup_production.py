import os, sys, secrets, bcrypt

# Ensure we can import pg_compat
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pg_compat as _db

DATABASE_URL = os.environ.get(
    'DATABASE_URL',
    'postgresql://asseto:asseto@localhost:5432/asseto',
)


def setup():
    print("=== ASSETO Cloud ERP: Установка Production Окружения (PostgreSQL) ===")

    # 1. Generate secure .env
    env_path = ".env"
    if not os.path.exists(env_path):
        print("[*] Генерация файла .env с надежными ключами...")
        secret_key = secrets.token_hex(32)
        with open(env_path, "w", encoding="utf-8") as f:
            f.write(f"SECRET_KEY={secret_key}\n")
            f.write("FLASK_ENV=production\n")
            f.write("SECURE_COOKIES=True\n")
            f.write("JWT_EXPIRY=43200\n")
            f.write(f"DATABASE_URL={DATABASE_URL}\n")
        print("[+] .env успешно создан.")
    else:
        print("[!] .env уже существует. Пропускаем.")

    # 2. Setup Database and First Admin
    try:
        from app import init_db, migrate_db
        print("[*] Инициализация таблиц базы данных...")
        init_db()
        migrate_db()
        print("[+] Таблицы созданы.")
    except Exception as e:
        print(f"[!] Ошибка инициализации таблиц: {e}")
        return

    # 3. Create Super Admin
    admin_email = os.environ.get("ADMIN_EMAIL", "admin@asseto.uz").strip()
    admin_pass = os.environ.get("ADMIN_PASS", "admin123").strip()

    try:
        db = _db.connect(DATABASE_URL)

        row = db.execute("SELECT id FROM users WHERE email=?", (admin_email,)).fetchone()
        if row:
            print(f"[!] Пользователь {admin_email} уже существует.")
        else:
            print(f"[*] Создание супер-админа {admin_email}...")
            hashed = bcrypt.hashpw(admin_pass.encode('utf-8'), bcrypt.gensalt()).decode('utf-8')
            db.execute(
                """INSERT INTO users (name, email, password_hash, role, department, active)
                   VALUES (?, ?, ?, 'superadmin', 'IT', 1)""",
                ("Главный Администратор", admin_email, hashed),
            )
            db.commit()
            print(f"[+] Супер-админ {admin_email} успешно создан!")
        db.close()
    except Exception as e:
        print(f"[!] Ошибка работы с БД: {e}")

    print("\n=== Установка завершена! ===")
    print("Теперь вы можете запустить сервер командой:")
    print("docker compose up -d --build")
    print("Или локально: gunicorn -w 3 app:app")


if __name__ == "__main__":
    setup()
