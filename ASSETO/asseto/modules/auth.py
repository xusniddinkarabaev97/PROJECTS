"""Authentication Blueprint — auth helpers, decorators, CSRF, rate limiting, and core routes."""

import os, io, uuid, socket, time, secrets, base64, traceback
from datetime import date, datetime
from functools import wraps
import hmac as _hmac

import bcrypt
import jwt as pyjwt
from openpyxl import Workbook
from PIL import Image, ImageDraw

from flask import (Blueprint, render_template, request, jsonify, send_file,
                   abort, redirect, make_response)

from modules.config import (app, JWT_EXPIRY, SECURE_COOKIES, MAX_UPLOAD_MB,
                            ROLES, CATEGORIES, CONDITIONS, STATUSES, SIGS,
                            DatabaseError)
from modules.db import get_db

bp = Blueprint('auth', __name__)

# ── Module-level state ───────────────────────────────────────────────────────────
_rate_buckets: dict = {}
_CSRF_SKIP_PATHS = {'/api/auth/login', '/api/health'}
_RATE_STORE: dict = {}  # ip -> {"count":N, "reset":timestamp}
RATE_LIMIT_DEFAULT = 300  # requests per minute for API
RATE_LIMIT_LOGIN   = 5    # login attempts per 5 min


# ══════════════════════════════════════════════════════════════════════════════════
#  AUTH HELPERS
# ══════════════════════════════════════════════════════════════════════════════════

def check_rate_limit(ip):
    """Returns (allowed, seconds_left). Uses DB so it persists across restarts."""
    now = time.time()
    window = now - 600  # 10-minute window
    with get_db() as db:
        row = db.execute(
            "SELECT COUNT(*) as cnt FROM login_log WHERE ip=? AND success=0 AND ts > datetime(?, 'unixepoch')",
            (ip, window)
        ).fetchone()
        failures = row["cnt"] if row else 0
    if failures >= 5:
        # Find when the 5th failure happened to calculate lockout end
        with get_db() as db:
            first_fail = db.execute(
                "SELECT ts FROM login_log WHERE ip=? AND success=0 AND ts > datetime(?, 'unixepoch') ORDER BY ts ASC LIMIT 1 OFFSET 4",
                (ip, window)
            ).fetchone()
        if first_fail:
            from datetime import datetime as _dt
            try:
                fail_ts = _dt.strptime(first_fail["ts"], "%Y-%m-%d %H:%M:%S").timestamp()
                locked_until = fail_ts + 300
                if now < locked_until:
                    return False, int(locked_until - now)
            except Exception:
                pass
    return True, 0


def record_failed_login(ip):
    pass  # Recorded in login_log by api_login — no separate store needed


def clear_failed_login(ip):
    pass  # Not needed — window-based check auto-clears after 10 min


def make_token(u):
    return pyjwt.encode({"sub": str(u["id"]), "role": u["role"], "name": u["name"],
                          "tv": u.get("token_version", 0),
                          "exp": int(time.time()) + JWT_EXPIRY},
                         app.config["SECRET_KEY"], algorithm="HS256")


def get_current_user():
    token = request.cookies.get("token") or request.headers.get("Authorization", "").replace("Bearer ", "")
    if not token:
        return None
    try:
        p = pyjwt.decode(token, app.config["SECRET_KEY"], algorithms=["HS256"])
        user_id = int(p["sub"]) if p.get("sub") else None
        if not user_id:
            return None
        with get_db() as db:
            u = db.execute("SELECT * FROM users WHERE id=? AND active=1", (user_id,)).fetchone()
        if not u:
            return None
        u = dict(u)
        # Check token_version (invalidated on logout/password change)
        token_ver = p.get("tv", 0)
        if token_ver != (u.get("token_version") or 0):
            return None
        # Check account expiry
        if u.get("expires_at"):
            from datetime import date as _date
            if _date.today().isoformat() > u["expires_at"]:
                return None
        return u
    except Exception as e:
        app.logger.error(f"Auth error: {e}")
        return None


def login_required(f):
    @wraps(f)
    def dec(*a, **kw):
        u = get_current_user()
        if not u:
            if request.is_json:
                return jsonify({"error": "Не авторизован"}), 401
            return redirect("/login")
        request.current_user = u
        return f(*a, **kw)
    return dec


