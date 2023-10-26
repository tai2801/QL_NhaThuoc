using PharmaCare.Core.Exceptions;
using PharmaCare.Core.Models;

namespace PharmaCare.Core.Services;

/// <summary>Một dòng yêu cầu bán (trước khi FEFO tách lô).</summary>
public record SaleLineRequest(Medicine Medicine, int Quantity);

/// <summary>
/// Quy tắc bán hàng cấp domain (tầng Web gọi trước khi ghi EF):
///  - Thuốc kê đơn phải có đơn thuốc còn hiệu lực (Active và chưa quá ValidUntil).
///  - Phân bổ FEFO cho từng dòng; dòng hóa đơn sinh ra LUÔN gắn BatchId (truy xuất lô nguồn).
///  - Chặn bán lô hết hạn nằm trong chính sách của FefoAllocator (lô hết hạn không bao giờ được chọn).
/// </summary>
public static class SalesService
{
    public static IReadOnlyList<SalesInvoiceLine> BuildInvoiceLines(
        IEnumerable<SaleLineRequest> requests,
        IEnumerable<MedicineBatch> allBatches,
        Prescription? prescription,
        DateOnly today)
    {
        var batchList = allBatches.ToList();
        var lines = new List<SalesInvoiceLine>();

        foreach (var req in requests)
        {
            if (req.Medicine.RequiresPrescription)
            {
                var valid = prescription is not null
                    && prescription.Status == PrescriptionStatus.Active
                    && (prescription.ValidUntil is null || today <= prescription.ValidUntil.Value);
                if (!valid) throw new PrescriptionRequiredException(req.Medicine.Name);
            }

            foreach (var alloc in FefoAllocator.Allocate(req.Medicine, batchList, req.Quantity, today))
            {
                lines.Add(new SalesInvoiceLine
                {
                    MedicineId = req.Medicine.Id,
                    Medicine = req.Medicine,
                    BatchId = alloc.Batch.Id,
                    Batch = alloc.Batch,
                    Quantity = alloc.Quantity,
                    UnitPrice = req.Medicine.SalePrice
                });
            }
        }
        return lines;
    }

    /// <summary>Áp dụng phân bổ vào tồn lô (gọi trong transaction sau khi BuildInvoiceLines thành công).</summary>
    public static void ApplyAllocations(IEnumerable<SalesInvoiceLine> lines, IEnumerable<MedicineBatch> batches)
    {
        var byId = batches.ToDictionary(b => b.Id);
        foreach (var line in lines)
        {
            var batch = byId[line.BatchId];
            if (batch.IsExpired(DateOnly.FromDateTime(DateTime.Today)))
                throw new ExpiredBatchException(batch.BatchNumber, batch.ExpiryDate);
            if (line.Quantity > batch.QuantityOnHand)
                throw new InsufficientStockException(line.MedicineId, line.Quantity, batch.QuantityOnHand);
            batch.QuantityOnHand -= line.Quantity;
            if (batch.QuantityOnHand == 0 && batch.Status == BatchStatus.Active)
                batch.Status = BatchStatus.Depleted;
        }
    }
}
