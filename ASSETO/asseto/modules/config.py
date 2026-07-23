from flask import Flask
import os, json, secrets
import logging
from logging.handlers import RotatingFileHandler

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# ── Load .env BEFORE reading any env vars ──────────────────────────────
def _load_env():
    env_path = os.path.join(BASE_DIR, '.env')
    if os.path.exists(env_path):
        with open(env_path, encoding='utf-8') as f:
            for line in f:
                line = line.strip()
                if line and not line.startswith('#') and '=' in line:
                    k, v = line.split('=', 1)
                    os.environ.setdefault(k.strip(), v.strip())
_load_env()

# ── Database backend: auto-detect SQLite or PostgreSQL ──────────────────
import os as _os
DATABASE_URL = os.environ.get('DATABASE_URL',
    _os.path.join(_os.path.dirname(_os.path.dirname(_os.path.abspath(__file__))), 'inventory.db'))

if DATABASE_URL.startswith('postgresql://') or DATABASE_URL.startswith('postgres://'):
    import pg_compat as _db
else:
    import sqlite_compat as _db

DatabaseError    = _db.DatabaseError
OperationalError = _db.OperationalError
ProgrammingError = _db.ProgrammingError
IntegrityError   = _db.IntegrityError

app = Flask(__name__, template_folder=os.path.join(BASE_DIR, 'templates'),
         static_folder=os.path.join(BASE_DIR, 'static'))
app.config['TEMPLATES_AUTO_RELOAD'] = True

# ── Security Config ─────────────────────────────────────────────────────────────
_secret = os.environ.get('SECRET_KEY', '')
if not _secret or _secret == 'CHANGE_ME_USE_RANDOM_64_CHAR_STRING_IN_PRODUCTION':
    _secret = secrets.token_hex(32)
    print('  [!] SECRET_KEY не задан в .env — используется временный ключ.')
    print('  [!] Создайте файл .env и задайте SECRET_KEY для продакшна!')
app.config['SECRET_KEY'] = _secret

JWT_EXPIRY         = int(os.environ.get('JWT_EXPIRY', 60 * 60 * 12))   # 12h
SECURE_COOKIES     = os.environ.get('SECURE_COOKIES', 'False').lower() == 'true'
MAX_UPLOAD_MB      = int(os.environ.get('MAX_UPLOAD_MB', '16'))
app.config['MAX_CONTENT_LENGTH'] = MAX_UPLOAD_MB * 1024 * 1024

ALLOWED_EXTENSIONS = {'.jpg', '.jpeg', '.png', '.gif', '.webp', '.heic'}

app.jinja_env.filters['from_json'] = json.loads

# ── File logging with rotation ─────────────────────────────────────────────────
_log_dir = os.path.join(BASE_DIR, 'logs')
os.makedirs(_log_dir, exist_ok=True)
_handler = RotatingFileHandler(
    os.path.join(_log_dir, 'asseto.log'),
    maxBytes=5 * 1024 * 1024,  # 5 MB per file
    backupCount=7,
    encoding='utf-8',
)
_handler.setFormatter(logging.Formatter(
    '[%(asctime)s] %(levelname)s %(name)s: %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S',
))
_handler.setLevel(logging.WARNING)
app.logger.addHandler(_handler)
app.logger.setLevel(logging.WARNING)
logging.getLogger('werkzeug').addHandler(_handler)

UPLOADS = os.path.join(BASE_DIR, "static", "photos")
os.makedirs(UPLOADS, exist_ok=True)
SIGS = os.path.join(BASE_DIR, "static", "signatures")
os.makedirs(SIGS, exist_ok=True)

CATEGORIES = ["Ноутбук","Монитор","Телефон","Клавиатура","Мышь","Принтер",
              "Наушники","Удлинитель","Кресло","Стол","Оборудование","Другое"]
PREFIXES   = {"Ноутбук":"НТБ","Монитор":"МОН","Кресло":"КРС","Стол":"СТЛ",
               "Клавиатура":"КЛВ","Мышь":"МШ","Принтер":"ПРН","Телефон":"ТЛФ",
               "Наушники":"НШК","Удлинитель":"УДЛ","Оборудование":"ОБР","Другое":"ДРГ"}
CONDITIONS = ["Хорошее","Потёрто","Требует ремонта","Списано","Утеряно"]
STATUSES   = ["Занято","Свободно"]

ROLES = {
    "superadmin": {"label":"Супер-Админ",   "color":"5856D6","can_manage_users":True, "can_delete":True, "can_edit":True, "can_view_all":True, "can_issue":True, "can_export":True, "can_approve":True},
    "aho":        {"label":"АХО / IT",      "color":"007AFF","can_manage_users":False,"can_delete":False,"can_edit":True, "can_view_all":True, "can_issue":True, "can_export":True, "can_approve":True},
    "hr":         {"label":"HR",            "color":"34C759","can_manage_users":False,"can_delete":False,"can_edit":False,"can_view_all":True, "can_issue":True, "can_export":False,"can_approve":False},
    "employee":   {"label":"Сотрудник",     "color":"8E8E93","can_manage_users":False,"can_delete":False,"can_edit":False,"can_view_all":False,"can_issue":False,"can_export":False,"can_approve":False},
    "auditor":    {"label":"Аудитор",       "color":"5AC8FA","can_manage_users":False,"can_delete":False,"can_edit":False,"can_view_all":True, "can_issue":False,"can_export":True, "can_approve":False},
    "deputy":          {"label":"Зам. Директора",      "color":"FF9500","can_manage_users":False,"can_delete":False,"can_edit":False,"can_view_all":True, "can_issue":False,"can_export":True, "can_approve":True},
    "department_head": {"label":"Начальник департамента","color":"FF6B35","can_manage_users":False,"can_delete":False,"can_edit":False,"can_view_all":True, "can_issue":False,"can_export":True, "can_approve":True},
    "director":   {"label":"Ген. Директор", "color":"FF3B30","can_manage_users":True, "can_delete":True, "can_edit":True, "can_view_all":True, "can_issue":True, "can_export":True, "can_approve":True},
    "accountant": {"label":"Бухгалтер",     "color":"30B0C7","can_manage_users":False,"can_delete":False,"can_edit":False,"can_view_all":True, "can_issue":False,"can_export":True, "can_approve":True},
    "viewer":     {"label":"Наблюдатель",   "color":"5AC8FA","can_manage_users":False,"can_delete":False,"can_edit":False,"can_view_all":True, "can_issue":False,"can_export":False,"can_approve":False},
}
