#!/usr/bin/env bash
# =============================================================================
# ASSETO — Production Setup Script (Ubuntu 22.04 / Debian 12)
# Run once on a fresh server as root:
#   bash setup.sh
# =============================================================================
set -euo pipefail

# ── Config — edit before running ─────────────────────────────────────────────
APP_DIR="/opt/asseto"
APP_USER="asseto"
DOMAIN=""          # e.g. asseto.mycompany.uz  (leave empty to skip SSL)
EMAIL=""           # e.g. admin@mycompany.uz   (for Let's Encrypt)
# ─────────────────────────────────────────────────────────────────────────────

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
info()  { echo -e "${GREEN}[✓]${NC} $*"; }
warn()  { echo -e "${YELLOW}[!]${NC} $*"; }
error() { echo -e "${RED}[✗]${NC} $*"; exit 1; }

[[ $EUID -ne 0 ]] && error "Run as root: sudo bash setup.sh"

# ── 1. System packages ───────────────────────────────────────────────────────
info "Installing system packages..."
apt-get update -qq
apt-get install -y -qq python3.11 python3.11-venv python3-pip nginx certbot python3-certbot-nginx postgresql postgresql-client

# ── 2. App user & directory ─────────────────────────────────────────────────
info "Creating app user '$APP_USER'..."
id "$APP_USER" &>/dev/null || useradd --system --no-create-home --shell /usr/sbin/nologin "$APP_USER"

mkdir -p "$APP_DIR"/{logs,backups,static/{photos,signatures}}
chown -R "$APP_USER:$APP_USER" "$APP_DIR"

# ── 3. Copy files ────────────────────────────────────────────────────────────
info "Copying application files..."
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
rsync -a --exclude='__pycache__' --exclude='*.pyc' --exclude='.git' \
      --exclude='backups' --exclude='logs' --exclude='.env' \
      "$SCRIPT_DIR/" "$APP_DIR/"
chown -R "$APP_USER:$APP_USER" "$APP_DIR"

# ── 4. Python virtualenv ─────────────────────────────────────────────────────
info "Creating virtualenv & installing dependencies..."
sudo -u "$APP_USER" python3.11 -m venv "$APP_DIR/venv"
sudo -u "$APP_USER" "$APP_DIR/venv/bin/pip" install -q --upgrade pip
sudo -u "$APP_USER" "$APP_DIR/venv/bin/pip" install -q -r "$APP_DIR/requirements.txt"

# ── 5. .env file ────────────────────────────────────────────────────────────
if [[ ! -f "$APP_DIR/.env" ]]; then
    warn ".env not found — creating default. EDIT IT BEFORE STARTING!"
    SECRET=$(python3 -c "import secrets; print(secrets.token_hex(32))")
    cat > "$APP_DIR/.env" <<EOF