def roles_required(*roles):
    def decorator(f):
        @wraps(f)
        @login_required
        def dec(*a, **kw):
            if request.current_user["role"] not in roles:
                if request.is_json:
                    return jsonify({"error": "Нет доступа"}), 403
                abort(403)
            return f(*a, **kw)
        return dec
    return decorator


def get_lan_ip():
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("8.8.8.8", 80))
        ip = s.getsockname()[0]
        s.close()
        return ip
    except Exception:
        return "127.0.0.1"


def _save_signature(base64_str, prefix):
    if not base64_str or "," not in base64_str:
        return None
    try:
        header, encoded = base64_str.split(",", 1)
        data = base64.b64decode(encoded)
        fname = f"{prefix}_{uuid.uuid4().hex[:8]}.png"
        fpath = os.path.join(SIGS, fname)
        with open(fpath, "wb") as f:
            f.write(data)
        return "/static/signatures/" + fname
    except Exception as e:
        app.logger.error(f"Save signature error: {e}")
        return None


# ══════════════════════════════════════════════════════════════════════════════════
#  BEFORE / AFTER REQUEST HANDLERS
# ══════════════════════════════════════════════════════════════════════════════════

@bp.before_app_request
def _attach_user():
    """Attach the current user (or None) to every request."""
    request.current_user = get_current_user()


@bp.before_app_request
def enforce_csrf():
    """Validate CSRF token on all authenticated mutating requests."""
    if request.method not in ('POST', 'PUT', 'DELETE', 'PATCH'):
        return None
    if request.path in _CSRF_SKIP_PATHS:
        return None
    if not request.cookies.get('token'):
        return None  # unauthenticated — auth decorator handles it
    header_token = request.headers.get('X-CSRF-Token', '')
    cookie_token = request.cookies.get('csrf_token', '')
    if not header_token or not cookie_token:
        app.logger.warning(f"CSRF token missing: {request.method} {request.path} from {request.remote_addr}")
        return jsonify({"error": "Запрос отклонён: CSRF токен отсутствует"}), 403
    if not _hmac.compare_digest(header_token, cookie_token):
        app.logger.warning(f"CSRF mismatch: {request.method} {request.path} from {request.remote_addr}")
        return jsonify({"error": "Запрос отклонён: недействительный токен безопасности"}), 403


def check_global_rate_limit(ip: str, limit: int = RATE_LIMIT_DEFAULT, window: int = 60) -> bool:
    """True = allowed, False = rate limited."""
    now = time.time()
    key = f"global:{ip}"
    if key not in _RATE_STORE or now > _RATE_STORE[key]["reset"]:
        _RATE_STORE[key] = {"count": 1, "reset": now + window}
        return True
    _RATE_STORE[key]["count"] += 1
    return _RATE_STORE[key]["count"] <= limit


@bp.before_app_request
def global_rate_limit():
    """Apply global rate limit to all API endpoints."""
    if request.path.startswith("/api/") and request.path not in ("/api/auth/login",):
        ip = request.remote_addr or "unknown"
        if not check_global_rate_limit(ip):
            return jsonify({"error": "Слишком много запросов. Подождите минуту."}), 429


@bp.after_app_request
def _attach_csrf_cookie(resp):
    """Attach a stable per-session CSRF token as a readable (non-HttpOnly) cookie."""
    # Only set if not already present
    csrf = request.cookies.get('csrf_token')
    if not csrf:
        token = secrets.token_hex(32)
        resp.set_cookie(
            'csrf_token', token,
            httponly=False,       # must be readable by JavaScript
            samesite='Strict',
            secure=SECURE_COOKIES,
            max_age=JWT_EXPIRY,
            path='/'
        )
    return resp


