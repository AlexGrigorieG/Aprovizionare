using Aprovizionare.Api.Enums;

namespace Aprovizionare.Api.Entities;

public class PurchaseOrder
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public DateOnly OrderDate { get; set; }
    public DateOnly ExpectedDeliveryDate { get; set; }
    public Currency Currency { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public List<PurchaseOrderLine> Lines { get; set; } = [];
}