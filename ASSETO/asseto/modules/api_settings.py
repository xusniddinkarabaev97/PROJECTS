"""Settings Blueprint — company, SMTP, 2FA, API keys, notifications, AI, security."""

import os, json, secrets, hashlib, threading, ssl
from datetime import date, timedelta

from flask import Blueprint, render_template, request, jsonify

import bcrypt, pyotp, qrcode

from modules.config import app, ROLES
from modules.db import get_db
from modules.auth import login_required, roles_required
from modules.api_items import send_telegram as _send_telegram_ref

bp = Blueprint('settings', __name__)


# ══════════════════════════════════════════════════════════════════════════════
#  TELEGRAM HELPERS
# ══════════════════════════════════════════════════════════════════════════════

def send_telegram(chat_id, text):
    """Отправить уведомление в Telegram. chat_id — числовой ID чата."""
    return _send_telegram_ref(chat_id, text)


def send_tg_notification(user_id, message):
    """Helper to send Telegram notifications if chat_id exists."""
    with get_db() as db:
        user = db.execute("SELECT telegram_chat_id FROM users WHERE id=?", (user_id,)).fetchone()
    if user and user["telegram_chat_id"]:
        send_telegram(user["telegram_chat_id"], message)


def _tg_notify(chat_id, text):
    """Короткий хелпер для системных уведомлений (не блокирует запрос)."""
    threading.Thread(target=send_telegram, args=(chat_id, text), daemon=True).start()


# ══════════════════════════════════════════════════════════════════════════════
#  SMTP / EMAIL HELPERS
# ══════════════════════════════════════════════════════════════════════════════

def _get_smtp_cfg():
    """Read SMTP config from app_settings. Returns dict or None."""
    try:
        with get_db() as db:
            rows = db.execute(
                "SELECT key_name, key_value FROM app_settings WHERE key_name LIKE 'smtp_%'"
            ).fetchall()
        cfg = {r["key_name"]: r["key_value"] for r in rows}
        if cfg.get("smtp_host") and cfg.get("smtp_user") and cfg.get("smtp_pass"):
            return cfg
    except Exception:
        pass
    return None


def send_email(to_addr, subject, body_html):
    """Send email via SMTP if configured. Non-blocking — called in thread."""
    cfg = _get_smtp_cfg()
    if not cfg or not to_addr:
        return False
    import smtplib
    from email.mime.multipart import MIMEMultipart
    from email.mime.text import MIMEText
    try:
        host = cfg.get("smtp_host", "")
        port = int(cfg.get("smtp_port", 587))
        user = cfg.get("smtp_user", "")
        pwd  = cfg.get("smtp_pass", "")
        frm  = cfg.get("smtp_from") or user
        msg = MIMEMultipart("alternative")
        msg["Subject"] = subject
        msg["From"]    = f"ASSETO <{frm}>"
        msg["To"]      = to_addr
        msg.attach(MIMEText(body_html, "html", "utf-8"))
        ctx = ssl.create_default_context()
        if port == 465:
            with smtplib.SMTP_SSL(host, port, context=ctx, timeout=8) as s:
                s.login(user, pwd)
                s.sendmail(frm, [to_addr], msg.as_string())
        else:
            with smtplib.SMTP(host, port, timeout=8) as s:
                s.ehlo(); s.starttls(context=ctx); s.ehlo()
                s.login(user, pwd)
                s.sendmail(frm, [to_addr], msg.as_string())
        return True
    except Exception as e:
        app.logger.warning(f"Email send failed to {to_addr}: {e}")
        return False


def _email_body(title, message, action_url=None, action_label="Открыть в ASSETO"):
    """Simple HTML email template."""
    action_block = ""
    if action_url:
        action_block = f"""
        <div style="margin:24px 0;">
          <a href="{action_url}" style="display:inline-block;padding:12px 28px;background:#007AFF;color:#fff;
             text-decoration:none;border-radius:10px;font-weight:700;font-size:15px;">{action_label}</a>
        </div>"""
    return f"""
    <div style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;max-width:520px;margin:0 auto;padding:32px 24px;">
      <div style="font-size:22px;font-weight:800;color:#1a1a1a;margin-bottom:8px;">📦 ASSETO</div>
      <hr style="border:none;border-top:1px solid #eee;margin:16px 0;">
      <div style="font-size:18px;font-weight:700;color:#1a1a1a;margin-bottom:12px;">{title}</div>
      <div style="font-size:15px;color:#555;line-height:1.6;">{message}</div>
      {action_block}
      <hr style="border:none;border-top:1px solid #eee;margin:24px 0 8px;">
      <div style="font-size:12px;color:#aaa;">ASSETO — система управления активами</div>
    </div>"""


# ══════════════════════════════════════════════════════════════════════════════
#  NOTIFICATION HELPERS
# ══════════════════════════════════════════════════════════════════════════════

