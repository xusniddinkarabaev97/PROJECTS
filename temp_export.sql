BEGIN TRANSACTION;
CREATE TABLE api_keys (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            key_hash TEXT UNIQUE NOT NULL,
            scopes TEXT DEFAULT 'read',
            user_id INTEGER,
            last_used TIMESTAMP,
            expires_at DATE,
            active INTEGER DEFAULT 1,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP);
INSERT INTO "api_keys" VALUES(1,'у','f10dc32f073ba10d79b002dcf3ab0bf89f41611ef4beb6b167cc118791f3fdc0','read,write,delete',1,NULL,NULL,1,'2026-05-13 06:38:29');
CREATE TABLE app_settings (
            key_name TEXT PRIMARY KEY,
            key_value TEXT);
INSERT INTO "app_settings" VALUES('gemini_api_key','AIzaSyDr89vfWV7qtALMRjGdTWOrxZ2nkqmEQMY');
CREATE TABLE asset_requests (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            employee_id INTEGER NOT NULL,
            employee_name TEXT NOT NULL,
            category TEXT NOT NULL,
            reason TEXT,
            status TEXT DEFAULT 'pending',
            rejection_reason TEXT,
            resolved_by TEXT,
            resolved_at TIMESTAMP,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP, company_id INTEGER DEFAULT 1);
INSERT INTO "asset_requests" VALUES(3,23,'Хусниддин','Ноутбук','432432','pending',NULL,NULL,NULL,'2026-07-22 12:27:23',1);
CREATE TABLE audit_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            signed_by_id INTEGER,
            signed_by_name TEXT,
            item_count INTEGER DEFAULT 0,
            note TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP);
CREATE TABLE companies (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            slug TEXT UNIQUE NOT NULL,
            plan TEXT DEFAULT 'trial',
            max_users INTEGER DEFAULT 25,
            max_items INTEGER DEFAULT 500,
            trial_ends DATE,
            active INTEGER DEFAULT 1,
            logo TEXT,
            primary_color TEXT DEFAULT '007AFF',
            contact_email TEXT,
            contact_phone TEXT,
            address TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP);
INSERT INTO "companies" VALUES(1,'Моя компания','default','enterprise',25,500,NULL,1,NULL,'007AFF',NULL,NULL,NULL,'2026-05-30 16:42:32');
CREATE TABLE contractors (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            tin TEXT,
            email TEXT,
            phone TEXT,
            address TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP);
CREATE TABLE dismissals (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            employee_id INTEGER NOT NULL, employee_name TEXT NOT NULL,
            employee_email TEXT,
            initiated_by INTEGER NOT NULL, initiated_by_name TEXT NOT NULL,
            items_json TEXT NOT NULL,
            status TEXT DEFAULT 'pending',
            notes TEXT,
            photos_json TEXT, signature TEXT,
            confirmed_by INTEGER, confirmed_by_name TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            completed_at TIMESTAMP, aho_signature TEXT, it_signature TEXT, hr_signature TEXT, hr_at TIMESTAMP, hr_by_id INTEGER, hr_by_name TEXT, employee_signature TEXT, deadline DATE, item_conditions TEXT DEFAULT '{}', item_comments TEXT DEFAULT '{}', confirmed_signature INTEGER DEFAULT 0, aho_cleared INTEGER DEFAULT 0, aho_at TIMESTAMP, aho_by_id INTEGER, aho_by_name TEXT, it_cleared INTEGER DEFAULT 0, it_at TIMESTAMP, it_by_id INTEGER, it_by_name TEXT, company_id INTEGER DEFAULT 1);
INSERT INTO "dismissals" VALUES(1,10,'Ozod','dir@tracko.uz',1,'Администратор','[{"id": 43, "inv_num": "\u041d\u0422\u0411-010", "category": "\u041d\u043e\u0443\u0442\u0431\u0443\u043a", "model": "QA-Test-Book Pro", "room": "Room 101"}]','pending_aho','Test dismissal',NULL,NULL,NULL,NULL,'2026-05-12 07:18:07',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'{}','{}',0,0,NULL,NULL,NULL,0,NULL,NULL,NULL,1);
INSERT INTO "dismissals" VALUES(2,20,'Test QA User','qa_user_7f3130b0@tracko.uz',1,'Администратор','[{"id": 43, "inv_num": "\u041d\u0422\u0411-010", "category": "\u041d\u043e\u0443\u0442\u0431\u0443\u043a", "model": "QA-Test-Book Pro", "room": "Room 101"}, {"id": 44, "inv_num": "\u041d\u0422\u0411-011", "category": "\u041d\u043e\u0443\u0442\u0431\u0443\u043a", "model": "QA-Test-Book Pro", "room": "Room 101"}, {"id": 45, "inv_num": "\u041d\u0422\u0411-012", "category": "\u041d\u043e\u0443\u0442\u0431\u0443\u043a", "model": "QA-Test-Book Pro", "room": "Room 101"}, {"id": 46, "inv_num": "\u041d\u0422\u0411-013", "category": "\u041d\u043e\u0443\u0442\u0431\u0443\u043a", "model": "QA-Test-Book Pro", "room": "Room 101"}]','completed','Test dismissal',NULL,NULL,NULL,NULL,'2026-05-12 07:19:49','2026-05-12 07:19:49','/static/signatures/dis_2_aho_d229ce6e.png','/static/signatures/dis_2_it_482d739f.png','/static/signatures/dis_2_hr_315e4b01.png','2026-05-12 07:19:49',1,'Администратор',NULL,NULL,'{}','{}',0,1,'2026-05-12 07:19:49',1,'Администратор',1,'2026-05-12 07:19:49',1,'Администратор',1);
INSERT INTO "dismissals" VALUES(3,26,'TestAdd','testadd_1784549463@test.com',1,'Администратор','[]','pending_aho','',NULL,NULL,NULL,NULL,'2026-07-20 12:39:50',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'{}','{}',0,0,NULL,NULL,NULL,0,NULL,NULL,NULL,1);
CREATE TABLE doc_approvals (
        id          INTEGER PRIMARY KEY AUTOINCREMENT,
        doc_id      INTEGER NOT NULL,
        step        INTEGER NOT NULL,
        role        TEXT NOT NULL,
        role_label  TEXT,
        approver_id   INTEGER,
        approver_name TEXT,
        action      TEXT,
        comment     TEXT,
        acted_at    TIMESTAMP,
        created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP, signature TEXT,
        FOREIGN KEY (doc_id) REFERENCES documents(id)
    );
INSERT INTO "doc_approvals" VALUES(1,2,1,'aho','АХО / IT',NULL,NULL,NULL,NULL,NULL,'2026-05-10 11:49:00',NULL);
INSERT INTO "doc_approvals" VALUES(2,2,2,'deputy','Зам. Директора',NULL,NULL,NULL,NULL,NULL,'2026-05-10 11:49:00',NULL);
INSERT INTO "doc_approvals" VALUES(3,2,3,'director','Ген. Директор',NULL,NULL,NULL,NULL,NULL,'2026-05-10 11:49:00',NULL);
INSERT INTO "doc_approvals" VALUES(4,2,4,'accountant','Бухгалтер',NULL,NULL,NULL,NULL,NULL,'2026-05-10 11:49:00',NULL);
INSERT INTO "doc_approvals" VALUES(5,3,1,'aho','АХО / IT',2,'АХО Менеджер','approved','','2026-05-19 06:37:01','2026-05-12 05:58:14','/static/signatures/doc_3_step_1_0cc9443d.png');
INSERT INTO "doc_approvals" VALUES(6,3,2,'deputy','Зам. Директора',7,'Зам. Директора','approved','','2026-06-01 06:40:52','2026-05-12 05:58:14','/static/signatures/doc_3_step_2_404e3018.png');
INSERT INTO "doc_approvals" VALUES(7,3,3,'director','Ген. Директор',NULL,NULL,NULL,NULL,NULL,'2026-05-12 05:58:14',NULL);
INSERT INTO "doc_approvals" VALUES(8,3,4,'accountant','Бухгалтер',NULL,NULL,NULL,NULL,NULL,'2026-05-12 05:58:14',NULL);
INSERT INTO "doc_approvals" VALUES(9,4,1,'aho','АХО / IT',2,'АХО Менеджер','approved','','2026-05-19 06:36:53','2026-05-13 13:38:26','/static/signatures/doc_4_step_1_f841a6b9.png');
INSERT INTO "doc_approvals" VALUES(10,4,2,'director','Ген. Директор',8,'Ген. Директор','approved','','2026-05-29 19:52:08','2026-05-13 13:38:26','/static/signatures/doc_4_step_2_3c675736.png');
INSERT INTO "doc_approvals" VALUES(11,4,3,'accountant','Бухгалтер',13,'Главный Бухгалтер','approved','','2026-05-29 23:35:37','2026-05-13 13:38:26','/static/signatures/doc_4_step_3_2b6ede2a.png');
INSERT INTO "doc_approvals" VALUES(16,6,1,'aho','АХО / IT',22,'Cобиров Бегзод','approved','','2026-07-29 15:34:04','2026-07-22 12:18:50','/static/signatures/doc_6_step_1_06923c4e.png');
INSERT INTO "doc_approvals" VALUES(17,6,2,'deputy','Зам. Директора',NULL,NULL,NULL,NULL,NULL,'2026-07-22 12:18:50',NULL);
INSERT INTO "doc_approvals" VALUES(18,6,3,'director','Ген. Директор',NULL,NULL,NULL,NULL,NULL,'2026-07-22 12:18:50',NULL);
INSERT INTO "doc_approvals" VALUES(19,6,4,'accountant','Бухгалтер',NULL,NULL,NULL,NULL,NULL,'2026-07-22 12:18:50',NULL);
INSERT INTO "doc_approvals" VALUES(20,7,1,'aho','АХО / IT',NULL,NULL,NULL,NULL,NULL,'2026-07-30 05:27:52',NULL);
INSERT INTO "doc_approvals" VALUES(21,7,2,'deputy','Зам. Директора',NULL,NULL,NULL,NULL,NULL,'2026-07-30 05:27:52',NULL);
INSERT INTO "doc_approvals" VALUES(22,7,3,'director','Ген. Директор',NULL,NULL,NULL,NULL,NULL,'2026-07-30 05:27:52',NULL);
INSERT INTO "doc_approvals" VALUES(23,7,4,'accountant','Бухгалтер',NULL,NULL,NULL,NULL,NULL,'2026-07-30 05:27:52',NULL);
CREATE TABLE doc_comments (
        id       INTEGER PRIMARY KEY AUTOINCREMENT,
        doc_id   INTEGER NOT NULL,
        user_id  INTEGER,
        user_name TEXT,
        user_role TEXT,
        text     TEXT NOT NULL,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (doc_id) REFERENCES documents(id)
    );
