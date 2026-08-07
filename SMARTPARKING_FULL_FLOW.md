# SmartParking — Полный цикл: Dahua → QR → Click → Фискализация → Шлагбаум

> **Swagger:** `https://whirl.uz/smartparking/swagger/`

---

## 🔄 Полная цепочка

```
Камера Dahua         SmartParking           Click              OFD/ФН
    |                     |                    |                   |
    |-- Въезд ANPR ----->|                    |                   |
    |                     |-- сессия создана   |                   |
    |<-- шлагбаум открыт -|                    |                   |
    |                     |                    |                   |
    |-- Выезд ANPR ----->|                    |                   |
    |                     |-- расчёт стоимости |                   |
    |                     |-- транзакция       |                   |
    |<-- QR код ----------|                    |                   |
    |                     |                    |                   |
    | (пользователь сканирует QR в Click)      |                   |
    |                     |                    |                   |
    |                     |<-- Prepare --------|                   |
    |                     |-- валидация ------>|                   |
    |                     |                    |                   |
    |                     |<-- Complete -------|                   |
    |                     |-- статус=Оплачен   |                   |
    |                     |-- фискальный чек -------------------->|
    |                     |-- открыть шлагбаум |                   |
    |<-- шлагбаум открыт -|                    |                   |
```

---

## 1. Въезд (Dahua → SmartParking)

```
POST https://whirl.uz/api/DahuaIntegration/events
X-Webhook-Secret: dss_webhook_secret_2026

{
    "eventType": "ANPR",
    "channelId": 1,
    "plateNumber": "01A123AA",
    "direction": "entry",
    "timestamp": "2026-08-07T10:00:00Z"
}
```

Ответ: `{"status":"active", "sessionId":156, "barrierOpened":true}`

---

## 2. Выезд + QR (Dahua → SmartParking)

```
POST https://whirl.uz/api/DahuaIntegration/events
X-Webhook-Secret: dss_webhook_secret_2026

{
    "eventType": "ANPR",
    "channelId": 1,
    "plateNumber": "01A123AA",
    "direction": "exit",
    "timestamp": "2026-08-07T12:30:00Z"
}
```

Ответ (с QR для оплаты):
```json
{
    "status": "completed",
    "sessionId": 156,
    "parkingFee": 15000,
    "transactionId": 89,
    "qrCodeBase64": "iVBORw0KGgo...",
    "qrContent": "service_id=2005&merchant_id=19876&amount=1500000&transaction_param=89"
}
```

---

## 3. Click Prepare

```
POST https://whirl.uz/api/Transactions/click/prepare

{
    "click_trans_id": 20260807001,
    "service_id": 2005,
    "click_paydoc_id": 5236147,
    "merchant_trans_id": "89",
    "amount": 1500000,
    "action": 0,
    "sign_time": "2026-08-07T12:30:01Z",
    "sign_string": "md5hash..."
}
```

Ответ: `{"click_trans_id":20260807001, "merchant_trans_id":"89", "error":0, "error_note":"Success"}`

---

## 4. Click Complete + Фискализация + Шлагбаум

```
POST https://whirl.uz/api/Transactions/click/complete

{
    "click_trans_id": 20260807001,
    "service_id": 2005,
    "merchant_trans_id": "89",
    "merchant_prepare_id": 89,
    "amount": 1500000,
    "action": 1,
    "error": 0,
    "sign_time": "2026-08-07T12:31:00Z",
    "sign_string": "md5hash..."
}
```

**Что происходит после Complete:**
1. ✅ Транзакция → статус `Completed`
2. ✅ Фискальный чек отправляется в ОФД
3. ✅ Шлагбаум открывается автоматически
4. ✅ Чек сохраняется в БД

Ответ:
```json
{
    "click_trans_id": 20260807001,
    "merchant_trans_id": "89",
    "merchant_confirm_id": 89,
    "fiscal_receipt_id": "FP-20260807-00089",
    "fiscal_status": "registered",
    "barrier_opened": true,
    "error": 0,
    "error_note": "Success"
}
```

---

## 5. Фискализация

Фискальный чек формируется автоматически после успешной оплаты.

**Данные чека:**
| Поле | Значение |
|------|----------|
| `receiptId` | `FP-{date}-{txnId}` |
| `amount` | Сумма оплаты |
| `service` | Парковка |
| `plateNumber` | Госномер |
| `entryTime` | Время въезда |
| `exitTime` | Время выезда |
| `duration` | Длительность |
| `paymentMethod` | Click |
| `fiscalStatus` | `registered` / `failed` |

---

## 📡 Webhook обратного вызова (опционально)

После оплаты SmartParking может отправить статус обратно в Dahua:

```
POST http://DAHUA_IP:8080/callback
Content-Type: application/json

{
    "transactionId": 89,
    "plateNumber": "01A123AA",
    "status": "paid",
    "fiscalReceiptId": "FP-20260807-00089",
    "barrierCommand": "open"
}
```

---

## 📊 Статусы транзакции

| Статус | Описание |
|--------|----------|
| `New` | Создана при выезде |
| `Pending` | Click Prepare выполнен |
| `Completed` | Оплачено + фискализировано ✅ |
| `Failed` | Ошибка оплаты ❌ |
| `Cancelled` | Отменено |
