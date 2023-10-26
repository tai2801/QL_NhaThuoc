using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Core.Services;
using PharmaCare.Web.Data;

namespace PharmaCare.Web.Controllers;

[Authorize]
public class BatchesController : Controller
{
    private readonly ApplicationDbContext _db;
    public BatchesController(ApplicationDbContext db) => _db = db;

    /// <summary>Tồn kho theo lô + mức cảnh báo hạn dùng từng lô.</summary>
    public async Task<IActionResult> Index(string? level)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var batches = await _db.MedicineBatches
            .Include(b => b.Medicine)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync();
        ViewBag.Today = today;
        ViewBag.Level = level;
        ViewBag.Classify = (Func<DateOnly, PharmaCare.Core.Models.ExpiryLevel>)(
            exp => ExpiryMonitor.Classify(exp, today));
        if (!string.IsNullOrWhiteSpace(level) &&
            Enum.TryParse<PharmaCare.Core.Models.ExpiryLevel>(level, out var lv))
            batches = batches.Where(b => ExpiryMonitor.Classify(b.ExpiryDate, today) == lv).ToList();
        return View(batches);
    }
}
