/* =====================================================================
   PharmaCare Pro — Schema SQL Server (khớp 1-1 với EF Core Models)
   Chạy: sqlcmd -S localhost,1433 -U sa -P <password> -i schema.sql
   Script chạy lại được (idempotent ở mức tạo DB/bảng nếu chưa tồn tại).
   ===================================================================== */
IF DB_ID(N'PharmaCarePro') IS NULL
    CREATE DATABASE PharmaCarePro;
GO
USE PharmaCarePro;
GO

/* ---------- Danh mục ---------- */
IF OBJECT_ID(N'dbo.ActiveIngredients', N'U') IS NULL
CREATE TABLE dbo.ActiveIngredients (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(150) NOT NULL UNIQUE,
    Description NVARCHAR(500) NULL
);
GO
IF OBJECT_ID(N'dbo.DosageForms', N'U') IS NULL
CREATE TABLE dbo.DosageForms (
    Id   INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(80) NOT NULL UNIQUE
);
GO
IF OBJECT_ID(N'dbo.MedicineCategories', N'U') IS NULL
CREATE TABLE dbo.MedicineCategories (
    Id   INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(120) NOT NULL UNIQUE
);
GO
IF OBJECT_ID(N'dbo.Medicines', N'U') IS NULL
CREATE TABLE dbo.Medicines (
    Id                   INT IDENTITY(1,1) PRIMARY KEY,
    Code                 NVARCHAR(30)  NOT NULL UNIQUE,
    Name                 NVARCHAR(200) NOT NULL,
    CategoryId           INT NOT NULL REFERENCES dbo.MedicineCategories(Id),
    DosageFormId         INT NOT NULL REFERENCES dbo.DosageForms(Id),
    Strength             NVARCHAR(60)  NOT NULL,
    Unit                 NVARCHAR(30)  NOT NULL,
    RegistrationNumber   NVARCHAR(60)  NULL,
    SalePrice            DECIMAL(18,2) NOT NULL CHECK (SalePrice >= 0),
    RequiresPrescription BIT NOT NULL DEFAULT 0,
    ReorderLevel         INT NOT NULL DEFAULT 20 CHECK (ReorderLevel >= 0),
    IsActive             BIT NOT NULL DEFAULT 1
);
GO
IF OBJECT_ID(N'dbo.MedicineIngredients', N'U') IS NULL
CREATE TABLE dbo.MedicineIngredients (
    MedicineId        INT NOT NULL REFERENCES dbo.Medicines(Id) ON DELETE CASCADE,
    ActiveIngredientId INT NOT NULL REFERENCES dbo.ActiveIngredients(Id),
    StrengthPerUnit   NVARCHAR(60) NULL,
    CONSTRAINT PK_MedicineIngredients PRIMARY KEY (MedicineId, ActiveIngredientId)
);
GO

