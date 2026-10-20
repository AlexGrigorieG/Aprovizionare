namespace Aprovizionare.Api.Entities;
public class PurchaseOrderLine
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal QuantityOrdered { get; set; }
    public decimal UnitPrice { get; set; }
}
