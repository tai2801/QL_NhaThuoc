using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Core.Models;
using PharmaCare.Web.Data;

namespace PharmaCare.Web.Controllers;

[Authorize]
public class PrescriptionsController : Controller
{
    private readonly ApplicationDbContext _db;
    public PrescriptionsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var list = await _db.Prescriptions
            .Include(p => p.Customer)
            .Include(p => p.Lines).ThenInclude(l => l.Medicine)
            .OrderByDescending(p => p.IssuedDate).ToListAsync();
        return View(list);
    }

    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = new SelectList(await _db.Customers.OrderBy(c => c.FullName).ToListAsync(), "Id", "FullName");
        return View(new Prescription { IssuedDate = DateOnly.FromDateTime(DateTime.Today), Status = PrescriptionStatus.Active });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Prescription prescription)
    {
        if (await _db.Prescriptions.AnyAsync(p => p.Code == prescription.Code))
            ModelState.AddModelError(nameof(prescription.Code), "Mã đơn thuốc đã tồn tại.");
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = new SelectList(await _db.Customers.OrderBy(c => c.FullName).ToListAsync(), "Id", "FullName");
            return View(prescription);
        }
        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
