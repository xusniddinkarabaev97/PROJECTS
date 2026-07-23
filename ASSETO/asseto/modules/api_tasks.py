"""
Tasks & Projects Blueprint — task management with roles, checklists, comments, time tracking.
"""
from flask import Blueprint, render_template, request, jsonify, abort
from modules.auth import login_required, roles_required
from modules.db import get_db
from modules.config import ROLES
import time

bp = Blueprint('tasks', __name__)

# ── Role constants ──
TASK_ROLE_CREATOR     = 'creator'
TASK_ROLE_RESPONSIBLE = 'responsible'
TASK_ROLE_PARTICIPANT = 'participant'
TASK_ROLE_OBSERVER    = 'observer'

TASK_STATUSES = ['new', 'in_progress', 'done', 'on_hold', 'rejected']
TASK_PRIORITIES = ['low', 'medium', 'high', 'critical']

PRIORITY_LABELS = {'low': 'Низкий', 'medium': 'Средний', 'high': 'Высокий', 'critical': 'Критичный'}
PRIORITY_COLORS = {'low': '#8E8E93', 'medium': '#007AFF', 'high': '#FF9500', 'critical': '#FF3B30'}
STATUS_LABELS  = {'new': 'Новая', 'in_progress': 'В работе', 'done': 'Выполнена', 'on_hold': 'Отложена', 'rejected': 'Отклонена'}


# ══════════════════════════════════════════════════════════════════════════════
#  PAGES
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/tasks")
@login_required
def tasks_page():
    u = request.current_user
    return render_template("tasks.html", user=u, current_user=u,
                           role_info=ROLES.get(u["role"], {}), roles=ROLES,
                           statuses=TASK_STATUSES, priorities=TASK_PRIORITIES,
                           priority_labels=PRIORITY_LABELS, priority_colors=PRIORITY_COLORS,
                           status_labels=STATUS_LABELS)


# ══════════════════════════════════════════════════════════════════════════════
#  TASKS CRUD API
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/tasks")
@login_required
def task_list():
    u = request.current_user
    status = request.args.get('status', '')
    priority = request.args.get('priority', '')
    my = request.args.get('my', '')  # '1' = only my tasks

    with get_db() as db:
        sql = """
            SELECT t.*,
                   creator.name as creator_name,
                   resp.name as responsible_name,
                   (SELECT COUNT(*) FROM task_checklist_items WHERE task_id=t.id) as checklist_total,
                   (SELECT COUNT(*) FROM task_checklist_items WHERE task_id=t.id AND done=1) as checklist_done,
                   (SELECT SUM(minutes) FROM task_time_log WHERE task_id=t.id) as total_minutes
            FROM tasks t
            LEFT JOIN users creator ON t.creator_id = creator.id
            LEFT JOIN users resp ON t.responsible_id = resp.id
            WHERE 1=1
        """
        params = []

        if status:
            sql += " AND t.status = ?"
            params.append(status)
        if priority:
            sql += " AND t.priority = ?"
            params.append(priority)
        if my == '1':
            sql += " AND (t.creator_id = ? OR t.responsible_id = ? OR t.id IN (SELECT task_id FROM task_participants WHERE user_id = ?))"
            params.extend([u["id"], u["id"], u["id"]])

        sql += " ORDER BY t.priority_order DESC, t.deadline ASC, t.created_at DESC"
        rows = db.execute(sql, params).fetchall()

        tasks = []
        for row in rows:
            t = dict(row)
            # Get checklists
            t["checklist"] = [dict(c) for c in db.execute(
                "SELECT * FROM task_checklist_items WHERE task_id=? ORDER BY sort_order", (t["id"],)
            ).fetchall()]
            tasks.append(t)

    return jsonify(tasks)


