using PharmaCare.Core.Exceptions;
using PharmaCare.Core.Models;
using PharmaCare.Core.Services;
using Xunit;

namespace PharmaCare.Core.Tests;

/// <summary>Test FEFO + chặn hết hạn + chặn vượt tồn. Ngày "hôm nay" cố định để tất định: 01/11/2023.</summary>
public class FefoAllocatorTests
{
    private static readonly DateOnly Today = new(2023, 11, 1);

    private static Medicine Med(int id = 1) => new()
    {
        Id = id, Code = "T" + id, Name = "Thuốc test", SalePrice = 1000, Unit = "Viên"
    };

    private static MedicineBatch Batch(int id, string expiry, int qty, int medId = 1,
        BatchStatus status = BatchStatus.Active, string mfg = "2022-01-01") => new()
    {
        Id = id, MedicineId = medId, BatchNumber = "L" + id,
        ManufactureDate = DateOnly.Parse(mfg), ExpiryDate = DateOnly.Parse(expiry),
        QuantityReceived = qty, QuantityOnHand = qty, ImportPrice = 500, Status = status
    };

    [Fact]
    public void Allocate_TakesEarliestExpiryFirst()
    {
        var med = Med();
        var batches = new[]
        {
            Batch(1, "2024-06-01", 100),
            Batch(2, "2023-12-01", 50),   // HSD gần nhất
            Batch(3, "2024-01-15", 80)
        };
        var alloc = FefoAllocator.Allocate(med, batches, 30, Today);
        Assert.Single(alloc);
        Assert.Equal(2, alloc[0].Batch.Id);
        Assert.Equal(30, alloc[0].Quantity);
    }

    [Fact]
    public void Allocate_SplitsAcrossBatchesInFefoOrder()
    {
        var med = Med();
        var batches = new[]
        {
            Batch(1, "2024-06-01", 100),
            Batch(2, "2023-12-01", 50),
            Batch(3, "2024-01-15", 80)
        };
        var alloc = FefoAllocator.Allocate(med, batches, 100, Today);
        Assert.Equal(2, alloc.Count);
        Assert.Equal((2, 50), (alloc[0].Batch.Id, alloc[0].Quantity));
        Assert.Equal((3, 50), (alloc[1].Batch.Id, alloc[1].Quantity));
    }

    [Fact]
    public void Allocate_SkipsExpiredBatch_EvenIfItHasStock()
    {
        var med = Med();
        var batches = new[]
        {
            Batch(1, "2023-10-01", 500),  // đã hết hạn trước 01/11/2023
            Batch(2, "2024-03-01", 40)
        };
        var alloc = FefoAllocator.Allocate(med, batches, 40, Today);
        Assert.Single(alloc);
        Assert.Equal(2, alloc[0].Batch.Id);
        Assert.Equal(500, FefoAllocator.ExpiredStock(batches, 1, Today));
    }

    [Fact]
    public void Allocate_OnlyExpiredStock_ThrowsInsufficient_WithZeroAvailable()
    {
        var med = Med();
        var batches = new[] { Batch(1, "2023-01-01", 100) };
        var ex = Assert.Throws<InsufficientStockException>(
            () => FefoAllocator.Allocate(med, batches, 10, Today));
        Assert.Equal(0, ex.Available);
    }

    [Fact]
    public void Allocate_MoreThanSellable_ThrowsInsufficient()
    {
        var med = Med();
        var batches = new[] { Batch(1, "2024-01-01", 30), Batch(2, "2024-02-01", 20) };
        var ex = Assert.Throws<InsufficientStockException>(
            () => FefoAllocator.Allocate(med, batches, 51, Today));
        Assert.Equal(50, ex.Available);
    }

    [Fact]
    public void Allocate_SkipsQuarantinedAndEmptyBatches()
    {
        var med = Med();
        var batches = new[]
        {
            Batch(1, "2023-12-01", 100, status: BatchStatus.Quarantined),
            Batch(2, "2023-12-15", 0),
            Batch(3, "2024-01-01", 25)
        };
        var alloc = FefoAllocator.Allocate(med, batches, 25, Today);
        Assert.Single(alloc);
        Assert.Equal(3, alloc[0].Batch.Id);
    }

    [Fact]
    public void Allocate_NonPositiveQuantity_Throws()
    {
        var med = Med();
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FefoAllocator.Allocate(med, Array.Empty<MedicineBatch>(), 0, Today));
    }

    [Fact]
    public void Allocate_DoesNotMutateBatches()
    {
        var med = Med();
        var b = Batch(1, "2024-01-01", 30);
        FefoAllocator.Allocate(med, new[] { b }, 10, Today);
        Assert.Equal(30, b.QuantityOnHand); // hàm thuần — tầng gọi mới trừ tồn
    }
}