def notify_user(user_id, text, subject=None, action_url=None):
    """Send notification via Web Push + Telegram + Email."""
    def _do():
        try:
            with get_db() as db:
                u = db.execute(
                    "SELECT telegram_chat_id, email, name FROM users WHERE id=?", (user_id,)
                ).fetchone()
            if not u:
                return
            clean_title = subject or "ASSETO"
            clean_body = text.replace("<b>", "").replace("</b>", "").replace("\n", " ").strip()[:120]
            send_web_push(user_id, clean_title, clean_body, url=action_url or "/dashboard")
            if u["telegram_chat_id"]:
                send_telegram(u["telegram_chat_id"], text)
            if u["email"] and _get_smtp_cfg():
                subj = subject or "Уведомление ASSETO"
                html_text = text.replace("<b>", "<strong>").replace("</b>", "</strong>").replace("\n", "<br>")
                body = _email_body(subj, html_text, action_url)
                send_email(u["email"], subj, body)
        except Exception as e:
            app.logger.warning(f"notify_user error: {e}")
    threading.Thread(target=_do, daemon=True).start()


def notify_role(role, text, subject=None, action_url=None):
    """Send notification to all users with a given role."""
    try:
        with get_db() as db:
            users = db.execute("SELECT id FROM users WHERE role=? AND active=1", (role,)).fetchall()
            for u in users:
                notify_user(u["id"], text, subject=subject, action_url=action_url)
    except Exception as e:
        app.logger.warning(f"notify_role error: {role}, {e}")


# ══════════════════════════════════════════════════════════════════════════════
#  WEB PUSH HELPERS
# ══════════════════════════════════════════════════════════════════════════════

def _get_vapid():
    pub  = os.environ.get("VAPID_PUBLIC_KEY", "")
    priv = os.environ.get("VAPID_PRIVATE_KEY", "")
    claim_email = os.environ.get("VAPID_CLAIM_EMAIL", "admin@asseto.app")
    return pub, priv, claim_email


def send_web_push(user_id, title, body, url="/dashboard", urgent=False, tag="asseto"):
    """Send Web Push notification to all subscriptions of a user."""
    try:
        HAS_WEBPUSH = True
        try:
            from pywebpush import webpush, WebPushException
        except ImportError:
            HAS_WEBPUSH = False
            return False

        pub, priv, claim_email = _get_vapid()
        if not pub or not priv:
            return False

        with get_db() as db:
            subs = db.execute(
                "SELECT * FROM push_subscriptions WHERE user_id=?", (user_id,)
            ).fetchall()

        if not subs:
            return False

        payload = json.dumps({
            "title": title,
            "body": body,
            "url": url,
            "tag": tag,
            "urgent": urgent,
        })

        success = 0
        for sub in subs:
            try:
                webpush(
                    subscription_info={
                        "endpoint": sub["endpoint"],
                        "keys": {"p256dh": sub["p256dh"], "auth": sub["auth"]},
                    },
                    data=payload,
                    vapid_private_key=priv,
                    vapid_claims={"sub": f"mailto:{claim_email}"},
                )
                success += 1
            except Exception as we:
                if hasattr(we, "response") and we.response and we.response.status_code in (404, 410):
                    with get_db() as db:
                        db.execute("DELETE FROM push_subscriptions WHERE endpoint=?", (sub["endpoint"],))
                else:
                    app.logger.warning(f"Web push failed for user {user_id}: {we}")
        return success > 0
    except Exception as e:
        app.logger.warning(f"send_web_push error: {e}")
        return False


def push_role(role, title, body, url="/dashboard"):
    """Send Web Push to all users of a role."""
    try:
        with get_db() as db:
            users = db.execute("SELECT id FROM users WHERE role=? AND active=1", (role,)).fetchall()
        for u in users:
            send_web_push(u["id"], title, body, url)
    except Exception as e:
        app.logger.warning(f"push_role error: {role}, {e}")


# ══════════════════════════════════════════════════════════════════════════════
#  HTML PAGES
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/settings")
@login_required
def settings_page():
    u = request.current_user
    with get_db() as db:
        row = db.execute("SELECT totp_enabled,telegram_chat_id FROM users WHERE id=?", (u["id"],)).fetchone()
    return render_template("settings.html",
        user=u, current_user=u,
        role_info=ROLES.get(u["role"], {}),
        totp_enabled=bool(row["totp_enabled"]) if row else False,
        telegram_connected=bool(row["telegram_chat_id"]) if row else False,
        roles=ROLES
    )


@bp.route("/security")
@roles_required("superadmin")
def security_page():
    u = request.current_user
    with get_db() as db:
        try:
            logs = db.execute(
                "SELECT l.*,COALESCE(u2.name,l.email) as name "
                "FROM login_log l LEFT JOIN users u2 ON l.user_id=u2.id "
                "ORDER BY l.ts DESC LIMIT 300"
            ).fetchall()
            logs = [dict(r) for r in logs]
        except Exception: logs = []
    return render_template("security.html", user=u, current_user=u,
        role_info=ROLES.get(u["role"],{}), roles=ROLES, logs=logs)


