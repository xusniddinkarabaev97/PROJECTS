"""Quick migration of remaining tables with column mapping."""
import sqlite3, pg_compat, os

src = sqlite3.connect('/app/inventory.db')
src.row_factory = sqlite3.Row
url = os.environ.get('DATABASE_URL', 'postgresql://asseto:asseto_secret_change_me@db:5432/asseto')
dst = pg_compat.connect(url)

# Column name mappings: SQLite -> PG
REMAP = {
    'documents': {'current_role': 'pending_role'},
}

TABLES = [
    'dismissals',
    'returns',
    'documents',
    'doc_approvals',
    'doc_comments',
    'notifications',
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
]

for table in TABLES:
    # Check SQLite
    exists = src.execute("SELECT name FROM sqlite_master WHERE type='table' AND name=?", (table,)).fetchone()
    if not exists:
        print(f'  SKIP {table}: not in SQLite')
        continue

    count = src.execute(f'SELECT COUNT(*) FROM {table}').fetchone()[0]
    if count == 0:
        print(f'  SKIP {table}: empty')
        continue

    # Get PG columns  
    pg_rows = dst.execute(f"SELECT column_name FROM information_schema.columns WHERE table_name='{table}'").fetchall()
    pg_cols = [r['column_name'] for r in pg_rows]

    # Get SQLite rows
    rows = src.execute(f'SELECT * FROM {table}').fetchall()
    sqlite_cols = [desc[0] for desc in src.execute(f'SELECT * FROM {table} LIMIT 0').description]

    # Build PG column list with remapping
    pg_target_cols = []
    for c in sqlite_cols:
        mapping = REMAP.get(table, {})
        pg_target_cols.append(mapping.get(c, c))

    # Only use columns that exist in PG
    valid_pairs = [(sc, pc) for sc, pc in zip(sqlite_cols, pg_target_cols) if pc in pg_cols]
    if not valid_pairs:
        print(f'  SKIP {table}: no matching columns')
        continue

    pg_col_list = [pc for _, pc in valid_pairs]
    placeholders = ', '.join(['%s'] * len(valid_pairs))
    cols_str = ', '.join(pg_col_list)
    sql = f'INSERT INTO {table} ({cols_str}) VALUES ({placeholders}) ON CONFLICT DO NOTHING'

    print(f'  {table}: {count} rows ({len(valid_pairs)} cols)...', end=' ', flush=True)

    # Insert in batches
    inserted = 0
    for row in rows:
        vals = []
        for sc, pc in valid_pairs:
            vals.append(row[sc])
        try:
            cur = dst._conn.cursor()
            cur.execute(sql, vals)
            dst._conn.commit()
            inserted += 1
        except Exception as e:
            dst._conn.rollback()
            # Try one row at a time for debugging
            pass

    print(f'{inserted} inserted')

dst.close()
src.close()
print('\nDone!')
