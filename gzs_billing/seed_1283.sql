INSERT INTO filling_stations ("Id", "Name", "Address", "Region") VALUES (1283, 'GZS 1283', 'По данным UGaz', 'Ташкент') ON CONFLICT DO NOTHING;
INSERT INTO dispensers ("FillingStationId", "Name", "FuelType") VALUES (1283, 'Колонка 1', 'Метан'), (1283, 'Колонка 2', 'Метан'), (1283, 'Колонка 3', 'Метан');
