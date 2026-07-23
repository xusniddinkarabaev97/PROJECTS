"""Office / Clerical Blueprint — канцелярия и делопроизводство."""

import os, uuid
from datetime import date

from flask import Blueprint, render_template, request, jsonify

from modules.auth import login_required
from modules.db import get_db
from modules.config import ROLES

bp = Blueprint('office', __name__)

# ══════════════════════════════════════════════════════════════════════════════
#  CONSTANTS
# ══════════════════════════════════════════════════════════════════════════════

OFFICE_STATUSES = ['draft','registered','review','in_progress','completed','archived']
OFFICE_STATUS_LABELS = {
    'draft':'Черновик','registered':'Зарегистрирован','review':'На рассмотрении',
    'in_progress':'В работе','completed':'Исполнен','archived':'В архиве'
}
OFFICE_TYPES = {'incoming':'Входящий','outgoing':'Исходящий','internal':'Внутренний'}


# ══════════════════════════════════════════════════════════════════════════════
#  HELPERS
# ══════════════════════════════════════════════════════════════════════════════

def _next_reg_number(db, doc_type):
    """Генерирует регистрационный номер: ВХ-1/2026, ИСХ-1/2026, 1-П"""
    year = date.today().year
    if doc_type == 'incoming':
        prefix = 'ВХ'
    elif doc_type == 'outgoing':
        prefix = 'ИСХ'
    else:
        prefix = ''
    
    if doc_type == 'internal':
        row = db.execute("SELECT COUNT(*) as cnt FROM office_docs WHERE doc_type='internal'").fetchone()
        num = (row['cnt'] if row else 0) + 1
        return f"{num}-П"
    else:
        row = db.execute(
            "SELECT MAX(reg_number) as mx FROM office_docs WHERE doc_type=? AND reg_number LIKE ?",
            (doc_type, f'{prefix}-%/{year}')
        ).fetchone()
        mx = row['mx'] if row else None
        if mx:
            try:
                num = int(mx.split('-')[1].split('/')[0]) + 1
            except:
                num = 1
        else:
            num = 1
        return f"{prefix}-{num}/{year}"


def _office_doc_dict(row):
    """Форматирует строку документа для JSON."""
    d = dict(row)
    d['status_label'] = OFFICE_STATUS_LABELS.get(d.get('status',''), d.get('status',''))
    d['type_label'] = OFFICE_TYPES.get(d.get('doc_type',''), d.get('doc_type',''))
    if d.get('reg_date'): d['reg_date'] = str(d['reg_date'])
    if d.get('created_at'): d['created_at'] = str(d['created_at'])
    if d.get('deadline'): d['deadline'] = str(d['deadline'])
    return d


# ══════════════════════════════════════════════════════════════════════════════
#  КОНТРАГЕНТЫ
# ══════════════════════════════════════════════════════════════════════════════

@bp.route('/api/office/contractors')
@login_required
def office_contractors_list():
    with get_db() as db:
        rows = db.execute('SELECT * FROM contractors ORDER BY name').fetchall()
    return jsonify([dict(r) for r in rows])


@bp.route('/api/office/contractors', methods=['POST'])
@login_required
def office_contractors_add():
    d = request.json or {}
    name = d.get('name','').strip()
    if not name: return jsonify({'error':'Название обязательно'}),400
    with get_db() as db:
        cur = db.execute(
            'INSERT INTO contractors (name,tin,email,phone,address) VALUES (?,?,?,?,?)',
            (name, d.get('tin',''), d.get('email',''), d.get('phone',''), d.get('address','')))
    return jsonify({'ok':True,'id':cur.lastrowid})


@bp.route('/api/office/contractors/<int:cid>', methods=['PUT'])
@login_required
def office_contractors_update(cid):
    d = request.json or {}
    with get_db() as db:
        db.execute('UPDATE contractors SET name=?,tin=?,email=?,phone=?,address=? WHERE id=?',
                   (d.get('name',''), d.get('tin',''), d.get('email',''), d.get('phone',''), d.get('address',''), cid))
    return jsonify({'ok':True})


@bp.route('/api/office/contractors/<int:cid>', methods=['DELETE'])
@login_required
def office_contractors_delete(cid):
    with get_db() as db:
        db.execute('DELETE FROM contractors WHERE id=?',(cid,))
    return jsonify({'ok':True})


