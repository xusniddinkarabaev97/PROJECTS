"""
SQLite compatibility wrapper — drop-in replacement for pg_compat.py.
Uses sqlite3 instead of psycopg2. Accepts a file path as DATABASE_URL.
"""
import sqlite3
import re


def sqlite_to_pg(sql: str) -> str:
    """No-op — SQLite SQL is already compatible. Just return as-is."""
    return sql


class RowDict(dict):
    """A dict that also supports integer-index access (mimicking sqlite3.Row)."""
    def __getitem__(self, key):
        if isinstance(key, int):
            return list(self.values())[key]
        return super().__getitem__(key)


class SqliteCur:
    """Wraps sqlite3 cursor to mimic PgCur interface."""

    def __init__(self, cursor):
        self._c = cursor
        self.lastrowid = None

    def execute(self, sql, params=()):
        self._c.execute(sql, params or ())
        self.lastrowid = self._c.lastrowid
        return self

    def executemany(self, sql, params_list):
        self._c.executemany(sql, params_list)
        return self

    def executescript(self, sql):
        self._c.executescript(sql)
        return self

    def fetchone(self):
        row = self._c.fetchone()
        if row:
            # Convert sqlite3.Row to RowDict
            rd = RowDict()
            for key in row.keys():
                rd[key] = row[key]
            return rd
        return None

    def fetchall(self):
        rows = self._c.fetchall()
        result = []
        for row in rows:
            rd = RowDict()
            for key in row.keys():
                rd[key] = row[key]
            result.append(rd)
        return result

    def __iter__(self):
        for row in self._c:
            rd = RowDict()
            for key in row.keys():
                rd[key] = row[key]
            yield rd

    @property
    def rowcount(self):
        return self._c.rowcount


class SqliteConn:
    """Wraps sqlite3 connection to mimic PgConn interface."""

    def __init__(self, conn):
        self._conn = conn
        # Enable WAL mode for better concurrency
        conn.execute("PRAGMA journal_mode=WAL")
        # Enable foreign keys
        conn.execute("PRAGMA foreign_keys=ON")

    def execute(self, sql, params=()):
        cur = self._conn.cursor()
        # Ensure Row factory for dict-like access
        cur.row_factory = sqlite3.Row
        sc = SqliteCur(cur)
        sc.execute(sql, params)
        return sc

    def executemany(self, sql, params_list):
        cur = self._conn.cursor()
        cur.row_factory = sqlite3.Row
        sc = SqliteCur(cur)
        sc.executemany(sql, params_list)
        return sc

    def executescript(self, sql):
        cur = self._conn.cursor()
        cur.row_factory = sqlite3.Row
        sc = SqliteCur(cur)
        sc.executescript(sql)
        return sc

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


def connect(database_url: str) -> SqliteConn:
    """Create a SQLite connection from a file path (database_url is treated as file path)."""
    # If it looks like a PostgreSQL URL, extract just the db name for SQLite
    path = database_url
    if '://' in database_url:
        # Try to extract db name from postgresql://.../dbname
        parts = database_url.split('/')
        dbname = parts[-1] if parts[-1] else 'asseto'
        path = dbname + '.db'
    conn = sqlite3.connect(path)
    conn.row_factory = sqlite3.Row
    return SqliteConn(conn)


# ── Exception aliases for drop-in replacement ──────────────────────────
DatabaseError = sqlite3.DatabaseError
IntegrityError = sqlite3.IntegrityError
OperationalError = sqlite3.OperationalError
ProgrammingError = sqlite3.ProgrammingError
InternalError = sqlite3.InternalError
