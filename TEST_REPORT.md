# TEST REPORT — PHARMACARE PRO

Ngày chạy: 05/11/2023 · Môi trường: Windows 10, .NET SDK 8.0, SQL Server 2022, Python 3.11.

## 1. Build Solution — PASS

```
dotnet build src/PharmaCare.Core          → Build succeeded, 0 Warning, 0 Error
dotnet build src/PharmaCare.Web           → Build succeeded, 0 Warning, 0 Error
dotnet build PharmaCarePro.sln            → Build succeeded, 0 Error
```

## 2. Kiểm thử logic nghiệp vụ — PASS

### 2a. xUnit (`PharmaCare.Core.Tests`) — 32/32 PASS
```
=== TEST EXECUTION SUMMARY ===
PharmaCare.Core.Tests  Total: 32, Errors: 0, Failed: 0, Skipped: 0, Time: 0.16s
```
Phân bổ kịch bản kiểm thử:
- **FEFO (8 test):** xuất lô có hạn sử dụng gần nhất trước, tự động tách dòng khi mua vượt tồn 1 lô, bỏ qua lô đã hết hạn / đang cách ly (`Quarantined`) / hết hàng, chặn bán vượt tổng tồn khả dụng, hàm phân bổ thuần không làm biến đổi trạng thái đầu vào.
- **Bán hàng & trả thuốc (11 test):** mọi dòng hóa đơn luôn gắn mã lô nguồn (`BatchId`), thuốc kê đơn bắt buộc gắn đơn thuốc còn hiệu lực, trừ tồn kho và chuyển trạng thái `Depleted` khi lô về 0, chặn phiếu trả thiếu lý do hoặc trả vượt số lượng đã bán, chặn hoàn kho với lô đã hết hạn hoặc hàng không còn nguyên vẹn, chặn nhân viên (`Staff`) duyệt phiếu trả, hoàn kho cộng lại đúng lô nguồn.
- **Cảnh báo hạn dùng & nhập kho (13 test):** phân loại mốc hạn dùng 90/60/30 ngày theo 8 giá trị biên, danh sách cảnh báo loại trừ lô `Safe` và lô đã hết hàng, cảnh báo sắp hết hàng chỉ tính trên tồn bán được, tạo lô mới khi nhập kho và chặn nhập hàng đã hết hạn hoặc ngày HSD trước NSX.

### 2b. Test Python đối chiếu (`tests/python`) — 11/11 PASS
```
python -m unittest discover -s tests/python -v → Ran 11 tests — OK
```
Kiểm chứng độc lập quy tắc FEFO, phân loại hạn dùng và điều kiện hoàn kho.

### 2c. Công cụ kiểm tra nhất quán (`tools/check_consistency.py`) — OK
- 17/17 bảng trong `database/schema.sql` khớp với các `DbSet` trong EF Core `DbContext`.
- 12 lô thuốc trong `database/seed.sql` khớp công thức tồn kho: `QuantityOnHand = QuantityReceived − Đã bán (+ Hoàn kho)`.
- Lô đã hết hạn (`#1`) không xuất hiện trong bất kỳ dòng hóa đơn bán hàng nào.
