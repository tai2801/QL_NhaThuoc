# PHÂN TÍCH NGHIỆP VỤ — PHARMACARE PRO

> Môn: Công nghệ .NET — Đồ án phần mềm quản lý nhà thuốc theo lô và thuật toán FEFO.

## 1. Bối cảnh

Nhà thuốc quy mô vừa (1 cơ sở, 5–15 nhân viên). Vấn đề trung tâm: thuốc là hàng hóa **có hạn dùng** — quản lý tồn kho theo tổng số lượng (như nhiều phần mềm đơn giản) gây 3 rủi ro: bán nhầm thuốc hết hạn, không truy xuất được lô khi có thông báo thu hồi, và hàng cận hạn bị ứ đọng đến khi phải hủy. PharmaCare Pro đặt **LÔ** làm đơn vị tồn kho trung tâm và áp dụng **FEFO** khi bán.

## 2. Actors

| Actor | Mô tả |
|---|---|
| Quản trị (Admin) | Quản lý tài khoản/vai trò, danh mục, cấu hình mốc cảnh báo, xem mọi báo cáo |
| Dược sĩ (Pharmacist) | Bán thuốc kê đơn, nhập hàng theo lô, duyệt phiếu trả, xử lý cảnh báo hạn dùng |
| Nhân viên (Staff) | Bán thuốc không kê đơn, tạo phiếu trả (không được duyệt), nhập liệu khách hàng |
| Khách hàng | Mua thuốc, mang đơn thuốc, yêu cầu trả thuốc |

## 3. Use Cases chính

| Mã | Use case | Actor chính | Ghi chú |
|---|---|---|---|
| UC01 | Đăng nhập & phân quyền | Tất cả | Identity + Roles Admin/Pharmacist/Staff |
| UC02 | Quản lý danh mục thuốc (hoạt chất, dạng bào chế, nhóm) | Admin, Dược sĩ | Tìm theo tên/mã/hoạt chất |
| UC03 | Nhập thuốc theo lô | Dược sĩ, Admin | Mỗi dòng nhập tạo đúng 1 lô (NSX, HSD, giá nhập) |
| UC04 | Xem tồn kho theo lô & cảnh báo hạn | Dược sĩ | Lọc theo mức Expired/Critical/Warning/Watch |
| UC05 | Bán thuốc (POS) | Nhân viên, Dược sĩ | FEFO tự xuất lô; kê đơn phải gắn đơn hợp lệ |
| UC06 | Quản lý đơn thuốc kê đơn | Dược sĩ | Bác sĩ/đơn vị kê, chẩn đoán, hiệu lực |
| UC07 | Trả thuốc & duyệt trả | Nhân viên (tạo), Dược sĩ (duyệt) | Hoàn kho chỉ khi còn hạn + nguyên vẹn |
| UC08 | Dashboard dược sĩ | Dược sĩ | Doanh thu, bảng cảnh báo hạn, sắp hết hàng |
| UC09 | Báo cáo doanh thu & tồn kho | Admin, Dược sĩ | Giá vốn theo giá nhập của lô nguồn |
| UC10 | Truy xuất lô nguồn của hóa đơn | Dược sĩ | Mỗi dòng hóa đơn → BatchId → phiếu nhập → NCC |

## 4. Business Rules

