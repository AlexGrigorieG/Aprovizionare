using System.ComponentModel.DataAnnotations;
using Aprovizionare.Api.Enums;

namespace Aprovizionare.Api.Dtos;

public record CreateInvoiceLineDto(
    int PurchaseOrderLineId,
    [Range(0.001, double.MaxValue, ErrorMessage = "Cantitatea trebuie să fie mai mare ca 0.")] decimal Quantity,
    [Range(0.01, double.MaxValue, ErrorMessage = "Prețul trebuie să fie mai mare ca 0.")] decimal UnitPrice);

public record CreateInvoiceDto(
    [Required, MaxLength(50)] string Number,
    int SupplierId,
    int PurchaseOrderId,
    DateOnly InvoiceDate,
    Currency Currency,
    List<CreateInvoiceLineDto> Lines);

public record ApproveInvoiceDto(string? Justification);

public record RejectInvoiceDto([Required] string Reason);

public record InvoiceLineDto(
    int Id,
    int PurchaseOrderLineId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record InvoiceDto(
    int Id,
    string Number,
    string SupplierName,
    int PurchaseOrderId,
    string PurchaseOrderNumber,
    DateOnly InvoiceDate,
    Currency Currency,
    decimal ExchangeRate,
    decimal TotalAmount,
    decimal TotalRon,
    InvoiceStatus Status,
    string? BlockReason,
    string? DecisionNote,
    List<InvoiceLineDto> Lines);
