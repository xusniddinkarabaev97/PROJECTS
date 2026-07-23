"""Rooms API Blueprint — office rooms/locations management."""

from flask import Blueprint, render_template, request, jsonify

from modules.auth import login_required, roles_required, bhost
from modules.db import get_db

bp = Blueprint('rooms', __name__)


@bp.route("/api/rooms")
@login_required
def list_rooms():
    with get_db() as db:
        rows = db.execute("""
            SELECT r.*, COUNT(i.id) as item_count
            FROM rooms r
            LEFT JOIN items i ON i.room = r.name AND i.status != 'Списано'
            GROUP BY r.id ORDER BY r.floor, r.name
        """).fetchall()
    return jsonify([dict(r) for r in rows])


@bp.route("/api/rooms", methods=["POST"])
@roles_required("superadmin", "aho")
def create_room():
    d = request.json
    name = (d.get("name") or "").strip()
    if not name:
        return jsonify({"error": "Название обязательно"}), 400
    with get_db() as db:
        cur = db.execute(
            "INSERT INTO rooms (name, floor, wing, capacity, responsible, description, color) VALUES (?,?,?,?,?,?,?)",
            (name, d.get("floor","1"), d.get("wing",""), d.get("capacity",0),
             d.get("responsible",""), d.get("description",""), d.get("color","#007AFF"))
        )
    return jsonify({"ok": True, "id": cur.lastrowid})


@bp.route("/api/rooms/<int:rid>", methods=["PUT"])
@roles_required("superadmin", "aho")
def update_room(rid):
    d = request.json
    with get_db() as db:
        db.execute("""UPDATE rooms SET name=?, floor=?, wing=?, capacity=?,
                      responsible=?, description=?, color=? WHERE id=?""",
                   (d.get("name"), d.get("floor","1"), d.get("wing",""),
                    d.get("capacity",0), d.get("responsible",""),
                    d.get("description",""), d.get("color","#007AFF"), rid))
    return jsonify({"ok": True})


@bp.route("/api/rooms/<int:rid>", methods=["DELETE"])
@roles_required("superadmin", "aho")
def delete_room(rid):
    with get_db() as db:
        room = db.execute("SELECT name FROM rooms WHERE id=?", (rid,)).fetchone()
        if not room: return jsonify({"error": "Не найдено"}), 404
        count = db.execute("SELECT COUNT(*) FROM items WHERE room=?", (room["name"],)).fetchone()[0]
        if count > 0:
            return jsonify({"error": f"В кабинете {count} единиц техники. Переместите сначала."}), 400
        db.execute("DELETE FROM rooms WHERE id=?", (rid,))
    return jsonify({"ok": True})


@bp.route("/api/rooms/<int:rid>/items")
@login_required
def room_items(rid):
    with get_db() as db:
        room = db.execute("SELECT name FROM rooms WHERE id=?", (rid,)).fetchone()
        if not room: return jsonify({"error": "Не найдено"}), 404
        items = db.execute("""SELECT id, inv_num, category, model, condition, status, employee,
                                     purchase_price, serial_num
                              FROM items WHERE room=? ORDER BY category, model""",
                           (room["name"],)).fetchall()
    return jsonify([dict(i) for i in items])


@bp.route("/rooms")
@login_required
def rooms_page():
    return render_template("rooms.html", user=request.current_user, host=bhost())
