using PharmaCare.Core.Models;

namespace PharmaCare.Core.Services;

/// <summary>Mốc cảnh báo cận hạn cấu hình được (mặc định 90 / 60 / 30 ngày).</summary>
public record ExpiryThresholds(int WatchDays = 90, int WarningDays = 60, int CriticalDays = 30)
{
    public static ExpiryThresholds Default => new();
}

/// <summary>Một dòng cảnh báo hạn dùng cho dashboard.</summary>
public record ExpiryAlert(
    Medicine Medicine, MedicineBatch Batch, int DaysToExpiry, ExpiryLevel Level, int QuantityOnHand);

/// <summary>Phân loại hạn dùng và dựng danh sách cảnh báo cho dashboard dược sĩ.</summary>
public static class ExpiryMonitor
{
    public static ExpiryLevel Classify(DateOnly expiryDate, DateOnly today, ExpiryThresholds? thresholds = null)
    {
        thresholds ??= ExpiryThresholds.Default;
        var days = expiryDate.DayNumber - today.DayNumber;
        if (days < 0) return ExpiryLevel.Expired;
        if (days <= thresholds.CriticalDays) return ExpiryLevel.Critical;
        if (days <= thresholds.WarningDays) return ExpiryLevel.Warning;
        if (days <= thresholds.WatchDays) return ExpiryLevel.Watch;
        return ExpiryLevel.Safe;
    }

    /// <summary>
    /// Cảnh báo cho mọi lô còn hàng (kể cả đã hết hạn) có mức từ Watch trở xuống,
    /// sắp xếp hết hạn trước, rồi theo số ngày tăng dần.
    /// </summary>
    public static IReadOnlyList<ExpiryAlert> BuildAlerts(
        IEnumerable<(Medicine Medicine, MedicineBatch Batch)> stock, DateOnly today, ExpiryThresholds? thresholds = null)
    {
        thresholds ??= ExpiryThresholds.Default;
        return stock
            .Where(x => x.Batch.QuantityOnHand > 0)
            .Select(x => new ExpiryAlert(x.Medicine, x.Batch, x.Batch.DaysToExpiry(today),
                Classify(x.Batch.ExpiryDate, today, thresholds), x.Batch.QuantityOnHand))
            .Where(a => a.Level != ExpiryLevel.Safe)
            .OrderBy(a => a.Level)
            .ThenBy(a => a.DaysToExpiry)
            .ToList();
    }

    /// <summary>Thuốc sắp hết hàng: tổng tồn bán được &lt;= ngưỡng reorder của thuốc.</summary>
    public static bool IsLowStock(Medicine medicine, IEnumerable<MedicineBatch> batches, DateOnly today) =>
        FefoAllocator.SellableStock(batches, medicine.Id, today) <= medicine.ReorderLevel;
}
