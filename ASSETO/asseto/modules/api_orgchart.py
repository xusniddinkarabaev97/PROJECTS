"""Organization Chart Blueprint — org structure like Bitrix24."""
from flask import Blueprint, render_template, request, jsonify
from modules.auth import login_required
from modules.db import get_db
from modules.config import ROLES

bp = Blueprint('orgchart', __name__)


@bp.route('/api/orgchart')
@login_required
def orgchart_data():
    """Return flat user list with manager_id for tree building."""
    with get_db() as db:
        users = db.execute('''
            SELECT id, name, email, role, department, position, phone, manager_id, active,
                   avatar_color, hire_date
            FROM users WHERE active=1 AND role != 'superadmin'
            ORDER BY name
        ''').fetchall()
        depts = db.execute('''
            SELECT DISTINCT department FROM users WHERE active=1 AND department IS NOT NULL
            ORDER BY department
        ''').fetchall()

    result = []
    for u in users:
        item = dict(u)
        item['role_label'] = ROLES.get(item['role'], {}).get('label', item['role'])
        item['role_color'] = ROLES.get(item['role'], {}).get('color', '8E8E93')
        if item.get('hire_date'):
            item['hire_date'] = str(item['hire_date'])
        result.append(item)

    return jsonify({
        'users': result,
        'departments': [d['department'] for d in depts]
    })


@bp.route('/api/orgchart', methods=['POST'])
@login_required
def orgchart_update():
    """Update user orgchart fields."""
    d = request.json or {}
    uid = d.get('id')
    with get_db() as db:
        # Build SET clause dynamically to not overwrite fields with empty strings
        sets = []
        vals = []
        for field in ['manager_id','position','phone','department']:
            v = d.get(field)
            if v is not None and v != '':
                # Prevent self-reference
                if field == 'manager_id' and int(v) == int(uid):
                    continue
                sets.append(f'{field}=?')
                vals.append(v)
        if sets:
            vals.append(uid)
            db.execute(f"UPDATE users SET {', '.join(sets)} WHERE id=?", vals)
    return jsonify({'ok': True})


@bp.route('/api/orgchart/user', methods=['POST'])
@login_required
def orgchart_add_user():
    """Add new user via orgchart."""
    d = request.json or {}
    name = (d.get('name') or '').strip()
    email = (d.get('email') or '').strip().lower()
    if not name or not email:
        return jsonify({'error': 'Имя и email обязательны'}), 400
    import bcrypt
    pw = bcrypt.hashpw(b'123456', bcrypt.gensalt()).decode()
    with get_db() as db:
        mgr = d.get('manager_id')
        cur = db.execute('''
            INSERT INTO users (name,email,password_hash,role,department,position,manager_id,active,onboarding_done)
            VALUES (?,?,?,?,?,?,?,1,0)
        ''', (name, email, pw, d.get('role','employee'), d.get('department'),
              d.get('position'), mgr))
        # Prevent self-reference after insert
        if mgr and cur.lastrowid == int(mgr):
            db.execute('UPDATE users SET manager_id=NULL WHERE id=?', (cur.lastrowid,))
    return jsonify({'ok': True, 'id': cur.lastrowid})


@bp.route('/api/orgchart/user/<int:uid>', methods=['DELETE'])
@login_required
def orgchart_remove_user(uid):
    """Deactivate user (soft delete)."""
    with get_db() as db:
        db.execute('UPDATE users SET active=0, manager_id=NULL WHERE id=?', (uid,))
        db.execute('UPDATE users SET manager_id=NULL WHERE manager_id=?', (uid,))
    return jsonify({'ok': True})


@bp.route('/orgchart2')
@login_required
def orgchart_react_page():
    u = request.current_user
    return render_template('orgchart_react.html', user=u, current_user=u, role_info=ROLES.get(u['role'], {}))


@bp.route('/orgchart')
@login_required
def orgchart_page():
    u = request.current_user
    with get_db() as db:
        users = db.execute('''
            SELECT id, name, department FROM users WHERE active=1 ORDER BY name
        ''').fetchall()
    return render_template('orgchart.html',
        user=u, current_user=u,
        role_info=ROLES.get(u['role'], {}),
        users=[dict(r) for r in users])
