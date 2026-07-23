"""Documents API Blueprint — document workflow, equipment templates, and onboarding."""

import os, io, json, uuid, threading, smtplib, ssl
from datetime import date, datetime
from email.mime.multipart import MIMEMultipart
from email.mime.text import MIMEText

from flask import Blueprint, render_template, request, jsonify

from modules.config import app, ROLES, UPLOADS, SIGS, CONDITIONS
from modules.db import get_db
from modules.auth import (login_required, roles_required, bhost,
                          get_current_user, _save_signature)
from modules.api_items import log_h, next_inv

bp = Blueprint('docs', __name__)


# ══════════════════════════════════════════════════════════════════════════════
#  CONSTANTS
# ══════════════════════════════════════════════════════════════════════════════

APPROVAL_CHAIN = {
    "doc_request": [
        {"step": 1, "role": "aho",        "label": "АХО / IT"},
        {"step": 2, "role": "deputy",     "label": "Зам. Директора"},
        {"step": 3, "role": "director",   "label": "Ген. Директор"},
        {"step": 4, "role": "accountant", "label": "Бухгалтер"},
    ],
    "write_off": [
        {"step": 1, "role": "aho",        "label": "АХО / IT"},
        {"step": 2, "role": "director",   "label": "Ген. Директор"},
        {"step": 3, "role": "accountant", "label": "Бухгалтер"},
    ],
    "repair": [
        {"step": 1, "role": "aho",        "label": "АХО / IT"},
        {"step": 2, "role": "director",   "label": "Ген. Директор"},
    ],
    "transfer": [
        {"step": 1, "role": "aho",        "label": "АХО / IT"},
        {"step": 2, "role": "deputy",     "label": "Зам. Директора"},
    ],
}

DOC_TYPES = {
    "doc_request": "Заявка на технику",
    "write_off":   "Списание техники",
    "repair":      "Заявка на ремонт",
    "transfer":    "Передача техники",
}


# ══════════════════════════════════════════════════════════════════════════════
#  HELPERS
# ══════════════════════════════════════════════════════════════════════════════

def _gen_doc_number(db, doc_type):
    """Сгенерировать номер документа: ЗАЯ-2025-0001"""
    prefixes = {"doc_request":"ЗАЯ","write_off":"СПС","repair":"РЕМ","transfer":"ПЕР"}
    pref = prefixes.get(doc_type, "ДОК")
    year = date.today().year
    cnt = db.execute(
        "SELECT COUNT(*) FROM documents WHERE doc_type=? AND strftime('%Y',created_at)=?",
        (doc_type, str(year))
    ).fetchone()[0] + 1
    return f"{pref}-{year}-{cnt:04d}"


def _next_doc_step(doc_type, current_step):
    """Вернуть следующий шаг и роль согласования."""
    chain = APPROVAL_CHAIN.get(doc_type, [])
    nxt = [s for s in chain if s["step"] > current_step]
    return nxt[0] if nxt else None


def _doc_status_label(status):
    return {
        "draft":    "Черновик",
        "pending":  "На согласовании",
        "approved": "Утверждено",
        "rejected": "Отклонено",
        "printed":  "Распечатано",
        "closed":   "Закрыто",
    }.get(status, status)


# ─── NOTIFICATION HELPERS ──────────────────────────────────────────────────

