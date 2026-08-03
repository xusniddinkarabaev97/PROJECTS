#!/bin/bash
# === BillingSCHR Deploy — Run on 10.0.1.88 ===
set -e

echo "=== BillingSCHR Deploy ==="

# 1. Install Docker
echo "[1/4] Checking Docker..."
if ! command -v docker &>/dev/null; then
    curl -fsSL https://get.docker.com | sh
fi

# 2. Clone repo
echo "[2/4] Pulling code..."
REPO_DIR=/opt/billingschr
if [ -d "$REPO_DIR/.git" ]; then
    cd "$REPO_DIR"
    git pull origin main
else
    rm -rf "$REPO_DIR"
    git clone https://github.com/xusniddinkarabaev97/PROJECTS.git "$REPO_DIR"
fi

# 3. Start services
echo "[3/4] Starting containers..."
cd "$REPO_DIR"
docker compose -f docker-compose.billingschr.yml down --remove-orphans 2>/dev/null || true
docker compose -f docker-compose.billingschr.yml up -d --build

# 4. Wait & verify
echo "[4/4] Checking..."
sleep 10
curl -sf http://127.0.0.1:8080/swagger/index.html >/dev/null 2>&1 && echo "OK" || echo "Waiting..."
echo ""
echo "=== BillingSCHR ==="
echo "  API:     http://10.0.1.88:8080"
echo "  Swagger: http://10.0.1.88:8080/swagger"
echo "  Admin:   admin / admin123"
