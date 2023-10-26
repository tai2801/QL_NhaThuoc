using PharmaCare.Core.Services;

namespace PharmaCare.Web.Models.ViewModels;

public class DashboardViewModel
{
    public decimal RevenueToday { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public int InvoicesToday { get; set; }
    public int TotalMedicines { get; set; }
    public int TotalBatchesInStock { get; set; }
    public IReadOnlyList<ExpiryAlert> ExpiryAlerts { get; set; } = Array.Empty<ExpiryAlert>();
    public IReadOnlyList<string> LowStockMedicineNames { get; set; } = Array.Empty<string>();
    public int PendingReturns { get; set; }
}

public class PosViewModel
{
    public string? Query { get; set; }
    public List<PosMedicineRow> Results { get; set; } = new();
    public List<Core.Models.Prescription> ActivePrescriptions { get; set; } = new();
}

public class PosMedicineRow
{
    public int MedicineId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public int SellableStock { get; set; }
    public bool RequiresPrescription { get; set; }
    public string? NextExpiryBatch { get; set; }   // Lô sẽ xuất trước theo FEFO (HSD gần nhất)
}

/// <summary>Form POST bán hàng từ màn hình POS: danh sách MedicineId + Quantity + đơn thuốc (nếu có).</summary>
public class CheckoutRequest
{
    public List<int> MedicineIds { get; set; } = new();
    public List<int> Quantities { get; set; } = new();
    public int? CustomerId { get; set; }
    public int? PrescriptionId { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
}
