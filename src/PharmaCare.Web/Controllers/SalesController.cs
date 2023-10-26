using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Core.Exceptions;
using PharmaCare.Core.Models;
using PharmaCare.Core.Services;
using PharmaCare.Web.Data;
using PharmaCare.Web.Models.ViewModels;

namespace PharmaCare.Web.Controllers;

/// <summary>
/// Màn hình bán thuốc (POS) tối ưu thao tác nhanh:
/// tìm theo tên / mã / hoạt chất → chọn số lượng → FEFO tự xuất lô HSD gần nhất.
/// Mọi vai trò đã đăng nhập đều bán được; thuốc kê đơn bắt buộc gắn đơn còn hiệu lực.
/// </summary>
[Authorize]
public class SalesController : Controller
{
    private readonly ApplicationDbContext _db;
    public SalesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var vm = new PosViewModel { Query = q };

        var query = _db.Medicines
            .Include(m => m.Ingredients).ThenInclude(i => i.ActiveIngredient)
            .Where(m => m.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(m =>
                m.Name.Contains(keyword) || m.Code.Contains(keyword) ||
                m.Ingredients.Any(i => i.ActiveIngredient!.Name.Contains(keyword)));
        }

        var medicines = await query.OrderBy(m => m.Name).Take(50).ToListAsync();
        var batches = await _db.MedicineBatches.ToListAsync();

        vm.Results = medicines.Select(m =>
        {
            var sellable = batches.Where(b => b.MedicineId == m.Id && b.IsSellable(today))
                                  .OrderBy(b => b.ExpiryDate).ToList();
            return new PosMedicineRow
            {
                MedicineId = m.Id, Code = m.Code, Name = m.Name, Strength = m.Strength,
                Unit = m.Unit, SalePrice = m.SalePrice,
                SellableStock = sellable.Sum(b => b.QuantityOnHand),
                RequiresPrescription = m.RequiresPrescription,
                NextExpiryBatch = sellable.FirstOrDefault() is { } b
                    ? $"{b.BatchNumber} (HSD {b.ExpiryDate:dd/MM/yyyy})" : null
            };
        }).ToList();

        vm.ActivePrescriptions = await _db.Prescriptions
            .Include(p => p.Customer)
            .Where(p => p.Status == PrescriptionStatus.Active)
            .OrderByDescending(p => p.IssuedDate).ToListAsync();
        return View(vm);
    }

    /// <summary>Tạo hóa đơn: FEFO tách lô trong 1 transaction — lỗi nghiệp vụ thì rollback toàn bộ.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutRequest request)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var items = request.MedicineIds
            .Zip(request.Quantities, (id, qty) => (id, qty))
            .Where(x => x.qty > 0).ToList();
        if (items.Count == 0)
        {
            TempData["Error"] = "Chưa chọn thuốc nào.";
            return RedirectToAction(nameof(Index));
        }

        var medicines = await _db.Medicines.Where(m => items.Select(i => i.id).Contains(m.Id)).ToListAsync();
        var batches = await _db.MedicineBatches
            .Where(b => items.Select(i => i.id).Contains(b.MedicineId)).ToListAsync();
        var prescription = request.PrescriptionId is int pid
            ? await _db.Prescriptions.FindAsync(pid) : null;

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var requests = items.Select(x => new SaleLineRequest(
                medicines.First(m => m.Id == x.id), x.qty)).ToList();
            var lines = SalesService.BuildInvoiceLines(requests, batches, prescription, today);
            SalesService.ApplyAllocations(lines, batches);

            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Username == User.Identity!.Name)
                           ?? new Employee { FullName = User.Identity!.Name ?? "unknown", Username = User.Identity!.Name ?? "unknown" };

            var invoice = new SalesInvoice
            {
                InvoiceCode = "HD" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                CustomerId = request.CustomerId,
                PrescriptionId = request.PrescriptionId,
                SoldByEmployeeId = employee.Id,
                CreatedAt = DateTime.Now,
                PaymentMethod = request.PaymentMethod,
                Status = InvoiceStatus.Completed,
                Lines = lines.Select(l => new SalesInvoiceLine
                {
                    MedicineId = l.MedicineId, BatchId = l.BatchId,
                    Quantity = l.Quantity, UnitPrice = l.UnitPrice
                }).ToList()
            };
            _db.SalesInvoices.Add(invoice);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            TempData["Success"] = $"Đã tạo hóa đơn {invoice.InvoiceCode} — {invoice.FinalAmount:N0} ₫ (FEFO đã gắn lô nguồn cho từng dòng).";
        }
        catch (Exception ex) when (ex is InsufficientStockException or ExpiredBatchException
                                   or PrescriptionRequiredException or InvalidReturnStateException)
        {
            await tx.RollbackAsync();
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Tra cứu hóa đơn — xem từng dòng kèm lô nguồn (truy xuất nguồn gốc).</summary>
    public async Task<IActionResult> Invoice(int id)
    {
        var invoice = await _db.SalesInvoices
            .Include(i => i.Lines).ThenInclude(l => l.Medicine)
            .Include(i => i.Lines).ThenInclude(l => l.Batch)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == id);
        return invoice is null ? NotFound() : View(invoice);
    }
}
