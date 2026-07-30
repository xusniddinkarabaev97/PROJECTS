"""
ASSETO — Inventory & Office Management System (PostgreSQL)
"""
import os, sys

# Ensure modules/ is importable
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

# ── Load .env BEFORE any config reads ────────────────────────────────────
def _load_env():
    env_path = os.path.join(os.path.dirname(__file__), '.env')
    if os.path.exists(env_path):
        with open(env_path, encoding='utf-8') as f:
            for line in f:
                line = line.strip()
                if line and not line.startswith('#') and '=' in line:
                    k, v = line.split('=', 1)
                    os.environ.setdefault(k.strip(), v.strip())
_load_env()

# ── Import config (creates Flask app) ────────────────────────────────────
from modules.config import app

# ── Register all blueprints ──────────────────────────────────────────────
from modules.auth         import bp as auth_bp
from modules.api_items    import bp as items_bp
from modules.api_users    import bp as users_bp
from modules.api_rooms    import bp as rooms_bp
from modules.api_docs     import bp as docs_bp
from modules.api_dismissals import bp as dismissals_bp
from modules.api_maintenance import bp as maintenance_bp
from modules.api_office   import bp as office_bp
from modules.api_reports  import bp as reports_bp
from modules.api_settings import bp as settings_bp
from modules.api_orgchart  import bp as orgchart_bp
from modules.api_tasks    import bp as tasks_bp

app.register_blueprint(auth_bp)
app.register_blueprint(items_bp)
app.register_blueprint(users_bp)
app.register_blueprint(rooms_bp)
app.register_blueprint(docs_bp)
app.register_blueprint(dismissals_bp)
app.register_blueprint(maintenance_bp)
app.register_blueprint(office_bp)
app.register_blueprint(reports_bp)
app.register_blueprint(settings_bp)
app.register_blueprint(orgchart_bp)
app.register_blueprint(tasks_bp)

# ── Initialize database tables ───────────────────────────────────────────
from modules.db import init_db, migrate_db
init_db()
migrate_db()

# ── Development server ───────────────────────────────────────────────────
if __name__ == "__main__":
    import subprocess
    backup_script = os.path.join(os.path.dirname(__file__), "backup.py")
    if os.path.exists(backup_script):
        subprocess.Popen([sys.executable, backup_script],
                         stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    app.run(debug=False, host=os.environ.get('HOST', '0.0.0.0'),
            port=int(os.environ.get('PORT', 5000)), threaded=True)
