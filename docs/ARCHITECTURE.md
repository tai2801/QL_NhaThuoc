# KIẾN TRÚC — PHARMACARE PRO

## 1. Tổng quan

```
┌──────────────────────────────────────────────────────────────┐
│ PharmaCare.Web (ASP.NET Core MVC, .NET 8)                    │
│  Controllers + Razor Views (Bootstrap)                       │
│  ASP.NET Core Identity — Roles: Admin / Pharmacist / Staff   │
│  EF Core 8 → SQL Server (PharmaCarePro)                      │
└───────────────┬──────────────────────────────────────────────┘
                │ ProjectReference
┌───────────────▼──────────────────────────────────────────────┐
│ PharmaCare.Core (class library thuần, không phụ thuộc EF/UI) │
│  Models: Medicine, MedicineBatch, SalesInvoice(Line), ...    │
│  Services: FefoAllocator · ExpiryMonitor · SalesService      │
│            ReturnService · ReceiptService                    │
│  Exceptions: InsufficientStock · ExpiredBatch · ...          │
└───────────────┬──────────────────────────────────────────────┘
                │ test trực tiếp (không cần DB)
┌───────────────▼──────────────────────────────────────────────┐
│ PharmaCare.Core.Tests (xUnit) + tests/python (đối chiếu)     │
└──────────────────────────────────────────────────────────────┘
```

Nguyên tắc: **mọi quy tắc nghiệp vụ quan trọng nằm ở Core** và được test mà không cần SQL Server. Tầng Web chỉ điều phối: đọc EF → gọi Core → ghi EF trong transaction.

## 2. Quyết định thiết kế

| Quyết định | Lý do |
|---|---|
| Tồn kho chỉ ở lô, thuốc không có cột "tồn tổng" | Tránh lệch dữ liệu; tồn của thuốc luôn là tổng suy ra từ các lô |
| FefoAllocator là hàm thuần (không trừ tồn) | Test tất định, tầng gọi quyết định transaction; tránh trừ trùng |
| Dòng hóa đơn tách theo lô + BatchId NOT NULL, FK Restrict | Truy xuất nguồn gốc là yêu cầu pháp lý với dược phẩm; cấm xóa lô đã bán |
| HSD là "ngày cuối còn bán được" (hết hạn khi today > HSD) | Khớp cách ghi hạn dùng trên bao bì Việt Nam (dùng hết ngày in trên nhãn) |
| Mốc cảnh báo 90/60/30 là tham số (ExpiryThresholds + appsettings) | Mỗi nhà thuốc có chính sách xử lý cận hạn khác nhau |
| Phân quyền 2 lớp: [Authorize(Roles)] ở Web + kiểm tra Role trong ReturnService | Duyệt trả là thao tác nhạy cảm — không dựa hoàn toàn vào tầng giao diện |
| SQL schema.sql viết tay khớp EF Models | Môn học yêu cầu nộp cả script DB lẫn EF; hai phía đối chiếu bằng tools/check_consistency.py |

## 3. Luồng dữ liệu bán hàng (sequence rút gọn)

POS (SalesController.Checkout) → nạp Medicines + Batches (EF) → SalesService.BuildInvoiceLines (kiểm tra đơn + FEFO) → SalesService.ApplyAllocations (trừ tồn) → lưu SalesInvoice + Lines (gắn BatchId) → Commit. Mọi exception nghiệp vụ → Rollback + TempData lỗi.

## 4. Bảo mật

- Mật khẩu qua ASP.NET Core Identity (PBKDF2 mặc định), chính sách ≥8 ký tự, có số + chữ hoa.
- Chuỗi kết nối & mật khẩu seed đọc từ appsettings/biến môi trường — repo chỉ chứa placeholder.
- Mọi controller đều `[Authorize]`; thao tác nhạy cảm giới hạn Role (nhập hàng, duyệt trả, báo cáo: Admin/Pharmacist; thêm/sửa thuốc: Admin/Pharmacist).
- Anti-forgery token trên mọi POST.

## 5. Hạn chế đã biết

- POS gửi số lượng theo danh sách thuốc hiển thị (tối đa 50 kết quả tìm kiếm) — đủ cho quy mô đồ án; bản thương mại nên có giỏ hàng phía client.
- Chưa có in hóa đơn PDF, chưa tích hợp máy quét mã vạch (điểm mở rộng tự nhiên: ô tìm kiếm POS đã nhận mã thuốc).
- Identity tables do EF tạo (migrations/database update), không nằm trong schema.sql viết tay — xem README.
