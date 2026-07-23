import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import pg_compat as _db

DATABASE_URL = os.environ.get(
    'DATABASE_URL',
    'postgresql://asseto:asseto@localhost:5432/asseto',
)

db = _db.connect(DATABASE_URL)
users = db.execute("SELECT * FROM users").fetchall()
for u in users:
    print(f"ID: {u['id']}, Email: {u['email']}, Role: {u['role']}, Name: {u['name']}")
db.close()
