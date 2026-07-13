INSERT INTO filling_stations ("Id", "Name", "Address", "Region", "CreatedAt") VALUES (1283, 'GZS 1283', 'UGaz', 'Tashkent', NOW()) ON CONFLICT DO NOTHING;
INSERT INTO dispensers ("FillingStationId", "Name", "FuelType", "CreatedAt") VALUES (1283, 'Колонка 1', 'Метан', NOW());
INSERT INTO dispensers ("FillingStationId", "Name", "FuelType", "CreatedAt") VALUES (1283, 'Колонка 2', 'Метан', NOW());
INSERT INTO dispensers ("FillingStationId", "Name", "FuelType", "CreatedAt") VALUES (1283, 'Колонка 3', 'Метан', NOW());
INSERT INTO dispensers ("FillingStationId", "Name", "FuelType", "CreatedAt") VALUES (1283, 'Колонка 4', 'Метан', NOW());
INSERT INTO dispensers ("FillingStationId", "Name", "FuelType", "CreatedAt") VALUES (1283, 'Колонка 5', 'Метан', NOW());
INSERT INTO dispensers ("FillingStationId", "Name", "FuelType", "CreatedAt") VALUES (1283, 'Колонка 6', 'Метан', NOW());
INSERT INTO dispensers ("FillingStationId", "Name", "FuelType", "CreatedAt") VALUES (1283, 'Колонка 7', 'Метан', NOW());
INSERT INTO dispensers ("FillingStationId", "Name", "FuelType", "CreatedAt") VALUES (1283, 'Колонка 8', 'Метан', NOW());
