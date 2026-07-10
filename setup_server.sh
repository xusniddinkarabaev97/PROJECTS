#!/bin/bash
# === Whirl Site Setup - Run directly on the Ubuntu ECS server ===
# Copy-paste this entire script into the server terminal (VNC console)

set -e

echo "=== Whirl Site Setup ==="

# 1. Update & install nginx
echo "[1/5] Installing nginx..."
sudo apt-get update -qq
sudo apt-get install -y -qq nginx git curl

# 2. Download site files from GitHub
echo "[2/5] Downloading site files..."
sudo rm -rf /var/www/whirl
sudo mkdir -p /var/www/whirl
cd /tmp
curl -sL https://github.com/xusniddinkarabaev97/PROJECTS/raw/main/index.html -o index.html
curl -sL https://github.com/xusniddinkarabaev97/PROJECTS/raw/main/whirl.html -o whirl.html
curl -sL https://github.com/xusniddinkarabaev97/PROJECTS/raw/main/about.html -o about.html
curl -sL https://github.com/xusniddinkarabaev97/PROJECTS/raw/main/careers.html -o careers.html
curl -sL https://github.com/xusniddinkarabaev97/PROJECTS/raw/main/privacy.html -o privacy.html
curl -sL https://github.com/xusniddinkarabaev97/PROJECTS/raw/main/appeals.html -o appeals.html
curl -sL https://github.com/xusniddinkarabaev97/PROJECTS/raw/main/disclosure.html -o disclosure.html
curl -sL https://github.com/xusniddinkarabaev97/PROJECTS/raw/main/nginx.conf -o nginx.conf
curl -sL https://github.com/xusniddinkarabaev97/PROJECTS/raw/main/anticorruption_policy.pdf -o anticorruption_policy.pdf
curl -sL https://github.com/xusniddinkarabaev97/PROJECTS/raw/main/appeals_rules.pdf -o appeals_rules.pdf
curl -sL https://github.com/xusniddinkarabaev97/PROJECTS/raw/main/code_of_ethics.pdf -o code_of_ethics.pdf
curl -sL https://github.com/xusniddinkarabaev97/PROJECTS/raw/main/esg_policy.pdf -o esg_policy.pdf
curl -sL https://github.com/xusniddinkarabaev97/PROJECTS/raw/main/license_66_2026.pdf -o license_66_2026.pdf

# 3. Move files to site directory
echo "[3/5] Setting up site directory..."
sudo cp *.html /var/www/whirl/
sudo cp *.pdf /var/www/whirl/
sudo chown -R www-data:www-data /var/www/whirl
sudo chmod -R 755 /var/www/whirl

# 4. Configure nginx
echo "[4/5] Configuring nginx..."
sudo cp nginx.conf /etc/nginx/sites-available/whirl
sudo ln -sf /etc/nginx/sites-available/whirl /etc/nginx/sites-enabled/whirl
sudo rm -f /etc/nginx/sites-enabled/default

# 5. Test & restart nginx
echo "[5/5] Testing and restarting nginx..."
sudo nginx -t && sudo systemctl reload nginx

echo ""
echo "======================================="
echo "  DEPLOY COMPLETE!"
echo "  Internal IP: http://10.0.1.16"
echo "======================================="
echo ""
echo "To make it public, bind an EIP to this server in the console."