# ══════════════════════════════════════════════════════════════════════════════
#  COMPANY SETTINGS
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/company", methods=["GET", "PUT"])
@roles_required("superadmin")
def company_settings():
    """Get or update current company settings."""
    with get_db() as db:
        if request.method == "PUT":
            d = request.json or {}
            allowed = {"name","contact_email","contact_phone","address","primary_color"}
            sets = []; vals = []
            for k, v in d.items():
                if k in allowed:
                    sets.append(f"{k}=?"); vals.append(v)
            if sets:
                vals.append(1)
                db.execute(f"UPDATE companies SET {', '.join(sets)} WHERE id=?", vals)
            return jsonify({"ok": True})
        row = db.execute("SELECT * FROM companies WHERE id=1").fetchone()
        if not row:
            return jsonify({"error": "Company not found"}), 404
        c = dict(row)
        c["user_count"] = db.execute("SELECT COUNT(*) FROM users WHERE active=1 AND company_id=1").fetchone()[0]
        c["item_count"] = db.execute("SELECT COUNT(*) FROM items WHERE company_id=1").fetchone()[0]
        return jsonify(c)


# ══════════════════════════════════════════════════════════════════════════════
#  SMTP SETTINGS
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/smtp/settings", methods=["GET", "POST"])
@roles_required("superadmin")
def smtp_settings():
    """Get or update SMTP email settings."""
    SMTP_KEYS = ("smtp_host", "smtp_port", "smtp_user", "smtp_pass", "smtp_from")
    if request.method == "POST":
        d = request.json or {}
        with get_db() as db:
            for k in SMTP_KEYS:
                if k in d:
                    db.execute(
                        "INSERT INTO app_settings (key_name, key_value) VALUES (?,?) "
                        "ON CONFLICT(key_name) DO UPDATE SET key_value=excluded.key_value",
                        (k, str(d[k]).strip())
                    )
        return jsonify({"ok": True})
    with get_db() as db:
        rows = db.execute(
            f"SELECT key_name, key_value FROM app_settings WHERE key_name IN ({','.join('?'*len(SMTP_KEYS))})",
            SMTP_KEYS
        ).fetchall()
    cfg = {r["key_name"]: r["key_value"] for r in rows}
    cfg.pop("smtp_pass", None)
    cfg["configured"] = bool(_get_smtp_cfg())
    return jsonify(cfg)


@bp.route("/api/smtp/test", methods=["POST"])
@roles_required("superadmin")
def smtp_test():
    """Send a test email to verify SMTP settings."""
    u = request.current_user
    ok = send_email(u["email"],
        "ASSETO — SMTP тест",
        _email_body("Тест SMTP", f"Привет, {u['name']}!<br>SMTP настроен корректно. Уведомления будут работать."))
    if ok:
        return jsonify({"ok": True, "sent_to": u["email"]})
    return jsonify({"error": "Не удалось отправить. Проверьте настройки."}), 400


# ══════════════════════════════════════════════════════════════════════════════
#  TELEGRAM SETTINGS
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/telegram/verify", methods=["POST"])
@login_required
def verify_telegram():
    """Пользователь вводит свой Telegram chat_id и мы отправляем тестовое сообщение."""
    u = request.current_user
    chat_id = (request.json or {}).get("chat_id", "")
    if not chat_id:
        return jsonify({"error": "Укажите chat_id"}), 400
    ok = send_telegram(chat_id, f"✅ <b>ASSETO</b>\nПривет, {u['name']}! Уведомления подключены.")
    if ok:
        with get_db() as db:
            db.execute("UPDATE users SET telegram_chat_id=? WHERE id=?", (str(chat_id), u["id"]))
        return jsonify({"ok": True})
    return jsonify({"error": "Не удалось отправить. Проверьте chat_id и бот-токен"}), 400


@bp.route("/api/telegram/disconnect", methods=["POST"])
@login_required
def disconnect_telegram():
    u = request.current_user
    with get_db() as db:
        db.execute("UPDATE users SET telegram_chat_id=NULL WHERE id=?", (u["id"],))
    return jsonify({"ok": True})


# ══════════════════════════════════════════════════════════════════════════════
#  2FA / TOTP
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/2fa/setup", methods=["POST"])
@login_required
def totp_setup():
    """Сгенерировать TOTP секрет и QR для Google Authenticator."""
    u = request.current_user
    if u["role"] not in ("superadmin", "director", "accountant", "deputy"):
        return jsonify({"error": "2FA доступна только для администраторов"}), 403
    secret = pyotp.random_base32()
    totp = pyotp.TOTP(secret)
    uri = totp.provisioning_uri(name=u["email"], issuer_name="ASSETO")
    import io as _io, base64 as _b64
    qr_img = qrcode.make(uri)
    buf = _io.BytesIO(); qr_img.save(buf, "PNG"); buf.seek(0)
    qr_b64 = _b64.b64encode(buf.read()).decode()
    with get_db() as db:
        db.execute("UPDATE users SET totp_secret=?,totp_enabled=0 WHERE id=?", (secret, u["id"]))
    return jsonify({"ok": True, "secret": secret, "qr": f"data:image/png;base64,{qr_b64}", "uri": uri})


