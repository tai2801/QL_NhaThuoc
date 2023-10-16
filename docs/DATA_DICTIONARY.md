# DATA DICTIONARY — PHARMACARE PRO (17 bảng nghiệp vụ)

Quy ước enum lưu INT: BatchStatus 1=Active, 2=Quarantined, 3=Depleted · InvoiceStatus 1=Completed, 2=Cancelled · PrescriptionStatus 1=Active, 2=Dispensed, 3=Expired, 4=Cancelled · ReturnStatus 1=Pending, 2=ApprovedRestock, 3=ApprovedDestroy, 4=Rejected · UserRole 1=Admin, 2=Pharmacist, 3=Staff.

## ActiveIngredients — Hoạt chất
| Cột | Kiểu | Ràng buộc | Ý nghĩa |
|---|---|---|---|
| Id | INT IDENTITY | PK | |
| Name | NVARCHAR(150) | UNIQUE NOT NULL | Tên hoạt chất |
| Description | NVARCHAR(500) | NULL | Mô tả |

## DosageForms — Dạng bào chế
Id PK · Name NVARCHAR(80) UNIQUE (Viên nén, Viên nang, Siro, Viên sủi, Thuốc mỡ/gel...)

## MedicineCategories — Nhóm thuốc
Id PK · Name NVARCHAR(120) UNIQUE.

## Medicines — Thuốc (mặt hàng, KHÔNG chứa tồn kho)
| Cột | Kiểu | Ràng buộc | Ý nghĩa |
|---|---|---|---|
| Id | INT IDENTITY | PK | |
| Code | NVARCHAR(30) | UNIQUE NOT NULL | Mã thuốc (tra cứu/quét mã) |
| Name | NVARCHAR(200) | NOT NULL | Tên thương mại |
| CategoryId / DosageFormId | INT | FK NOT NULL | Nhóm / dạng bào chế |
| Strength | NVARCHAR(60) | NOT NULL | Hàm lượng trình bày (500mg, 500mg/125mg) |
| Unit | NVARCHAR(30) | NOT NULL | Đơn vị bán nhỏ nhất |
| RegistrationNumber | NVARCHAR(60) | NULL | Số đăng ký lưu hành |
| SalePrice | DECIMAL(18,2) | CHECK ≥ 0 | Giá bán hiện hành / đơn vị |
| RequiresPrescription | BIT | DEFAULT 0 | Thuốc kê đơn |
| ReorderLevel | INT | DEFAULT 20 | Ngưỡng cảnh báo sắp hết (trên tồn bán được) |
| IsActive | BIT | DEFAULT 1 | Còn kinh doanh |

## MedicineIngredients — Thuốc × Hoạt chất
PK (MedicineId, ActiveIngredientId) · StrengthPerUnit NVARCHAR(60) NULL (hàm lượng hoạt chất / 1 đơn vị).

## Suppliers — Nhà cung cấp
Id PK · Name · TaxCode · Phone · Email · Address · IsActive.

## Customers — Khách hàng
Id PK · FullName · Phone · Address · AllergyNote (ghi chú dị ứng — dược sĩ tham chiếu).

## Employees — Nhân viên
Id PK · FullName · Username UNIQUE (khớp tài khoản Identity) · Role INT CHECK IN (1,2,3) · PharmacyLicenseNo (chứng chỉ hành nghề dược sĩ) · IsActive.

## MedicineBatches — LÔ THUỐC (trung tâm)
| Cột | Kiểu | Ràng buộc | Ý nghĩa |
|---|---|---|---|
| Id | INT IDENTITY | PK | |
| MedicineId | INT | FK NOT NULL | Thuốc |
| BatchNumber | NVARCHAR(50) | UNIQUE theo (MedicineId, BatchNumber) | Số lô nhà sản xuất |
| ManufactureDate | DATE | CHECK ExpiryDate > ManufactureDate | NSX |
| ExpiryDate | DATE | | HSD — ngày cuối còn bán được |
| QuantityReceived | INT | CHECK > 0 | Số lượng lúc nhập |
| QuantityOnHand | INT | CHECK 0 ≤ OnHand ≤ Received | Tồn hiện tại của lô |
| ImportPrice | DECIMAL(18,2) | CHECK > 0 | Giá nhập / đơn vị của lần nhập |
| Status | INT | CHECK IN (1,2,3) | Active / Quarantined / Depleted |
| CreatedAt | DATETIME2 | DEFAULT SYSDATETIME() | |

Index `IX_Batches_FEFO (MedicineId, Status, ExpiryDate)` phục vụ truy vấn FEFO.

## StockReceipts — Phiếu nhập
Id PK · ReceiptCode UNIQUE · SupplierId FK · ReceivedByEmployeeId FK · ReceivedAt · SupplierInvoiceNo · Note.

## StockReceiptLines — Dòng nhập (mỗi dòng tạo đúng 1 lô)
Id PK · ReceiptId FK CASCADE · MedicineId FK · BatchId FK (lô được tạo) · Quantity CHECK > 0 · ImportPrice CHECK > 0.

## Prescriptions — Đơn thuốc
Id PK · Code UNIQUE · CustomerId FK · DoctorName (bác sĩ kê) · IssuingUnit (đơn vị kê) · Diagnosis · IssuedDate · ValidUntil NULL · Status · Note.

## PrescriptionLines — Chi tiết đơn
Id PK · PrescriptionId FK CASCADE · MedicineId FK · DosageInstruction (cách dùng) · QuantityPrescribed CHECK > 0.

## SalesInvoices — Hóa đơn bán
Id PK · InvoiceCode UNIQUE · CustomerId FK NULL (khách lẻ) · SoldByEmployeeId FK · PrescriptionId FK NULL · CreatedAt · DiscountAmount · PaymentMethod (Cash/Card/Transfer) · Status.

## SalesInvoiceLines — Dòng hóa đơn (gắn lô nguồn)
Id PK · InvoiceId FK CASCADE · MedicineId FK · **BatchId FK NOT NULL RESTRICT (lô nguồn)** · Quantity CHECK > 0 · UnitPrice (snapshot giá bán). Index trên BatchId phục vụ truy xuất lô → các hóa đơn đã bán.

## SalesReturns — Phiếu trả
Id PK · InvoiceId FK · CreatedByEmployeeId FK · ReviewedByEmployeeId FK NULL · CreatedAt · ReviewedAt NULL · Reason NOT NULL (bắt buộc) · Status · ReviewNote.

## SalesReturnLines — Dòng trả
Id PK · ReturnId FK CASCADE · InvoiceLineId FK RESTRICT (trả theo đúng dòng/lô đã bán) · Quantity CHECK > 0 · IsIntact (còn nguyên vẹn).
