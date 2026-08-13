# -*- coding: utf-8 -*-
"""
ASSETO — загрузка реальной техники (PostgreSQL).
1. Удаляет все старые мок-данные (items + связанные таблицы)
2. Вставляет реальную технику из списка закупки
"""
import os, sys

# Ensure we can import pg_compat from parent dir
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pg_compat as _db

from datetime import date

DATABASE_URL = os.environ.get(
    'DATABASE_URL',
    'postgresql://asseto:asseto@localhost:5432/asseto',
)

# Курс на июнь 2026: 1 USD ≈ 13 000 UZS
USD_RATE = 13_000

PREFIXES = {
    "Ноутбук":    "НТБ",
    "Монитор":    "МОН",
    "Кресло":     "КРС",
    "Стол":       "СТЛ",
    "Клавиатура": "КЛВ",
    "Мышь":       "МШ",
    "Принтер":    "ПРН",
    "Телефон":    "ТЛФ",
    "Другое":     "ДРГ",
}

# ─── РЕАЛЬНАЯ ТЕХНИКА (из списка закупки) ───────────────────────────────────
EQUIPMENT = [
    {"category": "Ноутбук",    "model": "Lenovo Legion 5",                "qty": 4,  "price_uzs": 22_964_000, "supplier": "Список закупки"},
    {"category": "Монитор",    "model": 'Монитор 27" 2K (QHD) 200Hz IPS',"qty": 9,  "price_uzs":  2_870_000, "supplier": "Список закупки"},
    {"category": "Другое",     "model": "Наушники Razer Kraken X Lite",   "qty": 10, "price_uzs":    545_000,  "supplier": "Список закупки"},
    {"category": "Клавиатура", "model": "Клавиатура + мышь набор HP",     "qty": 3,  "price_uzs":    717_000,  "supplier": "Список закупки"},
    {"category": "Клавиатура", "model": "Клавиатура + мышь набор Aula",   "qty": 7,  "price_uzs":    970_000,  "supplier": "Список закупки"},
    {"category": "Другое",     "model": "Dok-станция / хаб MX1 Type-C",   "qty": 10, "price_uzs":    780_000,  "supplier": "Список закупки"},
    {"category": "Телефон",    "model": "Samsung DeX + S11 Ultra",         "qty": 3,  "price_uzs": 15_000_000, "supplier": "Список закупки"},
    {"category": "Монитор",    "model": "Монитор Asus ProArt PA279CRV",    "qty": 1,  "price_uzs":  8_678_000, "supplier": "Список закупки"},
    {"category": "Другое",     "model": "Удлинитель Pilot",                "qty": 10, "price_uzs":    430_000,  "supplier": "Список закупки"},
]

TODAY = date.today().isoformat()
PURCHASE_DATE = "2026-06-28"


def uzs_to_usd(uzs: int) -> float:
    return round(uzs / USD_RATE, 2)


def next_inv(db, category: str, counters: dict) -> str:
    prefix = PREFIXES.get(category, "ДРГ")
    counters[prefix] = counters.get(prefix, 0) + 1
    return f"{prefix}-{counters[prefix]:03d}"


def main():
    conn = _db.connect(DATABASE_URL)
    db = conn

    print("=" * 60)
    print("ASSETO — Загрузка реальной техники (PostgreSQL)")
    print("=" * 60)

    # ── 1. Удалить все мок-данные ────────────────────────────────
    old = db.execute("SELECT COUNT(*) FROM items").fetchone()[0]
    print(f"\n[1] Удаляем {old} мок-записей...")

    tables = ["inventory_checks", "maintenance", "issuances", "returns",
              "asset_requests", "history"]
    for t in tables:
        try:
            n = db.execute(f"DELETE FROM {t}").rowcount
            if n:
                print(f"    {t}: удалено {n}")
        except Exception as e:
            print(f"    {t}: пропуск ({e})")

    deleted = db.execute("DELETE FROM items").rowcount
    print(f"    items: удалено {deleted}")
    db.commit()

    # Сбросить автоинкрементные счётчики (PostgreSQL)
    for seq_table in ["items", "history", "maintenance", "issuances",
                       "returns", "asset_requests", "inventory_checks"]:
        try:
            db.execute(f"ALTER SEQUENCE {seq_table}_id_seq RESTART WITH 1")
        except Exception:
            pass
    db.commit()

    print("    ✓ Мок-данные удалены\n")

    # ── 2. Вставить реальную технику ─────────────────────────────
    print("[2] Добавляем реальную технику...")
    counters = {}
    total_uzs = 0
    total_usd = 0
    inserted = 0

    for item in EQUIPMENT:
        cat   = item["category"]
        model = item["model"]
        qty   = item["qty"]
        price_uzs = item["price_uzs"]
        price_usd = uzs_to_usd(price_uzs)
        supplier  = item.get("supplier", "Список закупки")

        for i in range(qty):
            inv_num = next_inv(db, cat, counters)
            serial_num = "—"
            place = "Склад"
            room  = "Склад"
            employee    = "—"
            employee_id = None
            status    = "Свободно"
            condition = "Хорошее"
            notes = f"Поступила: {PURCHASE_DATE}. Цена: {price_uzs:,} сум / ${price_usd:,.2f}"

            db.execute(
                """INSERT INTO items
                   (place, inv_num, category, model, serial_num, room,
                    employee, employee_id, status, condition, check_date,
                    notes, photo, purchase_price, purchase_date, supplier, warranty_until)
                   VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
                (place, inv_num, cat, model, serial_num, room,
                 employee, employee_id, status, condition, TODAY,
                 notes, None,
                 price_usd,
                 PURCHASE_DATE,
                 supplier,
                 None)
            )
            inserted += 1

        total_uzs += price_uzs * qty
        total_usd += price_usd * qty
        print(f"    ✓ {qty}x {model} [{cat}] | {price_uzs:>13,} сум | ${price_usd:>8,.2f}/шт")

    db.commit()
    db.close()

    # ── 3. Итоги ─────────────────────────────────────────────────
    print()
    print("=" * 60)
    print(f"  Добавлено единиц техники: {inserted}")
    print(f"  Общая сумма (сум):        {total_uzs:>15,} сум")
    print(f"  Общая сумма (USD):        ${total_usd:>12,.2f}")
    print(f"  Курс конвертации:         1 USD = {USD_RATE:,} сум")
    print()
    print("  Следующие шаги:")
    print("  1. Запустить платформу:  python app.py")
    print("  2. Войти:                admin@asseto.uz / admin123")
    print("  3. Назначить сотрудников на каждую единицу")
    print("  4. Обновить серийные номера (если есть)")
    print("  5. Сменить пароль admin!")
    print("=" * 60)


if __name__ == "__main__":
    main()
