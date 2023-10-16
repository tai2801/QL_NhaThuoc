/* =====================================================================
   PharmaCare Pro — Dữ liệu mẫu (SQL Server)
   "Hôm nay" tham chiếu: đầu tháng 11/2023.
   Điểm demo có chủ đích:
   - Lô #1 (Paracetamol L-P21A) ĐÃ HẾT HẠN (HSD 01/02/2023) còn tồn 120 → FEFO bỏ qua, bán bị chặn.
   - Lô #2 cận hạn mức Critical, lô #4 Warning, lô #9 Watch (mốc 90/60/30).
   - Hóa đơn HD2023-0001 bán Paracetamol lấy từ lô #2 (FEFO bỏ qua lô hết hạn #1).
   - Mọi dòng hóa đơn đều gắn BatchId (lô nguồn) — nhất quán: tồn hiện tại = đã nhập - đã bán (+ hoàn kho).
   Chạy lại được: xóa dữ liệu nghiệp vụ theo thứ tự FK trước khi nạp (chỉ dùng cho môi trường học tập).
   ===================================================================== */
USE PharmaCarePro;
GO
DELETE FROM dbo.SalesReturnLines; DELETE FROM dbo.SalesReturns;
DELETE FROM dbo.SalesInvoiceLines; DELETE FROM dbo.SalesInvoices;
DELETE FROM dbo.PrescriptionLines; DELETE FROM dbo.Prescriptions;
DELETE FROM dbo.StockReceiptLines; DELETE FROM dbo.StockReceipts;
DELETE FROM dbo.MedicineBatches; DELETE FROM dbo.MedicineIngredients;
DELETE FROM dbo.Medicines; DELETE FROM dbo.MedicineCategories;
DELETE FROM dbo.DosageForms; DELETE FROM dbo.ActiveIngredients;
DELETE FROM dbo.Suppliers; DELETE FROM dbo.Customers; DELETE FROM dbo.Employees;
GO

SET IDENTITY_INSERT dbo.DosageForms ON;
INSERT INTO dbo.DosageForms (Id, Name) VALUES
(1, N'Viên nén'), (2, N'Viên nang'), (3, N'Siro'), (4, N'Viên sủi'), (5, N'Thuốc mỡ / gel');
SET IDENTITY_INSERT dbo.DosageForms OFF;

SET IDENTITY_INSERT dbo.MedicineCategories ON;
INSERT INTO dbo.MedicineCategories (Id, Name) VALUES
(1, N'Giảm đau - hạ sốt'), (2, N'Kháng sinh'), (3, N'Vitamin & khoáng chất'),
(4, N'Tiêu hóa'), (5, N'Hô hấp & dị ứng');
SET IDENTITY_INSERT dbo.MedicineCategories OFF;

SET IDENTITY_INSERT dbo.ActiveIngredients ON;
INSERT INTO dbo.ActiveIngredients (Id, Name, Description) VALUES
(1, N'Paracetamol', N'Giảm đau, hạ sốt'),
(2, N'Ibuprofen', N'Kháng viêm không steroid (NSAID)'),
(3, N'Amoxicillin', N'Kháng sinh nhóm penicillin'),
(4, N'Acid clavulanic', N'Chất ức chế beta-lactamase'),
(5, N'Vitamin C', N'Acid ascorbic'),
(6, N'Omeprazole', N'Ức chế bơm proton (PPI)'),
(7, N'Cetirizine', N'Kháng histamin H1 thế hệ 2'),
(8, N'Ambroxol', N'Long đờm');
SET IDENTITY_INSERT dbo.ActiveIngredients OFF;

SET IDENTITY_INSERT dbo.Medicines ON;
INSERT INTO dbo.Medicines (Id, Code, Name, CategoryId, DosageFormId, Strength, Unit, RegistrationNumber, SalePrice, RequiresPrescription, ReorderLevel, IsActive) VALUES
(1, N'PARA500', N'Paracetamol 500mg',        1, 1, N'500mg',      N'Viên', N'VD-10001-21', 2000,  0, 100, 1),
(2, N'IBU400',  N'Ibuprofen 400mg',          1, 1, N'400mg',      N'Viên', N'VD-10002-21', 3000,  0,  50, 1),
(3, N'AMX500',  N'Amoxicillin 500mg',        2, 2, N'500mg',      N'Viên', N'VD-10003-21', 3500,  1, 100, 1),
(4, N'AUG625',  N'Amoxicillin + Clavulanate 625mg', 2, 1, N'500mg/125mg', N'Viên', N'VD-10004-21', 8000, 1, 50, 1),
(5, N'VITC1000',N'Vitamin C 1000mg',         3, 4, N'1000mg',     N'Viên', N'VD-10005-21', 5000,  0,  40, 1),
(6, N'OME20',   N'Omeprazole 20mg',          4, 2, N'20mg',       N'Viên', N'VD-10006-21', 2500,  1,  50, 1),
(7, N'CET10',   N'Cetirizine 10mg',          5, 1, N'10mg',       N'Viên', N'VD-10007-21', 1500,  0,  30, 1),
(8, N'AMB30',   N'Ambroxol 30mg',            5, 1, N'30mg',       N'Viên', N'VD-10008-21', 2000,  0,  50, 1);
SET IDENTITY_INSERT dbo.Medicines OFF;

