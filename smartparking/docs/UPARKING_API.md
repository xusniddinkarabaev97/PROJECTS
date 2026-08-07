# SmartParking ↔ Dahua Интеграция (Финальная)

> **Webhook:** `https://whirl.uz/api/DahuaIntegration/events`  
> **Swagger:** `https://whirl.uz/smartparking/swagger/`

---

## 🔄 Бизнес-процесс

```
Dahua                   SmartParking                Click                Клиент
  |                         |                         |                    |
  |-- Выезд (plate+сумма)->|                         |                    |
  |                         |-- транзакция            |                    |
  |                         |-- QR-код (base64)       |                    |
  |<-- QR код --------------|                         |                    |
  |                         |                         |                    |
  |  (показывает QR клиенту)                          |                    |
  |                         |                         |                    |
  |                         |                    (клиент сканирует)        |
  |                         |                         |                    |
  |                         |<-- Prepare -------------|                    |
  |                         |-- валидация ----------->|                    |
  |                         |                         |                    |
  |                         |<-- Complete ------------|                    |
  |                         |-- статус = Paid         |                    |
  |                         |-- фискальный чек        |                    |
  |                         |                         |                    |
  |<-- {status: "paid"} ----|                         |                    |
  |                         |                         |                    |
  |-- Открыть шлагбаум                                |                    |
```

---

## 1. Dahua → SmartParking (выезд)

Dahua отправляет данные при выезде: номер авто + сумма к оплате.

```
POST https://whirl.uz/api/DahuaIntegration/events
Content-Type: application/json
X-Webhook-Secret: dss_webhook_secret_2026
```

**Запрос:**
```json
{
    "eventType": "ANPR",
    "channelId": 1,
    "plateNumber": "01A123AA",
    "direction": "exit",
    "timestamp": "2026-08-07T12:30:00Z",
    "amount": 15000,
    "fiscalData": {
        "receiptId": "DH-20260807-001",
        "entryTime": "2026-08-07T10:00:00Z",
        "exitTime": "2026-08-07T12:30:00Z",
        "duration": "2h 30m"
    }
}
```

**Ответ (с QR):**
```json
{
    "status": "completed",
    "sessionId": 156,
    "plateNumber": "01A123AA",
    "transactionId": 89,
    "parkingFee": 15000,
    "qrCodeBase64": "iVBORw0KGgoAAAANSUhEUgAA...",
    "qrContent": "service_id=2005&merchant_id=19876&amount=1500000&transaction_param=89",
    "barrierOpened": false
}
```

---

## 2. Click Prepare

```
POST https://whirl.uz/api/Transactions/click/prepare

{
    "click_trans_id": 20260807001,
    "service_id": 2005,
    "merchant_trans_id": "89",
    "amount": 1500000,
    "action": 0,
    "sign_time": "2026-08-07T12:31:00Z",
    "sign_string": "md5..."
}
```

Ответ: `{"error": 0, "error_note": "Success"}`

---

## 3. Click Complete + Уведомление Dahua

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
    "sign_time": "2026-08-07T12:32:00Z",
    "sign_string": "md5..."
}
```

Ответ:
```json
{
    "error": 0,
    "error_note": "Success",
    "fiscalReceiptId": "FP-20260807-00089",
    "fiscalStatus": "registered",
    "barrierOpened": true,
    "dahuaNotified": true
}
```

После Complete SmartParking автоматически отправляет статус в Dahua:

```
POST {dahua_callback_url}
Content-Type: application/json

{
    "transactionId": 89,
    "plateNumber": "01A123AA",
    "status": "paid",
    "fiscalReceiptId": "FP-20260807-00089"
}
```

---

## Поля запроса (от Dahua)

| Поле | Тип | Обязательно | Описание |
|------|-----|-------------|----------|
| `eventType` | string | Да | `ANPR` |
| `channelId` | int | Да | ID канала |
| `plateNumber` | string | Да | Госномер |
| `direction` | string | Да | Только `exit` |
| `timestamp` | datetime | Да | Время события |
| `amount` | decimal | Да | Сумма к оплате (UZS) |
| `fiscalData.receiptId` | string | Нет | ID фискального чека |
| `fiscalData.entryTime` | datetime | Нет | Время въезда |
| `fiscalData.exitTime` | datetime | Нет | Время выезда |
| `fiscalData.duration` | string | Нет | Длительность |

---

## Поля ответа (SmartParking → Dahua)

| Поле | Описание |
|------|----------|
| `status` | `completed` |
| `transactionId` | ID для оплаты |
| `parkingFee` | Сумма |
| `qrCodeBase64` | QR PNG в Base64 |
| `qrContent` | Данные для Click |

---

## Статус оплаты (SmartParking → Dahua callback)

| Поле | Значение |
|------|----------|
| `transactionId` | ID транзакции |
| `plateNumber` | Госномер |
| `status` | `paid` / `failed` |
| `fiscalReceiptId` | ID фискального чека |
