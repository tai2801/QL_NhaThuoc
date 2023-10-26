namespace PharmaCare.Core.Models;

/// <summary>Đơn thuốc kê đơn (từ bác sĩ / cơ sở khám chữa bệnh bên ngoài).</summary>
public class Prescription
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;          // Mã đơn, vd DT2023-0001
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string DoctorName { get; set; } = string.Empty;    // Bác sĩ kê đơn
    public string IssuingUnit { get; set; } = string.Empty;   // Đơn vị kê đơn (BV / phòng khám)
    public string? Diagnosis { get; set; }                    // Chẩn đoán
    public DateOnly IssuedDate { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Active;
    public string? Note { get; set; }
    public List<PrescriptionLine> Lines { get; set; } = new();
}

public class PrescriptionLine
{
    public int Id { get; set; }
    public int PrescriptionId { get; set; }
    public Prescription? Prescription { get; set; }
    public int MedicineId { get; set; }
    public Medicine? Medicine { get; set; }
    public string DosageInstruction { get; set; } = string.Empty; // vd "Sáng 1 viên, tối 1 viên sau ăn"
    public int QuantityPrescribed { get; set; }
}

/// <summary>Hóa đơn bán thuốc.</summary>
public class SalesInvoice
{
    public int Id { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;   // vd HD2023-0001
    public int? CustomerId { get; set; }                      // null = khách lẻ
    public Customer? Customer { get; set; }
    public int SoldByEmployeeId { get; set; }
    public Employee? SoldBy { get; set; }
    public int? PrescriptionId { get; set; }                  // Bán theo đơn (nếu có)
    public Prescription? Prescription { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal DiscountAmount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";       // Cash / Card / Transfer
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Completed;
    public List<SalesInvoiceLine> Lines { get; set; } = new();

    public decimal TotalAmount => Lines.Sum(l => l.LineTotal);
    public decimal FinalAmount => TotalAmount - DiscountAmount;
}

/// <summary>
/// Dòng hóa đơn — LUÔN ghi rõ LÔ NGUỒN (BatchId) để truy xuất nguồn gốc từng viên thuốc đã bán.
/// Một thuốc mua số lượng lớn có thể tách thành nhiều dòng (mỗi dòng một lô) do FEFO.
/// </summary>
public class SalesInvoiceLine
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public SalesInvoice? Invoice { get; set; }
    public int MedicineId { get; set; }
    public Medicine? Medicine { get; set; }
    public int BatchId { get; set; }                          // Lô nguồn — bắt buộc, không null
    public MedicineBatch? Batch { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }                    // Giá bán tại thời điểm bán (snapshot)
    public decimal LineTotal => Quantity * UnitPrice;
}

/// <summary>Phiếu trả thuốc — bắt buộc có lý do và đi qua trạng thái xử lý.</summary>
public class SalesReturn
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public SalesInvoice? Invoice { get; set; }
    public int CreatedByEmployeeId { get; set; }
    public Employee? CreatedBy { get; set; }
    public int? ReviewedByEmployeeId { get; set; }            // Dược sĩ / Admin duyệt
    public Employee? ReviewedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string Reason { get; set; } = string.Empty;        // Lý do trả — bắt buộc
    public ReturnStatus Status { get; set; } = ReturnStatus.Pending;
    public string? ReviewNote { get; set; }
    public List<SalesReturnLine> Lines { get; set; } = new();
}

public class SalesReturnLine
{
    public int Id { get; set; }
    public int ReturnId { get; set; }
    public SalesReturn? Return { get; set; }
    public int InvoiceLineId { get; set; }                    // Trả theo đúng dòng hóa đơn (đúng lô nguồn)
    public SalesInvoiceLine? InvoiceLine { get; set; }
    public int Quantity { get; set; }
    public bool IsIntact { get; set; }                        // Còn nguyên vẹn (vỉ chưa bóc, hộp nguyên seal)
}