@bp.after_app_request
def add_security_headers(resp):
    resp.headers['X-Content-Type-Options'] = 'nosniff'
    resp.headers['X-Frame-Options'] = 'SAMEORIGIN'
    resp.headers['Referrer-Policy'] = 'strict-origin-when-cross-origin'
    resp.headers['Permissions-Policy'] = 'geolocation=(), microphone=(), payment=()'
    resp.headers['Content-Security-Policy'] = (
        "default-src 'self'; "
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://unpkg.com https://cdnjs.cloudflare.com; "
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; "
        "font-src 'self' https://fonts.gstatic.com; "
        "img-src 'self' data: blob:; "
        "connect-src 'self' https://cdn.jsdelivr.net; "
        "frame-ancestors 'self';"
    )
    if SECURE_COOKIES:
        resp.headers['Strict-Transport-Security'] = 'max-age=31536000; includeSubDomains; preload'
    if request.path.startswith('/api/'):
        resp.headers['Cache-Control'] = 'no-store, no-cache, must-revalidate, private'
        resp.headers['Pragma'] = 'no-cache'
    resp.headers.pop('Server', None)
    return resp


# ══════════════════════════════════════════════════════════════════════════════════
#  PER-ENDPOINT RATE LIMITER DECORATOR
# ══════════════════════════════════════════════════════════════════════════════════

def rate_limit(max_calls: int, window_sec: int = 60):
    """Decorator: max_calls per window_sec per IP. Thread-safe for single-process."""
    def decorator(f):
        bucket_key = f.__name__
        @wraps(f)
        def dec(*a, **kw):
            ip = request.remote_addr or 'unknown'
            key = f"{bucket_key}:{ip}"
            now = time.time()
            cutoff = now - window_sec
            calls = [t for t in _rate_buckets.get(key, []) if t > cutoff]
            if len(calls) >= max_calls:
                retry_after = int(window_sec - (now - calls[0]))
                return jsonify({"error": f"Слишком много запросов. Повторите через {retry_after} сек."}), 429
            calls.append(now)
            _rate_buckets[key] = calls
            return f(*a, **kw)
        return dec
    return decorator


# ══════════════════════════════════════════════════════════════════════════════════
#  UTILITY
# ══════════════════════════════════════════════════════════════════════════════════

def bhost():
    # Priority: Env Var > X-Forwarded-Host > Request Host
    base = os.environ.get('BASE_URL')
    if base:
        return base.rstrip("/")

    # Check for proxy headers first (important for Docker/Gunicorn)
    forwarded = request.headers.get("X-Forwarded-Host")
    if forwarded:
        proto = request.headers.get("X-Forwarded-Proto", "http")
        return f"{proto}://{forwarded}"

    h = request.host_url.rstrip("/")
    # If on localhost inside container, try to get reachable IP
    if "localhost" in h or "127.0.0.1" in h:
        lan = get_lan_ip()
        return h.replace("localhost", lan).replace("127.0.0.1", lan)
    return h


# ══════════════════════════════════════════════════════════════════════════════════
#  GLOBAL ERROR HANDLERS
# ══════════════════════════════════════════════════════════════════════════════════

@bp.app_errorhandler(404)
def not_found(e):
    if request.is_json or request.path.startswith('/api/'):
        return jsonify({'error': 'Не найдено'}), 404
    u = get_current_user()
    if u:
        return redirect('/dashboard')
    return render_template('login.html'), 404


@bp.app_errorhandler(403)
def forbidden(e):
    if request.is_json or request.path.startswith('/api/'):
        return jsonify({'error': 'Нет доступа'}), 403
    return redirect('/login')


@bp.app_errorhandler(413)
def too_large(e):
    return jsonify({'error': f'Файл слишком большой. Максимум {MAX_UPLOAD_MB}MB'}), 413


@bp.app_errorhandler(500)
def server_error(e):
    app.logger.error(f'500 error: {e}\n{traceback.format_exc()}')
    if request.path.startswith('/api/'):
        return jsonify({'error': 'Внутренняя ошибка сервера'}), 500
    return redirect("/dashboard")


# ══════════════════════════════════════════════════════════════════════════════════
#  AUTH ROUTES
# ══════════════════════════════════════════════════════════════════════════════════

@bp.route("/login")
def login_page():
    if get_current_user():
        return redirect("/dashboard")
    return render_template("login.html")