@bp.route("/api/2fa/confirm", methods=["POST"])
@login_required
def totp_confirm():
    """Подтвердить активацию 2FA кодом из приложения."""
    u = request.current_user
    code = (request.json or {}).get("code", "")
    with get_db() as db:
        row = db.execute("SELECT totp_secret FROM users WHERE id=?", (u["id"],)).fetchone()
        if not row or not row["totp_secret"]:
            return jsonify({"error": "Сначала настройте 2FA"}), 400
        totp = pyotp.TOTP(row["totp_secret"])
        if not totp.verify(code, valid_window=1):
            return jsonify({"error": "Неверный код"}), 400
        db.execute("UPDATE users SET totp_enabled=1 WHERE id=?", (u["id"],))
    return jsonify({"ok": True})


@bp.route("/api/2fa/disable", methods=["POST"])
@login_required
def totp_disable():
    """Отключить 2FA."""
    u = request.current_user
    code = (request.json or {}).get("code", "")
    with get_db() as db:
        row = db.execute("SELECT totp_secret,totp_enabled FROM users WHERE id=?", (u["id"],)).fetchone()
        if row and row["totp_enabled"]:
            totp = pyotp.TOTP(row["totp_secret"])
            if not totp.verify(code, valid_window=1):
                return jsonify({"error": "Неверный код для отключения"}), 400
        db.execute("UPDATE users SET totp_enabled=0,totp_secret=NULL WHERE id=?", (u["id"],))
    return jsonify({"ok": True})


# ══════════════════════════════════════════════════════════════════════════════
#  API KEYS
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/api-keys", methods=["GET"])
@roles_required("superadmin")
def list_api_keys():
    with get_db() as db:
        rows = db.execute(
            "SELECT id,name,scopes,last_used,expires_at,active,created_at FROM api_keys ORDER BY created_at DESC"
        ).fetchall()
    return jsonify([dict(r) for r in rows])


@bp.route("/api/api-keys", methods=["POST"])
@roles_required("superadmin")
def create_api_key():
    u = request.current_user
    d = request.json or {}
    name = (d.get("name") or "").strip()
    if not name:
        return jsonify({"error": "Укажите название ключа"}), 400
    raw_key = "trk_" + secrets.token_hex(32)
    key_hash = hashlib.sha256(raw_key.encode()).hexdigest()
    with get_db() as db:
        cur = db.execute(
            "INSERT INTO api_keys (name,key_hash,scopes,user_id,expires_at) VALUES (?,?,?,?,?)",
            (name, key_hash, d.get("scopes","read"), u["id"], d.get("expires_at"))
        )
    return jsonify({"ok": True, "id": cur.lastrowid, "key": raw_key,
                    "warning": "Сохраните ключ — он показывается только один раз!"})


@bp.route("/api/api-keys/<int:kid>", methods=["DELETE"])
@roles_required("superadmin")
def revoke_api_key(kid):
    with get_db() as db:
        db.execute("UPDATE api_keys SET active=0 WHERE id=?", (kid,))
    return jsonify({"ok": True})


# ══════════════════════════════════════════════════════════════════════════════
#  SECURITY LOG
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/security/login-log")
@roles_required("superadmin")
def get_login_log():
    with get_db() as db:
        rows = db.execute("""
            SELECT l.*, u.name as user_name FROM login_log l
            LEFT JOIN users u ON l.user_id = u.id
            ORDER BY l.ts DESC LIMIT 200
        """).fetchall()
    return jsonify([dict(r) for r in rows])


# ══════════════════════════════════════════════════════════════════════════════
#  AUDIT
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/audit/sign", methods=["POST"])
@roles_required("superadmin", "aho", "auditor")
def sign_audit():
    """Подписать акт инвентаризации — сохраняет timestamp и подписавшего."""
    u  = request.current_user
    d  = request.json or {}
    ids = d.get("ids", [])
    note = (d.get("note") or "").strip()[:500]
    with get_db() as db:
        today = date.today().isoformat()
        for iid in ids:
            db.execute("UPDATE items SET check_date=? WHERE id=?", (today, iid))
        cur = db.execute(
            "INSERT INTO audit_log (signed_by_id, signed_by_name, item_count, note) VALUES (?,?,?,?)",
            (u["id"], u["name"], len(ids), note)
        )
    return jsonify({"ok": True, "audit_id": cur.lastrowid, "date": today, "count": len(ids)})


