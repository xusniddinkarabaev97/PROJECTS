#!/bin/bash
# === Whirl Deploy via Huawei Cloud Shell ===
# Run this in Huawei Cloud Console -> Cloud Shell
# It has direct access to private IPs in your VPC

set -e

ECS_IP="10.0.1.16"
ECS_USER="root"
SITE_DIR="/var/www/whirl"

echo "=== Whirl Site Deploy to ECS $ECS_IP ==="

# 1. Clone project from GitHub
echo "1. Cloning project..."
rm -rf /tmp/whirl_deploy
git clone https://github.com/xusniddinkarabaev97/PROJECTS.git /tmp/whirl_deploy
cd /tmp/whirl_deploy

# 2. Copy files to ECS
echo "2. Copying files to ECS..."
scp -o StrictHostKeyChecking=no *.html *.pdf nginx.conf "$ECS_USER@$ECS_IP:/tmp/"

# 3. Run setup on ECS
echo "3. Setting up nginx on ECS..."
ssh -o StrictHostKeyChecking=no "$ECS_USER@$ECS_IP" << 'ENDSSH'
set -e

# Install nginx
which nginx || (apt-get update -qq && apt-get install -y -qq nginx)

# Create site dir
mkdir -p /var/www/whirl

# Move files
cp /tmp/*.html /var/www/whirl/
cp /tmp/*.pdf /var/www/whirl/
cp /tmp/nginx.conf /etc/nginx/sites-available/whirl

# Set permissions
chown -R www-data:www-data /var/www/whirl
chmod -R 755 /var/www/whirl

# Enable site
ln -sf /etc/nginx/sites-available/whirl /etc/nginx/sites-enabled/whirl
rm -f /etc/nginx/sites-enabled/default

# Test and reload
nginx -t && systemctl reload nginx

echo "Done! Site is live."
ENDSSH

echo ""
echo "=== DEPLOY COMPLETE ==="
echo "Internal: http://$ECS_IP"
