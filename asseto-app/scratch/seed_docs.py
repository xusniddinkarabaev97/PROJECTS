import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import pg_compat as _db

DATABASE_URL = os.environ.get(
    'DATABASE_URL',
    'postgresql://asseto:asseto@localhost:5432/asseto',
)


def seed_docs():
    db = _db.connect(DATABASE_URL)

    db.execute("DELETE FROM documents")
    db.execute("DELETE FROM doc_approvals")
    db.execute("DELETE FROM doc_comments")
    # Reset sequences
    for seq in ['documents', 'doc_approvals', 'doc_comments']:
        try:
            db.execute(f"ALTER SEQUENCE {seq}_id_seq RESTART WITH 1")
        except Exception:
            pass

    # Add a sample document
    db.execute(
        """INSERT INTO documents
        (doc_number, doc_type, title, description, priority, status, current_step, pending_role, created_by_id, created_by_name)
        VALUES (?,?,?,?,?,?,?,?,?,?)""",
        ("ЗАЯ-2026-0001", "doc_request", "Закупка MacBook M3", "Для нового дизайнера", "high", "pending", 1, "aho", 1, "Администратор"),
    )

    db.execute(
        """INSERT INTO documents
        (doc_number, doc_type, title, description, priority, status, current_step, pending_role, created_by_id, created_by_name)
        VALUES (?,?,?,?,?,?,?,?,?,?)""",
        ("СПИ-2026-0002", "write_off", "Списание серверов Dell", "Устаревшее оборудование", "medium", "pending", 1, "aho", 1, "Администратор"),
    )

    doc_id = db.execute("SELECT currval(pg_get_serial_sequence('documents','id'))").fetchone()
    doc_id = list(doc_id.values())[0] if doc_id else 1

    # Add approvals
    approvals = [
        (doc_id, 1, "aho", "АХО / IT", None, None, None, None),
        (doc_id, 2, "deputy", "Зам. Директора", None, None, None, None),
        (doc_id, 3, "director", "Ген. Директор", None, None, None, None),
        (doc_id, 4, "accountant", "Бухгалтер", None, None, None, None),
    ]

    db.executemany(
        "INSERT INTO doc_approvals (doc_id, step, role, role_label, approver_id, approver_name, action, comment) VALUES (?,?,?,?,?,?,?,?)",
        approvals,
    )

    # Add a comment
    db.execute(
        "INSERT INTO doc_comments (doc_id, user_id, user_name, user_role, text) VALUES (?,?,?,?,?)",
        (doc_id, 1, "Администратор", "superadmin", "Документ создан и ожидает проверки АХО."),
    )

    db.commit()
    db.close()
    print("Sample document seeded.")


if __name__ == "__main__":
    seed_docs()
