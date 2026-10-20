namespace Aprovizionare.Api.Entities;
public class GoodsReceiptLine
{
    public int Id { get; set; }
    public int GoodsReceiptId { get; set; }
    public int ProductId { get; set; }
    public int? PurchaseOrderLineId  { get; set; }
    public decimal QuantityReceived { get; set; }
    public Product Product { get; set; } = null!;
    public PurchaseOrderLine? PurchaseOrderLine { get; set; }
}