@bp.route("/api/auth/login", methods=["POST"])
def api_login():
    d = request.json or {}
    ip = request.remote_addr or "unknown"
    ua = request.headers.get("User-Agent", "")[:200]
    # Rate limit check
    allowed, secs = check_rate_limit(ip)
    if not allowed:
        return jsonify({"error": f"Слишком много попыток. Подождите {secs} сек."}), 429
    with get_db() as db:
        u = db.execute("SELECT * FROM users WHERE email=? AND active=1", (d.get("email", "").lower(),)).fetchone()
    ok = u and bcrypt.checkpw(d.get("password", "").encode(), u["password_hash"].encode())
    with get_db() as db:
        db.execute("INSERT INTO login_log (user_id,email,success,ip,user_agent) VALUES (?,?,?,?,?)",
                   (u["id"] if u else None, d.get("email", "").lower(), 1 if ok else 0, ip, ua))
    if not ok:
        record_failed_login(ip)
        return jsonify({"error": "Неверный email или пароль"}), 401
    clear_failed_login(ip)
    # Update last_login timestamp
    with get_db() as db:
        db.execute("UPDATE users SET last_login=CURRENT_TIMESTAMP WHERE id=?", (u["id"],))
    u_dict = dict(u)
    result = {
        "ok": True,
        "role": u["role"],
        "name": u["name"],
        "force_password_change": bool(u_dict.get("force_password_change", 0)),
        "onboarding_done": bool(u_dict.get("onboarding_done", 1)),
    }
    resp = jsonify(result)
    resp.set_cookie(
        "token", make_token(u_dict),
        httponly=True,
        samesite="Lax",
        secure=SECURE_COOKIES,
        max_age=JWT_EXPIRY
    )
    _attach_csrf_cookie(resp)
    return resp


@bp.route("/api/auth/csrf-refresh")
@login_required
def api_csrf_refresh():
    """Re-issue CSRF cookie for sessions that predate CSRF implementation."""
    resp = make_response(jsonify({"ok": True}))
    _attach_csrf_cookie(resp)
    return resp


@bp.route("/api/auth/logout", methods=["POST"])
@login_required
def api_logout():
    u = request.current_user
    with get_db() as db:
        db.execute("UPDATE users SET token_version=COALESCE(token_version,0)+1 WHERE id=?", (u["id"],))
    resp = make_response(jsonify({"ok": True}))
    resp.delete_cookie("token")
    resp.delete_cookie("csrf_token")
    return resp


@bp.route("/api/auth/me")
@login_required
def api_me():
    u = dict(request.current_user)
    u.pop("password_hash", None)
    u["role_info"] = ROLES.get(u["role"], {})
    return jsonify(u)


# ══════════════════════════════════════════════════════════════════════════════════
#  PAGES
# ══════════════════════════════════════════════════════════════════════════════════

@bp.route("/")
def index_redirect():
    if get_current_user():
        return redirect("/dashboard")
    return redirect("/login")


@bp.route("/landing")
def landing_page():
    landing_index = os.path.join(os.path.dirname(__file__), "..", "landing", "index.html")
    if os.path.exists(landing_index):
        return send_file(landing_index)
    return "Landing page not found", 404


# Landing page static assets
@bp.route("/css/<path:filename>")
def serve_landing_css(filename):
    return send_file(os.path.join(os.path.dirname(__file__), "..", "landing", "css", filename))


@bp.route("/js/<path:filename>")
def serve_landing_js(filename):
    return send_file(os.path.join(os.path.dirname(__file__), "..", "landing", "js", filename))


@bp.route("/img/<path:filename>")
def serve_landing_img(filename):
    return send_file(os.path.join(os.path.dirname(__file__), "..", "landing", "img", filename))


@bp.route("/dashboard")
@login_required
def index():
    u = request.current_user
    role = u["role"]
    with get_db() as db:
        all_emps = [r["name"] for r in db.execute("SELECT name FROM users WHERE active=1 ORDER BY name").fetchall()]
        employees = [dict(r) for r in db.execute(
            "SELECT id, name, department FROM users WHERE active=1 AND role='employee' ORDER BY name"
        ).fetchall()]

    if role in ('superadmin', 'deputy', 'director', 'department_head'):
        template = "dash_executive.html"
    elif role == 'hr':
        template = "dash_hr.html"
    elif role == 'accountant':
        template = "dash_finance.html"
    elif role == 'auditor':
        template = "dash_auditor.html"
    elif role == 'aho':
        template = "dash_operations.html"
    elif role == 'viewer':
        template = "dash_auditor.html"   # read-only, same as auditor
    else:
        template = "dash_employee.html"

    return render_template(template, categories=CATEGORIES, conditions=CONDITIONS,
                           statuses=STATUSES, user=u, role_info=ROLES.get(u["role"], {}), roles=ROLES,
                           current_user=u, all_emps=all_emps, employees=employees)


