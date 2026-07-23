"""Maintenance API Blueprint — repair tickets, asset requests, SLA, and exports."""

import os, io, json
from datetime import date, datetime

from flask import Blueprint, render_template, request, jsonify, send_file

from openpyxl import Workbook

from modules.config import app, ROLES
from modules.db import get_db
from modules.auth import login_required, roles_required
from modules.api_items import log_h

bp = Blueprint('maintenance', __name__)


# ══════════════════════════════════════════════════════════════════════════════
#  HELPERS
# ══════════════════════════════════════════════════════════════════════════════

def send_tg_notification(user_id, message):
    """Helper to send Telegram notifications if chat_id exists."""
    with get_db() as db:
        user = db.execute("SELECT telegram_chat_id FROM users WHERE id=?", (user_id,)).fetchone()
    if user and user["telegram_chat_id"]:
        token = os.environ.get('TELEGRAM_BOT_TOKEN')
        if not token: return
        try:
            import urllib.request as _ur
            url = f"https://api.telegram.org/bot{token}/sendMessage"
            data = {"chat_id": user["telegram_chat_id"], "text": message, "parse_mode": "HTML"}
            req = _ur.Request(url, data=json.dumps(data).encode(), headers={'Content-Type': 'application/json'})
            with _ur.urlopen(req) as resp:
                pass
        except Exception as e:
            print(f"  [!] Telegram error: {e}")


# ══════════════════════════════════════════════════════════════════════════════
#  MAINTENANCE — HTML PAGES
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/maintenance")
@login_required
def maintenance_page():
    u = request.current_user
    return render_template("maintenance.html", user=u, current_user=u,
        role_info=ROLES.get(u["role"],{}), roles=ROLES)


@bp.route("/requests")
@login_required
def requests_page():
    u = request.current_user
    return render_template("requests.html", user=u, current_user=u,
        role_info=ROLES.get(u["role"],{}), roles=ROLES)


# ══════════════════════════════════════════════════════════════════════════════
#  MAINTENANCE — API
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/maintenance", methods=["GET"])
@login_required
def list_maintenance():
    with get_db() as db:
        rows = db.execute(
            """SELECT m.*, i.inv_num, i.category, i.model, i.room
               FROM maintenance m
               LEFT JOIN items i ON m.item_id = i.id
               ORDER BY m.created_at DESC LIMIT 100"""
        ).fetchall()
    return jsonify([dict(r) for r in rows])


@bp.route("/api/maintenance", methods=["POST"])
@login_required
def create_maintenance():
    d = request.json or {}
    u = request.current_user
    item_id = d.get("item_id")
    if not item_id:
        return jsonify({"error": "Укажите актив"}), 400
    description = (d.get("description") or "").strip()[:1000]
    priority    = d.get("priority", "medium")
    if priority not in ("low", "medium", "high", "critical"):
        priority = "medium"
    with get_db() as db:
        item = db.execute("SELECT * FROM items WHERE id=?", (item_id,)).fetchone()
        if not item:
            return jsonify({"error": "Актив не найден"}), 404
        db.execute("UPDATE items SET condition='Требует ремонта' WHERE id=?", (item_id,))
        log_h(db, item_id, "Заявка на ремонт", uid=u["id"], uname=u["name"])
        cur = db.execute(
            "INSERT INTO maintenance (item_id, reported_by_id, reported_by_name, description, priority) VALUES (?,?,?,?,?)",
            (item_id, u["id"], u["name"], description, priority)
        )
    return jsonify({"ok": True, "id": cur.lastrowid})


@bp.route("/api/maintenance/<int:mid>", methods=["PUT"])
@roles_required("superadmin", "aho")
def update_maintenance(mid):
    d  = request.json or {}
    u  = request.current_user
    st = d.get("status")
    if st not in ("in_progress","completed","cancelled"):
        return jsonify({"error": "Некорректный статус"}), 400
    resolution = (d.get("resolution") or d.get("note") or d.get("comment") or "").strip()[:500]
    with get_db() as db:
        m = db.execute("SELECT * FROM maintenance WHERE id=?", (mid,)).fetchone()
        if not m:
            return jsonify({"error": "Не найдено"}), 404
        db.execute(
            "UPDATE maintenance SET status=?, resolved_by=?, resolved_at=CURRENT_TIMESTAMP, resolution=? WHERE id=?",
            (st, u["name"], resolution, mid)
        )
        if st == "completed":
            db.execute("UPDATE items SET condition='Хорошее' WHERE id=?", (m["item_id"],))
            log_h(db, m["item_id"], "Ремонт завершён", uid=u["id"], uname=u["name"])
    return jsonify({"ok": True})