def send_telegram(chat_id, text):
    """Отправить уведомление в Telegram. chat_id — числовой ID чата."""
    import urllib.request as _ur
    tok = os.environ.get("TELEGRAM_BOT_TOKEN", "")
    if not tok or not chat_id:
        return False
    try:
        payload = json.dumps({"chat_id": chat_id, "text": text, "parse_mode": "HTML"}).encode()
        req = _ur.Request(
            f"https://api.telegram.org/bot{tok}/sendMessage",
            data=payload, headers={"Content-Type": "application/json"}, method="POST"
        )
        _ur.urlopen(req, timeout=4)
        return True
    except Exception as e:
        app.logger.warning(f"Telegram send failed: {e}")
        return False


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
            # Web Push (fastest, works when browser is closed)
            send_web_push(user_id, clean_title, clean_body, url=action_url or "/dashboard")
            # Telegram
            if u["telegram_chat_id"]:
                send_telegram(u["telegram_chat_id"], text)
            # Email fallback
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
#  DOCUMENT WORKFLOW — HTML PAGES
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/documents")
@login_required
def documents_page():
    u = request.current_user
    with get_db() as db:
        # Count pending for badge
        if u["role"] in ("superadmin","aho","deputy","director","accountant"):
            pending = db.execute(
                "SELECT COUNT(*) FROM documents WHERE status='pending' AND pending_role=?",
                (u["role"],)
            ).fetchone()[0]
        else:
            pending = db.execute(
                "SELECT COUNT(*) FROM documents WHERE created_by_id=? AND status NOT IN ('closed','rejected')",
                (u["id"],)
            ).fetchone()[0]
    return render_template("documents.html",
        user=u, current_user=u,
        role_info=ROLES.get(u["role"],{}),
        doc_types=DOC_TYPES,
        approval_chain=APPROVAL_CHAIN,
        pending_count=pending,
        roles=ROLES
    )


# ══════════════════════════════════════════════════════════════════════════════
#  DOCUMENT WORKFLOW — API
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/documents", methods=["POST"])
@login_required
def create_document():
    u   = request.current_user
    d   = request.json or {}
    doc_type = d.get("doc_type")
    if doc_type not in APPROVAL_CHAIN:
        return jsonify({"error": "Неизвестный тип документа"}), 400
    title       = (d.get("title") or "").strip()
    description = (d.get("description") or "").strip()
    priority    = d.get("priority", "normal")
    if priority not in ("low","normal","high","urgent"):
        priority = "normal"
    if not title:
        return jsonify({"error": "Укажите название документа"}), 400
    chain = APPROVAL_CHAIN[doc_type]
    first_step = chain[0]
    with get_db() as db:
        doc_num = _gen_doc_number(db, doc_type)
        cur = db.execute(
            """INSERT INTO documents
               (doc_number,doc_type,title,description,priority,status,
                current_step,pending_role,created_by_id,created_by_name,
                item_id,item_inv,department,amount,deadline,employee_id,employee_name)
               VALUES (?,?,?,?,?,'pending',?,?,?,?,?,?,?,?,?,?,?)""",
            (doc_num, doc_type, title, description, priority,
             first_step["step"], first_step["role"],
             u["id"], u["name"],
             d.get("item_id"), d.get("item_inv"),
             d.get("department", u.get("department","")),
             d.get("amount"), d.get("deadline"),
             d.get("employee_id"), d.get("employee_name"))
        )
        doc_id = cur.lastrowid
        # Создать записи согласования для всех шагов
        for step in chain:
            db.execute(
                """INSERT INTO doc_approvals (doc_id,step,role,role_label)
                   VALUES (?,?,?,?)""",
                (doc_id, step["step"], step["role"], step["label"])
            )
        # Автокомментарий
        db.execute(
            "INSERT INTO doc_comments (doc_id,user_id,user_name,user_role,text) VALUES (?,?,?,?,?)",
            (doc_id, u["id"], u["name"], u["role"], f"Документ создан. Ожидает согласования: {first_step['label']}")
        )
        # Уведомление первого согласующего
        msg = f"📄 <b>Новый документ</b>\n{title}\nОжидает вашего согласования."
        _doc_url = f"{bhost()}/documents?id={doc_id}"
        notify_role(first_step["role"], msg, subject=f"Новый документ: {title}", action_url=_doc_url)
    return jsonify({"ok": True, "doc_id": doc_id, "doc_number": doc_num})


