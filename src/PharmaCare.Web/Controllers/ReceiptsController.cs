using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Core.Models;
using PharmaCare.Core.Services;
using PharmaCare.Web.Data;

namespace PharmaCare.Web.Controllers;

/// <summary>Nhập thuốc theo lô — chỉ Admin / Dược sĩ. Mỗi phiếu nhập ở đây nhập 1 dòng = tạo 1 lô mới.</summary>
[Authorize(Roles = "Admin,Pharmacist")]
public class ReceiptsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ReceiptsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var list = await _db.StockReceipts
            .Include(r => r.Supplier)
            .Include(r => r.Lines).ThenInclude(l => l.Medicine)
            .Include(r => r.Lines).ThenInclude(l => l.Batch)
            .OrderByDescending(r => r.ReceivedAt).ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        await LoadLookups();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int supplierId, int medicineId, string batchNumber,
        DateOnly manufactureDate, DateOnly expiryDate, int quantity, decimal importPrice, string? supplierInvoiceNo)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Username == User.Identity!.Name)
                       ?? new Employee { FullName = User.Identity!.Name ?? "unknown", Username = User.Identity!.Name ?? "unknown" };
        if (employee.Id == 0) { _db.Employees.Add(employee); await _db.SaveChangesAsync(); }

        var receipt = new StockReceipt
        {
            ReceiptCode = "PN" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
            SupplierId = supplierId,
            ReceivedByEmployeeId = employee.Id,
            ReceivedAt = DateTime.Now,
            SupplierInvoiceNo = supplierInvoiceNo
        };
        var line = new StockReceiptLine { MedicineId = medicineId, Quantity = quantity, ImportPrice = importPrice };

        try
        {
            var batch = ReceiptService.CreateBatchForLine(receipt, line, batchNumber, manufactureDate, expiryDate, today);
            _db.MedicineBatches.Add(batch);
            await _db.SaveChangesAsync();
            line.BatchId = batch.Id;
            receipt.Lines.Add(line);
            _db.StockReceipts.Add(receipt);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã nhập lô {batchNumber} — tạo lô #{batch.Id} với {quantity} đơn vị.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadLookups();
            return View();
        }
    }

    private async Task LoadLookups()
    {
        ViewBag.Suppliers = new SelectList(await _db.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(), "Id", "Name");
        ViewBag.Medicines = new SelectList(await _db.Medicines.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync(), "Id", "Name");
    }
}