@bp.route("/asset/<inv_num>")
def asset_page(inv_num):
    with get_db() as db:
        item = db.execute("SELECT * FROM items WHERE inv_num=?", (inv_num,)).fetchone()
    if not item:
        abort(404)
    item = dict(item)
    u = get_current_user()
    if u:
        can_edit = ROLES[u["role"]]["can_edit"]
        is_owner = str(item.get("employee_id")) == str(u["id"]) or item.get("employee") == u["name"]
        role = u["role"]
    else:
        can_edit = False
        is_owner = False
        role = "guest"

    # Financial Calculations
    fin = {"residual_value": None, "warranty_status": "unknown", "depreciation_pct": 0}
    if item.get("purchase_price") and item.get("purchase_date"):
        try:
            from datetime import date as _d
            purchased = _d.fromisoformat(item["purchase_date"])
            today = _d.today()
            useful_life = {"Ноутбук": 3, "Монитор": 5, "Кресло": 7, "Стол": 10, "Принтер": 4, "Телефон": 2}.get(item.get("category"), 5)
            years_used = (today - purchased).days / 365.25
            fin["depreciation_pct"] = min(100.0, round((years_used / useful_life) * 100, 1))
            fin["residual_value"] = max(0, round(item["purchase_price"] * (1 - fin["depreciation_pct"] / 100), 2))
        except Exception:
            pass

    if item.get("warranty_until"):
        try:
            from datetime import date as _d
            if _d.fromisoformat(item["warranty_until"]) > _d.today():
                fin["warranty_status"] = "active"
            else:
                fin["warranty_status"] = "expired"
        except Exception:
            pass

    return render_template("asset.html", item=item, user=u, can_edit=can_edit, is_owner=is_owner, role=role, financials=fin)


@bp.route("/employee/<path:name>")
@login_required
def employee_page(name):
    with get_db() as db:
        items = db.execute("SELECT * FROM items WHERE employee=? ORDER BY category", (name,)).fetchall()
    return render_template("employee.html", employee=name, items=[dict(i) for i in items], user=request.current_user)


@bp.route("/api/employee/<path:name>/export")
@login_required
def export_employee_items(name):
    u = request.current_user
    if u["role"] == "employee" and u["name"] != name:
        return jsonify({"error": "Нет доступа"}), 403
    with get_db() as db:
        rows = db.execute("SELECT * FROM items WHERE employee=? ORDER BY category", (name,)).fetchall()
    wb = Workbook()
    ws = wb.active
    ws.title = f"Техника {name}"
    headers = ["Инв. №", "Категория", "Модель", "Серийный №", "Кабинет", "Состояние", "Примечания"]
    ws.append(headers)
    for row in rows:
        ws.append([row["inv_num"], row["category"], row["model"] or "", row["serial_num"] or "—",
                   row["room"], row["condition"], row["notes"] or ""])
    buf = io.BytesIO()
    wb.save(buf)
    buf.seek(0)
    return send_file(buf, as_attachment=True, download_name=f"Items_{name.replace(' ', '_')}.xlsx",
                     mimetype="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")


# ══════════════════════════════════════════════════════════════════════════════════
#  PROFILE
# ══════════════════════════════════════════════════════════════════════════════════

@bp.route("/profile")
@login_required
def profile_page():
    u = request.current_user
    with get_db() as db:
        my_items = db.execute("SELECT COUNT(*) FROM items WHERE employee_id=? OR employee=?", (u["id"], u["name"])).fetchone()[0]
        try:
            my_actions = db.execute("SELECT COUNT(*) FROM history WHERE user_id=?", (u["id"],)).fetchone()[0]
        except DatabaseError:
            my_actions = 0
    return render_template("profile.html", user=u, role_info=ROLES.get(u["role"], {}),
                           my_items=my_items, my_actions=my_actions)