@bp.route("/api/documents")
@login_required
def list_documents():
    u    = request.current_user
    role = u["role"]
    status_f = request.args.get("status","")
    type_f   = request.args.get("doc_type","")
    with get_db() as db:
        # Расширенный список ролей, которые видят всё
        ADMIN_ROLES = ("superadmin","aho","deputy","director","accountant","auditor","viewer")

        if role in ADMIN_ROLES:
            where = "1=1"
            params = []
        else:
            # Обычный сотрудник видит свои документы и те, где он является объектом
            where = "(created_by_id=? OR employee_id=?)"
            params = [u["id"], u["id"]]
        if status_f:
            where += " AND status=?"; params.append(status_f)
        if type_f:
            where += " AND doc_type=?"; params.append(type_f)
        docs = db.execute(
            f"""SELECT d.*,
                (SELECT COUNT(*) FROM doc_comments WHERE doc_id=d.id) as comment_count,
                (SELECT COUNT(*) FROM doc_approvals WHERE doc_id=d.id AND action='approved') as approved_steps,
                (SELECT COUNT(*) FROM doc_approvals WHERE doc_id=d.id) as total_steps
            FROM documents d WHERE {where} ORDER BY d.created_at DESC LIMIT 100""",
            params
        ).fetchall()
    return jsonify([dict(r) for r in docs])


@bp.route("/api/documents/<int:did>")
@login_required
def get_document(did):
    u = request.current_user
    with get_db() as db:
        doc = db.execute("SELECT * FROM documents WHERE id=?", (did,)).fetchone()
        if not doc:
            return jsonify({"error": "Не найдено"}), 404
        # Проверка доступа
        if u["role"] not in ("superadmin",) and \
           doc["created_by_id"] != u["id"] and \
           doc["employee_id"] != u["id"] and \
           u["role"] not in ("aho","deputy","director","accountant"):
            return jsonify({"error": "Нет доступа"}), 403
        approvals = db.execute(
            "SELECT * FROM doc_approvals WHERE doc_id=? ORDER BY step",
            (did,)
        ).fetchall()
        comments = db.execute(
            "SELECT * FROM doc_comments WHERE doc_id=? ORDER BY created_at",
            (did,)
        ).fetchall()
    return jsonify({
        "doc": dict(doc),
        "approvals": [dict(a) for a in approvals],
        "comments": [dict(c) for c in comments],
        "chain": APPROVAL_CHAIN.get(doc["doc_type"], []),
    })


