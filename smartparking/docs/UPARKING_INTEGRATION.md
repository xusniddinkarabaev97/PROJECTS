# SmartParking — Интеграция UParking

> **Версия:** v1.0  
> **Сервер:** `https://whirl.uz`  
> **Swagger:** `https://whirl.uz/smartparking/swagger/`

---

## 🔄 Схема работы

```
┌──────────┐     ┌─────────────────┐     ┌──────────┐     ┌──────────┐
│ UParking │────►│  SmartParking   │────►│   QR     │────►│  Click   │
│ (выезд)  │     │  /api/billing/  │     │ водителю │     │ (оплата) │
└──────────┘     │     create      │     └──────────┘     └────┬─────┘
                 └────────┬────────┘                          │
                          │                                   │
                          ▼ callback                    подтверждение
                 ┌─────────────────┐                     оплаты
                 │    UParking     │◄───────────────────────┘
                 │  {callbackUrl}  │
                 └─────────────────┘
```

1. UParking при выезде отправляет данные → SmartParking
2. SmartParking возвращает QR-код
3. Водитель сканирует QR → оплачивает через Click
4. Click подтверждает оплату → SmartParking
5. SmartParking отправляет статус "оплачено" → UParking (callback)

---

## ➡️ Шаг 1. Создание платежа (UParking → SmartParking)

```
POST https://whirl.uz/api/billing/create
Content-Type: application/json
X-Billing-Secret: uparking-shared-secret-2026
```

**Запрос:**

| Поле | Тип | Обязательно | Описание |
|------|-----|:----------:|----------|
| `sessionId` | UUID | ✅ | ID платёжной сессии UParking |
| `parkingStart` | RFC 3339 | ✅ | Время въезда |
| `parkingEnd` | RFC 3339 | ✅ | Время выезда |
| `parkingTimeSeconds` | int | ✅ | Длительность парковки в секундах |
| `plateNo` | string | ✅ | Госномер автомобиля |
| `amount` | number | ✅ | Сумма к оплате (UZS) |
| `currency` | string | | `UZS` |
| `purpose` | string | | `Parking` |

```json
{
    "sessionId": "8f2c1a4e-9b7d-4c3a-8e21-0f5a6b7c8d90",
    "parkingStart": "2026-08-06T09:14:32.000+00:00",
    "parkingEnd": "2026-08-06T11:47:05.000+00:00",
    "parkingTimeSeconds": 9153,
    "plateNo": "01A123BC",
    "amount": 15000,
    "currency": "UZS",
    "purpose": "Parking"
}
```

**Ответ:**

| Поле | Тип | Описание |
|------|-----|----------|
| `billingReferenceId` | string | ID транзакции в SmartParking |
| `qrPayload` | string | QR-контент (для отображения) |
| `qrCodeBase64` | string | QR-код PNG в Base64 |

```json
{
    "billingReferenceId": "89",
    "qrPayload": "service_id=2005&merchant_id=19876&amount=1500000&transaction_param=89",
    "qrCodeBase64": "iVBORw0KGgoAAAANSUhEUgAA..."
}
```

---

## ⬅️ Шаг 2. Callback после оплаты (SmartParking → UParking)

После оплаты SmartParking отправляет статус на callback URL UParking.

```
POST {callback_url_от_UParking}
Content-Type: application/json
X-Billing-Secret: uparking-shared-secret-2026
```

**Запрос:**

| Поле | Тип | Описание |
|------|-----|----------|
| `sessionId` | UUID | ID сессии UParking |
| `billingReferenceId` | string | ID транзакции SmartParking |
| `paid` | bool | `true` — оплачено, `false` — ошибка |
| `paidAt` | RFC 3339 | Время оплаты |

```json
{
    "sessionId": "8f2c1a4e-9b7d-4c3a-8e21-0f5a6b7c8d90",
    "billingReferenceId": "89",
    "paid": true,
    "paidAt": "2026-08-06T11:49:10.000+00:00"
}
```

---

## 📊 Статусы сессии

| Статус | Описание |
|--------|----------|
| `PendingBilling` | Создана, ожидает QR |
| `QrReady` | QR сгенерирован |
| `Paid` | Оплачено |
| `Failed` | Ошибка оплаты |

---

## 🔐 Безопасность

- **TLS 1.2+** обязателен  
- **Аутентификация:** заголовок `X-Billing-Secret: uparking-shared-secret-2026`  
- Секрет не логируется, не передаётся в URL  

---

## 📎 Контакты

По вопросам интеграции: `admin@whirl.uz`
