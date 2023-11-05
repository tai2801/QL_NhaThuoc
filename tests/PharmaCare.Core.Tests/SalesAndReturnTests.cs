using PharmaCare.Core.Exceptions;
using PharmaCare.Core.Models;
using PharmaCare.Core.Services;
using Xunit;

namespace PharmaCare.Core.Tests;

public class SalesAndReturnTests
{
    private static readonly DateOnly Today = new(2023, 11, 1);

    private static MedicineBatch Batch(int id, string expiry, int qty, int medId = 1) => new()
    {
        Id = id, MedicineId = medId, BatchNumber = "L" + id,
        ManufactureDate = new DateOnly(2022, 1, 1), ExpiryDate = DateOnly.Parse(expiry),
        QuantityReceived = qty, QuantityOnHand = qty, ImportPrice = 500, Status = BatchStatus.Active
    };

    [Fact]
    public void BuildInvoiceLines_EveryLineCarriesSourceBatch()
    {
        var med = new Medicine { Id = 1, Code = "P", Name = "Para", SalePrice = 2000, Unit = "Viên" };
        var batches = new[] { Batch(1, "2023-12-01", 10), Batch(2, "2024-05-01", 100) };
        var lines = SalesService.BuildInvoiceLines(
            new[] { new SaleLineRequest(med, 15) }, batches, null, Today);
        Assert.Equal(2, lines.Count);
        Assert.All(lines, l => Assert.True(l.BatchId > 0));
        Assert.Equal(10, lines[0].Quantity);
        Assert.Equal(5, lines[1].Quantity);
        Assert.Equal(15 * 2000m, lines.Sum(l => l.LineTotal));
    }

    [Fact]
    public void PrescriptionMedicine_WithoutPrescription_Throws()
    {
        var med = new Medicine { Id = 1, Code = "AMX", Name = "Amoxicillin", SalePrice = 3000, RequiresPrescription = true };
        var batches = new[] { Batch(1, "2024-01-01", 100) };
        Assert.Throws<PrescriptionRequiredException>(() => SalesService.BuildInvoiceLines(
            new[] { new SaleLineRequest(med, 10) }, batches, null, Today));
    }

    [Fact]
    public void PrescriptionMedicine_WithExpiredPrescription_Throws()
    {
        var med = new Medicine { Id = 1, Code = "AMX", Name = "Amoxicillin", SalePrice = 3000, RequiresPrescription = true };
        var batches = new[] { Batch(1, "2024-01-01", 100) };
        var rx = new Prescription
        {
            Id = 1, Code = "DT1", Status = PrescriptionStatus.Active,
            ValidUntil = new DateOnly(2023, 10, 1) // đã quá hạn hiệu lực
        };
        Assert.Throws<PrescriptionRequiredException>(() => SalesService.BuildInvoiceLines(
            new[] { new SaleLineRequest(med, 10) }, batches, rx, Today));
    }

    [Fact]
    public void PrescriptionMedicine_WithValidPrescription_Sells()
    {
        var med = new Medicine { Id = 1, Code = "AMX", Name = "Amoxicillin", SalePrice = 3000, RequiresPrescription = true };
        var batches = new[] { Batch(1, "2024-01-01", 100) };
        var rx = new Prescription
        {
            Id = 1, Code = "DT1", Status = PrescriptionStatus.Active,
            ValidUntil = new DateOnly(2023, 12, 31)
        };
        var lines = SalesService.BuildInvoiceLines(new[] { new SaleLineRequest(med, 21) }, batches, rx, Today);
        Assert.Single(lines);
        Assert.Equal(21, lines[0].Quantity);
    }

    [Fact]
    public void ApplyAllocations_DecrementsAndMarksDepleted()
    {
        var med = new Medicine { Id = 1, Code = "P", Name = "Para", SalePrice = 1000 };
        var b = Batch(1, "2024-01-01", 10);
        var lines = SalesService.BuildInvoiceLines(new[] { new SaleLineRequest(med, 10) }, new[] { b }, null, Today);
        SalesService.ApplyAllocations(lines, new[] { b });
        Assert.Equal(0, b.QuantityOnHand);
        Assert.Equal(BatchStatus.Depleted, b.Status);
    }

    // ---------- Trả thuốc ----------

    private static SalesReturn MakeReturn(int soldLineId = 1, int qty = 5, bool intact = true, string reason = "Khách dị ứng nhẹ") =>
        new()
        {
            Id = 1, InvoiceId = 1, Reason = reason, Status = ReturnStatus.Pending,
            Lines = new List<SalesReturnLine>
            {
                new() { Id = 1, ReturnId = 1, InvoiceLineId = soldLineId, Quantity = qty, IsIntact = intact }
            }
        };

    [Fact]
    public void Return_WithoutReason_Rejected()
    {
        var ret = MakeReturn(reason: "  ");
        Assert.Throws<InvalidReturnStateException>(() =>
            ReturnService.ValidateCreate(ret, new Dictionary<int, int> { [1] = 10 }));
    }

    [Fact]
    public void Return_MoreThanSold_Rejected()
    {
        var ret = MakeReturn(qty: 11);
        Assert.Throws<InvalidReturnStateException>(() =>
            ReturnService.ValidateCreate(ret, new Dictionary<int, int> { [1] = 10 }));
    }

    [Fact]
    public void Restock_ExpiredBatch_Rejected()
    {
        var ret = MakeReturn();
        var map = new Dictionary<int, MedicineBatch> { [1] = Batch(9, "2023-06-01", 0) }; // lô đã hết hạn
        Assert.Throws<InvalidReturnStateException>(() =>
            ReturnService.ValidateDecision(ret, ReturnStatus.ApprovedRestock, UserRole.Pharmacist, map, Today));
    }

    [Fact]
    public void Restock_NotIntact_Rejected_ButDestroyAllowed()
    {
        var ret = MakeReturn(intact: false);
        var map = new Dictionary<int, MedicineBatch> { [1] = Batch(9, "2024-06-01", 0) };
        Assert.Throws<InvalidReturnStateException>(() =>
            ReturnService.ValidateDecision(ret, ReturnStatus.ApprovedRestock, UserRole.Pharmacist, map, Today));
        ReturnService.ValidateDecision(ret, ReturnStatus.ApprovedDestroy, UserRole.Pharmacist, map, Today); // không ném
    }

    [Fact]
    public void Staff_CannotApproveReturn()
    {
        var ret = MakeReturn();
        var map = new Dictionary<int, MedicineBatch> { [1] = Batch(9, "2024-06-01", 0) };
        Assert.Throws<InvalidReturnStateException>(() =>
            ReturnService.ValidateDecision(ret, ReturnStatus.ApprovedDestroy, UserRole.Staff, map, Today));
    }

    [Fact]
    public void ApplyRestock_AddsBackToSourceBatch()
    {
        var ret = MakeReturn(qty: 5);
        var batch = Batch(9, "2024-06-01", 20);
        ReturnService.ApplyRestock(ret, new Dictionary<int, MedicineBatch> { [1] = batch });
        Assert.Equal(25, batch.QuantityOnHand);
    }
}
