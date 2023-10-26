using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Core.Models;
using PharmaCare.Core.Services;
using PharmaCare.Web.Data;

namespace PharmaCare.Web.Controllers;

[Authorize]
public class MedicinesController : Controller
{
    private readonly ApplicationDbContext _db;
    public MedicinesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var query = _db.Medicines
            .Include(m => m.Category).Include(m => m.DosageForm)
            .Include(m => m.Ingredients).ThenInclude(i => i.ActiveIngredient)
            .Include(m => m.Batches)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var k = q.Trim();
            query = query.Where(m => m.Name.Contains(k) || m.Code.Contains(k) ||
                m.Ingredients.Any(i => i.ActiveIngredient!.Name.Contains(k)));
        }
        var list = await query.OrderBy(m => m.Name).ToListAsync();
        ViewBag.Today = today;
        ViewBag.Query = q;
        return View(list);
    }

    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> Create()
    {
        await LoadLookups();
        return View(new Medicine { IsActive = true, ReorderLevel = 20 });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Medicine medicine)
    {
        if (await _db.Medicines.AnyAsync(m => m.Code == medicine.Code))
            ModelState.AddModelError(nameof(medicine.Code), "Mã thuốc đã tồn tại.");
        if (!ModelState.IsValid)
        {
            await LoadLookups();
            return View(medicine);
        }
        _db.Medicines.Add(medicine);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> Edit(int id)
    {
        var medicine = await _db.Medicines.FindAsync(id);
        if (medicine is null) return NotFound();
        await LoadLookups();
        return View(medicine);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Medicine medicine)
    {
        if (!ModelState.IsValid)
        {
            await LoadLookups();
            return View(medicine);
        }
        _db.Medicines.Update(medicine);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadLookups()
    {
        ViewBag.Categories = new SelectList(await _db.MedicineCategories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        ViewBag.DosageForms = new SelectList(await _db.DosageForms.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");
    }
}
