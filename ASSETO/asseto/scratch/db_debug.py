import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import pg_compat as _db

DATABASE_URL = os.environ.get(
    'DATABASE_URL',
    'postgresql://asseto:asseto@localhost:5432/asseto',
)

db = _db.connect(DATABASE_URL)
docs = db.execute("SELECT id, doc_number, status, pending_role, created_by_id, created_by_name FROM documents").fetchall()
print(f"Total docs: {len(docs)}")
for d in docs:
    print(dict(d))

users = db.execute("SELECT id, name, email, role FROM users").fetchall()
print("\nUsers:")
for u in users:
    print(dict(u))
db.close()