@bp.route("/api/documents/<int:did>/approve", methods=["POST"])
@login_required
def approve_document(did):
    u      = request.current_user
    d      = request.json or {}
    action  = d.get("action")  # "approved" | "rejected"
    comment = (d.get("comment") or "").strip()[:1000]
    if action not in ("approved", "rejected"):
        return jsonify({"error": "Неверное действие"}), 400
    with get_db() as db:
        doc = db.execute("SELECT * FROM documents WHERE id=?", (did,)).fetchone()
        if not doc:
            return jsonify({"error": "Документ не найден"}), 404
        if doc["status"] != "pending":
            return jsonify({"error": "Документ уже обработан"}), 400
        # Superadmin НЕ должен подписывать за другие роли
        if doc["pending_role"] != u["role"]:
            return jsonify({"error": f"Это действие только для роли: {doc['pending_role']}"}), 403
        # Если это Superadmin, но роль совпадает - ок. Но обычно Superadmin не в цепочке.
        # Обновить текущий шаг согласования
        sig_path = None
        if action == "approved" and d.get("signature"):
            sig_path = _save_signature(d["signature"], f"doc_{did}_step_{doc['current_step']}")

        db.execute(
            """UPDATE doc_approvals SET action=?,approver_id=?,approver_name=?,
               comment=?,signature=?,acted_at=CURRENT_TIMESTAMP
               WHERE doc_id=? AND step=?""",
            (action, u["id"], u["name"], comment, sig_path, did, doc["current_step"])
        )
        # Добавить комментарий
        action_label = "✅ Согласовал" if action=="approved" else "❌ Отклонил"
        auto_comment = f"{action_label}: {u['name']} ({ROLES.get(u['role'],{}).get('label',u['role'])})"
        if comment:
            auto_comment += f"\nКомментарий: {comment}"
        db.execute(
            "INSERT INTO doc_comments (doc_id,user_id,user_name,user_role,text) VALUES (?,?,?,?,?)",
            (did, u["id"], u["name"], u["role"], auto_comment)
        )
        if action == "rejected":
            db.execute("UPDATE documents SET status='rejected',updated_at=CURRENT_TIMESTAMP WHERE id=?", (did,))
            new_status = "rejected"
        else:
            # Найти следующий шаг
            nxt = _next_doc_step(doc["doc_type"], doc["current_step"])
            if nxt:
                # Следующий согласующий
                db.execute(
                    "UPDATE documents SET current_step=?,pending_role=?,updated_at=CURRENT_TIMESTAMP WHERE id=?",
                    (nxt["step"], nxt["role"], did)
                )
                new_status = "pending"
                db.execute(
                    "INSERT INTO doc_comments (doc_id,user_id,user_name,user_role,text) VALUES (?,?,?,?,?)",
                    (did, u["id"], u["name"], u["role"], f"Передано на согласование: {nxt['label']}")
                )
                _durl = f"{bhost()}/documents?id={doc['id']}"
                notify_role(nxt["role"], f"📄 <b>Документ на подпись</b>\n{doc['title']}\nПередано вам на этап: {nxt['label']}",
                            subject=f"Документ ожидает подписи: {doc['title']}", action_url=_durl)
            else:
                # Все шаги пройдены — документ утверждён
                db.execute(
                    "UPDATE documents SET status='approved',pending_role=NULL,updated_at=CURRENT_TIMESTAMP WHERE id=?",
                    (did,)
                )
                new_status = "approved"
                db.execute(
                    "INSERT INTO doc_comments (doc_id,user_id,user_name,user_role,text) VALUES (?,?,?,?,?)",
                    (did, u["id"], u["name"], u["role"], "🎉 Документ полностью согласован и утверждён!")
                )
                _durl = f"{bhost()}/documents?id={doc['id']}"
                notify_user(doc["created_by_id"], f"✅ <b>Документ утверждён!</b>\n{doc['title']}\nВсе этапы согласования пройдены.",
                            subject=f"Утверждено: {doc['title']}", action_url=_durl)
        if action == "rejected":
            _durl = f"{bhost()}/documents?id={doc['id']}"
            notify_user(doc["created_by_id"], f"❌ <b>Документ отклонен</b>\n{doc['title']}\nПричина: {comment or 'не указана'}",
                        subject=f"Отклонено: {doc['title']}", action_url=_durl)
    return jsonify({"ok": True, "new_status": new_status})


@bp.route("/api/documents/<int:did>/comments", methods=["POST"])
@login_required
def add_doc_comment(did):
    u    = request.current_user
    text = (request.json or {}).get("text","").strip()
    if not text or len(text) > 2000:
        return jsonify({"error": "Некорректный комментарий"}), 400
    with get_db() as db:
        doc = db.execute("SELECT id FROM documents WHERE id=?", (did,)).fetchone()
        if not doc:
            return jsonify({"error": "Не найдено"}), 404
        db.execute(
            "INSERT INTO doc_comments (doc_id,user_id,user_name,user_role,text) VALUES (?,?,?,?,?)",
            (did, u["id"], u["name"], u["role"], text)
        )
    return jsonify({"ok": True})


@bp.route("/api/documents/<int:did>/print", methods=["POST"])
@login_required
def mark_printed(did):
    u = request.current_user
    if u["role"] not in ("accountant","superadmin"):
        return jsonify({"error": "Только бухгалтер"}), 403
    with get_db() as db:
        doc = db.execute("SELECT * FROM documents WHERE id=?", (did,)).fetchone()
        if not doc:
            return jsonify({"error": "Не найдено"}), 404
        if doc["status"] != "approved":
            return jsonify({"error": "Документ ещё не утверждён"}), 400
        db.execute(
            "UPDATE documents SET status='printed',closed_at=CURRENT_TIMESTAMP,updated_at=CURRENT_TIMESTAMP WHERE id=?",
            (did,)
        )
        db.execute(
            "INSERT INTO doc_comments (doc_id,user_id,user_name,user_role,text) VALUES (?,?,?,?,?)",
            (did, u["id"], u["name"], u["role"], f"🖨️ Документ распечатан и закрыт: {u['name']}")
        )
    return jsonify({"ok": True})


