#!/bin/bash
# === GZS Billing Deploy on Ubuntu 24.04 ===
# Run on server 10.0.1.25
set -e

echo "========================================="
echo "  GZS Billing - Server Setup"
echo "========================================="

# 1. Install dependencies
echo "[1/8] Installing .NET 9 + Node.js + PostgreSQL + nginx..."
sudo apt-get update -qq

# .NET 9
if ! command -v dotnet &> /dev/null; then
    wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O /tmp/msprod.deb
    sudo dpkg -i /tmp/msprod.deb
    sudo apt-get update -qq
    sudo apt-get install -y -qq dotnet-sdk-9.0
fi

# Node 20
if ! command -v node &> /dev/null; then
    curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
    sudo apt-get install -y -qq nodejs
fi

# PostgreSQL
if ! command -v psql &> /dev/null; then
    sudo apt-get install -y -qq postgresql postgresql-contrib
fi

# nginx
sudo apt-get install -y -qq nginx

echo "  Dependencies OK"

# 2. Clone project
echo "[2/8] Cloning project..."
sudo rm -rf /opt/gzs_billing
sudo git clone https://github.com/xusniddinkarabaev97/PROJECTS.git /opt/gzs_billing
cd /opt/gzs_billing/gzs_billing

# 3. Setup PostgreSQL
echo "[3/8] Setting up PostgreSQL..."
sudo -u postgres psql -tc "SELECT 1 FROM pg_roles WHERE rolname='dlpdpi'" | grep -q 1 || \
    sudo -u postgres psql -c "CREATE USER dlpdpi WITH PASSWORD 'dlpdpi_dev_password';"
sudo -u postgres psql -tc "SELECT 1 FROM pg_database WHERE datname='gzs_billing'" | grep -q 1 || \
    sudo -u postgres psql -c "CREATE DATABASE gzs_billing OWNER dlpdpi;"
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE gzs_billing TO dlpdpi;"

# 4. Build .NET API
echo "[4/8] Building .NET API..."
cd src/GzsBilling.Api
dotnet publish -c Release -o /opt/gzs_billing/api --nologo

# 5. Build React admin panel
echo "[5/8] Building Admin Panel..."
cd /opt/gzs_billing/gzs_billing/admin-panel
npm install --silent
npm run build

# 6. Copy admin panel to nginx
echo "[6/8] Configuring static files..."
sudo mkdir -p /var/www/gzs
sudo cp -r dist/* /var/www/gzs/

# 7. Create systemd service
echo "[7/8] Creating systemd service..."
sudo tee /etc/systemd/system/gzs-billing.service << 'EOF'
[Unit]
Description=GZS Billing API
After=network.target postgresql.service

[Service]
WorkingDirectory=/opt/gzs_billing/api
ExecStart=/usr/bin/dotnet GzsBilling.Api.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5036

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable gzs-billing
sudo systemctl restart gzs-billing

# 8. Configure nginx
echo "[8/8] Configuring nginx..."
sudo tee /etc/nginx/conf.d/gzs.conf << 'EOF'
server {
    listen 80;
    server_name _;

    # Admin panel
    root /var/www/gzs;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # API proxy
    location /api/ {
        proxy_pass http://127.0.0.1:5036;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }

    location /swagger/ {
        proxy_pass http://127.0.0.1:5036;
        proxy_set_header Host $host;
    }
}
EOF

sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t && sudo systemctl reload nginx

echo ""
echo "========================================="
echo "  GZS Billing Deployed!"
echo "  Internal: http://10.0.1.25"
echo ""
echo "  Login: admin / admin123!"
echo "========================================="