INSERT INTO "doc_comments" VALUES(1,2,1,'Администратор','superadmin','Документ создан и ожидает проверки АХО.','2026-05-10 11:49:00');
INSERT INTO "doc_comments" VALUES(2,1,1,'Администратор','superadmin','✅ Согласовал: Администратор (Супер-Админ)','2026-05-10 11:49:13');
INSERT INTO "doc_comments" VALUES(3,1,1,'Администратор','superadmin','Передано на согласование: Зам. Директора','2026-05-10 11:49:13');
INSERT INTO "doc_comments" VALUES(4,1,1,'Администратор','superadmin','✅ Согласовал: Администратор (Супер-Админ)','2026-05-10 13:54:01');
INSERT INTO "doc_comments" VALUES(5,1,1,'Администратор','superadmin','Передано на согласование: Ген. Директор','2026-05-10 13:54:01');
INSERT INTO "doc_comments" VALUES(6,1,1,'Администратор','superadmin','✅ Согласовал: Администратор (Супер-Админ)','2026-05-10 13:54:03');
INSERT INTO "doc_comments" VALUES(7,1,1,'Администратор','superadmin','Передано на согласование: Бухгалтер','2026-05-10 13:54:03');
INSERT INTO "doc_comments" VALUES(8,1,1,'Администратор','superadmin','✅ Согласовал: Администратор (Супер-Админ)','2026-05-10 13:54:06');
INSERT INTO "doc_comments" VALUES(9,1,1,'Администратор','superadmin','🎉 Документ полностью согласован и утверждён!','2026-05-10 13:54:06');
INSERT INTO "doc_comments" VALUES(10,3,1,'Администратор','superadmin','Документ создан. Ожидает согласования: АХО / IT','2026-05-12 05:58:14');
INSERT INTO "doc_comments" VALUES(11,4,1,'Администратор','superadmin','Документ создан. Ожидает согласования: АХО / IT','2026-05-13 13:38:26');
INSERT INTO "doc_comments" VALUES(12,4,2,'АХО Менеджер','aho','✅ Согласовал: АХО Менеджер (АХО / IT)','2026-05-19 06:36:53');
INSERT INTO "doc_comments" VALUES(13,4,2,'АХО Менеджер','aho','Передано на согласование: Ген. Директор','2026-05-19 06:36:53');
INSERT INTO "doc_comments" VALUES(14,3,2,'АХО Менеджер','aho','✅ Согласовал: АХО Менеджер (АХО / IT)','2026-05-19 06:37:01');
INSERT INTO "doc_comments" VALUES(15,3,2,'АХО Менеджер','aho','Передано на согласование: Зам. Директора','2026-05-19 06:37:01');
INSERT INTO "doc_comments" VALUES(16,4,8,'Ген. Директор','director','✅ Согласовал: Ген. Директор (Ген. Директор)','2026-05-29 19:52:08');
INSERT INTO "doc_comments" VALUES(17,4,8,'Ген. Директор','director','Передано на согласование: Бухгалтер','2026-05-29 19:52:08');
INSERT INTO "doc_comments" VALUES(18,4,13,'Главный Бухгалтер','accountant','✅ Согласовал: Главный Бухгалтер (Бухгалтер)','2026-05-29 23:35:15');
INSERT INTO "doc_comments" VALUES(19,4,13,'Главный Бухгалтер','accountant','🎉 Документ полностью согласован и утверждён!','2026-05-29 23:35:15');
INSERT INTO "doc_comments" VALUES(20,4,13,'Главный Бухгалтер','accountant','✅ Согласовал: Главный Бухгалтер (Бухгалтер)','2026-05-29 23:35:37');
INSERT INTO "doc_comments" VALUES(21,4,13,'Главный Бухгалтер','accountant','🎉 Документ полностью согласован и утверждён!','2026-05-29 23:35:37');
INSERT INTO "doc_comments" VALUES(22,3,7,'Зам. Директора','deputy','✅ Согласовал: Зам. Директора (Зам. Директора)','2026-06-01 06:40:52');
INSERT INTO "doc_comments" VALUES(23,3,7,'Зам. Директора','deputy','Передано на согласование: Ген. Директор','2026-06-01 06:40:52');
INSERT INTO "doc_comments" VALUES(25,6,1,'Администратор','superadmin','Документ создан. Ожидает согласования: АХО / IT','2026-07-22 12:18:50');
INSERT INTO "doc_comments" VALUES(26,4,1,'Администратор','superadmin','🖨️ Документ распечатан и закрыт: Администратор','2026-07-28 10:31:13');
INSERT INTO "doc_comments" VALUES(27,6,22,'Cобиров Бегзод','aho','✅ Согласовал: Cобиров Бегзод (АХО / IT)','2026-07-29 15:34:04');
INSERT INTO "doc_comments" VALUES(28,6,22,'Cобиров Бегзод','aho','Передано на согласование: Зам. Директора','2026-07-29 15:34:04');
INSERT INTO "doc_comments" VALUES(29,7,22,'Cобиров Бегзод','aho','Документ создан. Ожидает согласования: АХО / IT','2026-07-30 05:27:52');
CREATE TABLE documents (
        id          INTEGER PRIMARY KEY AUTOINCREMENT,
        doc_number  TEXT UNIQUE,
        doc_type    TEXT NOT NULL,
        title       TEXT NOT NULL,
        description TEXT,
        priority    TEXT DEFAULT 'normal',
        status      TEXT DEFAULT 'draft',
        current_step INTEGER DEFAULT 0,
        current_role TEXT,
        created_by_id   INTEGER,
        created_by_name TEXT,
        item_id     INTEGER,
        item_inv    TEXT,
        department  TEXT,
        amount      REAL,
        attachments TEXT DEFAULT '[]',
        created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        updated_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        deadline    DATE,
        closed_at   TIMESTAMP
    , signature TEXT, employee_id INTEGER, employee_name TEXT, company_id INTEGER DEFAULT 1, pending_role TEXT);
INSERT INTO "documents" VALUES(1,'ЗАЯ-2026-0001','doc_request','Закупка MacBook M3','Для нового дизайнера','high','approved',4,NULL,1,'Администратор',NULL,NULL,NULL,NULL,'[]','2026-05-10 11:49:00','2026-05-10 13:54:06',NULL,NULL,NULL,NULL,NULL,1,NULL);
INSERT INTO "documents" VALUES(2,'СПИ-2026-0002','write_off','Списание серверов Dell','Устаревшее оборудование','medium','pending',1,'aho',1,'Администратор',NULL,NULL,NULL,NULL,'[]','2026-05-10 11:49:00','2026-05-10 11:49:00',NULL,NULL,NULL,NULL,NULL,1,NULL);
INSERT INTO "documents" VALUES(3,'ЗАЯ-2026-0002','doc_request','wqdqwdqw','dwq','low','pending',3,'director',1,'Администратор',NULL,NULL,NULL,NULL,'[]','2026-05-12 05:58:14','2026-06-01 06:40:52','2026-05-13',NULL,NULL,NULL,NULL,1,NULL);
INSERT INTO "documents" VALUES(4,'СПС-2026-0002','write_off','d','ds','urgent','printed',3,NULL,1,'Администратор',NULL,NULL,NULL,NULL,'[]','2026-05-13 13:38:26','2026-07-28 10:31:13','2026-05-27','2026-07-28 10:31:13',NULL,NULL,NULL,1,NULL);
INSERT INTO "documents" VALUES(6,'ЗАЯ-2026-0003','doc_request','231321','213321321','normal','pending',2,NULL,1,'Администратор',NULL,NULL,'',NULL,'[]','2026-07-22 12:18:50','2026-07-29 15:34:04',NULL,NULL,NULL,NULL,NULL,1,'deputy');
INSERT INTO "documents" VALUES(7,'ЗАЯ-2026-0004','doc_request','cd','cd','normal','pending',1,NULL,22,'Cобиров Бегзод',NULL,NULL,'ИТ',NULL,'[]','2026-07-30 05:27:52','2026-07-30 05:27:52',NULL,NULL,NULL,NULL,NULL,1,'aho');
CREATE TABLE equipment_templates (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            description TEXT,
            items_json TEXT DEFAULT '[]',
            created_by TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP);
CREATE TABLE history (
        id INTEGER PRIMARY KEY AUTOINCREMENT, item_id INTEGER, user_name TEXT, 
        action TEXT, old_val TEXT, ts TIMESTAMP DEFAULT CURRENT_TIMESTAMP, user_id INTEGER, field TEXT, new_val TEXT);
