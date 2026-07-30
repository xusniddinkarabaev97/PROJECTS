"""Import data from JSON export into PostgreSQL"""
import json, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from modules.config import _db, DATABASE_URL

db = _db.connect(DATABASE_URL)

with open('data_export.json', encoding='utf-8') as f:
    data = json.load(f)

# Disable foreign key checks
try:
    db.execute("SET session_replication_role = 'replica'")
except:
    pass

for table, rows in data.items():
    if not rows:
        continue
    cols = list(rows[0].keys())
    placeholders = ', '.join(['%s'] * len(cols))
    colnames = ', '.join(f'"{c}"' for c in cols)
    sql = f'INSERT INTO "{table}" ({colnames}) VALUES ({placeholders}) ON CONFLICT DO NOTHING'
    for row in rows:
        try:
            vals = [row[c] for c in cols]
            db.execute(sql, vals)
        except Exception as e:
            print(f"  [{table}] Skip row: {e}")
    db.commit()
    print(f"{table}: {len(rows)} rows imported")

# Re-enable FK checks
try:
    db.execute("SET session_replication_role = 'origin'")
except:
    pass

# Reset sequences
for table in data:
    try:
        db.execute(f"SELECT setval('{table}_id_seq', COALESCE((SELECT MAX(id) FROM \"{table}\"), 1))")
    except:
        pass

print("Import complete!")
