using Aprovizionare.Api.Entities;
using Aprovizionare.Api.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Aprovizionare.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }
    public DbSet<GoodsReceipt> GoodsReceipts { get; set; }
    public DbSet<GoodsReceiptLine> GoodsReceiptLines { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceLine> InvoiceLines { get; set; }
    public DbSet<ExchangeRate> ExchangeRates { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<decimal>().HavePrecision(18, 4); 
        builder.Properties<Currency>().HaveConversion<string>();
        builder.Properties<PurchaseOrderStatus>().HaveConversion<string>();
        builder.Properties<InvoiceStatus>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

        modelBuilder.Entity<Product>().HasIndex(p => p.Code).IsUnique();
        modelBuilder.Entity<PurchaseOrder>().HasIndex(p => p.Number).IsUnique();
        modelBuilder.Entity<Invoice>().HasIndex(i => new { i.SupplierId, i.Number }).IsUnique();
        modelBuilder.Entity<ExchangeRate>().HasIndex(e => new { e.Date, e.Currency }).IsUnique();    
    }
}