@bp.route("/api/maintenance/<int:rid>/action", methods=["POST"])
@roles_required("superadmin", "aho", "deputy", "accountant")
def maintenance_action(rid):
    """Approve or reject a maintenance request."""
    d = request.json
    action = d.get("action","")
    note   = d.get("note") or d.get("reason") or d.get("comment") or ""
    # normalize: 'approve'/'approved' → 'approve'; 'reject'/'rejected' → 'reject'
    if action in ("approve","approved"): action = "approve"
    elif action in ("reject","rejected"): action = "reject"
    else: return jsonify({"error": "action must be approve or reject"}), 400
    reason = note
    u = request.current_user

    with get_db() as db:
        req = db.execute("SELECT * FROM maintenance WHERE id=?", (rid,)).fetchone()
        if not req: return jsonify({"error": "Заявка не найдена"}), 404

        status = 'resolved' if action == 'approve' else 'rejected'
        db.execute("UPDATE maintenance SET status=?, resolved_by=?, resolved_at=CURRENT_TIMESTAMP, resolution=?, rejection_reason=? WHERE id=?",
                   (status, u["name"], reason if action=='approve' else '', reason if action=='reject' else '', rid))

        # Notify user
        msg = f"<b>🔧 Заявка на ремонт #{rid}</b>\n\nСтатус: {'✅ Одобрена' if action=='approve' else '❌ Отклонена'}\n"
        if reason: msg += f"Комментарий: {reason}"
        send_tg_notification(req["reported_by_id"], msg)

    return jsonify({"ok": True})


@bp.route("/api/maintenance/sla")
@roles_required("superadmin", "aho", "director", "deputy")
def maintenance_sla():
    """Заявки на ремонт с просрочкой SLA (более N дней без ответа)."""
    SLA_DAYS = {"high": 1, "medium": 3, "low": 7}
    with get_db() as db:
        rows = db.execute(
            """SELECT m.*, i.inv_num, i.category, i.model, i.room, i.employee
               FROM maintenance m LEFT JOIN items i ON m.item_id=i.id
               WHERE m.status='pending' ORDER BY m.created_at ASC"""
        ).fetchall()
    today = date.today()
    overdue = []
    for r in rows:
        r = dict(r)
        try:
            created = datetime.fromisoformat(r["created_at"]).date()
        except Exception:
            continue
        sla = SLA_DAYS.get(r.get("priority","medium"), 3)
        days_open = (today - created).days
        r["days_open"] = days_open
        r["sla_days"] = sla
        r["overdue"] = days_open > sla
        overdue.append(r)
    overdue_list = [r for r in overdue if r["overdue"]]
    return jsonify({
        "all_pending": overdue,
        "overdue": overdue_list,
        "overdue_count": len(overdue_list)
    })


# ══════════════════════════════════════════════════════════════════════════════
#  MAINTENANCE — EXPORT
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/export/maintenance")
@roles_required("superadmin", "aho", "accountant")
def export_maintenance():
    with get_db() as db:
        rows = db.execute("""SELECT m.*, i.inv_num, i.category, i.model 
                             FROM maintenance m LEFT JOIN items i ON m.item_id=i.id 
                             ORDER BY m.created_at DESC""").fetchall()
    wb = Workbook(); ws = wb.active; ws.title = "Ремонты"
    ws.append(["ID", "Инв. №", "Категория", "Модель", "Описание проблемы", "Приоритет", "Статус", "Автор", "Кем решено", "Дата", "Результат/Причина"])
    for r in rows:
        ws.append([r["id"], r["inv_num"], r["category"], r["model"], r["description"], r["priority"], r["status"], r["reported_by_name"], r["resolved_by"], r["created_at"], r["resolution"] or r["rejection_reason"]])
    buf = io.BytesIO(); wb.save(buf); buf.seek(0)
    return send_file(buf, as_attachment=True, download_name=f"Maintenance_{date.today()}.xlsx", mimetype="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")


