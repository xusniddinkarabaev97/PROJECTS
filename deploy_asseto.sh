#!/bin/bash
# === ASSETO Deploy — Run on 10.0.1.131 ===
# Downloads repo from GitHub and starts via Docker Compose
set -e

echo "=== ASSETO DLP/DPI Deploy ==="

# 1. Install Docker if missing
echo "[1/5] Checking Docker..."
if ! command -v docker &>/dev/null; then
    echo "  Installing Docker..."
    curl -fsSL https://get.docker.com | sh
fi
if ! command -v docker-compose &>/dev/null && ! docker compose version &>/dev/null 2>&1; then
    echo "  Installing docker-compose-plugin..."
    apt-get update -qq && apt-get install -y -qq docker-compose-plugin
fi

# 2. Clone/pull repo
echo "[2/5] Pulling latest code..."
REPO_DIR=/opt/asseto
if [ -d "$REPO_DIR/.git" ]; then
    cd "$REPO_DIR"
    git pull origin main
else
    rm -rf "$REPO_DIR"
    git clone https://github.com/xusniddinkarabaev97/PROJECTS.git "$REPO_DIR"
fi

# 3. Generate a SECRET_KEY if not set
if ! grep -q "SECRET_KEY=CHANGE_ME" "$REPO_DIR/docker-compose.asseto.yml" 2>/dev/null; then
    echo "  SECRET_KEY already configured."
else
    NEW_KEY=$(python3 -c "import secrets; print(secrets.token_hex(32))" 2>/dev/null || openssl rand -hex 32)
    sed -i "s/CHANGE_ME_USE_RANDOM_64_CHAR_STRING_IN_PRODUCTION/$NEW_KEY/" "$REPO_DIR/docker-compose.asseto.yml"
    echo "  Generated new SECRET_KEY."
fi

# 4. Start services
echo "[3/5] Starting containers..."
cd "$REPO_DIR"
docker compose -f docker-compose.asseto.yml down --remove-orphans 2>/dev/null || true
docker compose -f docker-compose.asseto.yml up -d --build

# 5. Wait for health
echo "[4/5] Waiting for services..."
sleep 8
for i in $(seq 1 15); do
    if curl -sf http://127.0.0.1:8000/api/health >/dev/null 2>&1; then
        echo "  ASSETO is healthy!"
        break
    fi
    echo "  Waiting... ($i/15)"
    sleep 2
done

# 6. Verify
echo "[5/5] Status:"
docker compose -f docker-compose.asseto.yml ps
echo ""
echo "======================================="
echo "  ASSETO DEPLOY COMPLETE!"
echo "  Local:  http://10.0.1.131:8000"
echo "  Public: https://whirl.uz/asseto/"
echo "======================================="
