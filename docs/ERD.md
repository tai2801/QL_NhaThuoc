# ERD — PHARMACARE PRO

```mermaid
erDiagram
    MedicineCategories ||--o{ Medicines : "phân loại"
    DosageForms ||--o{ Medicines : "dạng bào chế"
    Medicines ||--o{ MedicineIngredients : "có"
    ActiveIngredients ||--o{ MedicineIngredients : "thuộc"
    Medicines ||--o{ MedicineBatches : "chia theo lô"
    Suppliers ||--o{ StockReceipts : "cung cấp"
    Employees ||--o{ StockReceipts : "nhận hàng"
    StockReceipts ||--o{ StockReceiptLines : "gồm"
    StockReceiptLines }o--|| MedicineBatches : "tạo ra đúng 1 lô"
    Customers ||--o{ Prescriptions : "được kê"
    Prescriptions ||--o{ PrescriptionLines : "gồm"
    Medicines ||--o{ PrescriptionLines : "được kê"
    Customers ||--o{ SalesInvoices : "mua"
    Employees ||--o{ SalesInvoices : "bán"
    Prescriptions ||--o{ SalesInvoices : "bán theo đơn"
    SalesInvoices ||--o{ SalesInvoiceLines : "gồm"
    MedicineBatches ||--o{ SalesInvoiceLines : "lô nguồn (FEFO)"
    SalesInvoices ||--o{ SalesReturns : "bị trả"
    SalesReturns ||--o{ SalesReturnLines : "gồm"
    SalesInvoiceLines ||--o{ SalesReturnLines : "trả theo dòng"

    Medicines {
        int Id PK
        string Code UK
        string Name
        decimal SalePrice
        bool RequiresPrescription
        int ReorderLevel
    }
    MedicineBatches {
        int Id PK
        int MedicineId FK
        string BatchNumber
        date ManufactureDate
        date ExpiryDate
        int QuantityReceived
        int QuantityOnHand
        decimal ImportPrice
        int Status
    }
    SalesInvoiceLines {
        int Id PK
        int InvoiceId FK
        int MedicineId FK
        int BatchId FK "lô nguồn - bắt buộc"
        int Quantity
        decimal UnitPrice
    }
```

## Đọc ERD

- **MedicineBatches** là thực thể trung tâm: mọi biến động tồn kho đều qua lô.
- **StockReceiptLines → MedicineBatches** là quan hệ tạo sinh: mỗi dòng nhập sinh đúng 1 lô.
- **SalesInvoiceLines → MedicineBatches** là quan hệ truy xuất: mỗi dòng bán chỉ rõ lô nguồn, cho phép tra ngược hóa đơn → lô → phiếu nhập → nhà cung cấp khi có thông báo thu hồi thuốc.
- Bảng Identity của ASP.NET Core (AspNetUsers, AspNetRoles...) quản lý tài khoản đăng nhập; `Employees.Username` liên kết mềm tới tài khoản Identity.
