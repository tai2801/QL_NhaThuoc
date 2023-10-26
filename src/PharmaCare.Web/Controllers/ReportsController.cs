using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Core.Models;
using PharmaCare.Web.Data;

namespace PharmaCare.Web.Controllers;

[Authorize(Roles = "Admin,Pharmacist")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ReportsController(ApplicationDbContext db) => _db = db;

    /// <summary>Báo cáo doanh thu theo ngày (30 ngày gần nhất) + tồn kho theo lô + top thuốc bán chạy kèm lô đã xuất.</summary>
    public async Task<IActionResult> Index()
    {
        var invoices = await _db.SalesInvoices
            .Include(i => i.Lines).ThenInclude(l => l.Medicine)
            .Include(i => i.Lines).ThenInclude(l => l.Batch)
            .Where(i => i.Status == InvoiceStatus.Completed)
            .ToListAsync();

        ViewBag.RevenueByDay = invoices
            .GroupBy(i => i.CreatedAt.Date)
            .OrderByDescending(g => g.Key).Take(30)
            .Select(g => new { Date = g.Key, Count = g.Count(), Revenue = g.Sum(i => i.FinalAmount) })
            .ToList();

        ViewBag.TopMedicines = invoices.SelectMany(i => i.Lines)
            .GroupBy(l => l.Medicine!.Name)
            .Select(g => new { Name = g.Key, Quantity = g.Sum(l => l.Quantity), Revenue = g.Sum(l => l.LineTotal) })
            .OrderByDescending(x => x.Quantity).Take(10).ToList();

        ViewBag.StockByBatch = await _db.MedicineBatches
            .Include(b => b.Medicine)
            .Where(b => b.QuantityOnHand > 0)
            .OrderBy(b => b.ExpiryDate).ToListAsync();

        ViewBag.TotalRevenue = invoices.Sum(i => i.FinalAmount);
        ViewBag.TotalCostOfSold = invoices.SelectMany(i => i.Lines)
            .Sum(l => l.Quantity * (l.Batch?.ImportPrice ?? 0));
        return View();
    }
}
