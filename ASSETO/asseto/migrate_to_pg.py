"""
Migrate existing SQLite data to PostgreSQL.
Usage:  python migrate_to_pg.py
        DATABASE_URL=postgresql://... python migrate_to_pg.py

This reads inventory.db and writes all data into PostgreSQL.
Requires: psycopg2-binary (for PG) and sqlite3 (built-in).
"""
import os, sys, sqlite3
import pg_compat as pg

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
SQLITE_PATH = os.path.join(BASE_DIR, 'inventory.db')
DATABASE_URL = os.environ.get(
    'DATABASE_URL',
    'postgresql://asseto:asseto@localhost:5432/asseto',
)

# Tables to migrate (in dependency order: parents first, children after)
TABLES = [
    'users',
    'items',
    'rooms',
    'companies',
    'history',
    'issuances',
    'returns',
    'dismissals',
    'login_log',
    'maintenance',
    'asset_requests',
    'audit_log',
    'app_settings',
    'revoked_tokens',
    'api_keys',
    'inventory_sessions',
    'inventory_checks',
    'equipment_templates',
    'push_subscriptions',
    'documents',
    'doc_approvals',
    'doc_comments',
]


def reset_sequences(db):
    """Reset all SERIAL sequences after data import."""
    for table in TABLES:
        try:
            db.execute(f"SELECT setval(pg_get_serial_sequence('{table}','id'), COALESCE((SELECT MAX(id) FROM {table}), 1))")
        except Exception:
            pass
    db.commit()


def migrate():
    if not os.path.exists(SQLITE_PATH):
        print(f"ERROR: SQLite DB not found at {SQLITE_PATH}")
        print("Nothing to migrate. If this is a fresh start, just run the app — it will create tables automatically.")
        sys.exit(1)

    print(f"Source:  {SQLITE_PATH}")
    print(f"Target:  {DATABASE_URL}")
    print()

    # Connect to both databases
    src = sqlite3.connect(SQLITE_PATH)
    src.row_factory = sqlite3.Row

    dst = pg.connect(DATABASE_URL)

    # First, ensure all tables exist in PostgreSQL (run init schema)
    print("[1] Creating tables in PostgreSQL...")
    import app  # triggers init_db() and migrate_db()
    print("    ✓ Tables created\n")

    total_rows = 0

    for table in TABLES:
        # Check if table exists in SQLite
        exists = src.execute(
            "SELECT name FROM sqlite_master WHERE type='table' AND name=?",
            (table,),
        ).fetchone()

        if not exists:
            print(f"  ⏭  {table}: not found in SQLite, skipping")
            continue

        # Count rows
        count = src.execute(f"SELECT COUNT(*) FROM {table}").fetchone()[0]
        if count == 0:
            print(f"  ⏭  {table}: empty, skipping")
            continue

        print(f"  📦 {table}: {count} rows ...", end=' ', flush=True)

        # Read all rows from SQLite
        rows = src.execute(f"SELECT * FROM {table}").fetchall()
        columns = [desc[0] for desc in src.execute(f"SELECT * FROM {table} LIMIT 0").description]

        # Build INSERT
        placeholders = ', '.join(['%s'] * len(columns))
        cols_str = ', '.join(columns)
        sql = f'INSERT INTO {table} ({cols_str}) VALUES ({placeholders}) ON CONFLICT DO NOTHING'

        # Insert into PostgreSQL in batches
        batch_size = 500
        for i in range(0, len(rows), batch_size):
            batch = rows[i:i + batch_size]
            tuples = [tuple(r) for r in batch]
            cur = dst._conn.cursor()
            cur.executemany(sql, tuples)
            dst._conn.commit()

        total_rows += count
        print("✓")

    dst.close()
    src.close()

    print(f"\n[2] Resetting sequences...")
    dst2 = pg.connect(DATABASE_URL)
    reset_sequences(dst2)
    dst2.close()

    print(f"\n{'='*60}")
    print(f"  Migration complete! {total_rows} rows transferred.")
    print(f"  Login: admin@asseto.uz / admin123")
    print(f"{'='*60}")


if __name__ == '__main__':
    migrate()
