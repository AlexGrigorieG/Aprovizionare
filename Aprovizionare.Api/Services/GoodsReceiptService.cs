using Aprovizionare.Api.Data;
using Aprovizionare.Api.Dtos;
using Aprovizionare.Api.Entities;
using Aprovizionare.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace Aprovizionare.Api.Services;

public class GoodsReceiptService(AppDbContext db)
{
    public Task<List<GoodsReceiptDto>> GetAllAsync() =>
        Project(db.GoodsReceipts.OrderByDescending(r => r.ReceivedDate).ThenByDescending(r => r.Id))
            .ToListAsync();

    public Task<GoodsReceiptDto?> GetByIdAsync(int id) =>
        Project(db.GoodsReceipts.Where(r => r.Id == id)).FirstOrDefaultAsync();

    public async Task<GoodsReceiptDto> RegisterAsync(CreateGoodsReceiptDto dto)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (dto.Lines.Count == 0)
            throw new BusinessRuleException("Recepția trebuie să aibă cel puțin o linie.");

        if (dto.ReceivedDate > today)
            throw new BusinessRuleException("Data recepției nu poate fi în viitor.");

        if (!await db.Suppliers.AnyAsync(s => s.Id == dto.SupplierId))
            throw new BusinessRuleException($"Furnizorul {dto.SupplierId} nu există.");

        var productIds = dto.Lines.Select(l => l.ProductId).Distinct().ToList();
        var existingProducts = await db.Products.CountAsync(p => productIds.Contains(p.Id));
        if (existingProducts != productIds.Count)
            throw new BusinessRuleException("Unul sau mai multe produse nu există.");

        var receipt = new GoodsReceipt
        {
            SupplierId = dto.SupplierId,
            PurchaseOrderId = dto.PurchaseOrderId,
            ReceivedDate = dto.ReceivedDate,
            Notes = dto.Notes
        };

        if (dto.PurchaseOrderId is int orderId)
        {
            var order = await db.PurchaseOrders.FindAsync(orderId)
                ?? throw new BusinessRuleException($"Comanda {orderId} nu există.");

            if (order.SupplierId != dto.SupplierId)
                throw new BusinessRuleException("Comanda aparține altui furnizor.");

            if (order.Status == PurchaseOrderStatus.Cancelled)
                throw new BusinessRuleException(
                    "Comanda a fost anulată. Înregistrează marfa fără comandă, ca să fie clarificată.");

            var openLines = await LoadOpenLinesAsync(orderId);

            foreach (var line in dto.Lines)
            {
                foreach (var allocation in ReceiptAllocator.Allocate(openLines, line.ProductId, line.Quantity))
                {
                    receipt.Lines.Add(new GoodsReceiptLine
                    {
                        ProductId = line.ProductId,
                        PurchaseOrderLineId = allocation.PurchaseOrderLineId,
                        QuantityReceived = allocation.Quantity
                    });
                }
            }

            if (openLines.All(l => l.OpenQuantity == 0))
                order.Status = PurchaseOrderStatus.Received;
            else if (openLines.Any(l => l.OpenQuantity < l.OrderedQuantity))
                order.Status = PurchaseOrderStatus.PartiallyReceived;
        }
        else
        {
            receipt.Lines.AddRange(dto.Lines.Select(l => new GoodsReceiptLine
            {
                ProductId = l.ProductId,
                QuantityReceived = l.Quantity
            }));
        }

        db.GoodsReceipts.Add(receipt);
        await db.SaveChangesAsync();

        return (await GetByIdAsync(receipt.Id))!;
    }

    public Task<List<StockItemDto>> GetStockAsync() =>
        db.Products
            .OrderBy(p => p.Name)
            .Select(p => new StockItemDto(
                p.Id,
                p.Code,
                p.Name,
                p.Unit,
                db.GoodsReceiptLines.Where(r => r.ProductId == p.Id).Sum(r => r.QuantityReceived)))
            .ToListAsync();

    private Task<List<OpenOrderLine>> LoadOpenLinesAsync(int orderId) =>
        db.PurchaseOrderLines
            .Where(l => l.PurchaseOrderId == orderId)
            .OrderBy(l => l.Id)
            .Select(l => new OpenOrderLine(
                l.Id,
                l.ProductId,
                l.QuantityOrdered,
                l.QuantityOrdered - db.GoodsReceiptLines
                    .Where(r => r.PurchaseOrderLineId == l.Id)
                    .Sum(r => r.QuantityReceived)))
            .ToListAsync();

    private static IQueryable<GoodsReceiptDto> Project(IQueryable<GoodsReceipt> receipts) =>
        receipts.Select(r => new GoodsReceiptDto(
            r.Id,
            r.Supplier.Name,
            r.PurchaseOrderId,
            r.PurchaseOrder != null ? r.PurchaseOrder.Number : null,
            r.ReceivedDate,
            r.Notes,
            r.Lines.Any(l => l.PurchaseOrderLineId == null),
            r.Lines.OrderBy(l => l.Id).Select(l => new GoodsReceiptLineDto(
                l.Id,
                l.ProductId,
                l.Product.Name,
                l.Product.Unit,
                l.QuantityReceived,
                l.PurchaseOrderLineId,
                l.PurchaseOrderLineId == null)).ToList()));
}
