# Whirl Sayti - Server O'rnatish Qo'llanmasi

## Fayl tarkibi

| Fayl | Tavsif |
|------|--------|
| whirl.html | Bosh sahifa |
| about.html | Biz haqimizda |
| careers.html | Karyera |
| privacy.html | Maxfiylik siyosati |
| appeals.html | Murojaatlar |
| disclosure.html | Oshkor ma'lumot |
| *.pdf | Hujjatlar (litsenziya, siyosatlar) |
| nginx.conf | Nginx konfiguratsiyasi |
| deploy.sh | Avtomatik o'rnatish skripti |

---

## Variant 1: Nginx orqali (Tavsiya etiladi)

### Talablar
- Ubuntu/Debian server
- Nginx o'rnatilgan: `sudo apt install nginx`

### Qadamlar

**1. nginx.conf faylini tahrirlang:**
```
server_name your-domain.com www.your-domain.com;
```
Bu yerda `your-domain.com` ni o'z domeningizga almashtiring.

**2. Skriptni ishga tushiring:**
```bash
chmod +x deploy.sh
./deploy.sh
```

**3. HTTPS (SSL) qo'shish uchun:**
```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d your-domain.com -d www.your-domain.com
```

---

## Variant 2: Apache orqali

```bash
sudo apt install apache2

# Papkani yarating va fayllarni ko'chiring
sudo mkdir -p /var/www/whirl
sudo cp *.html *.pdf /var/www/whirl/
sudo chown -R www-data:www-data /var/www/whirl

# Virtual host yarating
sudo nano /etc/apache2/sites-available/whirl.conf
```

Virtual host konfiguratsiyasi:
```apache
<VirtualHost *:80>
    ServerName your-domain.com
    DocumentRoot /var/www/whirl
    DirectoryIndex whirl.html

    <Directory /var/www/whirl>
        AllowOverride All
        Require all granted
    </Directory>
</VirtualHost>
```

```bash
sudo a2ensite whirl.conf
sudo systemctl reload apache2
```

---

## Variant 3: Python SimpleHTTPServer (Test uchun)

```bash
cd /home/youruser/whirl
python3 -m http.server 8080
```
Keyin brauzerda: `http://localhost:8080/whirl.html`

---

## Variant 4: GitHub Pages (Bepul hosting)

1. GitHub'da repository yarating
2. Barcha fayllarni upload qiling
3. Settings → Pages → Source: main branch
4. `whirl.html` ni `index.html` ga rename qiling

---

## Eslatmalar

- Barcha sahifalar o'zaro bog'liq, ularni bir papkada saqlang
- PDF fayllar ham shu papkada bo'lishi shart
- Sayt to'liq static (backend kerak emas)
