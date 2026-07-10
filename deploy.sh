#!/bin/bash
# Whirl sayti uchun deployment script

set -e

SITE_DIR="/var/www/whirl"
NGINX_CONF="/etc/nginx/sites-available/whirl"
NGINX_ENABLED="/etc/nginx/sites-enabled/whirl"

echo "=== Whirl saytini serverga o'rnatish ==="

# 1. Sayt papkasini yaratish
echo "1. Papka yaratilmoqda..."
sudo mkdir -p $SITE_DIR

# 2. Fayllarni ko'chirish
echo "2. Fayllar ko'chirilmoqda..."
sudo cp *.html $SITE_DIR/
sudo cp *.pdf $SITE_DIR/
sudo chown -R www-data:www-data $SITE_DIR
sudo chmod -R 755 $SITE_DIR

# 3. Nginx konfiguratsiyasini o'rnatish
echo "3. Nginx konfiguratsiyasi o'rnatilmoqda..."
sudo cp nginx.conf $NGINX_CONF
sudo ln -sf $NGINX_CONF $NGINX_ENABLED

# 4. Default nginx konfiguratsiyasini o'chirish (agar mavjud bo'lsa)
if [ -f /etc/nginx/sites-enabled/default ]; then
    sudo rm /etc/nginx/sites-enabled/default
fi

# 5. Nginx konfiguratsiyasini tekshirish
echo "4. Nginx tekshirilmoqda..."
sudo nginx -t

# 6. Nginx ni qayta ishga tushirish
echo "5. Nginx qayta ishga tushirilmoqda..."
sudo systemctl reload nginx

echo ""
echo "=== O'rnatish muvaffaqiyatli tugadi! ==="
echo "Saytingiz: http://your-domain.com"
echo ""
echo "MUHIM: nginx.conf faylida 'your-domain.com' ni haqiqiy domeningizga almashtiring!"
