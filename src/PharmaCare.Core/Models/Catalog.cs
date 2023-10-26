namespace PharmaCare.Core.Models;

/// <summary>Hoạt chất (vd Paracetamol, Amoxicillin).</summary>
public class ActiveIngredient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>Dạng bào chế (viên nén, viên nang, siro, ống tiêm...).</summary>
public class DosageForm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>Nhóm thuốc (Giảm đau - hạ sốt, Kháng sinh, Vitamin...).</summary>
public class MedicineCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Thuốc (mặt hàng). Tồn kho KHÔNG lưu ở đây — tồn kho nằm ở từng lô (MedicineBatch).
/// </summary>
public class Medicine
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;          // Mã thuốc duy nhất, vd PARA500
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public MedicineCategory? Category { get; set; }
    public int DosageFormId { get; set; }
    public DosageForm? DosageForm { get; set; }
    public string Strength { get; set; } = string.Empty;      // Hàm lượng trình bày: 500mg, 250mg/5ml
    public string Unit { get; set; } = string.Empty;          // Đơn vị bán lẻ nhỏ nhất: Viên, Vỉ, Chai, Tuýp
    public string? RegistrationNumber { get; set; }           // Số đăng ký lưu hành
    public decimal SalePrice { get; set; }                    // Giá bán hiện hành / đơn vị
    public bool RequiresPrescription { get; set; }            // Thuốc kê đơn
    public int ReorderLevel { get; set; } = 20;               // Ngưỡng cảnh báo sắp hết (tổng tồn các lô còn bán được)
    public bool IsActive { get; set; } = true;

    public List<MedicineIngredient> Ingredients { get; set; } = new();
    public List<MedicineBatch> Batches { get; set; } = new();
}

/// <summary>Bảng nối thuốc — hoạt chất (một thuốc có thể nhiều hoạt chất).</summary>
public class MedicineIngredient
{
    public int MedicineId { get; set; }
    public Medicine? Medicine { get; set; }
    public int ActiveIngredientId { get; set; }
    public ActiveIngredient? ActiveIngredient { get; set; }
    public string? StrengthPerUnit { get; set; }              // Hàm lượng của hoạt chất này / 1 đơn vị
}

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? AllergyNote { get; set; }                  // Ghi chú dị ứng (dược sĩ tham chiếu khi bán)
}

public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;      // Khớp tài khoản Identity ở tầng Web
    public UserRole Role { get; set; } = UserRole.Staff;
    public string? PharmacyLicenseNo { get; set; }            // Chứng chỉ hành nghề (với dược sĩ)
    public bool IsActive { get; set; } = true;
}
