"""
ASSETO Production Seed Script (PostgreSQL)
Запуск: python scratch/seed_production.py
Добавляет тестовые данные для демонстрации всех функций.
"""
import os, sys, bcrypt, json
from datetime import date, timedelta, datetime
import random

# Ensure we can import pg_compat
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import pg_compat as _db

DATABASE_URL = os.environ.get(
    'DATABASE_URL',
    'postgresql://asseto:asseto@localhost:5432/asseto',
)


def seed():
    db = _db.connect(DATABASE_URL)

    pw = bcrypt.hashpw(b'admin123', bcrypt.gensalt()).decode()

    # Users (все 9 ролей)
    users = [
        ('admin@asseto.uz',   pw, 'superadmin', 'Супер Администратор', 'АХО',        '#5856D6'),
        ('aho@asseto.uz',     pw, 'aho',        'Ахмад Каримов',       'АХО',        '#007AFF'),
        ('hr@asseto.uz',      pw, 'hr',         'Дилноза Юсупова',     'HR',         '#34C759'),
        ('emp1@asseto.uz',    pw, 'employee',   'Санжар Рашидов',      'IT',         '#FF9500'),
        ('emp2@asseto.uz',    pw, 'employee',   'Малика Хасанова',     'Бухгалтерия','#FF375F'),
        ('auditor@asseto.uz', pw, 'auditor',    'Бобур Исмоилов',      'Аудит',      '#5AC8FA'),
        ('deputy@asseto.uz',  pw, 'deputy',     'Феруза Назарова',     'Дирекция',   '#FF9500'),
        ('director@asseto.uz',pw, 'director',   'Отабек Мирзаев',      'Дирекция',   '#FF3B30'),
        ('accountant@asseto.uz',pw,'accountant','Зулфия Ташпулатова',  'Бухгалтерия','#30B0C7'),
    ]
    user_ids = {}
    for email, pwhash, role, name, dept, color in users:
        row = db.execute("SELECT id FROM users WHERE email=?", (email,)).fetchone()
        if not row:
            cur = db.execute(
                "INSERT INTO users(name,email,password_hash,role,active,department,avatar_color,onboarding_done) VALUES(?,?,?,?,1,?,?,1)",
                (name, email, pwhash, role, dept, color),
            )
            user_ids[email] = cur.lastrowid
        else:
            user_ids[email] = row['id']
    print(f"Users: {len(users)} ensured")

    # Items (20 активов с разными статусами)
    today = date.today()
    items_data = [
        ('НТБ-001','Ноутбук','Dell XPS 15','SN-001','Каб. 101','Санжар Рашидов','emp1@asseto.uz','Занято','Хорошее',1850,'2023-03-15',2025),
        ('НТБ-002','Ноутбук','MacBook Pro 14','SN-002','Каб. 203','Малика Хасанова','emp2@asseto.uz','Занято','Хорошее',2400,'2023-09-01',2026),
        ('НТБ-003','Ноутбук','Lenovo ThinkPad','SN-003','Каб. 102','Ахмад Каримов','aho@asseto.uz','Занято','Хорошее',1400,'2022-11-20',2025),
        ('НТБ-004','Ноутбук','HP EliteBook','SN-004','Склад','—',None,'Свободно','Хорошее',1100,'2022-05-10',2025),
        ('НТБ-005','Ноутбук','Asus ZenBook','SN-005','Каб. 305','Феруза Назарова','deputy@asseto.uz','Занято','Потёрто',950,'2021-08-15',2024),
        ('МНТ-001','Монитор','Dell U2722D','SN-006','Каб. 101','Санжар Рашидов','emp1@asseto.uz','Занято','Хорошее',650,'2022-06-20',2025),
        ('МНТ-002','Монитор','LG 27UK850','SN-007','Каб. 203','Малика Хасанова','emp2@asseto.uz','Занято','Хорошее',580,'2022-09-10',2025),
        ('МНТ-003','Монитор','Samsung 32"','SN-008','Склад','—',None,'Свободно','Хорошее',420,'2023-01-05',2026),
        ('КРС-001','Кресло','Herman Miller','SN-009','Каб. 101','Санжар Рашидов','emp1@asseto.uz','Занято','Хорошее',1200,'2022-03-10',2032),
        ('КРС-002','Кресло','Steelcase Leap','SN-010','Каб. 203','Малика Хасанова','emp2@asseto.uz','Занято','Хорошее',980,'2022-03-10',2032),
        ('ПРН-001','Принтер','HP LaserJet','SN-011','Каб. 102','Ахмад Каримов','aho@asseto.uz','Занято','Хорошее',450,'2022-08-20',2025),
        ('ПРН-002','Принтер','Canon MF445dw','SN-012','Каб. 305','Феруза Назарова','deputy@asseto.uz','Занято','Требует ремонта',380,'2021-04-15',2024),
        ('ТЕЛ-001','Телефон','iPhone 14 Pro','SN-013','Каб. 101','Санжар Рашидов','emp1@asseto.uz','Занято','Хорошее',1100,'2023-01-10',2025),
        ('ТЕЛ-002','Телефон','Samsung S23','SN-014','Склад','—',None,'Свободно','Хорошее',850,'2023-03-20',2026),
        ('КЛВ-001','Клавиатура','Logitech MX','SN-015','Каб. 101','Санжар Рашидов','emp1@asseto.uz','Занято','Хорошее',120,'2022-12-01',2025),
        ('МЫШ-001','Мышь','Logitech MX3','SN-016','Каб. 101','Санжар Рашидов','emp1@asseto.uz','Занято','Хорошее',80,'2022-12-01',2025),
        ('СРВ-001','Сервер','Dell R740','SN-017','Серверная','Ахмад Каримов','aho@asseto.uz','Занято','Хорошее',12500,'2023-01-15',2026),
        ('СТЛ-001','Стол','IKEA Bekant','SN-018','Каб. 101','Санжар Рашидов','emp1@asseto.uz','Занято','Хорошее',350,'2022-01-10',2032),
        ('НТБ-006','Ноутбук','HP Pavilion','SN-019','Каб. 102','Ахмад Каримов','aho@asseto.uz','Занято','Хорошее',800,'2023-07-05',2026),
        ('НТБ-007','Ноутбук','Dell Inspiron','SN-020','Склад','—',None,'Свободно','Потёрто',650,'2022-04-12',2025),
    ]
    item_ids = {}
    for row in items_data:
        inv_num, cat, model, sn, room, emp, emp_email, status, cond, price, pdate, wyear = row
        emp_id = user_ids.get(emp_email) if emp_email else None
        existing = db.execute("SELECT id FROM items WHERE inv_num=?", (inv_num,)).fetchone()
        if not existing:
            cur = db.execute(
                """INSERT INTO items(inv_num,category,model,serial_num,room,employee,employee_id,
                    status,condition,purchase_price,purchase_date,warranty_until,check_date,place)
                    VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
                (inv_num, cat, model, sn, room, emp, emp_id, status, cond, price, pdate,
                 f"{wyear}-12-31", (today - timedelta(days=random.randint(10, 200))).isoformat(),
                 'Главный офис'),
            )
            item_ids[inv_num] = cur.lastrowid
        else:
            item_ids[inv_num] = existing['id']
    print(f"Items: {len(items_data)} ensured")

    # Maintenance requests
    cnt = db.execute("SELECT COUNT(*) FROM maintenance").fetchone()[0]
    if cnt == 0:
        maint = [
            (item_ids.get('ПРН-002', 12), user_ids.get('emp1@asseto.uz', 4), 'Санжар Рашидов',
             'Принтер не захватывает бумагу', 'high', 'pending'),
            (item_ids.get('НТБ-001', 1), user_ids.get('emp1@asseto.uz', 4), 'Санжар Рашидов',
             'Ноутбук перегревается, вентилятор шумит', 'high', 'pending'),
            (item_ids.get('КРС-001', 9), user_ids.get('emp1@asseto.uz', 4), 'Санжар Рашидов',
             'Кресло скрипит при вращении', 'low', 'completed'),
        ]
        for m in maint:
            db.execute(
                "INSERT INTO maintenance(item_id,reported_by_id,reported_by_name,description,priority,status) VALUES(?,?,?,?,?,?)",
                m,
            )
        print(f"Maintenance: {len(maint)} added")

    # Asset requests
    cnt = db.execute("SELECT COUNT(*) FROM asset_requests").fetchone()[0]
    if cnt == 0:
        reqs = [
            (user_ids.get('emp1@asseto.uz', 4), 'Санжар Рашидов', 'Монитор', 'Нужен второй монитор для разработки', 'pending'),
            (user_ids.get('emp2@asseto.uz', 5), 'Малика Хасанова', 'Ноутбук', 'Мой ноутбук устарел, нужна замена', 'approved'),
        ]
        for r in reqs:
            db.execute(
                "INSERT INTO asset_requests(employee_id,employee_name,category,reason,status) VALUES(?,?,?,?,?)",
                r,
            )
        print(f"Requests: {len(reqs)} added")

    db.commit()
    db.close()
    print("\n✓ Seed complete!")
    print("Логины:")
    for email, _, role, name, *_ in users:
        print(f"  {email} / admin123  ({role})")


if __name__ == '__main__':
    seed()
