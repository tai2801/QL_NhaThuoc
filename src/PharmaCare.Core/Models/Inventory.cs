namespace PharmaCare.Core.Models;

/// <summary>Phiếu nhập hàng từ nhà cung cấp. MỖI dòng nhập tạo ra ĐÚNG MỘT lô mới.</summary>
public class StockReceipt
{
    public int Id { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;   // vd PN2023-0001
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public int ReceivedByEmployeeId { get; set; }
    public Employee? ReceivedBy { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string? SupplierInvoiceNo { get; set; }            // Số hóa đơn của NCC
    public string? Note { get; set; }
    public List<StockReceiptLine> Lines { get; set; } = new();

    public decimal TotalImportValue => Lines.Sum(l => l.Quantity * l.ImportPrice);
}

/// <summary>Dòng phiếu nhập — gắn 1-1 với lô được tạo ra.</summary>
public class StockReceiptLine
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public StockReceipt? Receipt { get; set; }
    public int MedicineId { get; set; }
    public Medicine? Medicine { get; set; }
    public int BatchId { get; set; }
    public MedicineBatch? Batch { get; set; }
    public int Quantity { get; set; }
    public decimal ImportPrice { get; set; }                  // Giá nhập / đơn vị của lần nhập này
}

/// <summary>
/// LÔ THUỐC — đơn vị tồn kho trung tâm của PharmaCare.
/// Mỗi lần nhập tạo 1 lô riêng với số lô, NSX, HSD, số lượng và giá nhập riêng.
/// </summary>
public class MedicineBatch
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public Medicine? Medicine { get; set; }
    public string BatchNumber { get; set; } = string.Empty;   // Số lô nhà sản xuất, vd L230901A
    public DateOnly ManufactureDate { get; set; }             // Ngày sản xuất
    public DateOnly ExpiryDate { get; set; }                  // Hạn sử dụng (ngày cuối còn được bán)
    public int QuantityReceived { get; set; }                 // Số lượng nhập ban đầu
    public int QuantityOnHand { get; set; }                   // Tồn hiện tại của lô
    public decimal ImportPrice { get; set; }
    public BatchStatus Status { get; set; } = BatchStatus.Active;
    public DateTime CreatedAt { get; set; }

    /// <summary>Đã hết hạn tại ngày cho trước (quá ngày HSD).</summary>
    public bool IsExpired(DateOnly today) => today > ExpiryDate;

    /// <summary>Còn bán được: còn hàng, đang Active và chưa hết hạn.</summary>
    public bool IsSellable(DateOnly today) =>
        Status == BatchStatus.Active && QuantityOnHand > 0 && !IsExpired(today);

    public int DaysToExpiry(DateOnly today) => ExpiryDate.DayNumber - today.DayNumber;
}
