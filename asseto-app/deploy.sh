#!/usr/bin/env bash
# =============================================================================
# ASSETO — Deploy / Update Script
# Run when deploying a new version:
#   sudo bash deploy.sh
# =============================================================================
set -euo pipefail

APP_DIR="/opt/asseto"
APP_USER="asseto"

GREEN='\033[0;32m'; YELLOW='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'
info()  { echo -e "${GREEN}[✓]${NC} $*"; }
warn()  { echo -e "${YELLOW}[!]${NC} $*"; }
error() { echo -e "${RED}[✗]${NC} $*"; exit 1; }

[[ $EUID -ne 0 ]] && error "Run as root: sudo bash deploy.sh"
[[ ! -d "$APP_DIR" ]] && error "$APP_DIR does not exist. Run setup.sh first."

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ── 1. Backup before deploy ───────────────────────────────────────────────────
info "Backing up database before deploy..."
sudo -u "$APP_USER" "$APP_DIR/venv/bin/python" "$APP_DIR/backup.py"

# ── 2. Copy new files ─────────────────────────────────────────────────────────
info "Copying updated files..."
rsync -a --exclude='__pycache__' --exclude='*.pyc' --exclude='.git' \
      --exclude='backups' --exclude='logs' --exclude='.env' \
      --exclude='static/photos' --exclude='static/signatures' \
      "$SCRIPT_DIR/" "$APP_DIR/"
chown -R "$APP_USER:$APP_USER" "$APP_DIR"

# ── 3. Install / update Python deps ───────────────────────────────────────────
info "Updating Python dependencies..."
sudo -u "$APP_USER" "$APP_DIR/venv/bin/pip" install -q --upgrade pip
sudo -u "$APP_USER" "$APP_DIR/venv/bin/pip" install -q -r "$APP_DIR/requirements.txt"

# ── 4. Run DB migrations ──────────────────────────────────────────────────────
info "Running database migrations..."
sudo -u "$APP_USER" "$APP_DIR/venv/bin/python" -c "
import sys; sys.path.insert(0, '$APP_DIR')
from app import app, migrate_db
with app.app_context():
    migrate_db()
print('Migrations done.')
"

# ── 5. Verify syntax ──────────────────────────────────────────────────────────
info "Syntax check..."
sudo -u "$APP_USER" "$APP_DIR/venv/bin/python" -m py_compile "$APP_DIR/app.py"

# ── 6. Reload service ─────────────────────────────────────────────────────────
info "Reloading ASSETO service..."
systemctl daemon-reload
systemctl restart asseto
sleep 2
systemctl is-active --quiet asseto \
    && info "Deploy complete — service running." \
    || error "Service failed! Check: journalctl -u asseto -n 30"
