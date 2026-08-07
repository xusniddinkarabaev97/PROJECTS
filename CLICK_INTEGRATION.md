# Click Payment Integration — SmartParking

> **Click API:** `https://api.click.uz/v2/`  
> **Умная парковка:** `https://whirl.uz/api/Transactions`

---

## 🔄 Бизнес-процесс Click

```
Пользователь                Click                   SmartParking
    |                          |                          |
    |-- Сканирует QR -------->|                          |
    |                          |-- POST /prepare ------->|  (валидация)
    |                          |<---- 200 OK ------------|
    |-- Оплачивает в Click -->|                          |
    |<-- Чек об оплате -------|                          |
    |                          |-- POST /complete ------>|  (подтверждение)
    |                          |<---- 200 OK ------------|
    |                          |                          |
    |<-- Шлагбаум открыт ------|                          |
```

---

## 1. Prepare (Click → SmartParking)

Click отправляет запрос для проверки возможности оплаты.

```
POST https://whirl.uz/api/Transactions/click/prepare
Content-Type: application/json
```

**Запрос от Click:**
```json
{
  "click_trans_id": "987654321",
  "service_id": "2005",
  "click_paydoc_id": "5236147",
  "merchant_trans_id": "42",
  "amount": "8250",
  "action": "0",
  "error": "0",
  "error_note": "",
  "sign_time": "2026-08-07T10:00:00",
  "sign_string": "a1b2c3d4...",
  "merchant_prepare_id": ""
}
```

**Ответ SmartParking:**
```json
{
  "click_trans_id": "987654321",
  "merchant_trans_id": "42",
  "merchant_prepare_id": "1",
  "error": "0",
  "error_note": "Success"
}
```

| Поле | Тип | Описание |
|------|-----|----------|
| `click_trans_id` | string | ID транзакции в Click |
| `service_id` | string | ID сервиса (2005 = парковка) |
| `click_paydoc_id` | string | Номер платёжного документа Click |
| `merchant_trans_id` | string | ID транзакции в SmartParking |
| `amount` | string | Сумма в тийинах (8250 = 8 250 UZS) |
| `action` | string | 0 = оплата, 1 = отмена |
| `sign_string` | string | MD5 подпись |
| `merchant_prepare_id` | int | ID в системе мерчанта |
| `error` | string | 0 = ОК, иначе код ошибки |

### Коды ошибок:
| Код | Значение |
|-----|----------|
| `0` | Успешно |
| `-1` | Ошибка подписи |
| `-2` | Транзакция не найдена |
| `-3` | Транзакция уже оплачена |
| `-4` | Транзакция отменена |
| `-5` | Неверная сумма |
| `-9` | Внутренняя ошибка |

---

## 2. Complete (Click → SmartParking)

Click подтверждает успешную оплату.

```
POST https://whirl.uz/api/Transactions/click/complete
Content-Type: application/json
```

**Запрос от Click:**
```json
{
  "click_trans_id": "987654321",
  "service_id": "2005",
  "click_paydoc_id": "5236147",
  "merchant_trans_id": "42",
  "merchant_prepare_id": "1",
  "amount": "8250",
  "action": "0",
  "error": "0",
  "error_note": "",
  "sign_time": "2026-08-07T10:01:00",
  "sign_string": "e5f6g7h8..."
}
```

**Ответ SmartParking:**
```json
{
  "click_trans_id": "987654321",
  "merchant_trans_id": "42",
  "merchant_confirm_id": "1",
  "error": "0",
  "error_note": "Success"
}
```

После успешного Complete:
- Транзакция помечается как `Completed`
- Шлагбаум открывается
- Статус возвращается в Click

---

## 3. Подпись (Sign String)

Click использует MD5 хеш для проверки подписи:

```
sign_string = MD5(
  click_trans_id +
  service_id +
  SECRET_KEY +
  merchant_trans_id +
  merchant_prepare_id +
  amount +
  action +
  sign_time
)
```

Где `SECRET_KEY` — секретный ключ, согласованный с Click.

---

## 4. Endpoint на стороне SmartParking

Требуется добавить контроллер:

```
POST /api/Transactions/click/prepare   — Prepare
POST /api/Transactions/click/complete  — Complete
```

**Алгоритм Prepare:**
1. Проверить подпись (sign_string)
2. Найти транзакцию по `merchant_trans_id`
3. Проверить сумму совпадает
4. Проверить статус = New/Pending
5. Сохранить `merchant_prepare_id`
6. Вернуть `error: "0"`

**Алгоритм Complete:**
1. Проверить подпись
2. Найти транзакцию
3. Обновить статус → Completed
4. Открыть шлагбаум (POST /DahuaIntegration/open-barrier)
5. Вернуть `error: "0"`
