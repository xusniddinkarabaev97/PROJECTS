import bcrypt
from modules.config import _db, DATABASE_URL, OperationalError, ProgrammingError, app

# ─── DATABASE ────────────────────────────────────────────────────────────────
def get_db():
    return _db.connect(DATABASE_URL)

def add_col(db, table, col, t):
    """Add a column — if already exists, rollback and continue."""
    try:
        db.execute(f"ALTER TABLE {table} ADD COLUMN {col} {t}")
        db.commit()
    except (OperationalError, ProgrammingError) as e:
        db.rollback()
        msg = str(e).lower()
        if "duplicate column" in msg or "already exists" in msg:
            return
        print(f"  [!] Ошибка миграции ({table}.{col}): {e}")
    except Exception as e:
        db.rollback()
        print(f"  [!] Ошибка при добавлении колонки {col}: {e}")

def migrate_db():
    """Safe migrations for existing databases."""
    with get_db() as db:
        migrations = [
            ("dismissals","deadline","DATE"),
            ("dismissals","item_conditions","TEXT DEFAULT '{}'"),
            ("dismissals","item_comments","TEXT DEFAULT '{}'"),
            ("dismissals","confirmed_signature","INTEGER DEFAULT 0"),
            ("users", "department", "TEXT"),
            ("users", "token_version", "INTEGER DEFAULT 0"),
            ("users", "force_password_change", "INTEGER DEFAULT 0"),
            ("users", "totp_secret", "TEXT"),
            ("users", "totp_enabled", "INTEGER DEFAULT 0"),
            ("users", "telegram_chat_id", "TEXT"),
            ("users", "expires_at", "DATE"),
            ("users", "onboarding_done", "INTEGER DEFAULT 1"),
            ("users", "last_login", "TIMESTAMP"),
            ("users", "avatar_color", "TEXT"),
            ("users", "created_at", "TIMESTAMP DEFAULT CURRENT_TIMESTAMP"),
            ("items", "purchase_price", "REAL"),
            ("items", "purchase_date", "DATE"),
            ("items", "supplier", "TEXT"),
            ("items", "warranty_until", "DATE"),
            ("items", "check_date", "TEXT"),
            ("items", "employee_id", "INTEGER"),
            ("issuances", "signature", "TEXT"),
            ("returns", "signature", "TEXT"),
            ("history", "field", "TEXT"),
            ("history", "new_val", "TEXT"),
            ("documents", "employee_id", "INTEGER"),
            ("documents", "employee_name", "TEXT"),
            ("dismissals", "signature", "TEXT"),
            ("dismissals", "aho_signature", "TEXT"),
            ("dismissals", "it_signature", "TEXT"),
            ("dismissals", "hr_signature", "TEXT"),
            ("dismissals", "hr_at", "TIMESTAMP"),
            ("dismissals", "hr_by_id", "INTEGER"),
            ("dismissals", "hr_by_name", "TEXT"),
            ("dismissals", "employee_signature", "TEXT"),
            ("documents", "signature", "TEXT"),
            ("doc_approvals", "signature", "TEXT"),
            ("documents", "employee_id", "INTEGER"),
            ("documents", "employee_name", "TEXT"),
            ("audit_log", "signed_by_name", "TEXT"),
            ("audit_log", "item_count", "INTEGER DEFAULT 0"),
            ("audit_log", "note", "TEXT"),
            ("issuances", "doc_id", "INTEGER"),
            ("issuances", "request_id", "INTEGER"),
            ("users", "manager_id", "INTEGER"),
            ("users", "position", "TEXT"),
            ("users", "phone", "TEXT"),
            ("users", "hire_date", "DATE"),
            # ── Multi-tenancy foundation (future-proof) ──────────────────────
            ("users",       "company_id", "INTEGER DEFAULT 1"),
            ("items",       "company_id", "INTEGER DEFAULT 1"),
            ("documents",   "pending_role", "TEXT"),
            ("documents",   "company_id", "INTEGER DEFAULT 1"),
            ("dismissals",  "company_id", "INTEGER DEFAULT 1"),
            ("maintenance", "company_id", "INTEGER DEFAULT 1"),
            ("asset_requests", "company_id", "INTEGER DEFAULT 1"),
            ("issuances",   "company_id", "INTEGER DEFAULT 1"),
            ("returns",     "company_id", "INTEGER DEFAULT 1"),
            ("inventory_sessions", "company_id", "INTEGER DEFAULT 1"),
        ]
        for table, col, typ in migrations:
            add_col(db, table, col, typ)