@bp.route("/api/audit/history")
@roles_required("superadmin", "aho", "auditor")
def audit_history():
    with get_db() as db:
        rows = db.execute(
            "SELECT * FROM audit_log ORDER BY created_at DESC LIMIT 50"
        ).fetchall()
    return jsonify([dict(r) for r in rows])


# ══════════════════════════════════════════════════════════════════════════════
#  INTEGRATIONS
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/webhooks/test", methods=["POST"])
@roles_required("superadmin")
def webhook_test():
    """Проверить webhook endpoint."""
    d = request.json or {}
    url = d.get("url", "")
    if not url.startswith("https://") and not url.startswith("http://"):
        return jsonify({"error": "Некорректный URL"}), 400
    try:
        import urllib.request as _ur
        payload = json.dumps({"event": "ping", "source": "asseto"}).encode()
        req = _ur.Request(url, data=payload,
                          headers={"Content-Type": "application/json"},
                          method="POST")
        _ur.urlopen(req, timeout=5)
        return jsonify({"ok": True})
    except Exception as e:
        app.logger.warning(f"Webhook test failed: {e}")
        return jsonify({"error": "Webhook не ответил или недоступен"}), 400


@bp.route("/api/integrations/uzinfocom/sync", methods=["POST"])
@roles_required("superadmin")
def uzinfocom_sync():
    """
    Заглушка для интеграции с Uzinfocom CRM / HR.
    Принимает список сотрудников и синхронизирует с users.
    Формат: {"employees": [{"name":"...", "email":"...", "department":"..."}]}
    """
    d = request.json or {}
    employees = d.get("employees", [])
    if not employees:
        return jsonify({"error": "Нет сотрудников"}), 400
    u = request.current_user
    created = 0; updated = 0; errors = []
    with get_db() as db:
        for emp in employees:
            name  = (emp.get("name") or "").strip()
            email = (emp.get("email") or "").strip().lower()
            dept  = (emp.get("department") or "").strip()
            if not name or not email:
                errors.append(f"Пропуск: нет имени или email — {emp}")
                continue
            try:
                existing = db.execute("SELECT id FROM users WHERE email=?", (email,)).fetchone()
                if existing:
                    db.execute("UPDATE users SET name=?, department=?, active=1 WHERE email=?",
                               (name, dept, email))
                    updated += 1
                else:
                    tmp_pw = bcrypt.hashpw(secrets.token_hex(16).encode(), bcrypt.gensalt()).decode()
                    db.execute("INSERT INTO users (name,email,password_hash,role,department) VALUES (?,?,?,?,?)",
                               (name, email, tmp_pw, "employee", dept))
                    created += 1
            except Exception as e:
                errors.append(f"{email}: {e}")
    return jsonify({"ok": True, "created": created, "updated": updated, "errors": errors})


# ══════════════════════════════════════════════════════════════════════════════
#  NOTIFICATIONS
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/notifications")
@login_required
def get_notifications():
    """Real-time notifications for nav badge."""
    u = request.current_user
    with get_db() as db:
        notifs = []
        if u["role"] in ("superadmin","aho","deputy","director","accountant"):
            pending_docs = db.execute(
                "SELECT COUNT(*) FROM documents WHERE status='pending' AND pending_role=?",
                (u["role"],)
            ).fetchone()[0]
            if pending_docs > 0:
                notifs.append({"type":"docs","count":pending_docs,"label":f"{pending_docs} документ(а) ждут согласования"})
        if u["role"] in ("superadmin","aho"):
            repair = db.execute(
                "SELECT COUNT(*) FROM maintenance WHERE status='pending'"
            ).fetchone()[0]
            if repair > 0:
                notifs.append({"type":"repair","count":repair,"label":f"{repair} заявок на ремонт"})
            overdue = db.execute(
                "SELECT COUNT(*) FROM items WHERE check_date < date('now','-180 days') OR check_date IS NULL"
            ).fetchone()[0]
            if overdue > 0:
                notifs.append({"type":"audit","count":overdue,"label":f"{overdue} активов без проверки 180+ дней"})
        
        if u["role"] == "employee":
            my_docs = db.execute(
                "SELECT COUNT(*) FROM documents WHERE created_by_id=? AND status IN ('approved','rejected') AND updated_at > datetime('now','-7 days')",
                (u["id"],)
            ).fetchone()[0]
            if my_docs > 0:
                notifs.append({"type":"docs","count":my_docs,"label":f"У вас {my_docs} обновленных документа"})
            
            pending_iss = db.execute(
                "SELECT COUNT(*) FROM issuances WHERE employee_id=? AND status='pending'",
                (u["id"],)
            ).fetchone()[0]
            if pending_iss > 0:
                notifs.append({"type":"issuance","count":pending_iss,"label":f"Ожидает подтверждения: {pending_iss} выдачи"})

        if u["role"] == "hr":
            onb = db.execute(
                "SELECT COUNT(*) FROM documents WHERE doc_type='onboarding' AND status='pending'"
            ).fetchone()[0]
            if onb > 0:
                notifs.append({"type":"onboarding","count":onb,"label":f"{onb} онбординг-заявок в процессе"})
            dis = db.execute(
                "SELECT COUNT(*) FROM dismissals WHERE status NOT IN ('completed','cancelled')"
            ).fetchone()[0]
            if dis > 0:
                notifs.append({"type":"dismissal","count":dis,"label":f"{dis} процессов увольнения"})

        if u["role"] == "auditor":
            open_sessions = db.execute(
                "SELECT COUNT(*) FROM inventory_sessions WHERE status='active'"
            ).fetchone()[0]
            if open_sessions > 0:
                notifs.append({"type":"inventory","count":open_sessions,"label":f"{open_sessions} активных сессий инвентаризации"})

        total = sum(n["count"] for n in notifs)
    return jsonify({"total": total, "items": notifs})


