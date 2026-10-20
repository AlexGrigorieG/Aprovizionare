using Aprovizionare.Api.Data;
using Aprovizionare.Api.Dtos;
using Aprovizionare.Api.Entities;
using Aprovizionare.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace Aprovizionare.Api.Services;

public class InvoiceService(AppDbContext db, IExchangeRateProvider exchangeRates)
{
    public Task<List<InvoiceDto>> GetAllAsync() =>
        Project(db.Invoices.OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.Id)).ToListAsync();

    public Task<InvoiceDto?> GetByIdAsync(int id) =>
        Project(db.Invoices.Where(i => i.Id == id)).FirstOrDefaultAsync();

    public async Task<InvoiceDto> RegisterAsync(CreateInvoiceDto dto)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (dto.Lines.Count == 0)
            throw new BusinessRuleException("Factura trebuie să aibă cel puțin o linie.");

        if (dto.InvoiceDate > today)
            throw new BusinessRuleException("Data facturii nu poate fi în viitor.");

        var order = await db.PurchaseOrders.FindAsync(dto.PurchaseOrderId)
            ?? throw new BusinessRuleException($"Comanda {dto.PurchaseOrderId} nu există.");

        if (order.SupplierId != dto.SupplierId)
            throw new BusinessRuleException("Comanda aparține altui furnizor.");

        if (order.Status == PurchaseOrderStatus.Cancelled)
            throw new BusinessRuleException("Comanda a fost anulată și nu mai poate fi facturată.");

        if (order.Currency != dto.Currency)
            throw new BusinessRuleException(
                $"Factura este în {dto.Currency}, dar comanda în {order.Currency}. Prețurile nu pot fi comparate.");

        var number = dto.Number.Trim();
        if (await db.Invoices.AnyAsync(i => i.SupplierId == dto.SupplierId && i.Number == number))
            throw new BusinessRuleException($"Factura {number} de la acest furnizor a fost deja înregistrată.");

        var invoice = new Invoice
        {
            Number = number,
            SupplierId = dto.SupplierId,
            PurchaseOrderId = dto.PurchaseOrderId,
            InvoiceDate = dto.InvoiceDate,
            Currency = dto.Currency,
            ExchangeRate = await exchangeRates.GetRateAsync(dto.Currency, dto.InvoiceDate),
            Lines = dto.Lines.Select(l => new InvoiceLine
            {
                PurchaseOrderLineId = l.PurchaseOrderLineId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList()
        };

        ApplyMatch(invoice, await MatchAsync(invoice));

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        return (await GetByIdAsync(invoice.Id))!;
    }

    public async Task<InvoiceDto?> RecheckAsync(int id)
    {
        var invoice = await LoadAsync(id);
        if (invoice is null)
            return null;

        if (invoice.Status is not (InvoiceStatus.Blocked or InvoiceStatus.Validated))
            throw new BusinessRuleException($"Factura în starea {invoice.Status} nu mai poate fi reverificată.");

        ApplyMatch(invoice, await MatchAsync(invoice));
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<InvoiceDto?> ApproveAsync(int id, string? justification)
    {
        var invoice = await LoadAsync(id);
        if (invoice is null)
            return null;

        if (invoice.Status == InvoiceStatus.Blocked)
        {
            if (string.IsNullOrWhiteSpace(justification))
                throw new BusinessRuleException("Factura este blocată. Aprobarea ei cere o justificare.");

            invoice.DecisionNote = justification.Trim();
        }
        else if (invoice.Status != InvoiceStatus.Validated)
        {
            throw new BusinessRuleException($"Factura în starea {invoice.Status} nu poate fi aprobată.");
        }

        invoice.Status = InvoiceStatus.ApprovedForPayment;
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<InvoiceDto?> MarkPaidAsync(int id)
    {
        var invoice = await LoadAsync(id);
        if (invoice is null)
            return null;

        if (invoice.Status != InvoiceStatus.ApprovedForPayment)
            throw new BusinessRuleException("Doar o factură aprobată la plată poate fi marcată ca plătită.");

        invoice.Status = InvoiceStatus.Paid;
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<InvoiceDto?> RejectAsync(int id, string reason)
    {
        var invoice = await LoadAsync(id);
        if (invoice is null)
            return null;

        if (invoice.Status is InvoiceStatus.ApprovedForPayment or InvoiceStatus.Paid or InvoiceStatus.Rejected)
            throw new BusinessRuleException($"Factura în starea {invoice.Status} nu mai poate fi respinsă.");

        invoice.Status = InvoiceStatus.Rejected;
        invoice.DecisionNote = reason.Trim();
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    private Task<Invoice?> LoadAsync(int id) =>
        db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id);

    private static void ApplyMatch(Invoice invoice, MatchResult match)
    {
        invoice.Status = match.IsValid ? InvoiceStatus.Validated : InvoiceStatus.Blocked;
        invoice.BlockReason = match.IsValid ? null : string.Join(" ", match.Problems);
    }

    private async Task<MatchResult> MatchAsync(Invoice invoice)
    {
        var orderLines = await db.PurchaseOrderLines
            .Where(l => l.PurchaseOrderId == invoice.PurchaseOrderId)
            .Select(l => new OrderLineFacts(
                l.Id,
                l.Product.Name,
                l.UnitPrice,
                db.GoodsReceiptLines
                    .Where(r => r.PurchaseOrderLineId == l.Id)
                    .Sum(r => r.QuantityReceived),
                db.Invoices
                    .Where(i => i.Id != invoice.Id && i.Status != InvoiceStatus.Rejected)
                    .SelectMany(i => i.Lines)
                    .Where(il => il.PurchaseOrderLineId == l.Id)
                    .Sum(il => il.Quantity)))
            .ToDictionaryAsync(l => l.LineId);

        return InvoiceMatcher.Match(
            invoice.Lines.Select(l => new InvoiceLineToMatch(l.PurchaseOrderLineId, l.Quantity, l.UnitPrice)),
            orderLines);
    }

    private static IQueryable<InvoiceDto> Project(IQueryable<Invoice> invoices) =>
        invoices.Select(i => new InvoiceDto(
            i.Id,
            i.Number,
            i.Supplier.Name,
            i.PurchaseOrderId,
            i.PurchaseOrder.Number,
            i.InvoiceDate,
            i.Currency,
            i.ExchangeRate,
            i.Lines.Sum(l => l.Quantity * l.UnitPrice),
            Math.Round(i.Lines.Sum(l => l.Quantity * l.UnitPrice) * i.ExchangeRate, 2),
            i.Status,
            i.BlockReason,
            i.DecisionNote,
            i.Lines.OrderBy(l => l.Id).Select(l => new InvoiceLineDto(
                l.Id,
                l.PurchaseOrderLineId,
                l.PurchaseOrderLine.Product.Name,
                l.Quantity,
                l.UnitPrice,
                l.Quantity * l.UnitPrice)).ToList()));
}
