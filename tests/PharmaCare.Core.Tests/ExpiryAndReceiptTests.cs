using PharmaCare.Core.Models;
using PharmaCare.Core.Services;
using Xunit;

namespace PharmaCare.Core.Tests;

public class ExpiryAndReceiptTests
{
    private static readonly DateOnly Today = new(2023, 11, 1);

    [Theory]
    [InlineData("2023-10-31", ExpiryLevel.Expired)]   // qua HSD
    [InlineData("2023-11-01", ExpiryLevel.Critical)]  // đúng ngày HSD: còn 0 ngày
    [InlineData("2023-12-01", ExpiryLevel.Critical)]  // 30 ngày
    [InlineData("2023-12-15", ExpiryLevel.Warning)]   // 44 ngày
    [InlineData("2023-12-31", ExpiryLevel.Warning)]   // 60 ngày
    [InlineData("2024-01-29", ExpiryLevel.Watch)]     // 89 ngày
    [InlineData("2024-01-30", ExpiryLevel.Watch)]     // 90 ngày
    [InlineData("2024-03-01", ExpiryLevel.Safe)]      // 120 ngày
    public void Classify_UsesConfigurableThresholds(string expiry, ExpiryLevel expected)
    {
        Assert.Equal(expected, ExpiryMonitor.Classify(DateOnly.Parse(expiry), Today));
    }

    [Fact]
    public void BuildAlerts_ExcludesSafe_IncludesExpiredWithStock()
    {
        var med = new Medicine { Id = 1, Code = "P", Name = "Para" };
        MedicineBatch B(int id, string exp, int qty) => new()
        {
            Id = id, MedicineId = 1, BatchNumber = "L" + id,
            ManufactureDate = new DateOnly(2022, 1, 1), ExpiryDate = DateOnly.Parse(exp),
            QuantityOnHand = qty, QuantityReceived = qty, Status = BatchStatus.Active
        };
        var stock = new[]
        {
            (med, B(1, "2023-09-01", 40)),  // hết hạn, còn hàng → cảnh báo đầu tiên
            (med, B(2, "2023-11-20", 10)),  // critical
            (med, B(3, "2025-01-01", 99)),  // safe → không cảnh báo
            (med, B(4, "2023-11-10", 0))    // hết hàng → không cảnh báo
        };
        var alerts = ExpiryMonitor.BuildAlerts(stock, Today);
        Assert.Equal(2, alerts.Count);
        Assert.Equal(ExpiryLevel.Expired, alerts[0].Level);
        Assert.Equal(ExpiryLevel.Critical, alerts[1].Level);
    }

    [Fact]
    public void LowStock_CountsOnlySellableBatches()
    {
        var med = new Medicine { Id = 1, Code = "P", Name = "Para", ReorderLevel = 50 };
        MedicineBatch B(int id, string exp, int qty) => new()
        {
            Id = id, MedicineId = 1, BatchNumber = "L" + id,
            ManufactureDate = new DateOnly(2022, 1, 1), ExpiryDate = DateOnly.Parse(exp),
            QuantityOnHand = qty, QuantityReceived = qty, Status = BatchStatus.Active
        };
        // 500 viên ở lô hết hạn + 30 viên bán được → vẫn coi là sắp hết (30 <= 50)
        var batches = new[] { B(1, "2023-01-01", 500), B(2, "2024-01-01", 30) };
        Assert.True(ExpiryMonitor.IsLowStock(med, batches, Today));
    }

    // ---------- Nhập kho tạo lô ----------

    private static (StockReceipt, StockReceiptLine) ReceiptAndLine(int qty = 100, decimal price = 800m)
    {
        var receipt = new StockReceipt { Id = 1, ReceiptCode = "PN1", SupplierId = 1, ReceivedByEmployeeId = 1 };
        var line = new StockReceiptLine { Id = 1, ReceiptId = 1, MedicineId = 1, Quantity = qty, ImportPrice = price };
        return (receipt, line);
    }

    [Fact]
    public void CreateBatch_Valid_SetsOnHandEqualsReceived()
    {
        var (r, l) = ReceiptAndLine();
        var batch = ReceiptService.CreateBatchForLine(r, l, "L2301",
            new DateOnly(2023, 1, 1), new DateOnly(2025, 1, 1), Today);
        Assert.Equal(100, batch.QuantityReceived);
        Assert.Equal(100, batch.QuantityOnHand);
        Assert.Equal(BatchStatus.Active, batch.Status);
    }

    [Fact]
    public void CreateBatch_ExpiredGoods_Rejected()
    {
        var (r, l) = ReceiptAndLine();
        Assert.Throws<InvalidOperationException>(() => ReceiptService.CreateBatchForLine(r, l, "L1",
            new DateOnly(2021, 1, 1), new DateOnly(2023, 6, 1), Today));
    }

    [Fact]
    public void CreateBatch_ExpiryBeforeManufacture_Rejected()
    {
        var (r, l) = ReceiptAndLine();
        Assert.Throws<InvalidOperationException>(() => ReceiptService.CreateBatchForLine(r, l, "L1",
            new DateOnly(2024, 1, 1), new DateOnly(2023, 6, 1), Today));
    }
}
