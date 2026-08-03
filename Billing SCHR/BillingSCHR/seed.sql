-- BillingSCHR — Мобилизационный призывной резерв
-- Database seed script

-- Default admin company
INSERT INTO "Companies" ("Name", "Login", "Inn", "Address", "Phone", "Email", "Role", "JwtAuthToken", "RefreshToken", "TokenExpiry", "CreatedAt", "UpdatedAt")
VALUES ('BillingSCHR Admin', 'admin', '000000000', 'Tashkent', '+998000000000', 'admin@billing-schr.uz', 'Admin', 'admin123', '', NULL, NOW(), NOW());