@bp.route("/api/alerts")
@roles_required("superadmin", "aho", "director", "deputy", "accountant", "auditor")
def get_alerts():
    """Proactive AHO alerts: warranty expiring, long repairs, pending requests, unverified assets."""
    today = date.today()
    soon = (today + timedelta(days=30)).isoformat()
    old_repair = (today - timedelta(days=30)).isoformat()
    alerts = []
    with get_db() as db:
        w_rows = db.execute(
            "SELECT inv_num, category, model, warranty_until FROM items "
            "WHERE warranty_until IS NOT NULL AND warranty_until <= ? AND warranty_until >= ? "
            "AND condition != 'Списано' ORDER BY warranty_until ASC LIMIT 10",
            (soon, today.isoformat())
        ).fetchall()
        if w_rows:
            alerts.append({
                "type": "warranty",
                "level": "warning",
                "title": f"Гарантия истекает в течение 30 дней",
                "count": len(w_rows),
                "items": [{"inv_num": r["inv_num"], "category": r["category"],
                           "model": r["model"] or "", "date": r["warranty_until"]} for r in w_rows]
            })
        r_rows = db.execute(
            "SELECT i.inv_num, i.category, i.model, m.created_at "
            "FROM maintenance m JOIN items i ON i.id = m.item_id "
            "WHERE m.status='pending' AND m.created_at <= ? "
            "ORDER BY m.created_at ASC LIMIT 10",
            (old_repair,)
        ).fetchall()
        if r_rows:
            alerts.append({
                "type": "repair",
                "level": "error",
                "title": f"В ремонте более 30 дней",
                "count": len(r_rows),
                "items": [{"inv_num": r["inv_num"], "category": r["category"],
                           "model": r["model"] or "", "since": r["created_at"][:10]} for r in r_rows]
            })
        req_count = db.execute("SELECT COUNT(*) FROM asset_requests WHERE status='pending'").fetchone()[0]
        if req_count:
            alerts.append({
                "type": "requests",
                "level": "info",
                "title": f"Заявки на оборудование",
                "count": req_count,
                "items": []
            })
        unverif = db.execute(
            "SELECT COUNT(*) FROM items WHERE (check_date IS NULL OR check_date < date('now','-180 days')) "
            "AND condition != 'Списано'"
        ).fetchone()[0]
        if unverif > 0:
            alerts.append({
                "type": "audit",
                "level": "warning" if unverif < 20 else "error",
                "title": f"Активы без проверки 180+ дней",
                "count": unverif,
                "items": []
            })
        dis_count = db.execute(
            "SELECT COUNT(*) FROM dismissals WHERE status IN ('pending_aho','pending','photos_submitted')"
        ).fetchone()[0]
        if dis_count:
            alerts.append({
                "type": "dismissal",
                "level": "warning",
                "title": f"Незавершённые увольнения",
                "count": dis_count,
                "items": []
            })
    return jsonify({"alerts": alerts, "total": sum(a["count"] for a in alerts)})


# ══════════════════════════════════════════════════════════════════════════════
#  PUSH NOTIFICATIONS (WEB PUSH)
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/push/vapid-public-key")
def push_vapid_key():
    """Return VAPID public key for client-side subscription."""
    pub, _, _ = _get_vapid()
    return jsonify({"publicKey": pub, "enabled": bool(pub)})


