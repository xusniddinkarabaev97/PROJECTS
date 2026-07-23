import os, sys, bcrypt

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import pg_compat as _db

DATABASE_URL = os.environ.get(
    'DATABASE_URL',
    'postgresql://asseto:asseto@localhost:5432/asseto',
)

PASSWORD = b"admin123"

ROLES = [
    ("Администратор", "admin@asseto.uz", "superadmin"),
    ("АХО Менеджер", "aho@asseto.uz", "aho"),
    ("HR Директор", "hr@asseto.uz", "hr"),
    ("Ген. Директор", "director@asseto.uz", "director"),
    ("Зам. Директора", "deputy@asseto.uz", "deputy"),
    ("Главный Бухгалтер", "acc@asseto.uz", "accountant"),
    ("Сотрудник 1", "emp1@asseto.uz", "employee"),
    ("Сотрудник 2", "emp2@asseto.uz", "employee"),
    ("Аудитор", "auditor@asseto.uz", "auditor"),
]


def seed():
    db = _db.connect(DATABASE_URL)

    pw_hash = bcrypt.hashpw(PASSWORD, bcrypt.gensalt()).decode()

    print("Seeding users...")
    for name, email, role in ROLES:
        row = db.execute("SELECT id FROM users WHERE email=?", (email,)).fetchone()
        if not row:
            cur = db.execute(
                "INSERT INTO users (name, email, password_hash, role, active, onboarding_done) VALUES (?,?,?,?,1,1)",
                (name, email, pw_hash, role),
            )
            print(f"  + {name} ({role}) — id={cur.lastrowid}")
        else:
            print(f"  = {name} ({role}) — already exists (id={row['id']})")

    db.commit()
    db.close()
    print("\n✓ Seed complete!")
    print("All users: admin123")


if __name__ == '__main__':
    seed()
