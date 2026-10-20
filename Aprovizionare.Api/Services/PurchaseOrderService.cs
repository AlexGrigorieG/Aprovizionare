using Aprovizionare.Api.Data;
using Aprovizionare.Api.Dtos;
using Aprovizionare.Api.Entities;
using Aprovizionare.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace Aprovizionare.Api.Services;

public class PurchaseOrderService(AppDbContext db)
{
    public Task<List<PurchaseOrderDto>> GetAllAsync(bool includePrices) =>
        Project(db.PurchaseOrders.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id), includePrices)
            .ToListAsync();

    public Task<PurchaseOrderDto?> GetByIdAsync(int id, bool includePrices) =>
        Project(db.PurchaseOrders.Where(o => o.Id == id), includePrices).FirstOrDefaultAsync();

    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (dto.Lines.Count == 0)
            throw new BusinessRuleException("Comanda trebuie să aibă cel puțin o linie.");

        if (dto.ExpectedDeliveryDate < today)
            throw new BusinessRuleException("Data estimată de livrare nu poate fi în trecut.");

        if (!await db.Suppliers.AnyAsync(s => s.Id == dto.SupplierId))
            throw new BusinessRuleException($"Furnizorul {dto.SupplierId} nu există.");

        var productIds = dto.Lines.Select(l => l.ProductId).Distinct().ToList();
        var existingProducts = await db.Products.CountAsync(p => productIds.Contains(p.Id));
        if (existingProducts != productIds.Count)
            throw new BusinessRuleException("Unul sau mai multe produse nu există.");

        var ordersThisYear = await db.PurchaseOrders.CountAsync(o => o.OrderDate.Year == today.Year);

        var order = new PurchaseOrder
        {
            Number = $"PO-{today.Year}-{ordersThisYear + 1:D4}",
            SupplierId = dto.SupplierId,
            OrderDate = today,
            ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
            Currency = dto.Currency,
            Status = PurchaseOrderStatus.Sent,
            Lines = dto.Lines.Select(l => new PurchaseOrderLine
            {
                ProductId = l.ProductId,
                QuantityOrdered = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList()
        };

        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync();

        return (await GetByIdAsync(order.Id, includePrices: true))!;
    }

    public async Task<bool> CancelAsync(int id)
    {
        var order = await db.PurchaseOrders.FindAsync(id);
        if (order is null)
            return false;

        if (order.Status == PurchaseOrderStatus.Cancelled)
            return true;

        if (await db.GoodsReceipts.AnyAsync(r => r.PurchaseOrderId == id))
            throw new BusinessRuleException("Comanda are deja recepții și nu mai poate fi anulată.");

        order.Status = PurchaseOrderStatus.Cancelled;
        await db.SaveChangesAsync();
        return true;
    }

    private IQueryable<PurchaseOrderDto> Project(IQueryable<PurchaseOrder> orders, bool includePrices)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return orders.Select(o => new PurchaseOrderDto(
            o.Id,
            o.Number,
            o.Supplier.Name,
            o.OrderDate,
            o.ExpectedDeliveryDate,
            o.Currency,
            o.Status,
            (o.Status == PurchaseOrderStatus.Sent || o.Status == PurchaseOrderStatus.PartiallyReceived)
                && o.ExpectedDeliveryDate < today,
            o.Lines.Select(l => new PurchaseOrderLineDto(
                l.Id,
                l.ProductId,
                l.Product.Name,
                l.Product.Unit,
                l.QuantityOrdered,
                db.GoodsReceiptLines
                    .Where(r => r.PurchaseOrderLineId == l.Id)
                    .Sum(r => r.QuantityReceived),
                includePrices ? l.UnitPrice : (decimal?)null)).ToList()));
    }
}