INSERT INTO "history" VALUES(45,72,'Администратор','Выдано: Cобиров Бегзод',NULL,'2026-06-29 08:00:46',1,NULL,NULL);
INSERT INTO "history" VALUES(46,83,'Администратор','Выдано: Рахманов Зухриддин',NULL,'2026-06-29 08:10:16',1,NULL,NULL);
INSERT INTO "history" VALUES(47,54,'Администратор','Выдано: Рахманов Зухриддин',NULL,'2026-06-29 08:10:16',1,NULL,NULL);
INSERT INTO "history" VALUES(48,63,'Администратор','Выдано: Рахманов Зухриддин',NULL,'2026-06-29 08:10:16',1,NULL,NULL);
INSERT INTO "history" VALUES(49,93,'Администратор','Выдано: Рахманов Зухриддин',NULL,'2026-06-29 08:10:16',1,NULL,NULL);
INSERT INTO "history" VALUES(50,97,'Администратор','Выдано: Рахманов Зухриддин',NULL,'2026-06-29 08:10:16',1,NULL,NULL);
INSERT INTO "history" VALUES(51,107,'Администратор','Добавлен',NULL,'2026-07-09 09:58:54',1,NULL,NULL);
INSERT INTO "history" VALUES(52,94,'Cобиров Бегзод','Выдано: Зафар Рахматуллаев',NULL,'2026-07-28 08:29:07',22,NULL,NULL);
INSERT INTO "history" VALUES(54,94,'Cобиров Бегзод','Прикреплён актив',NULL,'2026-07-29 15:27:42',22,NULL,'КЛВ-001');
INSERT INTO "history" VALUES(55,73,'Cобиров Бегзод','Прикреплён к комплекту',NULL,'2026-07-29 15:27:42',22,NULL,'ТЛФ-002');
INSERT INTO "history" VALUES(56,94,'Cобиров Бегзод','Прикреплён актив',NULL,'2026-07-29 15:27:46',22,NULL,'ДРГ-004');
INSERT INTO "history" VALUES(57,86,'Cобиров Бегзод','Прикреплён к комплекту',NULL,'2026-07-29 15:27:46',22,NULL,'ТЛФ-002');
INSERT INTO "history" VALUES(58,94,'Cобиров Бегзод','Прикреплён актив',NULL,'2026-07-29 15:27:50',22,NULL,'МОН-006');
INSERT INTO "history" VALUES(59,59,'Cобиров Бегзод','Прикреплён к комплекту',NULL,'2026-07-29 15:27:50',22,NULL,'ТЛФ-002');
INSERT INTO "history" VALUES(60,94,'Cобиров Бегзод','Прикреплён актив',NULL,'2026-07-29 15:28:01',22,NULL,'УДЛ-001');
INSERT INTO "history" VALUES(61,97,'Cобиров Бегзод','Прикреплён к комплекту',NULL,'2026-07-29 15:28:01',22,NULL,'ТЛФ-002');
CREATE TABLE inventory_checks (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            session_id INTEGER NOT NULL,
            item_id INTEGER NOT NULL,
            status TEXT DEFAULT 'pending',
            checked_by_id INTEGER,
            checked_by_name TEXT,
            photo TEXT,
            note TEXT,
            checked_at TIMESTAMP, condition TEXT,
            FOREIGN KEY (session_id) REFERENCES inventory_sessions(id));
CREATE TABLE inventory_sessions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            title TEXT NOT NULL,
            status TEXT DEFAULT 'active',
            created_by_id INTEGER,
            created_by_name TEXT,
            department TEXT,
            total_items INTEGER DEFAULT 0,
            checked_items INTEGER DEFAULT 0,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            completed_at TIMESTAMP, company_id INTEGER DEFAULT 1);
INSERT INTO "inventory_sessions" VALUES(1,'цу','active',1,'Администратор',NULL,26,1,'2026-05-10 12:36:53',NULL,1);
INSERT INTO "inventory_sessions" VALUES(2,'Test Session','active',1,'Администратор',NULL,30,3,'2026-05-12 08:14:25',NULL,1);
CREATE TABLE issuances (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            employee_id INTEGER NOT NULL, employee_name TEXT NOT NULL,
            issued_by INTEGER NOT NULL, issued_by_name TEXT NOT NULL,
            items_json TEXT NOT NULL, status TEXT DEFAULT 'pending',
            signature TEXT,
            confirmed_at TIMESTAMP, created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP, doc_id INTEGER, request_id INTEGER, company_id INTEGER DEFAULT 1);
INSERT INTO "issuances" VALUES(6,22,'Cобиров Бегзод',1,'Администратор','[72]','confirmed',NULL,'2026-06-29 08:00:54','2026-06-29 08:00:46',NULL,NULL,1);
INSERT INTO "issuances" VALUES(7,24,'Рахманов Зухриддин',1,'Администратор','[83, 54, 63, 93, 97]','confirmed',NULL,'2026-06-29 08:10:23','2026-06-29 08:10:16',NULL,NULL,1);
INSERT INTO "issuances" VALUES(8,29,'Зафар Рахматуллаев',22,'Cобиров Бегзод','[94]','confirmed',NULL,'2026-07-28 08:29:13','2026-07-28 08:29:07',NULL,NULL,1);
CREATE TABLE items (
        id INTEGER PRIMARY KEY AUTOINCREMENT, place TEXT, inv_num TEXT UNIQUE, 
        category TEXT, model TEXT, serial_num TEXT, room TEXT, employee TEXT, 
        status TEXT, condition TEXT, notes TEXT, purchase_price REAL, purchase_date DATE, employee_id INTEGER, photo TEXT, supplier TEXT, warranty_until DATE, check_date TEXT, company_id INTEGER DEFAULT 1, bundle_parent_id INTEGER);