INSERT INTO dbo.MedicineIngredients (MedicineId, ActiveIngredientId, StrengthPerUnit) VALUES
(1, 1, N'500mg'), (2, 2, N'400mg'), (3, 3, N'500mg'),
(4, 3, N'500mg'), (4, 4, N'125mg'),
(5, 5, N'1000mg'), (6, 6, N'20mg'), (7, 7, N'10mg'), (8, 8, N'30mg');
GO

SET IDENTITY_INSERT dbo.Suppliers ON;
INSERT INTO dbo.Suppliers (Id, Name, TaxCode, Phone, Email, Address, IsActive) VALUES
(1, N'Công ty CP Dược phẩm Minh Khang', N'0312001001', N'02838112233', N'lienhe@minhkhang.vn', N'120 Nguyễn Văn Trỗi, Phú Nhuận, TP.HCM', 1),
(2, N'Công ty TNHH Dược Sài Gòn Pharma', N'0312001002', N'02838445566', N'order@saigonpharma.vn', N'45 Cách Mạng Tháng 8, Quận 3, TP.HCM', 1),
(3, N'Công ty CP Phân phối Dược An Bình', N'0312001003', N'02837778899', N'hotro@anbinhpharma.vn', N'78 Lê Trọng Tấn, Tân Phú, TP.HCM', 1);
SET IDENTITY_INSERT dbo.Suppliers OFF;

SET IDENTITY_INSERT dbo.Customers ON;
INSERT INTO dbo.Customers (Id, FullName, Phone, Address, AllergyNote) VALUES
(1, N'Nguyễn Thị Hoa',  N'0903111222', N'Tân Phú, TP.HCM', NULL),
(2, N'Trần Văn Bình',   N'0913444555', N'Bình Tân, TP.HCM', N'Dị ứng penicillin — cần dược sĩ kiểm tra trước khi bán kháng sinh nhóm này'),
(3, N'Lê Thu Hằng',     N'0933666777', N'Tân Bình, TP.HCM', NULL),
(4, N'Phạm Quốc Tuấn',  N'0977888999', N'Gò Vấp, TP.HCM', NULL);
SET IDENTITY_INSERT dbo.Customers OFF;

SET IDENTITY_INSERT dbo.Employees ON;
INSERT INTO dbo.Employees (Id, FullName, Username, Role, PharmacyLicenseNo, IsActive) VALUES
(1, N'Quản trị hệ thống', N'admin',    1, NULL,          1),
(2, N'DS. Võ Thanh Sơn',  N'son.vt',   2, N'CCHN-DS-05211', 1),
(3, N'Nguyễn Mỹ Duyên',   N'duyen.nm', 3, NULL,          1);
SET IDENTITY_INSERT dbo.Employees OFF;
GO

/* ---------- LÔ THUỐC ---------- */
SET IDENTITY_INSERT dbo.MedicineBatches ON;
INSERT INTO dbo.MedicineBatches (Id, MedicineId, BatchNumber, ManufactureDate, ExpiryDate, QuantityReceived, QuantityOnHand, ImportPrice, Status) VALUES
(1,  1, N'L-P21A', '2021-03-01', '2023-02-01',  500,  120, 1100, 1),  -- ĐÃ HẾT HẠN: demo chặn bán (380 đã bán trước khi hết hạn)
(2,  1, N'L-P22C', '2022-06-01', '2023-11-20',  200,  180, 1200, 1),  -- Critical (~19 ngày) — FEFO xuất trước; đã bán 20 (HD 1)
(3,  1, N'L-P23A', '2023-01-15', '2024-07-01', 1000, 1000, 1250, 1),
(4,  2, N'L-I22B', '2022-05-10', '2023-12-25',  400,  400, 1700, 1),  -- Warning (~54 ngày)
(5,  2, N'L-I23A', '2023-02-01', '2024-09-01',  500,  500, 1750, 1),
(6,  3, N'L-A22D', '2022-07-01', '2023-12-10',  450,  420, 2100, 1),  -- Đã bán 30 theo đơn (HD 2)
(7,  3, N'L-A23B', '2023-03-01', '2024-05-01',  800,  800, 2150, 1),
(8,  4, N'L-G23A', '2023-01-20', '2024-02-15',  300,  300, 5200, 1),
(9,  5, N'L-V22E', '2022-08-01', '2024-01-20',  205,  200, 2900, 1),  -- Watch (~80 ngày); đã bán 5 (HD 3)
(10, 6, N'L-O23A', '2023-02-15', '2024-03-01',  350,  350, 1500, 1),
(11, 7, N'L-C22F', '2022-09-01', '2023-11-28',  100,   90,  800, 1),  -- Critical (~27 ngày); đã bán 10 (HD 3)
(12, 8, N'L-B23A', '2023-04-01', '2024-08-01',  500,  500, 1100, 1);
SET IDENTITY_INSERT dbo.MedicineBatches OFF;
GO

