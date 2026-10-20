namespace Aprovizionare.Api.Entities;
public class GoodsReceipt
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public DateOnly ReceivedDate { get; set; }
    public string? Notes { get; set; }
    public List<GoodsReceiptLine> Lines { get; set; } = [];
    public Supplier Supplier { get; set; } = null!;
    public PurchaseOrder? PurchaseOrder { get; set; }
}