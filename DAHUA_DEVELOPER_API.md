# SmartParking — Dahua API

> **URL:** `https://whirl.uz/api/DahuaIntegration`  
> **Swagger:** `https://whirl.uz/smartparking/swagger/` (выбрать "Dahua Интеграция")

---

## 1. Приём событий от камеры

Dahua DSS отправляет POST запрос при фиксации номера.

```
POST https://whirl.uz/api/DahuaIntegration/events
Content-Type: application/json
X-Webhook-Secret: dss_webhook_secret_2026
```

### Въезд (entry)

```json
{
    "eventType": "ANPR",
    "channelId": 1,
    "plateNumber": "01A123AA",
    "direction": "entry",
    "timestamp": "2026-08-07T10:00:00Z",
    "imageUrl": "http://КАМЕРА_IP/snapshot.jpg"
}
```

**Ответ:**
```json
{
    "status": "active",
    "sessionId": 156,
    "plate": "01A123AA",
    "category": "standard",
    "barrierOpened": true
}
```

### Выезд (exit)

```json
{
    "eventType": "ANPR",
    "channelId": 1,
    "plateNumber": "01A123AA",
    "direction": "exit",
    "timestamp": "2026-08-07T12:30:00Z"
}
```

**Ответ:**
```json
{
    "status": "completed",
    "sessionId": 156,
    "parkingFee": 15000,
    "transactionId": 89,
    "barrierOpened": false,
    "qrCodeBase64": "iVBORw0KGgo...",
    "qrContent": "service_id=2005&merchant_id=19876&amount=1500000&transaction_param=89"
}
```

---

## Поля запроса (от Dahua)

| Поле | Тип | Обязательно | Описание |
|------|-----|-------------|----------|
| `eventType` | string | Да | Тип события: `ANPR` |
| `channelId` | int | Да | ID канала камеры (настраивается в админке) |
| `plateNumber` | string | Да | Госномер авто |
| `direction` | string | Да | `entry` (въезд) / `exit` (выезд) |
| `timestamp` | datetime | Да | Время события (ISO 8601) |
| `imageUrl` | string | Нет | Ссылка на фото с камеры |

---

## Поля ответа

### Въезд:
| Поле | Описание |
|------|----------|
| `status` | `active` — авто на парковке |
| `sessionId` | ID парковочной сессии |
| `plate` | Номер авто |
| `category` | Категория ТС |
| `barrierOpened` | Шлагбаум открыт (`true`/`false`) |

### Выезд:
| Поле | Описание |
|------|----------|
| `status` | `completed` — выезд зафиксирован |
| `parkingFee` | Сумма к оплате (UZS) |
| `transactionId` | ID транзакции для оплаты |
| `qrCodeBase64` | QR-код PNG в Base64 |
| `qrContent` | Данные QR для Click |

---

## Заголовки

| Заголовок | Значение |
|-----------|----------|
| `Content-Type` | `application/json` |
| `X-Webhook-Secret` | `dss_webhook_secret_2026` |

---

## Настройка в Dahua DSS

1. Открыть DSS Management Client
2. System Integration → Event Transferal
3. Добавить Web Service:
   - **URL:** `https://whirl.uz/api/DahuaIntegration/events`
   - **Метод:** `POST`
   - **Формат:** `JSON`
   - **Secret:** `dss_webhook_secret_2026`
4. Выбрать события: `ANPR`, `VehicleDetection`
5. Сохранить
