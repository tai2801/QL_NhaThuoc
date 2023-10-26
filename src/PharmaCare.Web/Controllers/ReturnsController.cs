using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Core.Exceptions;
using PharmaCare.Core.Models;
using PharmaCare.Core.Services;
using PharmaCare.Web.Data;

namespace PharmaCare.Web.Controllers;

[Authorize]
public class ReturnsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ReturnsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var list = await _db.SalesReturns
            .Include(r => r.Invoice)
            .Include(r => r.Lines)
            .OrderByDescending(r => r.CreatedAt).ToListAsync();
        return View(list);
    }

    /// <summary>Tạo phiếu trả: bắt buộc lý do + số lượng không vượt số đã bán (ReturnService.ValidateCreate).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int invoiceLineId, int quantity, bool isIntact, string reason)
    {
        var invoiceLine = await _db.SalesInvoiceLines.FirstOrDefaultAsync(l => l.Id == invoiceLineId);
        if (invoiceLine is null) return NotFound();

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Username == User.Identity!.Name)
                       ?? new Employee { FullName = User.Identity!.Name ?? "unknown", Username = User.Identity!.Name ?? "unknown" };
        if (employee.Id == 0) { _db.Employees.Add(employee); await _db.SaveChangesAsync(); }

        var ret = new SalesReturn
        {
            InvoiceId = invoiceLine.InvoiceId,
            CreatedByEmployeeId = employee.Id,
            CreatedAt = DateTime.Now,
            Reason = reason,
            Status = ReturnStatus.Pending,
            Lines = new List<SalesReturnLine>
            {
                new() { InvoiceLineId = invoiceLineId, Quantity = quantity, IsIntact = isIntact }
            }
        };
        try
        {
            var alreadyReturned = await _db.SalesReturnLines
                .Where(l => l.InvoiceLineId == invoiceLineId && l.Return!.Status != ReturnStatus.Rejected)
                .SumAsync(l => l.Quantity);
            ReturnService.ValidateCreate(ret, new Dictionary<int, int>
            {
                [invoiceLineId] = invoiceLine.Quantity - alreadyReturned
            });
        }
        catch (InvalidReturnStateException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        _db.SalesReturns.Add(ret);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã tạo phiếu trả — chờ dược sĩ duyệt.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Duyệt phiếu trả — chỉ Dược sĩ/Admin. Hoàn kho bị chặn nếu lô hết hạn hoặc hàng không nguyên vẹn.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(int id, ReturnStatus decision, string? reviewNote)
    {
        var ret = await _db.SalesReturns.Include(r => r.Lines).FirstOrDefaultAsync(r => r.Id == id);
        if (ret is null) return NotFound();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var batchByLine = new Dictionary<int, MedicineBatch>();
        foreach (var line in ret.Lines)
        {
            var invLine = await _db.SalesInvoiceLines.Include(l => l.Batch)
                .FirstAsync(l => l.Id == line.InvoiceLineId);
            batchByLine[line.InvoiceLineId] = invLine.Batch!;
        }

        var role = User.IsInRole("Admin") ? UserRole.Admin : UserRole.Pharmacist;
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            ReturnService.ValidateDecision(ret, decision, role, batchByLine, today);
            if (decision == ReturnStatus.ApprovedRestock)
                ReturnService.ApplyRestock(ret, batchByLine);
            ret.Status = decision;
            ret.ReviewNote = reviewNote;
            ret.ReviewedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            TempData["Success"] = $"Phiếu trả #{id}: {decision}.";
        }
        catch (InvalidReturnStateException ex)
        {
            await tx.RollbackAsync();
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