# ══════════════════════════════════════════════════════════════════════════════
#  ДОКУМЕНТЫ КАНЦЕЛЯРИИ
# ══════════════════════════════════════════════════════════════════════════════

@bp.route('/api/office/docs')
@login_required
def office_docs_list():
    u = request.current_user
    doc_type = request.args.get('type','')
    status = request.args.get('status','')
    my = request.args.get('my','')  # assigned to me
    
    where = []
    params = []
    if doc_type:
        where.append('d.doc_type=?')
        params.append(doc_type)
    if status:
        where.append('d.status=?')
        params.append(status)
    if my:
        where.append('d.assigned_to_id=?')
        params.append(u['id'])
    
    # Employee sees only assigned to them; registrar/manager see all
    if u['role'] == 'employee':
        where.append('(d.assigned_to_id=? OR d.creator_id=?)')
        params.extend([u['id'], u['id']])
    
    where_clause = ' AND '.join(where) if where else '1=1'
    
    with get_db() as db:
        rows = db.execute(f'''
            SELECT d.*, c.name as contractor_name, u1.name as creator_name, u2.name as assignee_name
            FROM office_docs d
            LEFT JOIN contractors c ON d.contractor_id=c.id
            LEFT JOIN users u1 ON d.creator_id=u1.id
            LEFT JOIN users u2 ON d.assigned_to_id=u2.id
            WHERE {where_clause}
            ORDER BY d.created_at DESC LIMIT 200
        ''', params).fetchall()
    return jsonify([_office_doc_dict(r) for r in rows])


@bp.route('/api/office/docs/<int:did>')
@login_required
def office_doc_get(did):
    with get_db() as db:
        doc = db.execute('''
            SELECT d.*, c.name as contractor_name, u1.name as creator_name, u2.name as assignee_name,
                   rd.reg_number as reply_to_number, rd.title as reply_to_title
            FROM office_docs d
            LEFT JOIN contractors c ON d.contractor_id=c.id
            LEFT JOIN users u1 ON d.creator_id=u1.id
            LEFT JOIN users u2 ON d.assigned_to_id=u2.id
            LEFT JOIN office_docs rd ON d.reply_to_id=rd.id
            WHERE d.id=?
        ''',(did,)).fetchone()
        if not doc: return jsonify({'error':'Документ не найден'}),404
        
        files = db.execute('SELECT * FROM office_doc_files WHERE doc_id=? ORDER BY uploaded_at DESC',(did,)).fetchall()
        history = db.execute('''
            SELECT h.*, u.name as user_name FROM office_doc_history h
            LEFT JOIN users u ON h.user_id=u.id WHERE h.doc_id=? ORDER BY h.created_at DESC
        ''',(did,)).fetchall()
        ack_users = db.execute('''
            SELECT a.*, u.name as user_name FROM office_acknowledgments a
            LEFT JOIN users u ON a.user_id=u.id WHERE a.doc_id=?
        ''',(did,)).fetchall()
    
    return jsonify({
        'doc':_office_doc_dict(doc),
        'files':[dict(f) for f in files],
        'history':[dict(h) for h in history],
        'acknowledgments':[dict(a) for a in ack_users]
    })


@bp.route('/api/office/docs', methods=['POST'])
@login_required
def office_doc_create():
    u = request.current_user
    d = request.json or {}
    doc_type = d.get('doc_type','incoming')
    if doc_type not in OFFICE_TYPES: return jsonify({'error':'Неверный тип'}),400
    with get_db() as db:
        # Clean empty strings -> None for nullable fields
        sdd = d.get('sender_doc_date') or None
        rid = d.get('reply_to_id') or None
        cid = d.get('contractor_id') or None
        cur = db.execute('''
            INSERT INTO office_docs (doc_type,title,description,contractor_id,recipient_name,
                sender_name,sender_doc_number,sender_doc_date,reply_to_id,priority,creator_id,status)
            VALUES (?,?,?,?,?,?,?,?,?,?,?,'draft')
        ''',(doc_type, d.get('title',''), d.get('description',''), cid,
             d.get('recipient_name',''), d.get('sender_name',''), d.get('sender_doc_number',''),
             sdd, rid, d.get('priority','normal'), u['id']))
        did = cur.lastrowid
        db.execute('INSERT INTO office_doc_history (doc_id,user_id,action) VALUES (?,?,?)',(did,u['id'],'created'))
    return jsonify({'ok':True,'id':did})