INSERT INTO "items" VALUES(50,'Склад','НТБ-001','Ноутбук','Lenovo Legion 5','—','Склад','Хусниддин','Занято','Хорошее','Поступила: 2026-06-28. 22,964,000 сум / USD 1,766.46',1766.46,'2026-06-28',23,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(51,'Склад','НТБ-002','Ноутбук','Lenovo Legion 5','—','Склад','Ислом Рахматов','Занято','Хорошее','Поступила: 2026-06-28. 22,964,000 сум / USD 1,766.46',1766.46,'2026-06-28',21,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(52,'Склад','НТБ-003','Ноутбук','Lenovo Legion 5','—','Склад','Cобиров Бегзод','Занято','Хорошее','Поступила: 2026-06-28. 22,964,000 сум / USD 1,766.46',1766.46,'2026-06-28',22,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(53,'Склад','НТБ-004','Ноутбук','Lenovo Legion 5','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 22,964,000 сум / USD 1,766.46',1766.46,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(54,'Склад','МОН-001','Монитор','Монитор 27" 2K (QHD) 200Hz IPS','—','Склад','Рахманов Зухриддин','Занято','Хорошее','Поступила: 2026-06-28. 2,870,000 сум / USD 220.77',220.77,'2026-06-28',24,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(55,'Склад','МОН-002','Монитор','Монитор 27" 2K (QHD) 200Hz IPS','—','Склад','Хусниддин','Занято','Хорошее','Поступила: 2026-06-28. 2,870,000 сум / USD 220.77',220.77,'2026-06-28',23,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(56,'Склад','МОН-003','Монитор','Монитор 27" 2K (QHD) 200Hz IPS','—','Склад','Ислом Рахматов','Занято','Хорошее','Поступила: 2026-06-28. 2,870,000 сум / USD 220.77',220.77,'2026-06-28',21,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(57,'Склад','МОН-004','Монитор','Монитор 27" 2K (QHD) 200Hz IPS','—','Склад','Cобиров Бегзод','Занято','Хорошее','Поступила: 2026-06-28. 2,870,000 сум / USD 220.77',220.77,'2026-06-28',22,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(58,'Склад','МОН-005','Монитор','Монитор 27" 2K (QHD) 200Hz IPS','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 2,870,000 сум / USD 220.77',220.77,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(59,'Склад','МОН-006','Монитор','Монитор 27" 2K (QHD) 200Hz IPS','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 2,870,000 сум / USD 220.77',220.77,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,94);
INSERT INTO "items" VALUES(60,'Склад','МОН-007','Монитор','Монитор 27" 2K (QHD) 200Hz IPS','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 2,870,000 сум / USD 220.77',220.77,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(61,'Склад','МОН-008','Монитор','Монитор 27" 2K (QHD) 200Hz IPS','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 2,870,000 сум / USD 220.77',220.77,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(62,'Склад','МОН-009','Монитор','Монитор 27" 2K (QHD) 200Hz IPS','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 2,870,000 сум / USD 220.77',220.77,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(63,'Склад','НШК-001','Наушники','Наушники Razer Kraken X Lite','—','Склад','Рахманов Зухриддин','Занято','Хорошее','Поступила: 2026-06-28. 545,000 сум / USD 41.92',41.92,'2026-06-28',24,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(64,'Склад','НШК-002','Наушники','Наушники Razer Kraken X Lite','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 545,000 сум / USD 41.92',41.92,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(65,'Склад','НШК-003','Наушники','Наушники Razer Kraken X Lite','—','Склад','Хусниддин','Занято','Хорошее','Поступила: 2026-06-28. 545,000 сум / USD 41.92',41.92,'2026-06-28',23,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(66,'Склад','НШК-004','Наушники','Наушники Razer Kraken X Lite','—','Склад','Ислом Рахматов','Занято','Хорошее','Поступила: 2026-06-28. 545,000 сум / USD 41.92',41.92,'2026-06-28',21,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(67,'Склад','НШК-005','Наушники','Наушники Razer Kraken X Lite','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 545,000 сум / USD 41.92',41.92,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(68,'Склад','НШК-006','Наушники','Наушники Razer Kraken X Lite','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 545,000 сум / USD 41.92',41.92,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(69,'Склад','НШК-007','Наушники','Наушники Razer Kraken X Lite','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 545,000 сум / USD 41.92',41.92,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(70,'Склад','НШК-008','Наушники','Наушники Razer Kraken X Lite','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 545,000 сум / USD 41.92',41.92,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(71,'Склад','НШК-009','Наушники','Наушники Razer Kraken X Lite','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 545,000 сум / USD 41.92',41.92,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(72,'Склад','НШК-010','Наушники','Наушники Razer Kraken X Lite','—','Склад','Cобиров Бегзод','Занято','Хорошее','Поступила: 2026-06-28. 545,000 сум / USD 41.92',41.92,'2026-06-28',22,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(73,'Склад','КЛВ-001','Клавиатура','Клавиатура + мышь набор HP','—','Склад','Хусниддин','Занято','Хорошее','Поступила: 2026-06-28. 717,000 сум / USD 55.15',55.15,'2026-06-28',23,NULL,'Список закупки',NULL,'2026-06-28',1,94);
INSERT INTO "items" VALUES(74,'Склад','КЛВ-002','Клавиатура','Клавиатура + мышь набор HP','—','Склад','Ислом Рахматов','Занято','Хорошее','Поступила: 2026-06-28. 717,000 сум / USD 55.15',55.15,'2026-06-28',21,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(75,'Склад','КЛВ-003','Клавиатура','Клавиатура + мышь набор HP','—','Склад','Cобиров Бегзод','Занято','Хорошее','Поступила: 2026-06-28. 717,000 сум / USD 55.15',55.15,'2026-06-28',22,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(76,'Склад','КЛВ-004','Клавиатура','Клавиатура + мышь набор Aula','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 970,000 сум / USD 74.62',74.62,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(77,'Склад','КЛВ-005','Клавиатура','Клавиатура + мышь набор Aula','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 970,000 сум / USD 74.62',74.62,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(78,'Склад','КЛВ-006','Клавиатура','Клавиатура + мышь набор Aula','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 970,000 сум / USD 74.62',74.62,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(79,'Склад','КЛВ-007','Клавиатура','Клавиатура + мышь набор Aula','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 970,000 сум / USD 74.62',74.62,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(80,'Склад','КЛВ-008','Клавиатура','Клавиатура + мышь набор Aula','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 970,000 сум / USD 74.62',74.62,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(81,'Склад','КЛВ-009','Клавиатура','Клавиатура + мышь набор Aula','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 970,000 сум / USD 74.62',74.62,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(82,'Склад','КЛВ-010','Клавиатура','Клавиатура + мышь набор Aula','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 970,000 сум / USD 74.62',74.62,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(83,'Склад','ДРГ-001','Другое','Dok-станция / хаб MX1 Type-C','—','Склад','Рахманов Зухриддин','Занято','Хорошее','Поступила: 2026-06-28. 780,000 сум / USD 60.00',60.0,'2026-06-28',24,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(84,'Склад','ДРГ-002','Другое','Dok-станция / хаб MX1 Type-C','—','Склад','Хусниддин','Занято','Хорошее','Поступила: 2026-06-28. 780,000 сум / USD 60.00',60.0,'2026-06-28',23,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(85,'Склад','ДРГ-003','Другое','Dok-станция / хаб MX1 Type-C','—','Склад','Ислом Рахматов','Занято','Хорошее','Поступила: 2026-06-28. 780,000 сум / USD 60.00',60.0,'2026-06-28',21,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(86,'Склад','ДРГ-004','Другое','Dok-станция / хаб MX1 Type-C','—','Склад','Cобиров Бегзод','Занято','Хорошее','Поступила: 2026-06-28. 780,000 сум / USD 60.00',60.0,'2026-06-28',22,NULL,'Список закупки',NULL,'2026-06-28',1,94);
INSERT INTO "items" VALUES(87,'Склад','ДРГ-005','Другое','Dok-станция / хаб MX1 Type-C','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 780,000 сум / USD 60.00',60.0,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(88,'Склад','ДРГ-006','Другое','Dok-станция / хаб MX1 Type-C','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 780,000 сум / USD 60.00',60.0,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(89,'Склад','ДРГ-007','Другое','Dok-станция / хаб MX1 Type-C','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 780,000 сум / USD 60.00',60.0,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(90,'Склад','ДРГ-008','Другое','Dok-станция / хаб MX1 Type-C','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 780,000 сум / USD 60.00',60.0,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(91,'Склад','ДРГ-009','Другое','Dok-станция / хаб MX1 Type-C','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 780,000 сум / USD 60.00',60.0,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(92,'Склад','ДРГ-010','Другое','Dok-станция / хаб MX1 Type-C','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 780,000 сум / USD 60.00',60.0,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(93,'Склад','ТЛФ-001','Телефон','Samsung DeX + S11 Ultra','—','Склад','Рахманов Зухриддин','Занято','Хорошее','Поступила: 2026-06-28. 15,000,000 сум / USD 1,153.85',1153.85,'2026-06-28',24,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(94,'Склад','ТЛФ-002','Телефон','Samsung DeX + S11 Ultra','—','Склад','Зафар Рахматуллаев','Занято','Хорошее','Поступила: 2026-06-28. 15,000,000 сум / USD 1,153.85',1153.85,'2026-06-28',29,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(95,'Склад','ТЛФ-003','Телефон','Samsung DeX + S11 Ultra','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 15,000,000 сум / USD 1,153.85',1153.85,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(96,'Склад','МОН-010','Монитор','Монитор Asus ProArt PA279CRV','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 8,678,000 сум / USD 667.54',667.54,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(97,'Склад','УДЛ-001','Удлинитель','Удлинитель Pilot','—','Склад','Рахманов Зухриддин','Занято','Хорошее','Поступила: 2026-06-28. 430,000 сум / USD 33.08',33.08,'2026-06-28',24,NULL,'Список закупки',NULL,'2026-06-28',1,94);
INSERT INTO "items" VALUES(98,'Склад','УДЛ-002','Удлинитель','Удлинитель Pilot','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 430,000 сум / USD 33.08',33.08,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(99,'Склад','УДЛ-003','Удлинитель','Удлинитель Pilot','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 430,000 сум / USD 33.08',33.08,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(100,'Склад','УДЛ-004','Удлинитель','Удлинитель Pilot','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 430,000 сум / USD 33.08',33.08,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(101,'Склад','УДЛ-005','Удлинитель','Удлинитель Pilot','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 430,000 сум / USD 33.08',33.08,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(102,'Склад','УДЛ-006','Удлинитель','Удлинитель Pilot','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 430,000 сум / USD 33.08',33.08,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(103,'Склад','УДЛ-007','Удлинитель','Удлинитель Pilot','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 430,000 сум / USD 33.08',33.08,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(104,'Склад','УДЛ-008','Удлинитель','Удлинитель Pilot','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 430,000 сум / USD 33.08',33.08,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(105,'Склад','УДЛ-009','Удлинитель','Удлинитель Pilot','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 430,000 сум / USD 33.08',33.08,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
INSERT INTO "items" VALUES(106,'Склад','УДЛ-010','Удлинитель','Удлинитель Pilot','—','Склад','—','Свободно','Хорошее','Поступила: 2026-06-28. 430,000 сум / USD 33.08',33.08,'2026-06-28',NULL,NULL,'Список закупки',NULL,'2026-06-28',1,NULL);
CREATE TABLE login_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER, email TEXT, success INTEGER,
            ip TEXT, user_agent TEXT,
            ts TIMESTAMP DEFAULT CURRENT_TIMESTAMP);
INSERT INTO "login_log" VALUES(1,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-09 21:06:09');
INSERT INTO "login_log" VALUES(2,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-09 21:06:27');
INSERT INTO "login_log" VALUES(3,1,'admin@tracko.uz',1,'127.0.0.1','Python-urllib/3.11','2026-05-09 21:09:02');
INSERT INTO "login_log" VALUES(4,1,'admin@tracko.uz',1,'127.0.0.1','Python-urllib/3.11','2026-05-09 21:09:52');
INSERT INTO "login_log" VALUES(5,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-09 21:11:28');
INSERT INTO "login_log" VALUES(6,1,'admin@tracko.uz',1,'127.0.0.1','Python-urllib/3.11','2026-05-09 21:12:19');
INSERT INTO "login_log" VALUES(7,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 11:36:40');
INSERT INTO "login_log" VALUES(8,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 11:54:59');
INSERT INTO "login_log" VALUES(9,1,'admin@tracko.uz',1,'127.0.0.1','curl/8.5.0','2026-05-10 12:03:40');
INSERT INTO "login_log" VALUES(10,2,'aho@tracko.uz',1,'127.0.0.1','curl/8.5.0','2026-05-10 12:03:40');
INSERT INTO "login_log" VALUES(11,4,'emp1@tracko.uz',1,'127.0.0.1','curl/8.5.0','2026-05-10 12:03:41');
INSERT INTO "login_log" VALUES(12,8,'director@tracko.uz',1,'127.0.0.1','curl/8.5.0','2026-05-10 12:03:41');
INSERT INTO "login_log" VALUES(13,1,'admin@tracko.uz',1,'127.0.0.1','curl/8.5.0','2026-05-10 12:04:01');
INSERT INTO "login_log" VALUES(14,1,'admin@tracko.uz',1,'127.0.0.1','curl/8.5.0','2026-05-10 12:04:17');
INSERT INTO "login_log" VALUES(15,1,'admin@tracko.uz',1,'127.0.0.1','curl/8.5.0','2026-05-10 12:08:58');
INSERT INTO "login_log" VALUES(16,1,'admin@tracko.uz',1,'127.0.0.1','curl/8.5.0','2026-05-10 12:11:49');
INSERT INTO "login_log" VALUES(17,NULL,'islomtopg@gmail.com',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 12:38:23');
INSERT INTO "login_log" VALUES(18,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 12:38:27');
INSERT INTO "login_log" VALUES(19,10,'dir@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:00:48');
INSERT INTO "login_log" VALUES(20,10,'dir@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:00:51');
INSERT INTO "login_log" VALUES(21,10,'dir@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:00:55');
INSERT INTO "login_log" VALUES(22,8,'director@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:01:02');
INSERT INTO "login_log" VALUES(23,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:01:08');
INSERT INTO "login_log" VALUES(24,12,'a@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:32:52');
INSERT INTO "login_log" VALUES(25,12,'a@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:33:32');
INSERT INTO "login_log" VALUES(26,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:34:30');
INSERT INTO "login_log" VALUES(27,12,'a@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:34:35');
INSERT INTO "login_log" VALUES(28,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:34:39');
INSERT INTO "login_log" VALUES(29,12,'a@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:34:47');
INSERT INTO "login_log" VALUES(30,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:34:55');
INSERT INTO "login_log" VALUES(31,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:35:10');
INSERT INTO "login_log" VALUES(32,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:35:15');
INSERT INTO "login_log" VALUES(33,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:35:26');
INSERT INTO "login_log" VALUES(34,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:35:29');
INSERT INTO "login_log" VALUES(35,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-10 14:40:30');
INSERT INTO "login_log" VALUES(36,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 05:56:12');
INSERT INTO "login_log" VALUES(37,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 05:56:16');
INSERT INTO "login_log" VALUES(38,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 05:56:18');
INSERT INTO "login_log" VALUES(39,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 05:56:19');
INSERT INTO "login_log" VALUES(40,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 05:57:20');
INSERT INTO "login_log" VALUES(41,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 05:57:41');
INSERT INTO "login_log" VALUES(42,NULL,'axo@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:08:54');
INSERT INTO "login_log" VALUES(43,NULL,'axo@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:08:55');
INSERT INTO "login_log" VALUES(44,NULL,'axo@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:09:05');
INSERT INTO "login_log" VALUES(45,NULL,'axo@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:09:07');
INSERT INTO "login_log" VALUES(46,NULL,'axo@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:09:10');
INSERT INTO "login_log" VALUES(47,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:10:56');
INSERT INTO "login_log" VALUES(48,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:13:28');
INSERT INTO "login_log" VALUES(49,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:14:37');
INSERT INTO "login_log" VALUES(50,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:14:42');
INSERT INTO "login_log" VALUES(51,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:15:34');
INSERT INTO "login_log" VALUES(52,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:15:54');
INSERT INTO "login_log" VALUES(53,NULL,'dir@tracko.uzz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:19:13');
INSERT INTO "login_log" VALUES(54,NULL,'dir@tracko.uzz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:19:16');
INSERT INTO "login_log" VALUES(55,NULL,'dir@tracko.uzz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:19:17');
INSERT INTO "login_log" VALUES(56,NULL,'dir@tracko.uzz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:19:26');
INSERT INTO "login_log" VALUES(57,NULL,'dir@tracko.uzz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:19:27');
INSERT INTO "login_log" VALUES(58,10,'dir@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:39:30');
INSERT INTO "login_log" VALUES(59,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:40:34');
INSERT INTO "login_log" VALUES(60,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 06:47:29');
INSERT INTO "login_log" VALUES(61,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.33.1','2026-05-12 07:18:07');
INSERT INTO "login_log" VALUES(62,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.33.1','2026-05-12 07:19:10');
INSERT INTO "login_log" VALUES(63,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.33.1','2026-05-12 07:19:16');
INSERT INTO "login_log" VALUES(64,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.33.1','2026-05-12 07:19:48');
INSERT INTO "login_log" VALUES(65,NULL,'test@example.com',0,'127.0.0.1','python-requests/2.33.1','2026-05-12 07:24:34');
INSERT INTO "login_log" VALUES(66,NULL,'test@example.com',0,'127.0.0.1','python-requests/2.33.1','2026-05-12 07:24:34');
INSERT INTO "login_log" VALUES(67,NULL,'test@example.com',0,'127.0.0.1','python-requests/2.33.1','2026-05-12 07:24:34');
INSERT INTO "login_log" VALUES(68,NULL,'test@example.com',0,'127.0.0.1','python-requests/2.33.1','2026-05-12 07:24:34');
INSERT INTO "login_log" VALUES(69,NULL,'test@example.com',0,'127.0.0.1','python-requests/2.33.1','2026-05-12 07:24:34');
INSERT INTO "login_log" VALUES(70,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-12 07:39:18');
INSERT INTO "login_log" VALUES(71,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-12 07:39:29');
INSERT INTO "login_log" VALUES(72,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-12 07:39:35');
INSERT INTO "login_log" VALUES(73,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-12 07:39:44');
INSERT INTO "login_log" VALUES(74,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 10:32:12');
INSERT INTO "login_log" VALUES(75,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 10:32:17');
INSERT INTO "login_log" VALUES(76,9,'accountant@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 10:55:33');
INSERT INTO "login_log" VALUES(77,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 10:55:56');
INSERT INTO "login_log" VALUES(78,17,'qa_user_ce1f8002@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 10:56:11');
INSERT INTO "login_log" VALUES(79,17,'qa_user_ce1f8002@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 10:56:13');
INSERT INTO "login_log" VALUES(80,5,'emp2@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 10:56:31');
INSERT INTO "login_log" VALUES(81,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 10:56:49');
INSERT INTO "login_log" VALUES(82,1,'admin@tracko.uz',0,'10.16.12.46','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 10:59:26');
INSERT INTO "login_log" VALUES(83,1,'admin@tracko.uz',1,'10.16.12.46','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 10:59:29');
INSERT INTO "login_log" VALUES(84,1,'admin@tracko.uz',1,'10.16.12.46','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 10:59:37');
INSERT INTO "login_log" VALUES(85,1,'admin@tracko.uz',0,'10.16.12.46','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 10:59:50');
INSERT INTO "login_log" VALUES(86,1,'admin@tracko.uz',1,'10.16.12.46','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 10:59:59');
INSERT INTO "login_log" VALUES(87,1,'admin@tracko.uz',1,'10.16.12.46','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 11:00:07');
INSERT INTO "login_log" VALUES(88,5,'emp2@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 11:00:49');
INSERT INTO "login_log" VALUES(89,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-12 11:01:19');
INSERT INTO "login_log" VALUES(90,1,'admin@tracko.uz',1,'10.16.12.47','Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Mobile Safari/537.36','2026-05-12 11:02:03');
INSERT INTO "login_log" VALUES(91,1,'admin@tracko.uz',0,'10.16.12.47','Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Mobile Safari/537.36','2026-05-12 11:02:12');
INSERT INTO "login_log" VALUES(92,1,'admin@tracko.uz',1,'10.16.12.47','Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Mobile Safari/537.36','2026-05-12 11:02:18');
INSERT INTO "login_log" VALUES(93,NULL,'islomtopg@gmail.com',0,'10.16.12.47','Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Mobile Safari/537.36','2026-05-12 11:02:23');
INSERT INTO "login_log" VALUES(94,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-13 06:05:24');
INSERT INTO "login_log" VALUES(95,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-13 06:10:24');
INSERT INTO "login_log" VALUES(96,7,'deputy@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-13 08:57:40');
INSERT INTO "login_log" VALUES(97,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-13 08:58:01');
INSERT INTO "login_log" VALUES(98,8,'director@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-13 13:45:20');
INSERT INTO "login_log" VALUES(99,5,'emp2@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-13 13:51:38');
INSERT INTO "login_log" VALUES(100,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-13 13:59:30');
INSERT INTO "login_log" VALUES(101,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-18 13:16:19');
INSERT INTO "login_log" VALUES(102,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-19 06:34:59');
INSERT INTO "login_log" VALUES(103,12,'a@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-19 06:36:07');
INSERT INTO "login_log" VALUES(104,2,'aho@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-19 06:36:42');
INSERT INTO "login_log" VALUES(105,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36 Edg/148.0.0.0','2026-05-19 06:38:01');
INSERT INTO "login_log" VALUES(106,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 17:27:45');
INSERT INTO "login_log" VALUES(107,8,'director@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 19:51:40');
INSERT INTO "login_log" VALUES(108,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 22:31:54');
INSERT INTO "login_log" VALUES(109,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:29:51');
INSERT INTO "login_log" VALUES(110,2,'aho@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:31:08');
INSERT INTO "login_log" VALUES(111,3,'hr@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:32:31');
INSERT INTO "login_log" VALUES(112,3,'hr@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:32:34');
INSERT INTO "login_log" VALUES(113,3,'hr@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:32:41');
INSERT INTO "login_log" VALUES(114,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:33:24');
INSERT INTO "login_log" VALUES(115,5,'emp2@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:34:34');
INSERT INTO "login_log" VALUES(116,13,'acc@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:35:04');
INSERT INTO "login_log" VALUES(117,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:36:11');
INSERT INTO "login_log" VALUES(118,1,'admin@tracko.uz',0,'192.168.1.103','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:40:38');
INSERT INTO "login_log" VALUES(119,1,'admin@tracko.uz',0,'192.168.1.103','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:40:41');
INSERT INTO "login_log" VALUES(120,1,'admin@tracko.uz',0,'192.168.1.103','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:40:43');
INSERT INTO "login_log" VALUES(121,1,'admin@tracko.uz',0,'192.168.1.103','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:40:50');
INSERT INTO "login_log" VALUES(122,1,'admin@tracko.uz',0,'192.168.1.103','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:40:53');
INSERT INTO "login_log" VALUES(123,1,'admin@tracko.uz',0,'192.168.1.103','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:40:55');
INSERT INTO "login_log" VALUES(124,1,'admin@tracko.uz',0,'192.168.1.103','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:41:02');
INSERT INTO "login_log" VALUES(125,1,'admin@tracko.uz',0,'192.168.1.103','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:41:05');
INSERT INTO "login_log" VALUES(126,1,'admin@tracko.uz',0,'192.168.1.103','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:41:07');
INSERT INTO "login_log" VALUES(127,NULL,'islomtopg@gmail.com',0,'192.168.1.103','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:41:16');
INSERT INTO "login_log" VALUES(128,NULL,'islomtopg@gmail.com',0,'192.168.1.103','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:41:17');
INSERT INTO "login_log" VALUES(129,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:41:44');
INSERT INTO "login_log" VALUES(130,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:41:49');
INSERT INTO "login_log" VALUES(131,2,'aho@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:44:41');
INSERT INTO "login_log" VALUES(132,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:45:35');
INSERT INTO "login_log" VALUES(133,2,'aho@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:50:11');
INSERT INTO "login_log" VALUES(134,5,'emp2@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-29 23:51:06');
INSERT INTO "login_log" VALUES(135,2,'aho@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 00:09:21');
INSERT INTO "login_log" VALUES(136,3,'hr@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 00:14:19');
INSERT INTO "login_log" VALUES(137,2,'aho@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 00:14:34');
INSERT INTO "login_log" VALUES(138,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 00:15:31');
INSERT INTO "login_log" VALUES(139,2,'aho@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 09:03:31');
INSERT INTO "login_log" VALUES(140,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 09:04:03');
INSERT INTO "login_log" VALUES(141,1,'admin@tracko.uz',1,'192.168.1.101','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 09:21:04');
INSERT INTO "login_log" VALUES(142,2,'aho@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 09:57:51');
INSERT INTO "login_log" VALUES(143,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 11:02:57');
INSERT INTO "login_log" VALUES(144,1,'admin@tracko.uz',0,'192.168.1.101','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 14:21:47');
INSERT INTO "login_log" VALUES(145,1,'admin@tracko.uz',1,'192.168.1.101','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 14:21:54');
INSERT INTO "login_log" VALUES(146,6,'auditor@tracko.uz',1,'192.168.1.101','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 14:28:05');
INSERT INTO "login_log" VALUES(147,1,'admin@tracko.uz',1,'192.168.1.101','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 14:28:27');
INSERT INTO "login_log" VALUES(148,7,'deputy@tracko.uz',1,'192.168.1.101','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 14:29:01');
INSERT INTO "login_log" VALUES(149,1,'admin@tracko.uz',1,'192.168.1.101','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 14:29:19');
INSERT INTO "login_log" VALUES(150,1,'admin@tracko.uz',1,'192.168.1.101','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 14:54:45');
INSERT INTO "login_log" VALUES(151,1,'admin@tracko.uz',1,'192.168.1.101','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 16:27:59');
INSERT INTO "login_log" VALUES(152,7,'deputy@tracko.uz',1,'192.168.1.101','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 16:28:39');
INSERT INTO "login_log" VALUES(153,1,'admin@tracko.uz',1,'192.168.1.101','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-30 16:28:56');
INSERT INTO "login_log" VALUES(154,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-31 09:15:12');
INSERT INTO "login_log" VALUES(155,2,'aho@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-31 09:16:43');
INSERT INTO "login_log" VALUES(156,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-31 09:22:36');
INSERT INTO "login_log" VALUES(157,2,'aho@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-31 09:23:02');
INSERT INTO "login_log" VALUES(158,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-31 10:00:32');
INSERT INTO "login_log" VALUES(159,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-31 11:04:01');
INSERT INTO "login_log" VALUES(160,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-31 11:04:07');
INSERT INTO "login_log" VALUES(161,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-05-31 11:04:11');
INSERT INTO "login_log" VALUES(162,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-06-01 06:24:39');
INSERT INTO "login_log" VALUES(163,13,'acc@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-06-01 06:25:14');
INSERT INTO "login_log" VALUES(164,NULL,'dep@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-06-01 06:35:04');
INSERT INTO "login_log" VALUES(165,7,'deputy@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-06-01 06:35:07');
INSERT INTO "login_log" VALUES(166,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','2026-06-01 06:42:46');
INSERT INTO "login_log" VALUES(167,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36','2026-06-28 17:55:50');
INSERT INTO "login_log" VALUES(168,1,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36','2026-06-28 17:55:52');
INSERT INTO "login_log" VALUES(169,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36','2026-06-28 17:55:55');
INSERT INTO "login_log" VALUES(170,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36','2026-06-28 18:51:03');
INSERT INTO "login_log" VALUES(171,1,'admin@tracko.uz',1,'10.16.12.60','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36','2026-06-29 07:01:38');
INSERT INTO "login_log" VALUES(172,24,'zuxriddin@tracko.uz',1,'10.16.12.60','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36','2026-06-29 08:10:39');
INSERT INTO "login_log" VALUES(173,1,'admin@tracko.uz',1,'10.16.12.60','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36','2026-06-29 08:11:15');
INSERT INTO "login_log" VALUES(174,1,'admin@tracko.uz',0,'127.0.0.1','python-requests/2.34.2','2026-07-09 09:58:21');
INSERT INTO "login_log" VALUES(175,1,'admin@tracko.uz',0,'127.0.0.1','python-requests/2.34.2','2026-07-09 09:58:28');
INSERT INTO "login_log" VALUES(176,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-09 09:58:54');
INSERT INTO "login_log" VALUES(177,1,'admin@tracko.uz',1,'172.22.108.60','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 YaBrowser/26.4.0.0 Safari/537.36','2026-07-09 10:01:03');
INSERT INTO "login_log" VALUES(178,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-09 10:41:24');
INSERT INTO "login_log" VALUES(179,23,'xusniddin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 YaBrowser/26.6.0.0 Safari/537.36','2026-07-13 06:48:25');
INSERT INTO "login_log" VALUES(180,23,'xusniddin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 YaBrowser/26.6.0.0 Safari/537.36','2026-07-13 06:48:32');
INSERT INTO "login_log" VALUES(181,23,'xusniddin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 YaBrowser/26.6.0.0 Safari/537.36','2026-07-13 06:48:47');
INSERT INTO "login_log" VALUES(182,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 YaBrowser/26.6.0.0 Safari/537.36','2026-07-13 06:50:29');
INSERT INTO "login_log" VALUES(183,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-13 07:40:38');
INSERT INTO "login_log" VALUES(184,NULL,'admin@asseto.uz',0,'127.0.0.1','python-requests/2.34.2','2026-07-13 09:11:54');
INSERT INTO "login_log" VALUES(185,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-13 09:12:32');
INSERT INTO "login_log" VALUES(186,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-13 09:15:34');
INSERT INTO "login_log" VALUES(187,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-13 09:16:59');
INSERT INTO "login_log" VALUES(188,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-13 09:21:25');
INSERT INTO "login_log" VALUES(189,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-13 09:39:18');
INSERT INTO "login_log" VALUES(190,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-13 09:39:35');
INSERT INTO "login_log" VALUES(191,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-13 09:39:49');
INSERT INTO "login_log" VALUES(192,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-13 09:50:14');
INSERT INTO "login_log" VALUES(193,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-13 09:56:58');
INSERT INTO "login_log" VALUES(194,NULL,'admin@asseto.uz',0,'127.0.0.1','Python-urllib/3.14','2026-07-17 12:55:19');
INSERT INTO "login_log" VALUES(195,1,'admin@tracko.uz',1,'127.0.0.1','Python-urllib/3.14','2026-07-17 12:55:37');
INSERT INTO "login_log" VALUES(196,1,'admin@tracko.uz',1,'127.0.0.1','Python-urllib/3.14','2026-07-17 12:55:41');
INSERT INTO "login_log" VALUES(197,1,'admin@tracko.uz',1,'127.0.0.1','Python-urllib/3.14','2026-07-17 12:55:48');
INSERT INTO "login_log" VALUES(198,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-17 12:56:11');
INSERT INTO "login_log" VALUES(199,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-17 12:56:22');
INSERT INTO "login_log" VALUES(200,1,'admin@tracko.uz',1,'127.0.0.1','Python-urllib/3.14','2026-07-17 12:56:55');
INSERT INTO "login_log" VALUES(201,1,'admin@tracko.uz',1,'127.0.0.1','Python-urllib/3.14','2026-07-17 12:57:13');
INSERT INTO "login_log" VALUES(202,1,'admin@tracko.uz',1,'127.0.0.1','Python-urllib/3.14','2026-07-17 12:57:49');
INSERT INTO "login_log" VALUES(203,1,'admin@tracko.uz',1,'127.0.0.1','Python-urllib/3.14','2026-07-17 12:58:11');
INSERT INTO "login_log" VALUES(204,1,'admin@tracko.uz',1,'127.0.0.1','Python-urllib/3.14','2026-07-17 12:58:17');
INSERT INTO "login_log" VALUES(205,1,'admin@tracko.uz',1,'127.0.0.1','Python-urllib/3.14','2026-07-17 12:58:37');
INSERT INTO "login_log" VALUES(206,1,'admin@tracko.uz',1,'127.0.0.1','Python-urllib/3.14','2026-07-17 12:59:16');
INSERT INTO "login_log" VALUES(207,1,'admin@tracko.uz',1,'127.0.0.1','Python-urllib/3.14','2026-07-17 12:59:48');
INSERT INTO "login_log" VALUES(208,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-17 13:00:19');
INSERT INTO "login_log" VALUES(209,1,'admin@tracko.uz',1,'10.16.12.96','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-20 11:06:36');
INSERT INTO "login_log" VALUES(210,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 12:11:03');
INSERT INTO "login_log" VALUES(211,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 12:14:12');
INSERT INTO "login_log" VALUES(212,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 12:14:36');
INSERT INTO "login_log" VALUES(213,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 12:15:44');
INSERT INTO "login_log" VALUES(214,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 12:17:03');
INSERT INTO "login_log" VALUES(215,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 12:17:21');
INSERT INTO "login_log" VALUES(216,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 12:17:45');
INSERT INTO "login_log" VALUES(217,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 12:17:58');
INSERT INTO "login_log" VALUES(218,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 12:20:26');
INSERT INTO "login_log" VALUES(219,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 13:18:15');
INSERT INTO "login_log" VALUES(220,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 13:20:00');
INSERT INTO "login_log" VALUES(221,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 13:21:18');
INSERT INTO "login_log" VALUES(222,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 13:22:43');
INSERT INTO "login_log" VALUES(223,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 13:46:01');
INSERT INTO "login_log" VALUES(224,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 13:47:50');
INSERT INTO "login_log" VALUES(225,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 13:48:20');
INSERT INTO "login_log" VALUES(226,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 14:00:38');
INSERT INTO "login_log" VALUES(227,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 14:01:38');
INSERT INTO "login_log" VALUES(228,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 14:12:41');
INSERT INTO "login_log" VALUES(229,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 14:13:02');
INSERT INTO "login_log" VALUES(230,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 14:14:07');
INSERT INTO "login_log" VALUES(231,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 14:14:26');
INSERT INTO "login_log" VALUES(232,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-20 14:15:05');
INSERT INTO "login_log" VALUES(233,1,'admin@tracko.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-21 08:47:20');
INSERT INTO "login_log" VALUES(234,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-21 08:49:12');
INSERT INTO "login_log" VALUES(235,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-21 10:06:50');
INSERT INTO "login_log" VALUES(236,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-21 10:07:20');
INSERT INTO "login_log" VALUES(237,1,'admin@tracko.uz',1,'127.0.0.1','python-requests/2.34.2','2026-07-21 11:32:03');
INSERT INTO "login_log" VALUES(238,NULL,'admin@asseto.uz',0,'10.16.12.96','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-22 11:00:52');
INSERT INTO "login_log" VALUES(239,1,'admin@tracko.uz',1,'10.16.12.96','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-22 11:01:52');
INSERT INTO "login_log" VALUES(240,22,'bekzod@asseto.uz',0,'10.16.12.96','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-22 12:20:35');
INSERT INTO "login_log" VALUES(241,NULL,'admin@tracko.uz',0,'10.16.12.96','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-22 12:20:44');
INSERT INTO "login_log" VALUES(242,1,'admin@asseto.uz',1,'10.16.12.96','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-22 12:20:49');
INSERT INTO "login_log" VALUES(243,22,'bekzod@asseto.uz',1,'10.16.12.96','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-22 12:21:24');
INSERT INTO "login_log" VALUES(244,1,'admin@asseto.uz',1,'10.16.12.96','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-22 12:22:23');
INSERT INTO "login_log" VALUES(245,22,'bekzod@asseto.uz',1,'10.16.12.96','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-22 12:23:51');
INSERT INTO "login_log" VALUES(246,1,'admin@asseto.uz',1,'10.16.12.96','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-22 12:24:34');
INSERT INTO "login_log" VALUES(247,NULL,'xusniddin@asseto.uz',0,'10.16.12.96','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-22 12:25:52');
INSERT INTO "login_log" VALUES(248,23,'xusniddin@tracko.uz',1,'10.16.12.96','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-22 12:25:56');
INSERT INTO "login_log" VALUES(249,1,'admin@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-23 11:13:56');
INSERT INTO "login_log" VALUES(250,27,'rustam@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-23 11:21:08');
INSERT INTO "login_log" VALUES(251,NULL,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-23 17:25:41');
INSERT INTO "login_log" VALUES(252,NULL,'admin@tracko.uz',0,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-23 17:25:42');
INSERT INTO "login_log" VALUES(253,1,'admin@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-23 17:25:52');
INSERT INTO "login_log" VALUES(254,1,'admin@asseto.uz',0,'10.16.12.87','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-23 17:39:14');
INSERT INTO "login_log" VALUES(255,1,'admin@asseto.uz',1,'10.16.12.87','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-23 17:39:20');
INSERT INTO "login_log" VALUES(256,22,'bekzod@asseto.uz',1,'10.16.12.87','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-23 17:42:16');
INSERT INTO "login_log" VALUES(257,1,'admin@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-27 12:25:02');
INSERT INTO "login_log" VALUES(258,1,'admin@asseto.uz',1,'127.0.0.1','curl/8.7.1','2026-07-27 17:50:47');
INSERT INTO "login_log" VALUES(259,22,'bekzod@asseto.uz',0,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-27 17:57:23');
INSERT INTO "login_log" VALUES(260,22,'bekzod@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-27 17:57:27');
INSERT INTO "login_log" VALUES(261,1,'admin@asseto.uz',1,'127.0.0.1','curl/8.7.1','2026-07-28 07:47:22');
INSERT INTO "login_log" VALUES(262,32,'qa_test_employee@test.local',1,'127.0.0.1','curl/8.7.1','2026-07-28 08:20:31');
INSERT INTO "login_log" VALUES(263,22,'bekzod@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-28 08:27:11');
INSERT INTO "login_log" VALUES(264,1,'admin@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-28 08:28:18');
INSERT INTO "login_log" VALUES(265,22,'bekzod@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-28 08:28:49');
INSERT INTO "login_log" VALUES(266,1,'admin@asseto.uz',1,'127.0.0.1','curl/8.7.1','2026-07-28 09:32:44');
INSERT INTO "login_log" VALUES(267,1,'admin@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-28 10:30:52');
INSERT INTO "login_log" VALUES(268,27,'rustam@asseto.uz',0,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-28 12:49:32');
INSERT INTO "login_log" VALUES(269,27,'rustam@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-28 12:49:36');
INSERT INTO "login_log" VALUES(270,1,'admin@asseto.uz',1,'127.0.0.1','curl/8.7.1','2026-07-29 13:55:09');
INSERT INTO "login_log" VALUES(271,1,'admin@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-29 15:14:21');
INSERT INTO "login_log" VALUES(272,22,'bekzod@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-29 15:15:13');
INSERT INTO "login_log" VALUES(273,1,'admin@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-29 15:34:17');
INSERT INTO "login_log" VALUES(274,27,'rustam@asseto.uz',0,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-29 15:34:47');
INSERT INTO "login_log" VALUES(275,27,'rustam@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-29 15:34:50');
INSERT INTO "login_log" VALUES(276,27,'rustam@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-30 05:21:29');
INSERT INTO "login_log" VALUES(277,1,'admin@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-30 05:22:10');
INSERT INTO "login_log" VALUES(278,NULL,'begzod@asseto.uz',0,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-30 05:23:15');
INSERT INTO "login_log" VALUES(279,22,'bekzod@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-30 05:23:21');
INSERT INTO "login_log" VALUES(280,1,'admin@asseto.uz',1,'127.0.0.1','Python-urllib/3.14','2026-07-30 05:38:51');
INSERT INTO "login_log" VALUES(281,1,'admin@asseto.uz',0,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-30 05:42:03');
INSERT INTO "login_log" VALUES(282,1,'admin@asseto.uz',1,'127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36','2026-07-30 05:42:12');
CREATE TABLE maintenance (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            item_id INTEGER NOT NULL,
            reported_by_id INTEGER,
            reported_by_name TEXT,
            description TEXT,
            priority TEXT DEFAULT 'medium',
            status TEXT DEFAULT 'pending',
            resolved_by TEXT,
            resolved_at TIMESTAMP,
            resolution TEXT,
            rejection_reason TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP, company_id INTEGER DEFAULT 1);
CREATE TABLE notifications (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER NOT NULL,
            type TEXT NOT NULL DEFAULT 'info',
            title TEXT NOT NULL,
            body TEXT,
            link TEXT,
            is_read INTEGER DEFAULT 0,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP);
CREATE TABLE office_acknowledgments (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            doc_id INTEGER REFERENCES office_docs(id) ON DELETE CASCADE,
            user_id INTEGER REFERENCES users(id),
            acked_at TIMESTAMP,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP);
CREATE TABLE office_doc_files (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            doc_id INTEGER REFERENCES office_docs(id) ON DELETE CASCADE,
            file_name TEXT NOT NULL,
            file_path TEXT NOT NULL,
            file_type TEXT DEFAULT 'attachment',
            uploaded_by INTEGER REFERENCES users(id),
            uploaded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP);
CREATE TABLE office_doc_history (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            doc_id INTEGER REFERENCES office_docs(id) ON DELETE CASCADE,
            user_id INTEGER REFERENCES users(id),
            action TEXT NOT NULL,
            comment TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP);
CREATE TABLE office_docs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            doc_type TEXT NOT NULL,
            reg_number TEXT UNIQUE,
            reg_date DATE DEFAULT (date('now')),
            sender_doc_number TEXT,
            sender_name TEXT,
            sender_doc_date DATE,
            contractor_id INTEGER REFERENCES contractors(id),
            recipient_name TEXT,
            title TEXT NOT NULL,
            description TEXT,
            status TEXT DEFAULT 'draft',
            priority TEXT DEFAULT 'normal',
            creator_id INTEGER REFERENCES users(id),
            assigned_to_id INTEGER REFERENCES users(id),
            deadline DATE,
            resolution TEXT,
            reply_to_id INTEGER REFERENCES office_docs(id),
            completed_at TIMESTAMP,
            archived_at TIMESTAMP,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP);
CREATE TABLE push_subscriptions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER NOT NULL,
            endpoint TEXT NOT NULL UNIQUE,
            p256dh TEXT NOT NULL,
            auth TEXT NOT NULL,
            user_agent TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE);
CREATE TABLE returns (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            employee_id INTEGER NOT NULL, employee_name TEXT NOT NULL,
            initiated_by INTEGER NOT NULL, initiated_by_name TEXT NOT NULL,
            items_json TEXT NOT NULL, photos_json TEXT,
            accepted_by INTEGER, accepted_by_name TEXT,
            status TEXT DEFAULT 'pending', signature TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            completed_at TIMESTAMP, company_id INTEGER DEFAULT 1);
CREATE TABLE revoked_tokens (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER NOT NULL,
            token_version INTEGER NOT NULL,
            revoked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP);
CREATE TABLE rooms (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    floor TEXT DEFAULT "1",
    wing TEXT,
    capacity INTEGER DEFAULT 0,
    responsible TEXT,
    description TEXT,
    color TEXT DEFAULT "#007AFF",
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP);
INSERT INTO "rooms" VALUES(1,'Кабинет директора','2','',4,'',NULL,'#FF3B30','2026-06-29 06:54:49');
INSERT INTO "rooms" VALUES(2,'Бухгалтерия','1','',6,'',NULL,'#34C759','2026-06-29 06:54:49');
INSERT INTO "rooms" VALUES(3,'IT / АХО','1','',8,'',NULL,'#007AFF','2026-06-29 06:54:49');
INSERT INTO "rooms" VALUES(4,'HR отдел','2','',4,'',NULL,'#AF52DE','2026-06-29 06:54:49');
INSERT INTO "rooms" VALUES(5,'Переговорная','2','',12,'',NULL,'#FF9500','2026-06-29 06:54:49');
INSERT INTO "rooms" VALUES(6,'Склад','1','',0,'',NULL,'#636366','2026-06-29 06:54:49');
INSERT INTO "rooms" VALUES(7,'Серверная','1','',2,'',NULL,'#5856D6','2026-06-29 06:54:49');
CREATE TABLE users (
        id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, email TEXT UNIQUE,
        password_hash TEXT, role TEXT, active INTEGER DEFAULT 1, 
        avatar_color TEXT, token_version INTEGER DEFAULT 0, doc_role TEXT, department TEXT, force_password_change INTEGER DEFAULT 0, totp_secret TEXT, totp_enabled INTEGER DEFAULT 0, telegram_chat_id TEXT, expires_at DATE, onboarding_done INTEGER DEFAULT 0, last_login TIMESTAMP, created_at TIMESTAMP, hire_date DATE, company_id INTEGER DEFAULT 1, manager_id INTEGER, position TEXT, phone TEXT);
INSERT INTO "users" VALUES(1,'Администратор','admin@asseto.uz','$2b$12$5oYzLXUFsAjW/egWUrBhTeHWTXq8d0M91BnuXOs6PcV86BB/XqIn6','superadmin',1,'#4F46E5',40,NULL,'',0,NULL,0,NULL,NULL,0,'2026-07-30 05:42:12','2026-05-12 07:09:19',NULL,1,NULL,NULL,NULL);
INSERT INTO "users" VALUES(21,'Ислом Рахматов','islom@tracko.uz','$2b$12$.w9W6TOF0jqA82iFH/1tP.DEAW8Ras7ouF././zAwUgSHLxjTy2Em','employee',1,'#007AFF',0,NULL,'ИТ',0,NULL,0,NULL,NULL,0,NULL,'2026-06-28T23:58:19.137051',NULL,1,24,'ИТ Специалист',NULL);
INSERT INTO "users" VALUES(22,'Cобиров Бегзод','bekzod@asseto.uz','$2b$12$X60ORJl3QWITrfQaneRKKurwJXoDalAE31iLAa.igqgkehiaMpWfG','aho',1,NULL,6,NULL,'ИТ',1,NULL,0,NULL,NULL,0,'2026-07-30 05:23:21',NULL,NULL,1,24,NULL,NULL);
INSERT INTO "users" VALUES(23,'Хусниддин','xusniddin@tracko.uz','$2b$12$e4QC0E6VwNtyWqOFLKdWIOGSrdsNMZg84wFPdt8dJTmT7DwVBfPUO','employee',1,NULL,2,NULL,'ИТ',1,NULL,0,NULL,NULL,0,'2026-07-22 12:25:56',NULL,NULL,1,24,NULL,NULL);
INSERT INTO "users" VALUES(24,'Рахманов Зухриддин','zuxriddin@tracko.uz','$2b$12$l1dq0Gp/u9zSvQn3V8YeKOH192n2KkzffjNdd5yPuI8xro6oaF7nW','department_head',1,NULL,1,NULL,'Руководители',1,NULL,0,NULL,NULL,0,'2026-06-29 08:10:39',NULL,NULL,1,29,NULL,NULL);
INSERT INTO "users" VALUES(25,'Test User','test99@test.com','$2b$12$4pPtQGyx4IybmiaMQ6Z4..yp81P6Jz/YPW0J5hFO.Folt6BSZDKR2','employee',0,NULL,0,NULL,'IT',1,NULL,0,NULL,NULL,0,NULL,NULL,NULL,1,NULL,NULL,NULL);
INSERT INTO "users" VALUES(26,'TestAdd','testadd_1784549463@test.com','$2b$12$q/SPEpm45Pk5bi8.4n8iYeRc6XE9I41oqf8F7/CBEhQaRuX7ZYpEG','employee',0,NULL,0,NULL,'IT',1,NULL,0,NULL,NULL,0,NULL,NULL,NULL,1,NULL,NULL,NULL);
INSERT INTO "users" VALUES(27,'Рустам','rustam@asseto.uz','$2b$12$S8zLkwyTXjjr.WjqOKv0suiKZyOcrmRM6i6k.ns6KX2Of4ZMZJnjm','accountant',1,NULL,1,NULL,'Бухгалтерия',1,NULL,0,NULL,NULL,0,'2026-07-30 05:21:29',NULL,NULL,1,29,NULL,NULL);
INSERT INTO "users" VALUES(28,'Test Final','testfinal_1784549823@test.com','$2b$12$ZuOo7SIFkJ7udnA1BkXTeOJye//xE.9MwUj9bAeeyuzRYekOM5.7y','employee',0,NULL,0,NULL,'IT',1,NULL,0,NULL,NULL,0,NULL,NULL,NULL,1,NULL,NULL,NULL);
INSERT INTO "users" VALUES(29,'Зафар Рахматуллаев','zafar@asseto.uz','$2b$12$TZDPat/TY.rpT5cUphb89OBfPs6KyT.GxQfkCCFobeqPgNhJt8XTC','director',1,NULL,0,NULL,'Руководители',0,NULL,0,NULL,NULL,0,NULL,NULL,NULL,1,NULL,NULL,NULL);
INSERT INTO "users" VALUES(30,'T1784550026','t1784550026@t.com','$2b$12$5PvH4HcRQmcRqeWkwlCDTOxDn9stUDZcxp2zRTkX4d2eFIlmvFJa6','employee',0,NULL,0,NULL,'',1,NULL,0,NULL,NULL,0,NULL,NULL,NULL,1,NULL,NULL,NULL);
INSERT INTO "users" VALUES(31,'Test Org New','testorgnew_1784553495@test.com','$2b$12$iuw5oDZVvdAed9rrQjrPve33lxKyZ0MVBWwkU05mo24bolOZCYqzW','employee',0,NULL,0,NULL,'IT',0,NULL,0,NULL,NULL,0,NULL,NULL,NULL,1,NULL,'Dev',NULL);
INSERT INTO "users" VALUES(32,'QA Test Employee','qa_test_employee@test.local','$2b$12$uYY/xSv4MeO9OHEc1ppk0eBLSVg4tHwng7enBPnvJeUQ8mSSR140G','employee',0,NULL,0,NULL,'',1,NULL,0,NULL,NULL,0,'2026-07-28 08:20:31',NULL,NULL,1,NULL,NULL,NULL);
CREATE INDEX idx_items_employee   ON items(employee);
CREATE INDEX idx_items_employee_id ON items(employee_id);
CREATE INDEX idx_items_room       ON items(room);
CREATE INDEX idx_items_category   ON items(category);
CREATE INDEX idx_items_status     ON items(status);
CREATE INDEX idx_items_condition  ON items(condition);
CREATE INDEX idx_history_item_id  ON history(item_id);
CREATE INDEX idx_history_ts       ON history(ts);
CREATE INDEX idx_users_email      ON users(email);
CREATE INDEX idx_users_active     ON users(active);
CREATE INDEX idx_maintenance_item ON maintenance(item_id);
CREATE INDEX idx_maintenance_status ON maintenance(status);
DELETE FROM "sqlite_sequence";
INSERT INTO "sqlite_sequence" VALUES('users',32);
INSERT INTO "sqlite_sequence" VALUES('login_log',282);
INSERT INTO "sqlite_sequence" VALUES('documents',7);
INSERT INTO "sqlite_sequence" VALUES('doc_approvals',23);
INSERT INTO "sqlite_sequence" VALUES('doc_comments',29);
INSERT INTO "sqlite_sequence" VALUES('items',108);
INSERT INTO "sqlite_sequence" VALUES('history',61);
INSERT INTO "sqlite_sequence" VALUES('maintenance',5);
INSERT INTO "sqlite_sequence" VALUES('asset_requests',3);
INSERT INTO "sqlite_sequence" VALUES('inventory_sessions',2);
INSERT INTO "sqlite_sequence" VALUES('inventory_checks',56);
INSERT INTO "sqlite_sequence" VALUES('issuances',8);
INSERT INTO "sqlite_sequence" VALUES('dismissals',3);
INSERT INTO "sqlite_sequence" VALUES('api_keys',1);
INSERT INTO "sqlite_sequence" VALUES('companies',1);
INSERT INTO "sqlite_sequence" VALUES('rooms',7);
COMMIT;