@bp.route("/api/profile", methods=["PUT"])
@login_required
@rate_limit(max_calls=10, window_sec=60)
def update_profile():
    u = request.current_user
    d = request.json or {}
    sets = []
    vals = []
    if d.get("name"):
        sets.append("name=?")
        vals.append(d["name"].strip())
    if d.get("password"):
        pw_new = d["password"]
        # Verify old password (skip if force_password_change)
        if not u.get("force_password_change"):
            old_pw = d.get("old_password", "")
            if not old_pw:
                return jsonify({"error": "Введите текущий пароль"}), 400
            if not bcrypt.checkpw(old_pw.encode(), u["password_hash"].encode()):
                return jsonify({"error": "Неверный текущий пароль"}), 403
        if len(pw_new) < 8:
            return jsonify({"error": "Минимум 8 символов"}), 400
        if not any(c.isdigit() for c in pw_new):
            return jsonify({"error": "Пароль должен содержать цифры"}), 400
        if not any(c.isalpha() for c in pw_new):
            return jsonify({"error": "Пароль должен содержать буквы"}), 400
        sets.append("password_hash=?")
        vals.append(bcrypt.hashpw(pw_new.encode(), bcrypt.gensalt()).decode())
        sets.append("token_version=COALESCE(token_version,0)+1")
        sets.append("force_password_change=0")
    if not sets:
        return jsonify({"error": "Нет данных для обновления"}), 400
    with get_db() as db:
        db.execute(f"UPDATE users SET {','.join(sets)} WHERE id=?", vals + [u["id"]])
    return jsonify({"ok": True})


# ══════════════════════════════════════════════════════════════════════════════════
#  PWA / OFFLINE
# ══════════════════════════════════════════════════════════════════════════════════

@bp.route("/offline")
def offline_page():
    """PWA offline fallback page."""
    return render_template("offline.html")


@bp.route("/sw.js")
def service_worker():
    """Serve Service Worker with correct MIME type and no caching."""
    sw_path = os.path.join(os.path.dirname(__file__), "..", "static", "sw.js")
    resp = make_response(open(sw_path, encoding="utf-8").read())
    resp.headers["Content-Type"] = "application/javascript"
    resp.headers["Service-Worker-Allowed"] = "/"
    resp.headers["Cache-Control"] = "no-cache, no-store, must-revalidate"
    return resp


@bp.route("/api/manifest")
def pwa_manifest():
    return jsonify({
        "name": "ASSETO — Инвентаризация",
        "short_name": "ASSETO",
        "start_url": "/",
        "display": "standalone",
        "background_color": "#000",
        "theme_color": "#007AFF",
        "icons": [
            {"src": "/api/icon/192", "sizes": "192x192", "type": "image/png"},
            {"src": "/api/icon/512", "sizes": "512x512", "type": "image/png"}
        ]
    }), 200, {"Content-Type": "application/manifest+json"}


@bp.route("/api/icon/<int:size>")
def app_icon(size):
    sz = min(max(size, 48), 512)
    img = Image.new("RGB", (sz, sz), "#007AFF")
    draw = ImageDraw.Draw(img)
    p, bh, sw = sz // 6, sz // 8, sz // 8
    draw.rectangle([p, p, sz - p, p + bh], fill="white")
    draw.rectangle([sz // 2 - sw // 2, p, sz // 2 + sw // 2, sz - p], fill="white")
    buf = io.BytesIO()
    img.save(buf, "PNG")
    buf.seek(0)
    return send_file(buf, mimetype="image/png")


# ══════════════════════════════════════════════════════════════════════════════════
#  HEALTH / ONBOARDING
# ══════════════════════════════════════════════════════════════════════════════════

@bp.route("/api/health")
def health():
    """Public health check for monitoring / uptime tools."""
    try:
        with get_db() as db:
            db.execute("SELECT 1").fetchone()
        return jsonify({"status": "ok"})
    except Exception:
        return jsonify({"status": "error"}), 500


@bp.route("/api/onboarding/complete", methods=["POST"])
@login_required
def complete_onboarding():
    u = request.current_user
    with get_db() as db:
        db.execute("UPDATE users SET onboarding_done=1 WHERE id=?", (u["id"],))
    return jsonify({"ok": True})
