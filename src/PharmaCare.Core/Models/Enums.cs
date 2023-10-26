namespace PharmaCare.Core.Models;

/// <summary>Vai trò người dùng hệ thống (khớp ASP.NET Core Identity Roles ở tầng Web).</summary>
public enum UserRole
{
    Admin = 1,        // Quản trị: toàn quyền, cấu hình, phân quyền
    Pharmacist = 2,   // Dược sĩ: duyệt trả hàng, quản lý lô, bán thuốc kê đơn
    Staff = 3         // Nhân viên: bán hàng, nhập liệu cơ bản
}

public enum BatchStatus
{
    Active = 1,       // Đang lưu hành, được phép bán
    Quarantined = 2,  // Cách ly (chờ kiểm tra chất lượng) — không bán
    Depleted = 3      // Đã xuất hết
}

public enum InvoiceStatus
{
    Completed = 1,
    Cancelled = 2
}

public enum PrescriptionStatus
{
    Active = 1,      // Còn hiệu lực
    Dispensed = 2,   // Đã phát thuốc theo đơn
    Expired = 3,     // Hết hiệu lực
    Cancelled = 4
}

/// <summary>Trạng thái xử lý phiếu trả thuốc.</summary>
public enum ReturnStatus
{
    Pending = 1,          // Chờ duyệt
    ApprovedRestock = 2,  // Duyệt — hoàn kho (thuốc còn hạn, nguyên vẹn)
    ApprovedDestroy = 3,  // Duyệt — tiêu hủy (hết hạn / không nguyên vẹn)
    Rejected = 4          // Từ chối trả
}

public enum ExpiryLevel
{
    Expired = 0,   // Đã hết hạn — cấm bán
    Critical = 1,  // <= 30 ngày
    Warning = 2,   // <= 60 ngày
    Watch = 3,     // <= 90 ngày
    Safe = 4       // > 90 ngày
}