@bp.route('/api/office/docs/<int:did>', methods=['PUT'])
@login_required
def office_doc_update(did):
    d = request.json or {}
    with get_db() as db:
        db.execute('''
            UPDATE office_docs SET title=?,description=?,contractor_id=?,recipient_name=?,
            sender_doc_number=?,sender_doc_date=?,priority=?,updated_at=CURRENT_TIMESTAMP WHERE id=?
        ''',(d.get('title',''), d.get('description',''), d.get('contractor_id'),
             d.get('recipient_name',''), d.get('sender_doc_number',''),
             d.get('sender_doc_date'), d.get('priority','normal'), did))
    return jsonify({'ok':True})


# ══════════════════════════════════════════════════════════════════════════════
#  WORKFLOW ACTIONS
# ══════════════════════════════════════════════════════════════════════════════

@bp.route('/api/office/docs/<int:did>/register', methods=['POST'])
@login_required
def office_doc_register(did):
    """Регистратор регистрирует документ и присваивает номер."""
    u = request.current_user
    d = request.json or {}
    with get_db() as db:
        doc = db.execute('SELECT * FROM office_docs WHERE id=?',(did,)).fetchone()
        if not doc: return jsonify({'error':'Не найден'}),404
        if doc['status'] != 'draft': return jsonify({'error':'Можно зарегистрировать только черновик'}),400
        
        reg_number = _next_reg_number(db, doc['doc_type'])
        assigned_to = d.get('assigned_to_id')  # руководитель на рассмотрение
        
        if assigned_to:
            db.execute('''
                UPDATE office_docs SET status='review',reg_number=?,reg_date=CURRENT_DATE,
                assigned_to_id=?,updated_at=CURRENT_TIMESTAMP WHERE id=?
            ''',(reg_number, assigned_to, did))
            db.execute('INSERT INTO office_doc_history (doc_id,user_id,action,comment) VALUES (?,?,?,?)',
                       (did,u['id'],'registered',f'Присвоен №{reg_number}, направлен на рассмотрение'))
        else:
            db.execute('''
                UPDATE office_docs SET status='registered',reg_number=?,reg_date=CURRENT_DATE,
                updated_at=CURRENT_TIMESTAMP WHERE id=?
            ''',(reg_number, did))
            db.execute('INSERT INTO office_doc_history (doc_id,user_id,action,comment) VALUES (?,?,?,?)',
                       (did,u['id'],'registered',f'Присвоен №{reg_number}'))
    return jsonify({'ok':True,'reg_number':reg_number})


@bp.route('/api/office/docs/<int:did>/assign', methods=['POST'])
@login_required
def office_doc_assign(did):
    """Руководитель накладывает резолюцию: назначает исполнителя и дедлайн."""
    u = request.current_user
    d = request.json or {}
    assigned_to = d.get('assigned_to_id')
    deadline = d.get('deadline')
    resolution = d.get('resolution','')
    if not assigned_to: return jsonify({'error':'Укажите исполнителя'}),400
    if not deadline: return jsonify({'error':'Укажите срок исполнения'}),400
    
    with get_db() as db:
        doc = db.execute('SELECT * FROM office_docs WHERE id=?',(did,)).fetchone()
        if not doc: return jsonify({'error':'Не найден'}),404
        if doc['status'] not in ('draft','registered','review','in_progress'):
            return jsonify({'error':'Неверный статус для назначения'}),400
        
        db.execute('''
            UPDATE office_docs SET status='in_progress',assigned_to_id=?,deadline=?,
            resolution=?,updated_at=CURRENT_TIMESTAMP WHERE id=?
        ''',(assigned_to, deadline, resolution, did))
        db.execute('INSERT INTO office_doc_history (doc_id,user_id,action,comment) VALUES (?,?,?,?)',
                   (did,u['id'],'assigned',f'Назначен исполнитель, срок: {deadline}. {resolution}'))
    return jsonify({'ok':True})


