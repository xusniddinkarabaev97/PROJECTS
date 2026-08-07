# SmartParking — UParking Billing Provider API

> **Endpoint:** `POST https://whirl.uz/api/billing/create`  
> **Swagger:** `https://whirl.uz/smartparking/swagger/` (UParking Интеграция)  
> **Спецификация:** UParking Billing Integration v1.0

---

## Direction A: Create Payment (UParking → SmartParking)

UParking отправляет данные выезда, SmartParking возвращает QR для оплаты.

### Запрос

```
POST https://whirl.uz/api/billing/create
Content-Type: application/json
X-Billing-Secret: uparking-shared-secret-2026
```

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

| Поле | Тип | Описание |
|------|-----|----------|
| `sessionId` | UUID | ID платёжной сессии UParking |
| `parkingStart` | RFC 3339 | Время въезда |
| `parkingEnd` | RFC 3339 | Время выезда |
| `parkingTimeSeconds` | int | Длительность (сек) |
| `plateNo` | string | Госномер |
| `amount` | number | Сумма к оплате (UZS) |
| `currency` | string | `UZS` |
| `purpose` | string | `Parking` |

### Ответ (синхронный QR)

```json
{
    "billingReferenceId": "89",
    "qrPayload": "service_id=2005&merchant_id=19876&amount=1500000&transaction_param=89",
    "qrCodeBase64": "iVBORw0KGgoAAAANSUhEUgAA..."
}
```

| Поле | Тип | Описание |
|------|-----|----------|
| `billingReferenceId` | string | ID транзакции в SmartParking |
| `qrPayload` | string | QR-контент для отображения |
| `qrCodeBase64` | string | QR-код PNG в Base64 |

---

## Direction B2: Payment Callback (SmartParking → UParking)

После успешной оплаты SmartParking отправляет статус в UParking.

```
POST {UparkingCallbackUrl}/api/billing/payment
Content-Type: application/json
X-Billing-Secret: uparking-shared-secret-2026
```

```json
{
    "sessionId": "8f2c1a4e-9b7d-4c3a-8e21-0f5a6b7c8d90",
    "billingReferenceId": "89",
    "paid": true,
    "paidAt": "2026-08-06T11:49:10.000+00:00"
}
```

| Поле | Тип | Описание |
|------|-----|----------|
| `sessionId` | UUID | ID сессии UParking |
| `billingReferenceId` | string | ID транзакции SmartParking |
| `paid` | bool | `true` — оплачено, `false` — ошибка |
| `paidAt` | RFC 3339 | Время оплаты |

### Статусы сессии

| Статус | Описание |
|--------|----------|
| `PendingBilling` | Создана, ожидает QR |
| `QrReady` | QR сгенерирован |
| `Paid` | Оплачено |
| `Failed` | Ошибка оплаты |

---

## Безопасность

- **TLS 1.2+** обязателен
- **X-Billing-Secret** — общий секрет, сравнивается посимвольно
- Секрет НЕ логируется, НЕ передаётся в URL