@bp.route("/api/push/subscribe", methods=["POST"])
@login_required
def push_subscribe():
    """Save a Web Push subscription from the browser."""
    u = request.current_user
    d = request.json or {}
    endpoint = d.get("endpoint", "").strip()
    keys = d.get("keys", {})
    p256dh = keys.get("p256dh", "").strip()
    auth   = keys.get("auth", "").strip()
    if not endpoint or not p256dh or not auth:
        return jsonify({"error": "Неполные данные подписки"}), 400
    ua = request.headers.get("User-Agent", "")[:200]
    with get_db() as db:
        db.execute(
            """INSERT INTO push_subscriptions (user_id, endpoint, p256dh, auth, user_agent)
               VALUES (?,?,?,?,?)
               ON CONFLICT(endpoint) DO UPDATE SET
                 user_id=excluded.user_id, p256dh=excluded.p256dh,
                 auth=excluded.auth, user_agent=excluded.user_agent""",
            (u["id"], endpoint, p256dh, auth, ua)
        )
    return jsonify({"ok": True})


@bp.route("/api/push/unsubscribe", methods=["POST"])
@login_required
def push_unsubscribe():
    u = request.current_user
    d = request.json or {}
    endpoint = d.get("endpoint", "")
    with get_db() as db:
        db.execute(
            "DELETE FROM push_subscriptions WHERE user_id=? AND endpoint=?",
            (u["id"], endpoint)
        )
    return jsonify({"ok": True})


# ══════════════════════════════════════════════════════════════════════════════
#  AI SETTINGS & CHAT
# ══════════════════════════════════════════════════════════════════════════════

try:
    import google.generativeai as genai
    HAS_GEMINI = True
except ImportError:
    HAS_GEMINI = False


@bp.route("/api/settings/ai", methods=["GET", "POST"])
@roles_required("superadmin")
def ai_settings():
    if request.method == "POST":
        data = request.json or {}
        key = data.get("gemini_api_key", "").strip()
        with get_db() as db:
            db.execute("INSERT INTO app_settings (key_name, key_value) VALUES ('gemini_api_key', ?) ON CONFLICT(key_name) DO UPDATE SET key_value=excluded.key_value", (key,))
        return jsonify({"ok": True})
    else:
        with get_db() as db:
            row = db.execute("SELECT key_value FROM app_settings WHERE key_name='gemini_api_key'").fetchone()
            return jsonify({"gemini_api_key": row["key_value"] if row else ""})


@bp.route("/api/ai/chat", methods=["POST"])
@login_required
def ai_chat():
    """Smart Assistant for Employee Support (True LLM / Gemini) with RAG Context"""
    data = request.json or {}
    msg = data.get("message", "").strip()
    u = request.current_user
    
    if not msg:
        return jsonify({"reply": "Напишите мне что-нибудь."})

    with get_db() as db:
        row = db.execute("SELECT key_value FROM app_settings WHERE key_name='gemini_api_key'").fetchone()
        api_key = row["key_value"] if row else ""

    if not HAS_GEMINI or not api_key:
        repair_keywords = ["гудит", "греется", "перегрев", "сломался", "кулер", "экран", "не работает", "выключается", "тормозит"]
        if any(k in msg.lower() for k in repair_keywords):
            with get_db() as db:
                item = db.execute("SELECT id, category, model FROM items WHERE employee_id=?", (u["id"],)).fetchone()
                if item:
                    active = db.execute("SELECT id FROM maintenance WHERE item_id=? AND status='pending'", (item["id"],)).fetchone()
                    if not active:
                        db.execute(
                            "INSERT INTO maintenance (item_id, description, status, priority, reported_by_id, reported_by_name) VALUES (?, ?, ?, ?, ?, ?)",
                            (item["id"], f"Авто-заявка из чата. Причина: {msg}", "pending", "high", u["id"], u["name"])
                        )
                        db.execute("UPDATE items SET condition='Требует ремонта' WHERE id=?", (item["id"],))
                    return jsonify({"reply": f"Внимание: ИИ работает в режиме симуляции (отсутствует GEMINI_API_KEY). Однако я понял, что у вас сломался {item['category']} ({item['model']}). Заявка на ремонт автоматически создана для АХО!"})
        return jsonify({"reply": "Внимание: Cloud AI не настроен. Добавьте API ключ в настройках (раздел AI). Пока я работаю в оффлайн-режиме (Mock). Опишите поломку, и я создам заявку."})

    try:
        genai.configure(api_key=api_key)
        model = genai.GenerativeModel('gemini-2.5-flash')
        
        inventory_context = []
        with get_db() as db:
            items = db.execute("SELECT id, inv_num, category, model, condition FROM items WHERE employee_id=?", (u["id"],)).fetchall()
            for i in items:
                inventory_context.append(f"- {i['category']} {i['model']} (Инв: {i['inv_num']}, Состояние: {i['condition']}) [ID: {i['id']}]")
        
        inv_str = "\\n".join(inventory_context) if inventory_context else "У данного сотрудника нет прикрепленной техники."
        
        system_prompt = f"""
        Ты корпоративный ИИ-ассистент IT/АХО отдела на платформе ASSETO.
        Общайся с пользователем на 'вы', вежливо, по-деловому, но дружелюбно.
        Пользователь: {u['name']} (Роль: {u['role']}).
        За ним числится следующая техника:
        {inv_str}
        
        ПРАВИЛА ТВОЕЙ РАБОТЫ:
        1. Если пользователь жалуется на поломку конкретной техники из его списка, ты ДОЛЖЕН создать заявку. Для этого добавь в любом месте своего ответа секретный тег: [TICKET_CREATE_ID_XXX], где XXX — это числовой ID проблемного оборудования из списка.
        2. Если проблема решается простым советом (перезагрузить, протереть, нажать кнопку) — дай совет, но если пользователь настаивает на ремонте — создай заявку тегом.
        3. Если пользователь просто здоровается, поздоровайся в ответ.
        4. НИКОГДА не показывай тег [TICKET_CREATE_ID_XXX] пользователю напрямую, наша система вырежет его из ответа, просто включи его в генерируемый текст.
        5. Форматируй свой ответ красиво, используя HTML (<b>жирный</b>, <br> для переноса строки).
        """
        
        response = model.generate_content(f"{system_prompt}\\n\\n--- Сообщение пользователя: {msg} ---")
        reply_text = response.text
        
        if "[TICKET_CREATE_ID_" in reply_text:
            try:
                start_idx = reply_text.index("[TICKET_CREATE_ID_") + len("[TICKET_CREATE_ID_")
                end_idx = reply_text.index("]", start_idx)
                item_id = int(reply_text[start_idx:end_idx])
                
                with get_db() as db:
                    active = db.execute("SELECT id FROM maintenance WHERE item_id=? AND status='pending'", (item_id,)).fetchone()
                    if not active:
                        db.execute(
                            "INSERT INTO maintenance (item_id, description, status, priority, reported_by_id, reported_by_name) VALUES (?, ?, ?, ?, ?, ?)",
                            (item_id, f"Сгенерировано ИИ Gemini: {msg}", "pending", "high", u["id"], u["name"])
                        )
                        db.execute("UPDATE items SET condition='Требует ремонта' WHERE id=?", (item_id,))
                
                reply_text = reply_text[:reply_text.index("[TICKET_CREATE_ID_")] + reply_text[end_idx+1:]
            except Exception as e:
                app.logger.error(f"Gemini Ticket Parsing Error: {e}")
                
        return jsonify({"reply": reply_text.strip()})
        
    except Exception as e:
        app.logger.error(f"Gemini API Error: {e}")
        return jsonify({"reply": f"Ошибка связи с Cloud AI (Gemini): {str(e)}"})