| Mã | Quy tắc |
|---|---|
| BR01 | Mỗi lần nhập tạo 1 lô riêng: số lô, NSX, HSD, số lượng, giá nhập độc lập. Không cộng dồn vào "tồn tổng" của thuốc. |
| BR02 | Bán hàng theo FEFO: lô HSD gần nhất xuất trước; một dòng mua có thể tách nhiều lô, mỗi phần là 1 dòng hóa đơn gắn đúng BatchId. |
| BR03 | Lô đã hết hạn (quá HSD) TUYỆT ĐỐI không được bán — FEFO bỏ qua hoàn toàn; lô cách ly (Quarantined) và lô hết hàng cũng bị bỏ qua. |
| BR04 | Không bán vượt tổng tồn của các lô còn bán được (sau khi loại lô hết hạn/cách ly). |
| BR05 | Thuốc kê đơn chỉ bán khi gắn đơn thuốc trạng thái Active và còn hiệu lực (ValidUntil). |
| BR06 | Cảnh báo cận hạn theo mốc cấu hình được: ≤90 ngày Watch, ≤60 Warning, ≤30 Critical, quá hạn Expired. |
| BR07 | Sắp hết hàng tính trên tồn BÁN ĐƯỢC (không tính lô hết hạn) so với ReorderLevel của thuốc. |
| BR08 | Phiếu trả bắt buộc có lý do; số lượng trả ≤ số đã bán ở đúng dòng hóa đơn (trừ phần đã trả trước). |
| BR09 | Duyệt hoàn kho chỉ khi: hàng nguyên vẹn VÀ lô nguồn chưa hết hạn tại ngày duyệt. Ngược lại chỉ tiêu hủy hoặc từ chối. Nhân viên không được duyệt. |
| BR10 | Hoàn kho cộng lại ĐÚNG lô nguồn (không tạo lô mới) để giữ truy xuất 2 chiều. |
| BR11 | Không nhận hàng nhập đã hết hạn; HSD phải sau NSX và cách NSX ≥ 30 ngày. |
| BR12 | Giá bán dòng hóa đơn là snapshot tại thời điểm bán; giá vốn báo cáo lấy theo giá nhập của lô nguồn. |

## 5. Luồng chính — Bán thuốc FEFO (UC05)

1. Nhân viên tìm thuốc (tên/mã/hoạt chất) ở POS → hệ thống hiện tồn bán được + lô sẽ xuất trước.
2. Nhập số lượng; nếu thuốc kê đơn → chọn đơn thuốc.
3. Hệ thống kiểm tra đơn (BR05), chạy FEFO (BR02–BR04) sinh dòng hóa đơn gắn lô.
4. Trừ tồn từng lô trong cùng transaction; lô về 0 → Depleted.
5. In hóa đơn; khách cần tra lô → mở chi tiết hóa đơn (UC10).

**Luồng thay thế:** 3a. Không đủ tồn bán được → báo rõ số còn bán được, hủy toàn bộ giao dịch (không bán một phần). 3b. Thuốc kê đơn thiếu đơn → chặn, yêu cầu gắn đơn.

**Luồng ngoại lệ:** Toàn bộ thuốc chỉ còn ở lô hết hạn → coi như tồn bán được = 0, báo "chỉ còn lô đã hết hạn — cần xử lý tiêu hủy", không cho bán.

## 6. Luồng chính — Trả thuốc (UC07)

1. Nhân viên mở hóa đơn, chọn dòng, nhập số lượng + lý do + tình trạng nguyên vẹn → phiếu Pending.
2. Dược sĩ duyệt: Hoàn kho (kiểm tra BR09) / Tiêu hủy / Từ chối.
3. Hoàn kho → cộng tồn đúng lô nguồn; tiêu hủy → không cộng tồn, giữ lịch sử.

## 7. Điểm nổi bật của giải pháp

1. Quản lý tồn kho lấy **Lô thuốc** làm trung tâm, kiểm soát chặt hạn sử dụng và giá nhập từng lô.
2. Phân bổ xuất kho FEFO tự động kèm truy xuất lô nguồn trên từng dòng hóa đơn.
3. Cảnh báo cận hạn đa mốc cấu hình được (90/60/30 ngày).
4. Quy trình trả thuốc có kiểm soát trạng thái và điều kiện hoàn kho theo hạn sử dụng của lô nguồn.
5. Tách biệt thư viện nghiệp vụ `PharmaCare.Core` độc lập với giao diện và cơ sở dữ liệu, thuận tiện kiểm thử tự động bằng xUnit.
