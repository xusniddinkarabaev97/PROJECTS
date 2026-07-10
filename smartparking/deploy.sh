#!/bin/bash
set -e
echo "=== SmartParking Deploy ==="

echo "[1/6] .NET 9..."
if ! command -v dotnet &> /dev/null; then
    wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh
    sudo /tmp/dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet
    sudo ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
fi
export DOTNET_ROOT=/usr/share/dotnet
export PATH=$PATH:$DOTNET_ROOT

echo "[2/6] Node.js 22..."
if ! command -v node &> /dev/null; then
    curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash -
    sudo apt-get install -y -qq nodejs
fi

echo "[3/6] PostgreSQL + nginx..."
sudo apt-get update -qq
sudo apt-get install -y -qq postgresql postgresql-contrib nginx git

echo "[4/6] Clone & DB..."
sudo rm -rf /opt/smartparking
sudo git clone https://github.com/xusniddinkarabaev97/PROJECTS.git /opt/smartparking
sudo -u postgres psql -c "CREATE USER dlpdpi WITH PASSWORD ''dlpdpi_dev_password'';" 2>/dev/null || true
sudo -u postgres psql -c "CREATE DATABASE \"SmartParkingDb\" OWNER dlpdpi;" 2>/dev/null || true
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE \"SmartParkingDb\" TO dlpdpi;"

echo "[5/6] Build API..."
cd /opt/smartparking/smartparking/SmartParking
dotnet publish -c Release -o /opt/smartparking/api --nologo
sudo cp /opt/smartparking/smartparking/seed.sql /tmp/
sudo -u postgres psql -d SmartParkingDb -f /tmp/seed.sql 2>/dev/null || true

echo "[6/6] Build Admin Panel..."
cd /opt/smartparking/smartparking
npm install --silent
npx vite build
sudo mkdir -p /var/www/smartparking
sudo cp -r dist/* /var/www/smartparking/

sudo tee /etc/systemd/system/smartparking.service << "SVC"
[Unit]
Description=SmartParking API
After=network.target postgresql.service
[Service]
WorkingDirectory=/opt/smartparking/api
ExecStart=/usr/share/dotnet/dotnet SmartParking.dll
Restart=always
Environment=DOTNET_ROOT=/usr/share/dotnet
Environment=ASPNETCORE_URLS=http://0.0.0.0:5121
[Install]
WantedBy=multi-user.target
SVC

sudo tee /etc/nginx/conf.d/smartparking.conf << "NFX"
server {
    listen 80;
    root /var/www/smartparking;
    index index.html;
    location / { try_files $uri $uri/ /index.html; }
    location /api/ { proxy_pass http://127.0.0.1:5121; proxy_set_header Host $host; proxy_set_header X-Real-IP $remote_addr; }
    location /swagger/ { proxy_pass http://127.0.0.1:5121; proxy_set_header Host $host; }
    location /Billing/ { proxy_pass http://127.0.0.1:5121; proxy_set_header Host $host; }
    location /payment/ { proxy_pass http://127.0.0.1:5121; proxy_set_header Host $host; }
}
NFX

sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t && sudo systemctl reload nginx
sudo systemctl daemon-reload
sudo systemctl enable smartparking
sudo systemctl restart smartparking

echo "Done! http://10.0.1.206"