# ══════════════════════════════════════════════════════════════════════════════
#  ASSET REQUESTS — API
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/requests", methods=["GET"])
@login_required
def list_requests():
    u = request.current_user
    with get_db() as db:
        if u["role"] == "employee":
            rows = db.execute(
                "SELECT * FROM asset_requests WHERE employee_id=? ORDER BY created_at DESC LIMIT 50",
                (u["id"],)
            ).fetchall()
        else:
            rows = db.execute(
                "SELECT * FROM asset_requests ORDER BY created_at DESC LIMIT 100"
            ).fetchall()
    return jsonify([dict(r) for r in rows])


@bp.route("/api/requests", methods=["POST"])
@login_required
def create_request():
    d = request.json or {}
    u = request.current_user
    category = d.get("category", "Другое")
    reason   = (d.get("reason") or "").strip()[:500]
    if not category:
        return jsonify({"error": "Укажите категорию"}), 400
    with get_db() as db:
        cur = db.execute(
            "INSERT INTO asset_requests (employee_id, employee_name, category, reason, status) VALUES (?,?,?,?,?)",
            (u["id"], u["name"], category, reason, "pending")
        )
    return jsonify({"ok": True, "id": cur.lastrowid})


@bp.route("/api/requests/<int:rid>", methods=["PUT"])
@roles_required("superadmin", "aho", "hr")
def update_request(rid):
    d  = request.json or {}
    u  = request.current_user
    st = d.get("status")  # approved / rejected / completed
    if st not in ("approved", "rejected", "completed"):
        return jsonify({"error": "Некорректный статус"}), 400
    with get_db() as db:
        db.execute(
            "UPDATE asset_requests SET status=?, resolved_by=?, resolved_at=CURRENT_TIMESTAMP WHERE id=?",
            (st, u["name"], rid)
        )
    return jsonify({"ok": True})


@bp.route("/api/requests/<int:rid>/action", methods=["POST"])
@roles_required("superadmin", "aho", "deputy", "accountant")
def request_action(rid):
    """Approve or reject an asset purchase request."""
    d = request.json
    action = d.get("action")
    reason = d.get("reason", "")
    u = request.current_user

    with get_db() as db:
        req = db.execute("SELECT * FROM asset_requests WHERE id=?", (rid,)).fetchone()
        if not req: return jsonify({"error": "Заявка не найдена"}), 404

        status = 'approved' if action == 'approve' else 'rejected'
        db.execute("UPDATE asset_requests SET status=?, resolved_by=?, resolved_at=CURRENT_TIMESTAMP, rejection_reason=? WHERE id=?",
                   (status, u["name"], reason, rid))

        # Notify user
        msg = f"<b>📦 Заявка на приобретение #{rid} ({req['category']})</b>\n\nСтатус: {'✅ Одобрена' if action=='approve' else '❌ Отклонена'}\n"
        if reason: msg += f"Комментарий: {reason}"
        send_tg_notification(req["employee_id"], msg)

    return jsonify({"ok": True})


# ══════════════════════════════════════════════════════════════════════════════
#  ASSET REQUESTS — EXPORT
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/export/requests")
@roles_required("superadmin", "aho", "accountant", "deputy")
def export_requests():
    with get_db() as db:
        rows = db.execute("SELECT * FROM asset_requests ORDER BY created_at DESC").fetchall()
    wb = Workbook(); ws = wb.active; ws.title = "Заявки на закуп"
    ws.append(["ID", "Сотрудник", "Категория", "Причина", "Статус", "Кем решено", "Дата", "Причина отказа"])
    for r in rows:
        ws.append([r["id"], r["employee_name"], r["category"], r["reason"], r["status"], r["resolved_by"], r["created_at"], r["rejection_reason"]])
    buf = io.BytesIO(); wb.save(buf); buf.seek(0)
    return send_file(buf, as_attachment=True, download_name=f"Purchase_Requests_{date.today()}.xlsx", mimetype="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
