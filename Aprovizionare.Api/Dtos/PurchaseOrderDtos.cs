using System.ComponentModel.DataAnnotations;
using Aprovizionare.Api.Enums;

namespace Aprovizionare.Api.Dtos;


public record CreatePurchaseOrderLineDto(
    int ProductId,
    [Range(0.001, double.MaxValue, ErrorMessage = "Cantitatea trebuie să fie mai mare ca 0.")] decimal Quantity,
    [Range(0.01, double.MaxValue, ErrorMessage = "Prețul trebuie să fie mai mare ca 0.")] decimal UnitPrice);

public record CreatePurchaseOrderDto(
    int SupplierId,
    DateOnly ExpectedDeliveryDate,
    Currency Currency,
    List<CreatePurchaseOrderLineDto> Lines);

public record PurchaseOrderLineDto(
    int Id,
    int ProductId,
    string ProductName,
    string Unit,
    decimal QuantityOrdered,
    decimal QuantityReceived,
    decimal? UnitPrice); 
    
public record PurchaseOrderDto(
    int Id,
    string Number,
    string SupplierName,
    DateOnly OrderDate,
    DateOnly ExpectedDeliveryDate,
    Currency Currency,
    PurchaseOrderStatus Status,
    bool IsLate,
    List<PurchaseOrderLineDto> Lines);