@bp.route('/api/office/docs/<int:did>/complete', methods=['POST'])
@login_required
def office_doc_complete(did):
    """Исполнитель отмечает выполнение."""
    u = request.current_user
    d = request.json or {}
    with get_db() as db:
        doc = db.execute('SELECT * FROM office_docs WHERE id=?',(did,)).fetchone()
        if not doc: return jsonify({'error':'Не найден'}),404
        if doc['status'] != 'in_progress':
            return jsonify({'error':'Документ не в работе'}),400
        db.execute('''
            UPDATE office_docs SET status='completed',completed_at=CURRENT_TIMESTAMP,
            updated_at=CURRENT_TIMESTAMP WHERE id=?
        ''',(did,))
        comment = d.get('comment','Отмечено исполнение')
        db.execute('INSERT INTO office_doc_history (doc_id,user_id,action,comment) VALUES (?,?,?,?)',
                   (did,u['id'],'completed',comment))
    return jsonify({'ok':True})


@bp.route('/api/office/docs/<int:did>/archive', methods=['POST'])
@login_required
def office_doc_archive(did):
    """Регистратор отправляет в архив."""
    u = request.current_user
    with get_db() as db:
        doc = db.execute('SELECT * FROM office_docs WHERE id=?',(did,)).fetchone()
        if not doc: return jsonify({'error':'Не найден'}),404
        if doc['status'] not in ('completed','registered'):
            return jsonify({'error':'Можно архивировать только исполненные'}),400
        db.execute('''
            UPDATE office_docs SET status='archived',archived_at=CURRENT_TIMESTAMP,
            updated_at=CURRENT_TIMESTAMP WHERE id=?
        ''',(did,))
        db.execute('INSERT INTO office_doc_history (doc_id,user_id,action,comment) VALUES (?,?,?,?)',
                   (did,u['id'],'archived','Документ перемещён в архив'))
    return jsonify({'ok':True})


# ══════════════════════════════════════════════════════════════════════════════
#  ОЗНАКОМЛЕНИЕ С ПРИКАЗАМИ
# ══════════════════════════════════════════════════════════════════════════════

@bp.route('/api/office/docs/<int:did>/acknowledge', methods=['POST'])
@login_required
def office_doc_acknowledge(did):
    u = request.current_user
    with get_db() as db:
        doc = db.execute('SELECT * FROM office_docs WHERE id=?',(did,)).fetchone()
        if not doc: return jsonify({'error':'Не найден'}),404
        
        existing = db.execute('SELECT id FROM office_acknowledgments WHERE doc_id=? AND user_id=?',
                              (did,u['id'])).fetchone()
        if existing:
            db.execute('UPDATE office_acknowledgments SET acked_at=CURRENT_TIMESTAMP WHERE id=?',(existing['id'],))
        else:
            db.execute('INSERT INTO office_acknowledgments (doc_id,user_id,acked_at) VALUES (?,?,CURRENT_TIMESTAMP)',
                       (did,u['id']))
        db.execute('INSERT INTO office_doc_history (doc_id,user_id,action,comment) VALUES (?,?,?,?)',
                   (did,u['id'],'acknowledged','Ознакомлен с документом'))
    return jsonify({'ok':True})


@bp.route('/api/office/docs/<int:did>/add-ack-users', methods=['POST'])
@login_required
def office_doc_add_ack_users(did):
    """Добавить сотрудников в лист ознакомления."""
    d = request.json or {}
    user_ids = d.get('user_ids',[])
    with get_db() as db:
        for uid in user_ids:
            exists = db.execute('SELECT id FROM office_acknowledgments WHERE doc_id=? AND user_id=?',
                               (did,uid)).fetchone()
            if not exists:
                db.execute('INSERT INTO office_acknowledgments (doc_id,user_id) VALUES (?,?)',(did,uid))
    return jsonify({'ok':True})


# ══════════════════════════════════════════════════════════════════════════════
#  ФАЙЛЫ
# ══════════════════════════════════════════════════════════════════════════════

