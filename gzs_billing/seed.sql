DELETE FROM dispensers;
DELETE FROM stakeholders;
DELETE FROM tranzaktsiyalar;
DELETE FROM schetfakturalar;
DELETE FROM filling_stations;

INSERT INTO filling_stations ("Id", "Name", "Address", "Region", "IsActive", "CreatedAt") VALUES
(1, 'GZS Mirzo-Ulugbek', 'г.Ташкент, ул.Муминова, 7/2', 'Ташкент', true, NOW()),
(2, 'GZS Sergeli', 'г.Ташкент, Сергели-6, 15', 'Ташкент', true, NOW()),
(3, 'GZS Samarkand', 'г.Самарканд, ул.Буюк Ипак Йули, 82', 'Самарканд', true, NOW()),
(4, 'GZS Fergana', 'г.Фергана, ул.Навои, 45', 'Фергана', true, NOW());

INSERT INTO dispensers ("FillingStationId", "Name", "FuelType", "IsActive", "CreatedAt") VALUES
(1, 'Колонка 1', 'Метан', true, NOW()),
(1, 'Колонка 2', 'Метан', true, NOW()),
(2, 'Колонка 1', 'Метан', true, NOW()),
(2, 'Колонка 2', 'Метан', true, NOW()),
(3, 'Колонка 1', 'Метан', true, NOW()),
(4, 'Колонка 1', 'Метан', true, NOW());

INSERT INTO stakeholders ("Id", "FillingStationId", "PaymentId", "BankAccount", "SharePercent", "FullName") VALUES
(gen_random_uuid(), 1, 1, '20208000123456789001', 30, 'ООО UzGasTrade'),
(gen_random_uuid(), 1, 2, '20208000234567890012', 30, 'АО Whirl'),
(gen_random_uuid(), 1, 3, '20208000345678900123', 40, 'ИП Каримов А.А.'),
(gen_random_uuid(), 2, 1, '20208000456789001234', 25, 'ООО SergeliGas'),
(gen_random_uuid(), 2, 2, '20208000567890012345', 25, 'АО Whirl'),
(gen_random_uuid(), 2, 3, '20208000678900123456', 50, 'ООО TransGas'),
(gen_random_uuid(), 3, 1, '20208000789001234567', 40, 'ООО SamGas Plus'),
(gen_random_uuid(), 3, 2, '20208000890012345678', 60, 'АО Whirl'),
(gen_random_uuid(), 4, 4, '20208000900123456789', 50, 'ООО FerganaGas'),
(gen_random_uuid(), 4, 1, '20208000001234567890', 50, 'АО Whirl');

INSERT INTO tranzaktsiyalar ("Id", "TotalSum", "FillingStationId", "DispenserId", "CardType", "IdempotencyKey", "PaymentId", "Status", "CreatedAt") VALUES
(gen_random_uuid(), 150000, 1, 1, 'Uzcard', 'TX-20260710-001', 1, 'Completed', NOW()),
(gen_random_uuid(), 200000, 1, 2, 'Humo', 'TX-20260710-002', 2, 'Completed', NOW()),
(gen_random_uuid(), 95000, 2, 3, 'Uzcard', 'TX-20260710-003', 1, 'Completed', NOW()),
(gen_random_uuid(), 180000, 3, 5, 'Humo', 'TX-20260710-004', 2, 'Completed', NOW()),
(gen_random_uuid(), 125000, 4, 6, 'Click', 'TX-20260710-005', 3, 'Pending', NOW());
