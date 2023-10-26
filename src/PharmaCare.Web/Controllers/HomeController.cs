using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Core.Models;
using PharmaCare.Core.Services;
using PharmaCare.Web.Data;
using PharmaCare.Web.Models.ViewModels;

namespace PharmaCare.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    public HomeController(ApplicationDbContext db) => _db = db;

    /// <summary>Dashboard dược sĩ: doanh thu + bảng cảnh báo hạn dùng + sắp hết hàng + trả chờ duyệt.</summary>
    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var now = DateTime.Now;

        var invoices = await _db.SalesInvoices
            .Include(i => i.Lines)
            .Where(i => i.Status == InvoiceStatus.Completed)
            .ToListAsync();

        var medicines = await _db.Medicines.Where(m => m.IsActive).ToListAsync();
        var batches = await _db.MedicineBatches.Include(b => b.Medicine).ToListAsync();

        var vm = new DashboardViewModel
        {
            RevenueToday = invoices.Where(i => i.CreatedAt.Date == now.Date).Sum(i => i.FinalAmount),
            RevenueThisMonth = invoices.Where(i => i.CreatedAt.Year == now.Year && i.CreatedAt.Month == now.Month)
                                         .Sum(i => i.FinalAmount),
            InvoicesToday = invoices.Count(i => i.CreatedAt.Date == now.Date),
            TotalMedicines = medicines.Count,
            TotalBatchesInStock = batches.Count(b => b.QuantityOnHand > 0),
            ExpiryAlerts = ExpiryMonitor.BuildAlerts(
                batches.Where(b => b.Medicine is not null).Select(b => (b.Medicine!, b)), today),
            LowStockMedicineNames = medicines
                .Where(m => ExpiryMonitor.IsLowStock(m, batches, today))
                .Select(m => m.Name).ToList(),
            PendingReturns = await _db.SalesReturns.CountAsync(r => r.Status == ReturnStatus.Pending)
        };
        return View(vm);
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
