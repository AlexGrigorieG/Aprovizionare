namespace Aprovizionare.Api.Entities;
public class InvoiceLine
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int PurchaseOrderLineId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public PurchaseOrderLine PurchaseOrderLine { get; set; } = null!;
}