@bp.route('/api/office/docs/<int:did>/files', methods=['POST'])
@login_required
def office_doc_upload(did):
    u = request.current_user
    file = request.files.get('file')
    if not file: return jsonify({'error':'Файл не выбран'}),400
    fname = file.filename
    ext = os.path.splitext(fname)[1].lower()
    safe_name = f"{uuid.uuid4().hex}{ext}"
    office_uploads = os.path.join(os.path.dirname(os.path.dirname(__file__)), 'static', 'office_files')
    os.makedirs(office_uploads, exist_ok=True)
    fpath = os.path.join(office_uploads, safe_name)
    file.save(fpath)
    file_type = request.form.get('file_type','attachment')
    with get_db() as db:
        db.execute('''
            INSERT INTO office_doc_files (doc_id,file_name,file_path,file_type,uploaded_by)
            VALUES (?,?,?,?,?)
        ''',(did, fname, '/static/office_files/'+safe_name, file_type, u['id']))
        db.execute('INSERT INTO office_doc_history (doc_id,user_id,action,comment) VALUES (?,?,?,?)',
                   (did,u['id'],'file_uploaded',f'Загружен файл: {fname}'))
    return jsonify({'ok':True})


# ══════════════════════════════════════════════════════════════════════════════
#  ДАШБОРД КОНТРОЛЛИНГА
# ══════════════════════════════════════════════════════════════════════════════

@bp.route('/api/office/dashboard')
@login_required
def office_dashboard():
    u = request.current_user
    with get_db() as db:
        total = db.execute('SELECT COUNT(*) FROM office_docs').fetchone()[0]
        in_work = db.execute("SELECT COUNT(*) FROM office_docs WHERE status='in_progress'").fetchone()[0]
        overdue = db.execute(
            "SELECT COUNT(*) FROM office_docs WHERE deadline < CURRENT_DATE AND status NOT IN ('completed','archived')"
        ).fetchone()[0]
        due_today = db.execute(
            "SELECT COUNT(*) FROM office_docs WHERE deadline = CURRENT_DATE AND status NOT IN ('completed','archived')"
        ).fetchone()[0]
        due_tomorrow = db.execute(
            "SELECT COUNT(*) FROM office_docs WHERE deadline = date('now', '+1 day') AND status NOT IN ('completed','archived')"
        ).fetchone()[0]
        
        my_docs = db.execute(
            'SELECT COUNT(*) FROM office_docs WHERE assigned_to_id=? AND status NOT IN (?,?)',
            (u['id'],'completed','archived')
        ).fetchone()[0]
        
        recent = db.execute('''
            SELECT d.*, c.name as contractor_name, u1.name as creator_name, u2.name as assignee_name
            FROM office_docs d
            LEFT JOIN contractors c ON d.contractor_id=c.id
            LEFT JOIN users u1 ON d.creator_id=u1.id
            LEFT JOIN users u2 ON d.assigned_to_id=u2.id
            ORDER BY d.updated_at DESC LIMIT 10
        ''').fetchall()
        
        by_type = db.execute('''
            SELECT doc_type, COUNT(*) as cnt FROM office_docs GROUP BY doc_type
        ''').fetchall()
        by_status = db.execute('''
            SELECT status, COUNT(*) as cnt FROM office_docs GROUP BY status
        ''').fetchall()
    
    return jsonify({
        'total':total, 'in_work':in_work, 'overdue':overdue,
        'due_today':due_today, 'due_tomorrow':due_tomorrow,
        'my_docs':my_docs,
        'recent':[_office_doc_dict(r) for r in recent],
        'by_type':[dict(r) for r in by_type],
        'by_status':[dict(r) for r in by_status]
    })


# ══════════════════════════════════════════════════════════════════════════════
#  HTML PAGE
# ══════════════════════════════════════════════════════════════════════════════

@bp.route('/office')
@login_required
def office_page():
    u = request.current_user
    with get_db() as db:
        employees = [dict(r) for r in db.execute(
            "SELECT id, name, department FROM users WHERE active=1 ORDER BY name"
        ).fetchall()]
        contractors = [dict(r) for r in db.execute(
            'SELECT * FROM contractors ORDER BY name'
        ).fetchall()]
    return render_template('office.html',
        user=u, current_user=u, role_info=ROLES.get(u['role'],{}),
        employees=employees, contractors=contractors,
        statuses=OFFICE_STATUSES, status_labels=OFFICE_STATUS_LABELS,
        type_labels=OFFICE_TYPES)