@bp.route("/api/ai/ocr", methods=["POST"])
@roles_required("superadmin", "aho")
def ai_ocr():
    """Smart Invoice Scanner (AI OCR) using Gemini 1.5 Flash"""
    if "file" not in request.files:
        return jsonify({"error": "Нет файла"}), 400
        
    file = request.files["file"]
    if file.filename == "":
        return jsonify({"error": "Пустой файл"}), 400

    u = request.current_user
    with get_db() as db:
        row = db.execute("SELECT key_value FROM app_settings WHERE key_name='gemini_api_key'").fetchone()
        api_key = row["key_value"] if row else ""

    if not HAS_GEMINI or not api_key:
        import time
        time.sleep(1.5)
        mock_data = [
            {"category": "Ноутбук", "model": "Dell XPS 15 (Скан)", "quantity": 1, "price": 120000},
            {"category": "Монитор", "model": "LG UltraWide 34 (Скан)", "quantity": 2, "price": 45000}
        ]
        return jsonify({"items": mock_data, "message": "Offline Mock: Извлечено 2 позиции"})

    try:
        genai.configure(api_key=api_key)
        model = genai.GenerativeModel('gemini-2.5-flash')
        
        import PIL.Image
        import io as _io
        img = PIL.Image.open(_io.BytesIO(file.read()))
        
        prompt = """
        Ты OCR-ассистент для инвентаризации.
        Посмотри на это изображение (накладная, счет или чек).
        Извлеки список купленной техники (компьютеры, мебель, электроника).
        Верни ТОЛЬКО валидный JSON-массив объектов. Никакого Markdown (убери ```json).
        Формат каждого объекта: {"category": "Категория", "model": "Точная модель", "quantity": Число, "price": Цена за 1 шт (число)}.
        Пример: [{"category": "Ноутбук", "model": "MacBook Pro M3", "quantity": 2, "price": 150000}]
        Если техники нет, верни [].
        """
        
        response = model.generate_content([prompt, img])
        text = response.text.strip()
        
        if text.startswith("```json"): text = text[7:]
        if text.startswith("```"): text = text[3:]
        if text.endswith("```"): text = text[:-3]
        text = text.strip()
        
        items = json.loads(text)
        return jsonify({"items": items, "message": f"Извлечено позиций: {len(items)}"})
        
    except Exception as e:
        app.logger.error(f"Gemini OCR Error: {e}")
        return jsonify({"error": f"Ошибка AI OCR: {str(e)}"}), 500