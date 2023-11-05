# PharmaCare Pro — Phần mềm quản lý nhà thuốc (Nhóm 2 HUIT)

Môn: **Công nghệ .NET**. Stack: **C# .NET 8 · ASP.NET Core MVC · EF Core 8 · SQL Server · ASP.NET Core Identity · Bootstrap 5**.

Điểm khác biệt cốt lõi: quản lý thuốc **theo lô** với **FEFO** (First Expired, First Out) — mỗi lần nhập tạo một lô riêng (số lô, NSX, HSD, giá nhập), bán hàng tự xuất lô hạn gần nhất, **chặn tuyệt đối thuốc hết hạn**, và **mỗi dòng hóa đơn ghi rõ lô nguồn** để truy xuất nguồn gốc.

## Chức năng

- **Dashboard dược sĩ**: doanh thu hôm nay/tháng, bảng cảnh báo hạn dùng (mốc cấu hình 90/60/30 ngày), thuốc sắp hết hàng, phiếu trả chờ duyệt.
- **Danh mục thuốc**: hoạt chất (nhiều hoạt chất/thuốc), dạng bào chế, nhóm thuốc; tìm theo tên / mã / hoạt chất.
- **Nhập hàng theo lô**: mỗi dòng nhập = 1 lô mới; từ chối nhận hàng đã hết hạn.
- **Bán thuốc (POS)**: tìm nhanh, hiện tồn *bán được* + lô FEFO sẽ xuất; thuốc kê đơn bắt buộc gắn đơn còn hiệu lực; trừ tồn theo lô trong transaction.
- **Đơn thuốc**: bác sĩ kê, đơn vị kê, chẩn đoán, hiệu lực, chi tiết liều dùng.
- **Trả thuốc**: bắt buộc lý do; dược sĩ duyệt — hoàn kho chỉ khi hàng nguyên vẹn và lô chưa hết hạn, ngược lại tiêu hủy/từ chối.
- **Báo cáo**: doanh thu theo ngày, top bán chạy, tồn kho theo lô, giá vốn theo giá nhập của lô nguồn.
- **Phân quyền**: Admin / Pharmacist (Dược sĩ) / Staff (Nhân viên) bằng Identity Roles + kiểm tra lại ở tầng Core cho thao tác duyệt trả.

## Cấu trúc

```
PharmaCarePro.sln
├── src/PharmaCare.Core/     # Domain thuần: Models + Services (FEFO, cảnh báo hạn, bán, trả, nhập) + Exceptions
├── src/PharmaCare.Web/      # ASP.NET Core MVC + EF Core + Identity (Controllers, Views Bootstrap)
├── database/                # schema.sql + seed.sql (SQL Server, khớp 1-1 với EF Models)
├── docs/                    # BUSINESS_ANALYSIS, ARCHITECTURE, ERD, DATA_DICTIONARY
├── tests/PharmaCare.Core.Tests/  # xUnit — test logic FEFO/hết hạn/kê đơn/trả hàng/nhập kho
├── tests/python/            # Test Python mô hình hóa — đối chiếu độc lập logic Core
└── tools/check_consistency.py    # Kiểm tra nhất quán schema.sql ↔ EF Models ↔ seed.sql
```

## Chạy dự án

### Yêu cầu
- .NET 8 SDK
- SQL Server 2019+ (hoặc Docker: `docker compose up -d` — xem `docker-compose.yml`)

### 1. Tạo database
```bash
sqlcmd -S localhost,1433 -U sa -P "<mat-khau>" -i database/schema.sql
sqlcmd -S localhost,1433 -U sa -P "<mat-khau>" -i database/seed.sql
# Bảng Identity (AspNetUsers...) do EF tạo:
cd src/PharmaCare.Web && dotnet ef database update
```

### 2. Cấu hình
Sao chép `.env.example` → cấu hình thực, hoặc sửa `src/PharmaCare.Web/appsettings.json`:
- `ConnectionStrings:DefaultConnection` — chuỗi kết nối SQL Server (dùng mật khẩu của bạn, không commit).
- `Seed:AdminPassword` — mật khẩu tài khoản `admin` tạo lần chạy đầu (≥8 ký tự, có số + chữ hoa). Để trống = không seed admin.

### 3. Chạy web
```bash
dotnet run --project src/PharmaCare.Web
```
Đăng nhập bằng tài khoản đã seed (vai trò Admin), tạo thêm tài khoản Dược sĩ/Nhân viên qua trang Identity/Admin, và tạo bản ghi Employees trùng Username để gắn nghiệp vụ.

### 4. Chạy test
```bash
dotnet test tests/PharmaCare.Core.Tests
python3 -m unittest discover -s tests/python -v
python3 tools/check_consistency.py
```

## Kết quả kiểm thử

Xem `TEST_REPORT.md`. Toàn bộ logic nghiệp vụ trong `PharmaCare.Core` đã được kiểm thử bằng xUnit (32 test case), bộ test Python đối chiếu độc lập (11 test case) và công cụ kiểm tra tính nhất quán giữa `database/schema.sql`, EF Core Models và `database/seed.sql`.

## Dữ liệu mẫu đáng chú ý (seed.sql)

- Lô Paracetamol `L-P21A` **đã hết hạn** còn tồn 120 — FEFO bỏ qua, bán bị chặn.
- Các lô cận hạn ở cả 3 mốc Critical/Warning/Watch để xem dashboard cảnh báo.
- Hóa đơn mẫu có dòng gắn lô nguồn; 1 đơn thuốc + 1 phiếu trả chờ duyệt.
- Khách hàng Trần Văn Bình có ghi chú dị ứng penicillin.

## Giấy phép

MIT License — Nhóm 2 HUIT (2023).
