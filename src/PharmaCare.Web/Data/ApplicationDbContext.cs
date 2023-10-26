using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PharmaCare.Core.Models;

namespace PharmaCare.Web.Data;

/// <summary>
/// EF Core DbContext — ánh xạ 1-1 với database/schema.sql (cùng tên bảng/cột).
/// Có thể dùng `dotnet ef migrations` hoặc chạy schema.sql trực tiếp trên SQL Server.
/// </summary>
public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<ActiveIngredient> ActiveIngredients => Set<ActiveIngredient>();
    public DbSet<DosageForm> DosageForms => Set<DosageForm>();
    public DbSet<MedicineCategory> MedicineCategories => Set<MedicineCategory>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<MedicineIngredient> MedicineIngredients => Set<MedicineIngredient>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<StockReceipt> StockReceipts => Set<StockReceipt>();
    public DbSet<StockReceiptLine> StockReceiptLines => Set<StockReceiptLine>();
    public DbSet<MedicineBatch> MedicineBatches => Set<MedicineBatch>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionLine> PrescriptionLines => Set<PrescriptionLine>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceLine> SalesInvoiceLines => Set<SalesInvoiceLine>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnLine> SalesReturnLines => Set<SalesReturnLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<MedicineIngredient>()
            .HasKey(x => new { x.MedicineId, x.ActiveIngredientId });

        builder.Entity<Medicine>()
            .HasIndex(m => m.Code).IsUnique();
        builder.Entity<Medicine>().Property(m => m.SalePrice).HasPrecision(18, 2);

        builder.Entity<MedicineBatch>()
            .HasIndex(b => new { b.MedicineId, b.BatchNumber }).IsUnique();
        builder.Entity<MedicineBatch>().Property(b => b.ImportPrice).HasPrecision(18, 2);

        builder.Entity<StockReceiptLine>().Property(l => l.ImportPrice).HasPrecision(18, 2);
        builder.Entity<SalesInvoice>().Property(i => i.DiscountAmount).HasPrecision(18, 2);
        builder.Entity<SalesInvoiceLine>().Property(l => l.UnitPrice).HasPrecision(18, 2);

        // Dòng hóa đơn bắt buộc gắn lô nguồn — FK NOT NULL, không cascade delete để bảo toàn truy xuất.
        builder.Entity<SalesInvoiceLine>()
            .HasOne(l => l.Batch).WithMany()
            .HasForeignKey(l => l.BatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SalesReturnLine>()
            .HasOne(l => l.InvoiceLine).WithMany()
            .HasForeignKey(l => l.InvoiceLineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
