using Aprovizionare.Api.Enums;

namespace Aprovizionare.Api.Entities;

public class Invoice
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public int SupplierId { get; set; }
    public int PurchaseOrderId { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public Currency Currency { get; set; }
    public decimal ExchangeRate { get; set; }
    public InvoiceStatus Status { get; set; }
    public string? BlockReason { get; set; }
    public List<InvoiceLine> Lines  { get; set; } = [];
    public Supplier Supplier { get; set; } = null!;
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
}