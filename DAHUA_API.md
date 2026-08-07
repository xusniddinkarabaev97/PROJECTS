> **Swagger:** `https://whirl.uz/Billing/swagger/`

# Dahua Camera Integration → QR → Click

> **SmartParking:** `https://whirl.uz/Billing/api/DahuaIntegration`  
> **Dahua DSS Server:** `http://10.0.1.XX:8080`

---

## 🔄 Полный цикл парковки

```
   Dahua               SmartParking              QR / Click
  Камера                 Сервер                  Платёж
    |                      |                        |
    |-- Въезд (ANPR) ---->|                        |
    | plate: 01A123AA     |-- Создать сессию       |
    |                      |-- Открыть шлагбаум     |
    |<-- barrier: open ----|                        |
    |                      |                        |
    |-- Выезд (ANPR) ---->|                        |
    |                      |-- Расчёт стоимости     |
    |                      |-- Создать транзакцию   |
    |                      |-- Генерировать QR ----->|
    |                      |<-- QR (base64 PNG) ----|
    |                      |                        |
    |                      |         Click Prepare  |
    |                      |<------------------------|
    |                      |-- Валидация ---------->|
    |                      |                        |
    |                      |        Click Complete  |
    |                      |<------------------------|
    |                      |-- Статус = Completed   |
    |                      |-- Открыть шлагбаум     |
    |<-- barrier: open ----|                        |
```

---

## 📷 1. Въезд — Dahua отправляет событие

Камера фиксирует номер при въезде, Dahua DSS отправляет вебхук.

```
POST https://whirl.uz/Billing/api/DahuaIntegration/events
Content-Type: application/json
X-Webhook-Secret: dss_webhook_secret_2026
```

**Запрос от Dahua:**
```json
{
  "eventType": "ANPR",
  "channelId": 1,
  "plateNumber": "01A123AA",
  "direction": "entry",
  "timestamp": "2026-08-07T10:00:00Z",
  "imageUrl": "http://10.0.1.50:8080/snapshot/20260807_100000.jpg",
  "vehicle": {
    "plateNumber": "01A123AA",
    "color": "white",
    "brand": "Chevrolet"
  }
}
```

**Ответ SmartParking:**
```json
{
  "status": "active",
  "sessionId": 156,
  "plate": "01A123AA",
  "category": "standard",
  "barrierOpened": true
}
```

---

## 💰 2. Выезд — Dahua отправляет событие

Камера фиксирует номер при выезде.

```
POST https://whirl.uz/Billing/api/DahuaIntegration/events
X-Webhook-Secret: dss_webhook_secret_2026
```

**Запрос:**
```json
{
  "eventType": "ANPR",
  "channelId": 1,
  "plateNumber": "01A123AA",
  "direction": "exit",
  "timestamp": "2026-08-07T12:30:00Z"
}
```

**Ответ SmartParking (с QR для оплаты):**
```json
{
  "status": "completed",
  "sessionId": 156,
  "parkingFee": 15000,
  "transactionId": 89,
  "qrCodeBase64": "iVBORw0KGgoAAAANSUhEUgAA...",
  "qrContent": "service_id=2005&merchant_id=19876&amount=1500000&transaction_param=89",
  "barrierOpened": false
}
```

---

## 📱 3. QR-код для Click

После выезда генерируется QR с данными для оплаты через Click.

```
GET https://whirl.uz/Billing/api/Qr/89
```

**Ответ:**
```json
{
  "transactionId": 89,
  "amount": 15000,
  "qrContent": "service_id=2005&merchant_id=19876&amount=1500000&transaction_param=89",
  "base64": "iVBORw0KGgoAAAANSUhEUgAA...",
  "mimeType": "image/png"
}
```

**Формат Click QR:**
```
service_id={serviceId}&merchant_id={merchantId}&amount={tiyins}&transaction_param={txnId}
```

Пример: `service_id=2005&merchant_id=19876&amount=1500000&transaction_param=89`

---

## 💳 4. Click — Prepare

Приложение Click сканирует QR и вызывает Prepare.

```
POST https://whirl.uz/Billing/api/Transactions/click/prepare
```

**Запрос от Click:**
```json
{
  "click_trans_id": 20260807001,
  "service_id": 2005,
  "click_paydoc_id": 5236147,
  "merchant_trans_id": "89",
  "amount": 1500000,
  "action": 0,
  "error": 0,
  "sign_time": "2026-08-07T12:30:01Z",
  "sign_string": "a1b2c3d4..."
}
```

**Ответ:**
```json
{
  "click_trans_id": 20260807001,
  "merchant_trans_id": "89",
  "merchant_prepare_id": 89,
  "error": 0,
  "error_note": "Success"
}
```

---

## ✅ 5. Click — Complete

После успешной оплаты Click вызывает Complete.

```
POST https://whirl.uz/Billing/api/Transactions/click/complete
```

**Запрос от Click:**
```json
{
  "click_trans_id": 20260807001,
  "service_id": 2005,
  "merchant_trans_id": "89",
  "merchant_prepare_id": 89,
  "amount": 1500000,
  "action": 1,
  "error": 0,
  "sign_time": "2026-08-07T12:31:00Z",
  "sign_string": "e5f6g7h8..."
}
```

**Ответ:**
```json
{
  "click_trans_id": 20260807001,
  "merchant_trans_id": "89",
  "merchant_confirm_id": 89,
  "error": 0,
  "error_note": "Success"
}
```

**После Complete:**
- Транзакция → `Completed`
- Шлагбаум открывается автоматически → `barrierOpened: true`

---

## 🔧 6. Ручное управление шлагбаумом

```
POST https://whirl.uz/Billing/api/DahuaIntegration/barrier/open
Authorization: Bearer {token}
```

```json
{
  "channelId": 1,
  "barrierChannel": 1,
  "reason": "manual_override"
}
```

---

## 📊 Статусы парковочной сессии

| Статус | Описание |
|--------|----------|
| `active` | Авто на парковке |
| `completed` | Выезд зафиксирован |
| `expired` | Истекло время |
| `cancelled` | Отменено |

---

## 🔗 Сводка эндпоинтов

| Метод | URL | Описание |
|-------|-----|----------|
| `POST` | `/api/DahuaIntegration/events` | Приём ANPR событий от Dahua |
| `POST` | `/api/DahuaIntegration/barrier/open` | Открыть шлагбаум |
| `GET` | `/api/DahuaIntegration/sessions` | Активные сессии |
| `GET` | `/api/DahuaIntegration/vehicles` | Списки ТС |
| `GET` | `/api/Qr/{id}` | QR-код для Click |
| `POST` | `/api/Transactions/click/prepare` | Click Prepare |
| `POST` | `/api/Transactions/click/complete` | Click Complete |