@bp.route("/api/tasks", methods=["POST"])
@login_required
def task_create():
    u = request.current_user
    d = request.json or {}

    title = (d.get("title") or "").strip()
    if not title:
        return jsonify({"error": "Название обязательно"}), 400

    with get_db() as db:
        cur = db.execute("""
            INSERT INTO tasks (title, description, status, priority, priority_order,
                             deadline, planned_hours, creator_id, responsible_id, project_id)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            title[:255],
            d.get("description", ""),
            d.get("status", "new"),
            d.get("priority", "medium"),
            TASK_PRIORITIES.index(d.get("priority", "medium")),
            d.get("deadline"),
            d.get("planned_hours"),
            u["id"],
            d.get("responsible_id") or u["id"],
            d.get("project_id")
        ))
        task_id = cur.lastrowid

        # Add creator as participant
        db.execute("INSERT INTO task_participants (task_id, user_id, role) VALUES (?, ?, ?)",
                   (task_id, u["id"], TASK_ROLE_CREATOR))

        # Add responsible as participant
        resp_id = d.get("responsible_id")
        if resp_id and resp_id != u["id"]:
            db.execute("INSERT INTO task_participants (task_id, user_id, role) VALUES (?, ?, ?)",
                       (task_id, resp_id, TASK_ROLE_RESPONSIBLE))
        elif not resp_id:
            db.execute("INSERT INTO task_participants (task_id, user_id, role) VALUES (?, ?, ?)",
                       (task_id, u["id"], TASK_ROLE_RESPONSIBLE))

    return jsonify({"ok": True, "id": task_id})


@bp.route("/api/tasks/<int:tid>")
@login_required
def task_get(tid):
    with get_db() as db:
        row = db.execute("""
            SELECT t.*, creator.name as creator_name, resp.name as responsible_name
            FROM tasks t
            LEFT JOIN users creator ON t.creator_id = creator.id
            LEFT JOIN users resp ON t.responsible_id = resp.id
            WHERE t.id=?
        """, (tid,)).fetchone()
        if not row:
            abort(404)

        t = dict(row)
        t["checklist"] = [dict(c) for c in db.execute(
            "SELECT * FROM task_checklist_items WHERE task_id=? ORDER BY sort_order", (tid,)
        ).fetchall()]
        t["comments"] = [dict(c) for c in db.execute("""
            SELECT tc.*, u.name as author_name
            FROM task_comments tc LEFT JOIN users u ON tc.user_id=u.id
            WHERE tc.task_id=? ORDER BY tc.created_at ASC
        """, (tid,)).fetchall()]
        t["participants"] = [dict(p) for p in db.execute("""
            SELECT tp.*, u.name, u.email, u.avatar_color
            FROM task_participants tp LEFT JOIN users u ON tp.user_id=u.id
            WHERE tp.task_id=?
        """, (tid,)).fetchall()]
        t["time_logs"] = [dict(l) for l in db.execute(
            "SELECT * FROM task_time_log WHERE task_id=? ORDER BY created_at DESC", (tid,)
        ).fetchall()]

    return jsonify(t)


@bp.route("/api/tasks/<int:tid>", methods=["PUT"])
@login_required
def task_update(tid):
    u = request.current_user
    d = request.json or {}

    sets = []
    vals = []
    for field in ['title', 'description', 'status', 'priority', 'deadline', 'planned_hours', 'responsible_id']:
        if field in d:
            val = d[field]
            if field == 'title':
                val = (val or '').strip()[:255]
            if field == 'priority':
                sets.append('priority_order=?')
                vals.append(TASK_PRIORITIES.index(val) if val in TASK_PRIORITIES else 1)
            sets.append(f'{field}=?')
            vals.append(val)

    if sets:
        vals.append(tid)
        with get_db() as db:
            db.execute(f"UPDATE tasks SET {', '.join(sets)} WHERE id=?", vals)

    return jsonify({"ok": True})


@bp.route("/api/tasks/<int:tid>", methods=["DELETE"])
@login_required
def task_delete(tid):
    with get_db() as db:
        db.execute("DELETE FROM task_checklist_items WHERE task_id=?", (tid,))
        db.execute("DELETE FROM task_comments WHERE task_id=?", (tid,))
        db.execute("DELETE FROM task_participants WHERE task_id=?", (tid,))
        db.execute("DELETE FROM task_time_log WHERE task_id=?", (tid,))
        db.execute("DELETE FROM tasks WHERE id=?", (tid,))
    return jsonify({"ok": True})


# ══════════════════════════════════════════════════════════════════════════════
#  CHECKLIST API
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/tasks/<int:tid>/checklist", methods=["POST"])
@login_required
def checklist_add(tid):
    d = request.json or {}
    text = (d.get("text") or "").strip()
    if not text:
        return jsonify({"error": "Текст пункта обязателен"}), 400

    with get_db() as db:
        max_order = db.execute("SELECT MAX(sort_order) FROM task_checklist_items WHERE task_id=?", (tid,)).fetchone()[0] or 0
        cur = db.execute("INSERT INTO task_checklist_items (task_id, text, sort_order) VALUES (?, ?, ?)",
                         (tid, text[:500], max_order + 1))
    return jsonify({"ok": True, "id": cur.lastrowid})


@bp.route("/api/tasks/<int:tid>/checklist/<int:cid>", methods=["PUT"])
@login_required
def checklist_update(tid, cid):
    d = request.json or {}
    sets, vals = [], []
    if 'text' in d:
        sets.append('text=?')
        vals.append((d['text'] or '').strip()[:500])
    if 'done' in d:
        sets.append('done=?')
        vals.append(1 if d['done'] else 0)
    if sets:
        vals.extend([tid, cid])
        with get_db() as db:
            db.execute(f"UPDATE task_checklist_items SET {', '.join(sets)} WHERE task_id=? AND id=?", vals)
    return jsonify({"ok": True})


@bp.route("/api/tasks/<int:tid>/checklist/<int:cid>", methods=["DELETE"])
@login_required
def checklist_delete(tid, cid):
    with get_db() as db:
        db.execute("DELETE FROM task_checklist_items WHERE task_id=? AND id=?", (tid, cid))
    return jsonify({"ok": True})


# ══════════════════════════════════════════════════════════════════════════════
#  COMMENTS API
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/tasks/<int:tid>/comments", methods=["POST"])
@login_required
def comment_add(tid):
    u = request.current_user
    d = request.json or {}
    body = (d.get("body") or "").strip()
    if not body:
        return jsonify({"error": "Комментарий не может быть пустым"}), 400

    with get_db() as db:
        cur = db.execute("INSERT INTO task_comments (task_id, user_id, body) VALUES (?, ?, ?)",
                         (tid, u["id"], body[:5000]))
        comment = db.execute("""
            SELECT tc.*, u.name as author_name FROM task_comments tc
            LEFT JOIN users u ON tc.user_id=u.id WHERE tc.id=?
        """, (cur.lastrowid,)).fetchone()

    return jsonify({"ok": True, "comment": dict(comment)})


@bp.route("/api/tasks/<int:tid>/comments/<int:cid>", methods=["DELETE"])
@login_required
def comment_delete(tid, cid):
    with get_db() as db:
        db.execute("DELETE FROM task_comments WHERE task_id=? AND id=? AND user_id=?",
                   (tid, cid, request.current_user["id"]))
    return jsonify({"ok": True})


# ══════════════════════════════════════════════════════════════════════════════
#  TIME TRACKING API
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/tasks/<int:tid>/time", methods=["POST"])
@login_required
def time_add(tid):
    u = request.current_user
    d = request.json or {}
    minutes = d.get("minutes", 0)
    description = (d.get("description") or "").strip()

    if not minutes or minutes <= 0:
        return jsonify({"error": "Укажите время"}), 400

    with get_db() as db:
        cur = db.execute("INSERT INTO task_time_log (task_id, user_id, minutes, description) VALUES (?, ?, ?, ?)",
                         (tid, u["id"], minutes, description[:500]))

    return jsonify({"ok": True, "id": cur.lastrowid})


# ══════════════════════════════════════════════════════════════════════════════
#  PARTICIPANTS API
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/tasks/<int:tid>/participants", methods=["POST"])
@login_required
def participant_add(tid):
    d = request.json or {}
    user_id = d.get("user_id")
    role = d.get("role", TASK_ROLE_PARTICIPANT)
    if not user_id:
        return jsonify({"error": "user_id обязателен"}), 400

    with get_db() as db:
        exists = db.execute("SELECT id FROM task_participants WHERE task_id=? AND user_id=?", (tid, user_id)).fetchone()
        if exists:
            db.execute("UPDATE task_participants SET role=? WHERE task_id=? AND user_id=?", (role, tid, user_id))
        else:
            db.execute("INSERT INTO task_participants (task_id, user_id, role) VALUES (?, ?, ?)", (tid, user_id, role))
    return jsonify({"ok": True})


@bp.route("/api/tasks/<int:tid>/participants/<int:uid>", methods=["DELETE"])
@login_required
def participant_remove(tid, uid):
    with get_db() as db:
        db.execute("DELETE FROM task_participants WHERE task_id=? AND user_id=?", (tid, uid))
    return jsonify({"ok": True})


# ══════════════════════════════════════════════════════════════════════════════
#  USERS LIST (for assignee picker)
# ══════════════════════════════════════════════════════════════════════════════

@bp.route("/api/tasks/users")
@login_required
def task_users_list():
    with get_db() as db:
        users = db.execute(
            "SELECT id, name, email, role, department, avatar_color FROM users WHERE active=1 ORDER BY name"
        ).fetchall()
    return jsonify([dict(u) for u in users])
