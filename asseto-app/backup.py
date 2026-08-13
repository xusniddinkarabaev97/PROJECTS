"""
ASSETO — PostgreSQL backup (pg_dump)
Cron (Linux):   0 2 * * * python /opt/asseto/backup.py
Docker:         docker compose exec db pg_dump -U asseto asseto > backup.sql
                or run this script with DATABASE_URL set.

Rotation policy:
  - Daily backups: keep last 7
  - Weekly backups (Sunday): keep last 4
"""
import os, sys, subprocess
from datetime import datetime, date

BASE_DIR    = os.path.dirname(os.path.abspath(__file__))
DATABASE_URL = os.environ.get(
    'DATABASE_URL',
    'postgresql://asseto:asseto@localhost:5432/asseto',
)
BACKUP_DIR  = os.path.join(BASE_DIR, "backups")
KEEP_DAILY  = 7   # days
KEEP_WEEKLY = 4   # weeks

os.makedirs(BACKUP_DIR, exist_ok=True)


def _parse_url(url: str):
    """Parse DATABASE_URL into components for pg_dump."""
    # postgresql://user:pass@host:port/dbname
    rest = url.replace('postgresql://', '').replace('postgres://', '')
    user_pass, host_db = rest.split('@', 1)
    user, password = user_pass.split(':', 1) if ':' in user_pass else (user_pass, '')
    host_port, dbname = host_db.rsplit('/', 1) if '/' in host_db else (host_db, 'asseto')
    host, port = host_port.split(':', 1) if ':' in host_port else (host_port, '5432')
    return user, password, host, port, dbname


def backup():
    user, password, host, port, dbname = _parse_url(DATABASE_URL)

    today     = date.today()
    is_sunday = today.weekday() == 6
    ts        = datetime.now().strftime("%Y%m%d_%H%M%S")
    suffix    = "_weekly" if is_sunday else "_daily"
    fname     = f"asseto{suffix}_{ts}.sql"
    dest      = os.path.join(BACKUP_DIR, fname)

    env = os.environ.copy()
    env['PGPASSWORD'] = password

    cmd = [
        'pg_dump',
        '-U', user,
        '-h', host,
        '-p', port,
        '-d', dbname,
        '--no-owner',
        '--no-acl',
        '-f', dest,
    ]

    try:
        subprocess.run(cmd, env=env, check=True, capture_output=True, text=True)
    except subprocess.CalledProcessError as e:
        _log(f"pg_dump FAILED: {e.stderr}", err=True)
        sys.exit(1)
    except FileNotFoundError:
        _log("pg_dump not found. Install PostgreSQL client tools.", err=True)
        sys.exit(1)

    size_kb = os.path.getsize(dest) // 1024
    _log(f"Saved → {fname}  ({size_kb} KB)")
    _rotate()


def _rotate():
    files = [f for f in os.listdir(BACKUP_DIR)
             if f.startswith("asseto_") and f.endswith('.sql')]

    daily  = sorted([f for f in files if "_daily_"  in f], reverse=True)
    weekly = sorted([f for f in files if "_weekly_" in f], reverse=True)

    for fname in daily[KEEP_DAILY:] + weekly[KEEP_WEEKLY:]:
        fpath = os.path.join(BACKUP_DIR, fname)
        os.remove(fpath)
        _log(f"Rotated: {fname}")


def _log(msg, err=False):
    ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    line = f"[{ts}] [backup] {msg}"
    print(line, file=sys.stderr if err else sys.stdout)
    log_file = os.path.join(BASE_DIR, "logs", "backup.log")
    try:
        os.makedirs(os.path.dirname(log_file), exist_ok=True)
        with open(log_file, "a", encoding="utf-8") as f:
            f.write(line + "\n")
    except Exception:
        pass


if __name__ == "__main__":
    backup()
