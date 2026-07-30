"""Finance Blueprint — единый финансовый раздел для бухгалтера/директора:
заявки на согласование, акты списания/передачи, амортизация, бюджет по отделам."""

import datetime as _dt
from datetime import date as _d

from flask import Blueprint, render_template, request, jsonify

from modules.config import app, ROLES
from modules.db import get_db
from modules.auth import login_required, roles_required, bhost
from modules.api_items import parse_writeoff_notes
from modules.api_reports import USEFUL_LIFE

bp = Blueprint('finance', __name__)


@bp.route("/finance")
@roles_required("accountant", "director", "superadmin")
def finance_page():
    u = request.current_user
    return render_template("finance.html", user=u, current_user=u,
        role_info=ROLES.get(u["role"], {}), roles=ROLES, host=bhost())


@bp.route("/api/acts")
@roles_required("accountant", "aho", "director", "superadmin", "auditor")
def list_acts():
    """Листинг актов списания и приёма-передачи (сами акты уже существуют отдельными страницами)."""
    with get_db() as db:
        writeoff_rows = db.execute("""
            SELECT id, inv_num, category, model, notes FROM items
            WHERE status='Списано' ORDER BY id DESC LIMIT 100
        """).fetchall()
        transfer_rows = db.execute("""
            SELECT id, employee_name, issued_by_name, status, created_at
            FROM issuances ORDER BY created_at DESC LIMIT 100
        """).fetchall()

    writeoffs = []
    for r in writeoff_rows:
        wo = parse_writeoff_notes(r["notes"])
        writeoffs.append({
            "inv_num": r["inv_num"], "category": r["category"], "model": r["model"],
            "date": wo["date"], "act_num": wo["act_num"], "reason": wo["reason"],
            "url": f"/asset/{r['inv_num']}/writeoff-act",
        })

    transfers = [{
        "id": r["id"], "employee_name": r["employee_name"], "issued_by_name": r["issued_by_name"],
        "status": r["status"], "created_at": r["created_at"],
        "url": f"/issuances/{r['id']}/print-act",
    } for r in transfer_rows]

    return jsonify({"writeoffs": writeoffs, "transfers": transfers})


@bp.route("/api/reports/budget-by-department")
@roles_required("accountant", "director", "superadmin")
def budget_by_department():
    """Остаточная стоимость и расчётный бюджет замены техники, сгруппированные по отделам."""
    with get_db() as db:
        rows = db.execute("""
            SELECT u.department, i.category, i.purchase_price, i.purchase_date
            FROM items i JOIN users u ON u.name = i.employee
            WHERE i.status != 'Списано' AND u.department IS NOT NULL AND u.department != ''
        """).fetchall()

    today = _d.today()
    dept_totals = {}
    for r in rows:
        dept = r["department"]
        d = dept_totals.setdefault(dept, {
            "purchase_total": 0.0, "residual_total": 0.0,
            "replacement_budget": 0.0, "items_count": 0,
        })
        d["items_count"] += 1
        price = r["purchase_price"] or 0
        d["purchase_total"] += price
        residual = price
        if price and r["purchase_date"]:
            try:
                purchased = _d.fromisoformat(r["purchase_date"])
                useful = USEFUL_LIFE.get(r["category"], 5)
                years = (today - purchased).days / 365.25
                pct = min(1.0, years / useful)
                residual = max(0, price * (1 - pct))
                if years >= useful:
                    d["replacement_budget"] += price
            except Exception:
                pass
        d["residual_total"] += residual

    departments = []
    total_budget = 0.0
    for dept, d in sorted(dept_totals.items()):
        d["department"] = dept
        d["purchase_total"] = round(d["purchase_total"], 2)
        d["residual_total"] = round(d["residual_total"], 2)
        d["replacement_budget"] = round(d["replacement_budget"], 2)
        total_budget += d["replacement_budget"]
        departments.append(d)

    return jsonify({"departments": departments, "total_budget": round(total_budget, 2)})
