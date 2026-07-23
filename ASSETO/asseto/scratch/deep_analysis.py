import os, sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import pg_compat as _db

DATABASE_URL = os.environ.get(
    'DATABASE_URL',
    'postgresql://asseto:asseto@localhost:5432/asseto',
)

db = _db.connect(DATABASE_URL)

# Get all table names
tables = db.execute(
    "SELECT table_name FROM information_schema.tables WHERE table_schema='public' ORDER BY table_name"
).fetchall()

print(f"PostgreSQL tables ({len(tables)}):")
print("-" * 50)

for t in tables:
    name = t['table_name']
    count = db.execute(f"SELECT COUNT(*) FROM {name}").fetchone()
    cnt_val = list(count.values())[0] if count else 0
    print(f"  {name:30s}  {cnt_val:>6} rows")

db.close()

# Also analyze items in detail
db2 = _db.connect(DATABASE_URL)
print("\n--- Items by category ---")
cats = db2.execute("SELECT category, COUNT(*) as cnt, SUM(purchase_price) as total FROM items GROUP BY category ORDER BY cnt DESC").fetchall()
for c in cats:
    total = c.get('total') or 0
    print(f"  {c['category']:20s}  {c['cnt']:>4} шт  |  ${total:>10,.2f}")

print(f"\n--- Items by condition ---")
conds = db2.execute("SELECT condition, COUNT(*) as cnt FROM items GROUP BY condition ORDER BY cnt DESC").fetchall()
for c in conds:
    print(f"  {c['condition']:20s}  {c['cnt']:>4} шт")

print(f"\n--- Users ---")
users = db2.execute("SELECT role, COUNT(*) as cnt FROM users WHERE active=1 GROUP BY role ORDER BY cnt DESC").fetchall()
for u in users:
    print(f"  {u['role']:20s}  {u['cnt']:>4} users")

db2.close()
