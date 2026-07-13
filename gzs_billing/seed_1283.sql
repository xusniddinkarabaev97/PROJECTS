DELETE FROM dispensers WHERE "FillingStationId" = 1283;
INSERT INTO dispensers ("FillingStationId", "Name", "FuelType", "CreatedAt") VALUES
(1283, 'Колонка 1', 'Метан', NOW()),
(1283, 'Колонка 2', 'Метан', NOW()),
(1283, 'Колонка 3', 'Метан', NOW()),
(1283, 'Колонка 4', 'Метан', NOW()),
(1283, 'Колонка 5', 'Метан', NOW()),
(1283, 'Колонка 6', 'Метан', NOW()),
(1283, 'Колонка 7', 'Метан', NOW()),
(1283, 'Колонка 8', 'Метан', NOW());
SELECT 'Done! IDs:' AS result;
SELECT "Id", "Name" FROM dispensers WHERE "FillingStationId" = 1283 ORDER BY "Id";
