# SmartParking API — Парковочная система

> **Базовый URL:** `https://whirl.uz/api/`  
> **Локальный:** `http://10.0.1.206:5121/api/`

---

## 🔐 Авторизация

Все защищённые эндпоинты требуют JWT токен в заголовке:
```
Authorization: Bearer {accessToken}
```

### POST /Companies/login
Вход в систему. Возвращает `accessToken` + `refreshToken`.

```json
{
  "email": "admin@smartparking.uz",
  "password": "Qwerty123!"
}
```

Ответ:
```json
{
  "accessToken": "eyJhbG...",
  "refreshToken": "...",
  "accessTokenExpiration": "2026-08-04T00:00:00Z"
}
```

---

## 📊 Транзакции

### GET /Transactions
Список всех транзакций (требует авторизацию).

### GET /Transactions/{id}
Получить транзакцию по ID.

### POST /Transactions
Создать транзакцию (требует авторизацию).

```json
{
  "clientId": 1,
  "totalSum": 15000,
  "status": "company"
}
```

### POST /Transactions/{id}/complete
**Публичный.** Подтвердить оплату транзакции.

```
POST /api/Transactions/42/complete
```
Ответ: `{"id": 42, "status": "Transaction completed"}`

### POST /Transactions/{id}/fail
**Публичный.** Отменить транзакцию.

```
POST /api/Transactions/42/fail
```
Ответ: `{"id": 42, "status": "Transaction failed"}`

---

## 🅿️ Интеграция с Avto.itpanda.uz

### POST /Transactions/parking
**Публичный.** Принимает данные о парковке от `whirl.uz`.

```json
{
  "chekId": "CHK-12345",
  "avtoRaqam": "01A123AA",
  "kirish": "2026-08-07T10:00:00",
  "chiqish": "2026-08-07T11:30:00",
  "davomiyligi": "1h 30m",
  "jamiTolov": 8250
}
```

Ответ:
```json
{
  "id": 42,
  "chekId": "CHK-12345",
  "status": "created"
}
```

---

## 📱 QR-код

### GET /Qr/{id}?size=250
**Публичный.** Генерирует Click-совместимый QR-код для оплаты парковки.

```
GET /api/Qr/42?size=300
```

Ответ:
```json
{
  "transactionId": 42,
  "qrContent": "service_id=20050026merchant_id=198760026amount=8250000026transaction_param=42",
  "amount": 8250,
  "base64": "iVBORw0KGgoAAAANS...",
  "mimeType": "image/png"
}
```

---

## 📷 Dahua Интеграция (Камеры)

### POST /DahuaIntegration/receive-event
**Публичный (с webhook secret).** Принимает события от камер Dahua.

```
POST /api/DahuaIntegration/receive-event
X-Webhook-Secret: {secret}
```

```json
{
  "deviceId": "CAM-001",
  "plateNumber": "01A123AA",
  "eventType": "vehicle_enter",
  "timestamp": "2026-08-07T10:00:00Z",
  "imageUrl": "http://camera/snapshot.jpg"
}
```

### GET /DahuaIntegration/sessions?status=active
Активные парковочные сессии.

### GET /DahuaIntegration/events?page=1&pageSize=50
История событий с камер.

### POST /DahuaIntegration/open-barrier
Открыть шлагбаум.

```json
{
  "barrierId": "BARRIER-1",
  "reason": "manual_override"
}
```

### GET /DahuaIntegration/devices
Список устройств (камеры/шлагбаумы).

### GET /DahuaIntegration/vehicles?category=whitelist
Списки ТС (white/black list).

---

## 📈 Статусы платежей

| Статус | Описание |
|--------|----------|
| `New` | Новый |
| `Pending` | Ожидает оплаты |
| `Authorized` | Авторизован |
| `Completed` | Оплачен ✅ |
| `Cancelled` | Отменён |
| `Failed` | Ошибка ❌ |
| `Refunded` | Возврат |
| `Expired` | Просрочен |

---

## 🔄 Бизнес-процесс парковки

```
1. Камера → POST /DahuaIntegration/receive-event (въезд)
2. Камера → POST /DahuaIntegration/receive-event (выезд)
3. whirl.uz → POST /Transactions/parking (расчёт)
4. Пользователь → сканирует QR → GET /Qr/{id}
5. Пользователь → оплачивает → POST /Transactions/{id}/complete
6. Шлагбаум открывается → POST /DahuaIntegration/open-barrier
```