/* ---------- PHIẾU NHẬP (mỗi dòng = đúng 1 lô) ---------- */
SET IDENTITY_INSERT dbo.StockReceipts ON;
INSERT INTO dbo.StockReceipts (Id, ReceiptCode, SupplierId, ReceivedByEmployeeId, ReceivedAt, SupplierInvoiceNo, Note) VALUES
(1, N'PN2021-0001', 1, 2, '2021-03-20T09:00:00', N'HD-MK-8811', N'Nhập Paracetamol lô L-P21A'),
(2, N'PN2022-0007', 1, 2, '2022-06-25T09:30:00', N'HD-MK-9020', N'Nhập lô cận hạn L-P22C, L-A22D, L-C22F, L-I22B, L-V22E'),
(3, N'PN2023-0002', 2, 2, '2023-02-10T10:00:00', N'HD-SGP-3310', N'Nhập đợt đầu 2023'),
(4, N'PN2023-0005', 3, 2, '2023-04-12T08:45:00', N'HD-AB-1207', N'Nhập Ambroxol, Amoxicillin lô mới');
SET IDENTITY_INSERT dbo.StockReceipts OFF;

INSERT INTO dbo.StockReceiptLines (ReceiptId, MedicineId, BatchId, Quantity, ImportPrice) VALUES
(1, 1, 1, 500, 1100),
(2, 1, 2, 200, 1200), (2, 3, 6, 450, 2100), (2, 7, 11, 100, 800), (2, 2, 4, 400, 1700), (2, 5, 9, 205, 2900),
(3, 1, 3, 1000, 1250), (3, 2, 5, 500, 1750), (3, 4, 8, 300, 5200), (3, 6, 10, 350, 1500),
(4, 3, 7, 800, 2150), (4, 8, 12, 500, 1100);
GO

/* ---------- ĐƠN THUỐC ---------- */
SET IDENTITY_INSERT dbo.Prescriptions ON;
INSERT INTO dbo.Prescriptions (Id, Code, CustomerId, DoctorName, IssuingUnit, Diagnosis, IssuedDate, ValidUntil, Status, Note) VALUES
(1, N'DT2023-0001', 1, N'BS. Trần Minh Đức', N'Bệnh viện Quận Tân Phú', N'Viêm họng cấp do vi khuẩn', '2023-10-28', '2023-11-27', 1, N'Uống đủ 5 ngày, tái khám nếu sốt lại');
SET IDENTITY_INSERT dbo.Prescriptions OFF;

INSERT INTO dbo.PrescriptionLines (PrescriptionId, MedicineId, DosageInstruction, QuantityPrescribed) VALUES
(1, 3, N'Sáng 1 viên, tối 1 viên sau ăn', 30),
(1, 1, N'Khi sốt trên 38.5°C uống 1 viên, cách nhau tối thiểu 4 giờ', 10);
GO

/* ---------- HÓA ĐƠN BÁN (dòng gắn lô nguồn) ---------- */
SET IDENTITY_INSERT dbo.SalesInvoices ON;
INSERT INTO dbo.SalesInvoices (Id, InvoiceCode, CustomerId, SoldByEmployeeId, PrescriptionId, CreatedAt, DiscountAmount, PaymentMethod, Status) VALUES
(1, N'HD2023-0001', NULL, 3, NULL, '2023-10-30T09:15:00', 0, N'Cash', 1),
(2, N'HD2023-0002', 1,    2, 1,    '2023-10-30T10:02:00', 0, N'Transfer', 1),
(3, N'HD2023-0003', 3,    3, NULL, '2023-10-31T16:40:00', 0, N'Card', 1);
SET IDENTITY_INSERT dbo.SalesInvoices OFF;

SET IDENTITY_INSERT dbo.SalesInvoiceLines ON;
INSERT INTO dbo.SalesInvoiceLines (Id, InvoiceId, MedicineId, BatchId, Quantity, UnitPrice) VALUES
(1, 1, 1, 2, 20, 2000),   -- FEFO: bỏ qua lô #1 đã hết hạn, lấy lô #2
(2, 2, 3, 6, 30, 3500),   -- Bán theo đơn DT2023-0001 từ lô #6
(3, 3, 7, 11, 10, 1500),
(4, 3, 5, 9, 5, 5000);
SET IDENTITY_INSERT dbo.SalesInvoiceLines OFF;
GO

/* ---------- PHIẾU TRẢ MẪU (chờ duyệt) ---------- */
SET IDENTITY_INSERT dbo.SalesReturns ON;
INSERT INTO dbo.SalesReturns (Id, InvoiceId, CreatedByEmployeeId, ReviewedByEmployeeId, CreatedAt, ReviewedAt, Reason, Status, ReviewNote) VALUES
(1, 1, 3, NULL, '2023-10-31T09:00:00', NULL, N'Khách mua thừa số lượng, vỉ còn nguyên seal', 1, NULL);
SET IDENTITY_INSERT dbo.SalesReturns OFF;

INSERT INTO dbo.SalesReturnLines (ReturnId, InvoiceLineId, Quantity, IsIntact) VALUES
(1, 1, 2, 1);
GO