SECRET_KEY=$SECRET
PORT=5000
HOST=127.0.0.1
MAX_UPLOAD_MB=16
JWT_EXPIRY=43200
SECURE_COOKIES=True
DATABASE_URL=postgresql://asseto:asseto_secret@localhost:5432/asseto
BASE_URL=${DOMAIN:+https://$DOMAIN}
EOF
    chown "$APP_USER:$APP_USER" "$APP_DIR/.env"
    chmod 600 "$APP_DIR/.env"
    warn "Edit $APP_DIR/.env and set BASE_URL, then restart."
else
    info ".env already exists — skipping"
fi

# ── 6. PostgreSQL setup ────────────────────────────────────────────────────────
info "Setting up PostgreSQL..."
DB_NAME="asseto"
DB_USER="asseto"
DB_PASS="asseto_secret"

# Start PostgreSQL
systemctl enable postgresql
systemctl start postgresql

# Create user & database (idempotent)
su - postgres -c "psql -tc \"SELECT 1 FROM pg_roles WHERE rolname='$DB_USER'\" | grep -q 1 || psql -c \"CREATE USER $DB_USER WITH PASSWORD '$DB_PASS';\""
su - postgres -c "psql -tc \"SELECT 1 FROM pg_database WHERE datname='$DB_NAME'\" | grep -q 1 || psql -c \"CREATE DATABASE $DB_NAME OWNER $DB_USER;\""
su - postgres -c "psql -c 'GRANT ALL PRIVILEGES ON DATABASE $DB_NAME TO $DB_USER;'"

info "PostgreSQL: database '$DB_NAME' ready"

# Initialize tables via the app
info "Initializing database tables..."
sudo -u "$APP_USER" "$APP_DIR/venv/bin/python" -c "
import sys; sys.path.insert(0, '$APP_DIR')
from app import app, init_db, migrate_db
with app.app_context():
    init_db(); migrate_db()
print('DB initialized.')
"

# ── 7. Systemd service ───────────────────────────────────────────────────────
info "Installing systemd service..."
# Patch service file paths
sed "s|/opt/asseto|$APP_DIR|g" "$APP_DIR/asseto.service" \
    > /etc/systemd/system/asseto.service
sed -i "s|User=www-data|User=$APP_USER|g; s|Group=www-data|Group=$APP_USER|g" \
    /etc/systemd/system/asseto.service

systemctl daemon-reload
systemctl enable asseto
systemctl restart asseto
sleep 2
systemctl is-active --quiet asseto && info "ASSETO service: running" || error "Service failed — check: journalctl -u asseto -n 50"

# ── 8. Nginx ──────────────────────────────────────────────────────────────────
info "Configuring Nginx..."
NGINX_CONF="/etc/nginx/sites-available/asseto"

if [[ -n "$DOMAIN" ]]; then
    sed "s|your-domain.com|$DOMAIN|g; s|www.your-domain.com|www.$DOMAIN|g" \
        "$APP_DIR/nginx.conf" > "$NGINX_CONF"
else
    # HTTP-only config for LAN / no domain
    cat > "$NGINX_CONF" <<'NGINX'
server {
    listen 80 default_server;
    client_max_body_size 20M;
    location /static/ {
        alias /opt/asseto/static/;
        expires 7d;
    }
    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 60s;
    }
}
NGINX
    # Fix static path
    sed -i "s|/opt/asseto/static|$APP_DIR/static|g" "$NGINX_CONF"
fi

ln -sf "$NGINX_CONF" /etc/nginx/sites-enabled/asseto
rm -f /etc/nginx/sites-enabled/default
nginx -t && systemctl reload nginx
info "Nginx: configured"

# ── 9. SSL via Let's Encrypt ─────────────────────────────────────────────────
if [[ -n "$DOMAIN" && -n "$EMAIL" ]]; then
    info "Obtaining SSL certificate for $DOMAIN..."
    certbot --nginx -d "$DOMAIN" -d "www.$DOMAIN" --email "$EMAIL" \
            --agree-tos --non-interactive --redirect
    info "SSL: installed. Auto-renewal is handled by certbot systemd timer."
else
    warn "DOMAIN or EMAIL not set — skipping SSL. Set them in setup.sh and re-run."
fi

# ── 10. Cron: daily backup ───────────────────────────────────────────────────
info "Setting up daily backup (02:00)..."
cat > /etc/cron.d/asseto-backup <<CRON
# ASSETO daily DB backup — runs at 02:00
0 2 * * * $APP_USER $APP_DIR/venv/bin/python $APP_DIR/backup.py >> $APP_DIR/logs/backup.log 2>&1
CRON
chmod 644 /etc/cron.d/asseto-backup
info "Cron: /etc/cron.d/asseto-backup installed"

# ── 11. Log rotation ──────────────────────────────────────────────────────────
cat > /etc/logrotate.d/asseto <<LOGROTATE
$APP_DIR/logs/*.log {
    daily
    rotate 14
    compress
    delaycompress
    missingok
    notifempty
    sharedscripts
    postrotate
        systemctl reload asseto 2>/dev/null || true
    endscript
}
LOGROTATE
info "Logrotate: configured"

# ── Done ──────────────────────────────────────────────────────────────────────
echo ""
echo -e "${GREEN}══════════════════════════════════════════${NC}"
echo -e "${GREEN}  ASSETO installed successfully!          ${NC}"
echo -e "${GREEN}══════════════════════════════════════════${NC}"
echo ""
[[ -n "$DOMAIN" ]] && echo "  URL:     https://$DOMAIN" \
                   || echo "  URL:     http://$(hostname -I | awk '{print $1}')"
echo "  Logs:    journalctl -u asseto -f"
echo "  Restart: systemctl restart asseto"
echo "  Backup:  $APP_DIR/venv/bin/python $APP_DIR/backup.py"
echo ""
warn "First run: create superadmin account via the web UI."