def _init_docflow_tables(db):
    """Создать таблицы документооборота (вызывается из init_db)."""
    db.execute("""
    CREATE TABLE IF NOT EXISTS documents (
        id          INTEGER PRIMARY KEY AUTOINCREMENT,
        doc_number  TEXT UNIQUE,
        doc_type    TEXT NOT NULL,
        title       TEXT NOT NULL,
        description TEXT,
        priority    TEXT DEFAULT 'normal',
        status      TEXT DEFAULT 'draft',
        current_step INTEGER DEFAULT 0,
        pending_role TEXT,
        created_by_id   INTEGER,
        created_by_name TEXT,
        item_id     INTEGER,
        item_inv    TEXT,
        department  TEXT,
        amount      REAL,
        attachments TEXT DEFAULT '[]',
        created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        updated_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        deadline    DATE,
        closed_at   TIMESTAMP,
        employee_id INTEGER,
        employee_name TEXT,
        signature   TEXT
    )""")
    db.execute("""
    CREATE TABLE IF NOT EXISTS doc_approvals (
        id          INTEGER PRIMARY KEY AUTOINCREMENT,
        doc_id      INTEGER NOT NULL,
        step        INTEGER NOT NULL,
        role        TEXT NOT NULL,
        role_label  TEXT,
        approver_id   INTEGER,
        approver_name TEXT,
        action      TEXT,
        comment     TEXT,
        acted_at    TIMESTAMP,
        created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (doc_id) REFERENCES documents(id)
    )""")
    db.execute("""
    CREATE TABLE IF NOT EXISTS doc_comments (
        id       INTEGER PRIMARY KEY AUTOINCREMENT,
        doc_id   INTEGER NOT NULL,
        user_id  INTEGER,
        user_name TEXT,
        user_role TEXT,
        text     TEXT NOT NULL,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (doc_id) REFERENCES documents(id)
    )""")
    # Add docflow roles to existing users table
    add_col(db, "users", "doc_role", "TEXT")
    add_col(db, "users", "department", "TEXT")