/* ---------- Đối tác / con người ---------- */
IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NULL
CREATE TABLE dbo.Suppliers (
    Id      INT IDENTITY(1,1) PRIMARY KEY,
    Name    NVARCHAR(200) NOT NULL,
    TaxCode NVARCHAR(20) NULL,
    Phone   NVARCHAR(20) NULL,
    Email   NVARCHAR(120) NULL,
    Address NVARCHAR(300) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
GO
IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
CREATE TABLE dbo.Customers (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    FullName    NVARCHAR(120) NOT NULL,
    Phone       NVARCHAR(20) NULL,
    Address     NVARCHAR(300) NULL,
    AllergyNote NVARCHAR(500) NULL
);
GO
IF OBJECT_ID(N'dbo.Employees', N'U') IS NULL
CREATE TABLE dbo.Employees (
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    FullName          NVARCHAR(120) NOT NULL,
    Username          NVARCHAR(80) NOT NULL UNIQUE,
    Role              INT NOT NULL CHECK (Role IN (1,2,3)),  -- 1 Admin, 2 Pharmacist, 3 Staff
    PharmacyLicenseNo NVARCHAR(50) NULL,
    IsActive          BIT NOT NULL DEFAULT 1
);
GO

/* ---------- LÔ THUỐC — trung tâm tồn kho ---------- */
IF OBJECT_ID(N'dbo.MedicineBatches', N'U') IS NULL
CREATE TABLE dbo.MedicineBatches (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    MedicineId       INT NOT NULL REFERENCES dbo.Medicines(Id),
    BatchNumber      NVARCHAR(50) NOT NULL,
    ManufactureDate  DATE NOT NULL,
    ExpiryDate       DATE NOT NULL,
    QuantityReceived INT NOT NULL CHECK (QuantityReceived > 0),
    QuantityOnHand   INT NOT NULL CHECK (QuantityOnHand >= 0),
    ImportPrice      DECIMAL(18,2) NOT NULL CHECK (ImportPrice > 0),
    Status           INT NOT NULL DEFAULT 1 CHECK (Status IN (1,2,3)), -- 1 Active, 2 Quarantined, 3 Depleted
    CreatedAt        DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT UQ_Batch_Per_Medicine UNIQUE (MedicineId, BatchNumber),
    CONSTRAINT CK_Batch_Dates CHECK (ExpiryDate > ManufactureDate),
    CONSTRAINT CK_Batch_OnHand CHECK (QuantityOnHand <= QuantityReceived)
);
GO
CREATE INDEX IX_Batches_FEFO ON dbo.MedicineBatches (MedicineId, Status, ExpiryDate);
GO

/* ---------- Nhập hàng: mỗi dòng nhập = 1 lô ---------- */
IF OBJECT_ID(N'dbo.StockReceipts', N'U') IS NULL
CREATE TABLE dbo.StockReceipts (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    ReceiptCode         NVARCHAR(30) NOT NULL UNIQUE,
    SupplierId          INT NOT NULL REFERENCES dbo.Suppliers(Id),
    ReceivedByEmployeeId INT NOT NULL REFERENCES dbo.Employees(Id),
    ReceivedAt          DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    SupplierInvoiceNo   NVARCHAR(50) NULL,
    Note                NVARCHAR(500) NULL
);
GO
IF OBJECT_ID(N'dbo.StockReceiptLines', N'U') IS NULL
CREATE TABLE dbo.StockReceiptLines (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ReceiptId   INT NOT NULL REFERENCES dbo.StockReceipts(Id) ON DELETE CASCADE,
    MedicineId  INT NOT NULL REFERENCES dbo.Medicines(Id),
    BatchId     INT NOT NULL REFERENCES dbo.MedicineBatches(Id),
    Quantity    INT NOT NULL CHECK (Quantity > 0),
    ImportPrice DECIMAL(18,2) NOT NULL CHECK (ImportPrice > 0)
);
GO

/* ---------- Đơn thuốc kê đơn ---------- */
IF OBJECT_ID(N'dbo.Prescriptions', N'U') IS NULL
CREATE TABLE dbo.Prescriptions (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Code        NVARCHAR(30) NOT NULL UNIQUE,
    CustomerId  INT NOT NULL REFERENCES dbo.Customers(Id),
    DoctorName  NVARCHAR(120) NOT NULL,
    IssuingUnit NVARCHAR(200) NOT NULL,
    Diagnosis   NVARCHAR(500) NULL,
    IssuedDate  DATE NOT NULL,
    ValidUntil  DATE NULL,
    Status      INT NOT NULL DEFAULT 1 CHECK (Status IN (1,2,3,4)), -- 1 Active, 2 Dispensed, 3 Expired, 4 Cancelled
    Note        NVARCHAR(500) NULL
);
GO
IF OBJECT_ID(N'dbo.PrescriptionLines', N'U') IS NULL
CREATE TABLE dbo.PrescriptionLines (
    Id                 INT IDENTITY(1,1) PRIMARY KEY,
    PrescriptionId     INT NOT NULL REFERENCES dbo.Prescriptions(Id) ON DELETE CASCADE,
    MedicineId         INT NOT NULL REFERENCES dbo.Medicines(Id),
    DosageInstruction  NVARCHAR(300) NOT NULL,
    QuantityPrescribed INT NOT NULL CHECK (QuantityPrescribed > 0)
);
GO

/* ---------- Bán hàng: dòng hóa đơn LUÔN gắn lô nguồn ---------- */
IF OBJECT_ID(N'dbo.SalesInvoices', N'U') IS NULL
CREATE TABLE dbo.SalesInvoices (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceCode      NVARCHAR(30) NOT NULL UNIQUE,
    CustomerId       INT NULL REFERENCES dbo.Customers(Id),
    SoldByEmployeeId INT NOT NULL REFERENCES dbo.Employees(Id),
    PrescriptionId   INT NULL REFERENCES dbo.Prescriptions(Id),
    CreatedAt        DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    DiscountAmount   DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (DiscountAmount >= 0),
    PaymentMethod    NVARCHAR(20) NOT NULL DEFAULT N'Cash',
    Status           INT NOT NULL DEFAULT 1 CHECK (Status IN (1,2)) -- 1 Completed, 2 Cancelled
);
GO
IF OBJECT_ID(N'dbo.SalesInvoiceLines', N'U') IS NULL
CREATE TABLE dbo.SalesInvoiceLines (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceId  INT NOT NULL REFERENCES dbo.SalesInvoices(Id) ON DELETE CASCADE,
    MedicineId INT NOT NULL REFERENCES dbo.Medicines(Id),
    BatchId    INT NOT NULL REFERENCES dbo.MedicineBatches(Id),  -- LÔ NGUỒN: bắt buộc, phục vụ truy xuất
    Quantity   INT NOT NULL CHECK (Quantity > 0),
    UnitPrice  DECIMAL(18,2) NOT NULL CHECK (UnitPrice >= 0)
);
GO
CREATE INDEX IX_InvoiceLines_Batch ON dbo.SalesInvoiceLines (BatchId);
GO

/* ---------- Trả thuốc ---------- */
IF OBJECT_ID(N'dbo.SalesReturns', N'U') IS NULL
CREATE TABLE dbo.SalesReturns (
    Id                   INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceId            INT NOT NULL REFERENCES dbo.SalesInvoices(Id),
    CreatedByEmployeeId  INT NOT NULL REFERENCES dbo.Employees(Id),
    ReviewedByEmployeeId INT NULL REFERENCES dbo.Employees(Id),
    CreatedAt            DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    ReviewedAt           DATETIME2 NULL,
    Reason               NVARCHAR(500) NOT NULL,
    Status               INT NOT NULL DEFAULT 1 CHECK (Status IN (1,2,3,4)), -- 1 Pending, 2 ApprovedRestock, 3 ApprovedDestroy, 4 Rejected
    ReviewNote           NVARCHAR(500) NULL
);
GO
IF OBJECT_ID(N'dbo.SalesReturnLines', N'U') IS NULL
CREATE TABLE dbo.SalesReturnLines (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    ReturnId      INT NOT NULL REFERENCES dbo.SalesReturns(Id) ON DELETE CASCADE,
    InvoiceLineId INT NOT NULL REFERENCES dbo.SalesInvoiceLines(Id),
    Quantity      INT NOT NULL CHECK (Quantity > 0),
    IsIntact      BIT NOT NULL DEFAULT 1
);
GO
