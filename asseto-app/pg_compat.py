"""
PostgreSQL compatibility wrapper for SQLite-style code.
Converts ? → %s, translates SQLite SQL functions, handles AUTOINCREMENT,
and returns RowDict objects (accessible by column name AND integer index).

Usage: replace get_db() to return PgConn instead of sqlite3.connect().
"""
import re
import psycopg2
import psycopg2.extras
import psycopg2.errorcodes


def sqlite_to_pg(sql: str) -> str:
    """Translate SQLite-specific SQL to PostgreSQL-compatible SQL."""
    # ── Placeholder: ? → %s ──────────────────────────────────────────────
    sql = sql.replace('?', '%s')

    # ── INTEGER PRIMARY KEY AUTOINCREMENT → SERIAL PRIMARY KEY ──────────
    sql = re.sub(
        r'INTEGER\s+PRIMARY\s+KEY\s+AUTOINCREMENT',
        'SERIAL PRIMARY KEY',
        sql,
        flags=re.IGNORECASE,
    )

    # ── INSERT OR IGNORE → INSERT ... ON CONFLICT DO NOTHING ─────────────
    def _fix_insert_or_ignore(m):
        body = m.group(0)
        # Remove OR IGNORE/OR REPLACE
        body = re.sub(r'\s+OR\s+(IGNORE|REPLACE)\s+', ' ', body, flags=re.IGNORECASE)
        if not re.search(r'ON\s+CONFLICT', body, re.IGNORECASE):
            body += ' ON CONFLICT DO NOTHING'
        return body
    sql = re.sub(
        r"INSERT\s+OR\s+(?:IGNORE|REPLACE)\s+INTO\s+.*?VALUES\s*\([^)]+\)\s*$",
        _fix_insert_or_ignore,
        sql,
        flags=re.IGNORECASE | re.DOTALL,
    )

    # ── date('now','-N days') → CURRENT_DATE + INTERVAL 'N days' ────────
    sql = re.sub(
        r"date\('now'\s*,\s*'(-?\d+)\s*days'\)",
        r"CURRENT_DATE + INTERVAL '\1 days'",
        sql,
    )
    # ── date('now','start of month') → DATE_TRUNC('month', CURRENT_DATE)
    sql = re.sub(
        r"date\('now'\s*,\s*'start of month'\)",
        r"DATE_TRUNC('month', CURRENT_DATE)::date",
        sql,
    )
    # ── date('now','-N years') → CURRENT_DATE + INTERVAL 'N years' ──────
    sql = re.sub(
        r"date\('now'\s*,\s*'(-?\d+)\s*years'\)",
        r"CURRENT_DATE + INTERVAL '\1 years'",
        sql,
    )
    # ── datetime('now','-N days') → CURRENT_TIMESTAMP + INTERVAL ────────
    sql = re.sub(
        r"datetime\('now'\s*,\s*'(-?\d+)\s*days'\)",
        r"CURRENT_TIMESTAMP + INTERVAL '\1 days'",
        sql,
    )
    # ── datetime(?, 'unixepoch') → to_timestamp(%s) ─────────────────────
    sql = re.sub(
        r"datetime\(([^,]+)\s*,\s*'unixepoch'\)",
        r"to_timestamp(\1)",
        sql,
    )
    # ── strftime('%Y', X) → EXTRACT(YEAR FROM X) ────────────────────────
    sql = re.sub(
        r"strftime\('%Y'\s*,\s*([^)]+)\)",
        r"EXTRACT(YEAR FROM \1)",
        sql,
    )
    # ── strftime('%m', X) → EXTRACT(MONTH FROM X) ───────────────────────
    sql = re.sub(
        r"strftime\('%m'\s*,\s*([^)]+)\)",
        r"EXTRACT(MONTH FROM \1)",
        sql,
    )

    return sql


class RowDict(dict):
    """A dict that also supports integer-index access (mimicking sqlite3.Row)."""
    def __getitem__(self, key):
        if isinstance(key, int):
            return list(self.values())[key]
        return super().__getitem__(key)


class PgCur:
    """Wraps psycopg2 cursor to mimic sqlite3.Cursor interface."""

    def __init__(self, cursor):
        self._c = cursor
        self.lastrowid = None

    def execute(self, sql, params=()):
        sql_pg = sqlite_to_pg(sql)
        self._c.execute(sql_pg, params or None)
        # Capture lastrowid for INSERT statements (safe via savepoint)
        if sql_pg.lstrip().upper().startswith('INSERT'):
            try:
                self._c.execute('SAVEPOINT _lrid_sp')
                self._c.execute('SELECT lastval()')
                row = self._c.fetchone()
                self.lastrowid = row[0] if row else None
                self._c.execute('RELEASE SAVEPOINT _lrid_sp')
            except Exception:
                self._c.execute('ROLLBACK TO SAVEPOINT _lrid_sp')
                self.lastrowid = None
        return self

    def executemany(self, sql, params_list):
        self._c.executemany(sqlite_to_pg(sql), params_list)
        return self

    def executescript(self, sql):
        """Execute multiple SQL statements separated by ';' (sqlite3 compat)."""
        for stmt in sql.split(';'):
            stmt = stmt.strip()
            if stmt:
                self.execute(stmt)
        return self

    def fetchone(self):
        row = self._c.fetchone()
        return RowDict(row) if row else None

    def fetchall(self):
        rows = self._c.fetchall()
        return [RowDict(r) for r in rows] if rows else []

    def __iter__(self):
        for row in self._c:
            yield RowDict(row)

    @property
    def rowcount(self):
        return self._c.rowcount


class PgConn:
    """Wraps psycopg2 connection to mimic sqlite3.Connection interface."""

    def __init__(self, conn):
        self._conn = conn
        self._conn.autocommit = False

    def execute(self, sql, params=()):
        cur = self._conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor)
        pgc = PgCur(cur)
        pgc.execute(sql, params)
        return pgc

    def executemany(self, sql, params_list):
        cur = self._conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor)
        pgc = PgCur(cur)
        pgc.executemany(sql, params_list)
        return pgc

    def executescript(self, sql):
        """Execute multiple SQL statements (sqlite3 compat)."""
        cur = self._conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor)
        pgc = PgCur(cur)
        pgc.executescript(sql)
        return pgc

    def commit(self):
        self._conn.commit()

    def rollback(self):
        self._conn.rollback()

    def close(self):
        self._conn.close()

    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        if exc_type:
            self._conn.rollback()
        else:
            self._conn.commit()


def connect(database_url: str) -> PgConn:
    """Create a PG-compatible connection from a DATABASE_URL."""
    conn = psycopg2.connect(database_url)
    return PgConn(conn)


# ── Exception aliases for drop-in replacement ──────────────────────────
DatabaseError = Exception
IntegrityError = psycopg2.IntegrityError
OperationalError = psycopg2.OperationalError
ProgrammingError = psycopg2.ProgrammingError
InternalError = psycopg2.InternalError