def init_db():
    with get_db() as db:
        # ─── Core tables ───
        db.execute("""CREATE TABLE IF NOT EXISTS users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL, email TEXT UNIQUE NOT NULL,
            password_hash TEXT NOT NULL, role TEXT NOT NULL DEFAULT 'employee',
            department TEXT, position TEXT, phone TEXT,
            manager_id INTEGER, hire_date DATE,
            active INTEGER DEFAULT 1,
            token_version INTEGER DEFAULT 0,
            force_password_change INTEGER DEFAULT 0,
            totp_secret TEXT, totp_enabled INTEGER DEFAULT 0,
            telegram_chat_id TEXT, expires_at DATE,
            onboarding_done INTEGER DEFAULT 0,
            last_login TIMESTAMP, avatar_color TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS items (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            place TEXT NOT NULL, inv_num TEXT NOT NULL UNIQUE, category TEXT NOT NULL,
            model TEXT, serial_num TEXT, room TEXT NOT NULL,
            employee TEXT, employee_id INTEGER,
            status TEXT DEFAULT 'Свободно', condition TEXT DEFAULT 'Хорошее',
            check_date TEXT, notes TEXT, photo TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS history (
            id INTEGER PRIMARY KEY AUTOINCREMENT, item_id INTEGER NOT NULL,
            user_id INTEGER, user_name TEXT, action TEXT NOT NULL,
            field TEXT, old_val TEXT, new_val TEXT,
            ts TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS issuances (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            employee_id INTEGER NOT NULL, employee_name TEXT NOT NULL,
            issued_by INTEGER NOT NULL, issued_by_name TEXT NOT NULL,
            items_json TEXT NOT NULL, status TEXT DEFAULT 'pending',
            signature TEXT,
            confirmed_at TIMESTAMP, created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS returns (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            employee_id INTEGER NOT NULL, employee_name TEXT NOT NULL,
            initiated_by INTEGER NOT NULL, initiated_by_name TEXT NOT NULL,
            items_json TEXT NOT NULL, photos_json TEXT,
            accepted_by INTEGER, accepted_by_name TEXT,
            status TEXT DEFAULT 'pending', signature TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            completed_at TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS dismissals (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            employee_id INTEGER NOT NULL, employee_name TEXT NOT NULL,
            employee_email TEXT,
            initiated_by INTEGER NOT NULL, initiated_by_name TEXT NOT NULL,
            items_json TEXT NOT NULL,
            status TEXT DEFAULT 'pending',
            notes TEXT,
            photos_json TEXT, signature TEXT,
            confirmed_by INTEGER, confirmed_by_name TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            completed_at TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS login_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER, email TEXT, success INTEGER,
            ip TEXT, user_agent TEXT,
            ts TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS maintenance (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            item_id INTEGER NOT NULL,
            reported_by_id INTEGER,
            reported_by_name TEXT,
            description TEXT,
            priority TEXT DEFAULT 'medium',
            status TEXT DEFAULT 'pending',
            resolved_by TEXT,
            resolved_at TIMESTAMP,
            resolution TEXT,
            rejection_reason TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS asset_requests (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            employee_id INTEGER NOT NULL,
            employee_name TEXT NOT NULL,
            category TEXT NOT NULL,
            reason TEXT,
            status TEXT DEFAULT 'pending',
            rejection_reason TEXT,
            resolved_by TEXT,
            resolved_at TIMESTAMP,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS audit_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            signed_by_id INTEGER,
            action TEXT, details TEXT,
            ip_address TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS app_settings (
            key_name TEXT PRIMARY KEY,
            key_value TEXT)""")
        db.execute("""CREATE TABLE IF NOT EXISTS revoked_tokens (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER NOT NULL,
            token_version INTEGER NOT NULL,
            revoked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS api_keys (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            key_hash TEXT UNIQUE NOT NULL,
            scopes TEXT DEFAULT 'read',
            user_id INTEGER,
            last_used TIMESTAMP,
            expires_at DATE,
            active INTEGER DEFAULT 1,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS inventory_sessions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            title TEXT NOT NULL,
            status TEXT DEFAULT 'active',
            created_by_id INTEGER,
            created_by_name TEXT,
            department TEXT,
            total_items INTEGER DEFAULT 0,
            checked_items INTEGER DEFAULT 0,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            completed_at TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS inventory_checks (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            session_id INTEGER NOT NULL,
            item_id INTEGER NOT NULL,
            status TEXT DEFAULT 'pending',
            checked_by_id INTEGER,
            checked_by_name TEXT,
            photo TEXT,
            note TEXT,
            checked_at TIMESTAMP,
            FOREIGN KEY (session_id) REFERENCES inventory_sessions(id))""")
        db.execute("""CREATE TABLE IF NOT EXISTS equipment_templates (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            description TEXT,
            items_json TEXT DEFAULT '[]',
            created_by TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        # ── Companies table (multi-tenancy foundation) ────────────────────────
        db.execute("""CREATE TABLE IF NOT EXISTS companies (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            slug TEXT UNIQUE NOT NULL,
            plan TEXT DEFAULT 'trial',
            max_users INTEGER DEFAULT 25,
            max_items INTEGER DEFAULT 500,
            trial_ends DATE,
            active INTEGER DEFAULT 1,
            logo TEXT,
            primary_color TEXT DEFAULT '007AFF',
            contact_email TEXT,
            contact_phone TEXT,
            address TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        # Insert default company if none exists
        if not db.execute("SELECT id FROM companies WHERE id=1").fetchone():
            db.execute(
                "INSERT OR IGNORE INTO companies (id,name,slug,plan,active) VALUES (1,'Моя компания','default','enterprise',1)"
            )
        db.execute("""CREATE TABLE IF NOT EXISTS push_subscriptions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER NOT NULL,
            endpoint TEXT NOT NULL UNIQUE,
            p256dh TEXT NOT NULL,
            auth TEXT NOT NULL,
            user_agent TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE)""")
        # ── Rooms / Office Map ────────────────────────────────────────────────
        db.execute("""CREATE TABLE IF NOT EXISTS rooms (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            floor TEXT DEFAULT '1',
            wing TEXT,
            capacity INTEGER DEFAULT 0,
            responsible TEXT,
            description TEXT,
            color TEXT DEFAULT '#007AFF',
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        
        # ─── Indexes (idempotent) ─────────────────────────────────────────────
        db.executescript("""
            CREATE INDEX IF NOT EXISTS idx_items_employee   ON items(employee);
            CREATE INDEX IF NOT EXISTS idx_items_employee_id ON items(employee_id);
            CREATE INDEX IF NOT EXISTS idx_items_room       ON items(room);
            CREATE INDEX IF NOT EXISTS idx_items_category   ON items(category);
            CREATE INDEX IF NOT EXISTS idx_items_status     ON items(status);
            CREATE INDEX IF NOT EXISTS idx_items_condition  ON items(condition);
            CREATE INDEX IF NOT EXISTS idx_history_item_id  ON history(item_id);
            CREATE INDEX IF NOT EXISTS idx_history_ts       ON history(ts);
            CREATE INDEX IF NOT EXISTS idx_users_email      ON users(email);
            CREATE INDEX IF NOT EXISTS idx_users_active     ON users(active);
            CREATE INDEX IF NOT EXISTS idx_maintenance_item ON maintenance(item_id);
            CREATE INDEX IF NOT EXISTS idx_maintenance_status ON maintenance(status);
        """)

        # Migrations / Column updates
        add_col(db, "items", "employee_id", "INTEGER")
        add_col(db, "items", "photo", "TEXT")
        add_col(db, "history", "user_id", "INTEGER")
        add_col(db, "history", "user_name", "TEXT")
        # ─── Docflow tables ───
        _init_docflow_tables(db)
        # ── Fix TEXT columns that should be DATE (SQLite: no-op, TEXT is fine) ──

        if db.execute("SELECT COUNT(*) FROM users").fetchone()[0] == 0:
            pw = bcrypt.hashpw(b"admin123", bcrypt.gensalt()).decode()
            db.execute("INSERT INTO users (name,email,password_hash,role) VALUES (?,?,?,?)",
                       ("Администратор","admin@asseto.uz",pw,"superadmin"))
            print("  👤  admin@asseto.uz / admin123")

        # ── Office / Clerical tables ─────────────────────────────────────
        db.execute("""CREATE TABLE IF NOT EXISTS contractors (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            tin TEXT,
            email TEXT,
            phone TEXT,
            address TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS office_docs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            doc_type TEXT NOT NULL,
            reg_number TEXT UNIQUE,
            reg_date DATE DEFAULT (date('now')),
            sender_doc_number TEXT,
            sender_name TEXT,
            sender_doc_date DATE,
            contractor_id INTEGER REFERENCES contractors(id),
            recipient_name TEXT,
            title TEXT NOT NULL,
            description TEXT,
            status TEXT DEFAULT 'draft',
            priority TEXT DEFAULT 'normal',
            creator_id INTEGER REFERENCES users(id),
            assigned_to_id INTEGER REFERENCES users(id),
            deadline DATE,
            resolution TEXT,
            reply_to_id INTEGER REFERENCES office_docs(id),
            completed_at TIMESTAMP,
            archived_at TIMESTAMP,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS office_doc_files (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            doc_id INTEGER REFERENCES office_docs(id) ON DELETE CASCADE,
            file_name TEXT NOT NULL,
            file_path TEXT NOT NULL,
            file_type TEXT DEFAULT 'attachment',
            uploaded_by INTEGER REFERENCES users(id),
            uploaded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS office_doc_history (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            doc_id INTEGER REFERENCES office_docs(id) ON DELETE CASCADE,
            user_id INTEGER REFERENCES users(id),
            action TEXT NOT NULL,
            comment TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS office_acknowledgments (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            doc_id INTEGER REFERENCES office_docs(id) ON DELETE CASCADE,
            user_id INTEGER REFERENCES users(id),
            acked_at TIMESTAMP,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")

        # -- Tasks & Projects --
        db.execute("""CREATE TABLE IF NOT EXISTS tasks (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            title TEXT NOT NULL, description TEXT DEFAULT '',
            status TEXT DEFAULT 'new', priority TEXT DEFAULT 'medium',
            priority_order INTEGER DEFAULT 1,
            deadline TIMESTAMP, planned_hours REAL,
            creator_id INTEGER REFERENCES users(id),
            responsible_id INTEGER REFERENCES users(id),
            project_id INTEGER,
            rating INTEGER, budget REAL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS task_checklist_items (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            task_id INTEGER REFERENCES tasks(id) ON DELETE CASCADE,
            text TEXT NOT NULL, done INTEGER DEFAULT 0,
            sort_order INTEGER DEFAULT 0,
            assignee_id INTEGER REFERENCES users(id))""")
        db.execute("""CREATE TABLE IF NOT EXISTS task_comments (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            task_id INTEGER REFERENCES tasks(id) ON DELETE CASCADE,
            user_id INTEGER REFERENCES users(id),
            body TEXT NOT NULL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")
        db.execute("""CREATE TABLE IF NOT EXISTS task_participants (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            task_id INTEGER REFERENCES tasks(id) ON DELETE CASCADE,
            user_id INTEGER REFERENCES users(id),
            role TEXT DEFAULT 'participant')""")
        db.execute("""CREATE TABLE IF NOT EXISTS task_time_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            task_id INTEGER REFERENCES tasks(id) ON DELETE CASCADE,
            user_id INTEGER REFERENCES users(id),
            minutes INTEGER NOT NULL DEFAULT 0,
            description TEXT DEFAULT '',
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)""")

        if db.execute("SELECT COUNT(*) FROM users").fetchone()[0] == 0:
            pw = bcrypt.hashpw(b"admin123", bcrypt.gensalt()).decode()
            db.execute("INSERT INTO users (name,email,password_hash,role) VALUES (?,?,?,?)",
                       ("Администратор","admin@asseto.uz",pw,"superadmin"))
            print("  👤  admin@asseto.uz / admin123")

_FIELD_LIMITS = {
    "model": 200, "serial_num": 100, "place": 200, "room": 100,
    "employee": 150, "notes": 2000, "name": 150, "email": 254,
    "description": 3000, "resolution": 2000,
}

def _trunc(d, key, default=""):
    """Return field value truncated to its max allowed length."""
    val = (d.get(key) or default)
    limit = _FIELD_LIMITS.get(key, 500)
    return str(val)[:limit]

# ─── DATABASE ────────────────────────────────────────────────────────────────