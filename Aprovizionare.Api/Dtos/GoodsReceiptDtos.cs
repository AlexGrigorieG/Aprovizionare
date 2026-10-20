using System.ComponentModel.DataAnnotations;

namespace Aprovizionare.Api.Dtos;

public record CreateGoodsReceiptLineDto(
    int ProductId,
    [Range(0.001, double.MaxValue, ErrorMessage = "Cantitatea trebuie să fie mai mare ca 0.")] decimal Quantity);

public record CreateGoodsReceiptDto(
    int SupplierId,
    int? PurchaseOrderId,
    DateOnly ReceivedDate,
    string? Notes,
    List<CreateGoodsReceiptLineDto> Lines);

public record GoodsReceiptLineDto(
    int Id,
    int ProductId,
    string ProductName,
    string Unit,
    decimal QuantityReceived,
    int? PurchaseOrderLineId,
    bool IsUnordered);

public record GoodsReceiptDto(
    int Id,
    string SupplierName,
    int? PurchaseOrderId,
    string? PurchaseOrderNumber,
    DateOnly ReceivedDate,
    string? Notes,
    bool HasUnorderedItems,
    List<GoodsReceiptLineDto> Lines);

public record StockItemDto(int ProductId, string Code, string Name, string Unit, decimal Quantity);