@bp.route("/api/documents/stats")
@login_required
def doc_stats():
    u = request.current_user
    with get_db() as db:
        total    = db.execute("SELECT COUNT(*) FROM documents").fetchone()[0]
        pending  = db.execute("SELECT COUNT(*) FROM documents WHERE status='pending'").fetchone()[0]
        approved = db.execute("SELECT COUNT(*) FROM documents WHERE status='approved'").fetchone()[0]
        rejected = db.execute("SELECT COUNT(*) FROM documents WHERE status='rejected'").fetchone()[0]
        printed  = db.execute("SELECT COUNT(*) FROM documents WHERE status='printed'").fetchone()[0]
        my_pending = db.execute(
            "SELECT COUNT(*) FROM documents WHERE status='pending' AND pending_role=?",
            (u["role"],)
        ).fetchone()[0]
        # Типы
        by_type = db.execute(
            "SELECT doc_type,COUNT(*) as cnt FROM documents GROUP BY doc_type"
        ).fetchall()
    return jsonify({
        "total": total, "pending": pending, "approved": approved,
        "rejected": rejected, "printed": printed,
        "my_pending": my_pending,
        "by_type": [dict(r) for r in by_type]
    })


# ══════════════════════════════════════════════════════════════════════════════
#  EQUIPMENT TEMPLATES
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/equipment-templates", methods=["GET"])
@roles_required("superadmin", "aho", "hr", "director")
def list_eq_templates():
    with get_db() as db:
        rows = db.execute("SELECT * FROM equipment_templates ORDER BY name").fetchall()
    return jsonify([dict(r) for r in rows])


@bp.route("/api/equipment-templates", methods=["POST"])
@roles_required("superadmin", "aho")
def create_eq_template():
    u = request.current_user
    d = request.json or {}
    name = (d.get("name") or "").strip()
    if not name:
        return jsonify({"error": "Укажите название шаблона"}), 400
    items = d.get("items", [])  # [{"category":"Ноутбук","model":"..."}, ...]
    with get_db() as db:
        cur = db.execute(
            "INSERT INTO equipment_templates (name,description,items_json,created_by) VALUES (?,?,?,?)",
            (name, d.get("description",""), json.dumps(items, ensure_ascii=False), u["name"])
        )
    return jsonify({"ok": True, "id": cur.lastrowid})


@bp.route("/api/equipment-templates", methods=["GET", "POST"])
@login_required
def equipment_templates():
    """Шаблоны наборов оборудования для HR."""
    u = request.current_user
    if request.method == "POST":
        if u["role"] not in ("superadmin", "aho", "hr", "director"):
            return jsonify({"error": "Нет прав"}), 403
        d = request.json or {}
        name = (d.get("name") or "").strip()
        items = d.get("items", [])
        if not name:
            return jsonify({"error": "Укажите название шаблона"}), 400
        with get_db() as db:
            db.execute(
                "INSERT INTO equipment_templates (name,description,items_json,created_by) VALUES (?,?,?,?)",
                (name, d.get("description",""), json.dumps(items), u["name"])
            )
        return jsonify({"ok": True})
    with get_db() as db:
        rows = db.execute("SELECT * FROM equipment_templates ORDER BY name").fetchall()
    return jsonify([dict(r) for r in rows])


@bp.route("/api/equipment-templates/<int:tid>/apply", methods=["POST"])
@roles_required("superadmin", "aho", "hr")
def apply_eq_template(tid):
    """Применить шаблон к новому сотруднику — создать все активы."""
    u = request.current_user
    d = request.json or {}
    employee_name = (d.get("employee_name") or "").strip()
    employee_id   = d.get("employee_id")
    room          = (d.get("room") or "").strip()
    place         = (d.get("place") or "").strip()
    if not employee_name:
        return jsonify({"error": "Укажите сотрудника"}), 400
    with get_db() as db:
        tmpl = db.execute("SELECT * FROM equipment_templates WHERE id=?", (tid,)).fetchone()
        if not tmpl:
            return jsonify({"error": "Шаблон не найден"}), 404
        items_def = json.loads(tmpl["items_json"] or "[]")
        created_ids = []
        for item_def in items_def:
            cat   = item_def.get("category", "Другое")
            model = item_def.get("model", "")
            inv   = next_inv(cat)
            cur = db.execute(
                """INSERT INTO items (inv_num,category,model,room,place,employee,employee_id,
                   status,condition,check_date,notes)
                   VALUES (?,?,?,?,?,?,?,'Занято','Хорошее',?,?)""",
                (inv, cat, model, room, place, employee_name, employee_id,
                 date.today().isoformat(), f"Шаблон: {tmpl['name']}")
            )
            log_h(db, cur.lastrowid, f"Создан по шаблону: {tmpl['name']}", uid=u["id"], uname=u["name"])
            created_ids.append({"id": cur.lastrowid, "inv_num": inv, "category": cat})
    return jsonify({"ok": True, "created": created_ids, "count": len(created_ids)})


