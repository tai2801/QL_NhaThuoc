using PharmaCare.Core.Exceptions;
using PharmaCare.Core.Models;

namespace PharmaCare.Core.Services;

/// <summary>Một phần phân bổ: lấy Quantity đơn vị từ lô Batch.</summary>
public record BatchAllocation(MedicineBatch Batch, int Quantity);

/// <summary>
/// Bộ máy phân bổ FEFO (First Expired, First Out) — trái tim của PharmaCare.
///
/// Quy tắc:
///  1. Chỉ xét lô CÒN BÁN ĐƯỢC: Status = Active, QuantityOnHand &gt; 0, chưa hết hạn tại ngày bán.
///     Lô đã hết hạn bị bỏ qua hoàn toàn (và được thống kê riêng qua ExpiredQuantity bị chặn).
///  2. Sắp xếp theo HSD tăng dần → NSX tăng dần → Id tăng dần (ổn định, tất định).
///  3. Lấy tuần tự cho đến khi đủ số lượng; một yêu cầu có thể tách qua nhiều lô.
///  4. Không đủ tồn ở các lô bán được → InsufficientStockException (không phân bổ một phần).
/// Hàm này THUẦN (không sửa dữ liệu): tầng gọi (Web/EF) áp dụng trừ tồn trong cùng transaction.
/// </summary>
public static class FefoAllocator
{
    public static IReadOnlyList<BatchAllocation> Allocate(
        Medicine medicine, IEnumerable<MedicineBatch> batches, int quantity, DateOnly today)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Số lượng bán phải lớn hơn 0.");

        var candidates = batches
            .Where(b => b.MedicineId == medicine.Id && b.IsSellable(today))
            .OrderBy(b => b.ExpiryDate)
            .ThenBy(b => b.ManufactureDate)
            .ThenBy(b => b.Id)
            .ToList();

        var available = candidates.Sum(b => b.QuantityOnHand);
        if (available < quantity)
            throw new InsufficientStockException(medicine.Id, quantity, available);

        var result = new List<BatchAllocation>();
        var remaining = quantity;
        foreach (var batch in candidates)
        {
            if (remaining == 0) break;
            var take = Math.Min(remaining, batch.QuantityOnHand);
            result.Add(new BatchAllocation(batch, take));
            remaining -= take;
        }
        return result;
    }

    /// <summary>Tổng tồn còn bán được của một thuốc tại một ngày (loại lô hết hạn/cách ly).</summary>
    public static int SellableStock(IEnumerable<MedicineBatch> batches, int medicineId, DateOnly today) =>
        batches.Where(b => b.MedicineId == medicineId && b.IsSellable(today)).Sum(b => b.QuantityOnHand);

    /// <summary>Tổng số lượng đang kẹt ở lô đã hết hạn (để báo cáo xử lý tiêu hủy).</summary>
    public static int ExpiredStock(IEnumerable<MedicineBatch> batches, int medicineId, DateOnly today) =>
        batches.Where(b => b.MedicineId == medicineId && b.IsExpired(today)).Sum(b => b.QuantityOnHand);
}