@bp.route("/api/equipment-templates/<int:tid>", methods=["DELETE"])
@roles_required("superadmin", "aho", "hr")
def delete_equipment_template(tid):
    with get_db() as db:
        db.execute("DELETE FROM equipment_templates WHERE id=?", (tid,))
    return jsonify({"ok": True})


# ══════════════════════════════════════════════════════════════════════════════
#  ONBOARDING
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/onboarding/free-items")
@roles_required("superadmin","aho","hr")
def free_items_for_onboarding():
    room = request.args.get("room","")
    q    = "SELECT * FROM items WHERE status='Свободно'"
    p    = []
    if room: q += " AND room=?"; p.append(room)
    q += " ORDER BY category,room,inv_num"
    with get_db() as db:
        items = db.execute(q,p).fetchall()
    grouped = {}
    for item in items:
        cat = item["category"]
        if cat not in grouped: grouped[cat] = []
        grouped[cat].append(dict(item))
    return jsonify(grouped)


@bp.route("/api/documents/<int:did>/issue-onboarding", methods=["POST"])
@roles_required("superadmin", "aho")
def issue_onboarding_equipment(did):
    """АХО создаёт выдачу оборудования из утверждённого онбординг-документа."""
    u = request.current_user
    with get_db() as db:
        doc = db.execute("SELECT * FROM documents WHERE id=?", (did,)).fetchone()
        if not doc:
            return jsonify({"error": "Документ не найден"}), 404
        doc = dict(doc)
        if doc["doc_type"] != "onboarding":
            return jsonify({"error": "Документ не является онбордингом"}), 400
        if doc["status"] != "approved":
            return jsonify({"error": "Документ ещё не утверждён"}), 400
        # Check not already issued
        if db.execute("SELECT id FROM issuances WHERE doc_id=?", (did,)).fetchone():
            return jsonify({"error": "Оборудование уже выдано по этому документу"}), 400
        # Parse equipment items from doc attachments/description
        items_raw = doc.get("attachments") or "[]"
        try:
            req_items = json.loads(items_raw)
        except Exception:
            req_items = []
        emp_id = doc.get("employee_id")
        emp_name = doc.get("employee_name") or "—"
        # Create issuance record
        cur = db.execute(
            """INSERT INTO issuances
               (employee_id,employee_name,issued_by,issued_by_name,items_json,status,doc_id)
               VALUES (?,?,?,?,?,'pending',?)""",
            (emp_id, emp_name, u["id"], u["name"], json.dumps(req_items), did)
        )
        issuance_id = cur.lastrowid
        db.execute("UPDATE documents SET status='printed', closed_at=CURRENT_TIMESTAMP WHERE id=?", (did,))
        db.execute(
            "INSERT INTO doc_comments (doc_id,user_id,user_name,user_role,text) VALUES (?,?,?,?,?)",
            (did, u["id"], u["name"], u["role"],
             f"📦 Выдача оборудования создана (ID: {issuance_id}). АХО: {u['name']}")
        )
        if emp_id:
            notify_user(emp_id, f"📦 <b>Оборудование подготовлено к выдаче!</b>\nОбратитесь в АХО для получения техники.",
                        subject="Ваше оборудование готово к выдаче")
    return jsonify({"ok": True, "issuance_id": issuance_id})
