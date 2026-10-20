using Aprovizionare.Api.Data;
using Aprovizionare.Api.Entities;
using Aprovizionare.Api.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Aprovizionare.Api.Services;

public class AlertOptions
{
    public TimeOnly DailyAt { get; set; } = new(8, 0);
    public int UnorderedLookbackDays { get; set; } = 7;
}

public class AlertService(
    AppDbContext db,
    UserManager<AppUser> userManager,
    IEmailSender emailSender,
    IOptions<AlertOptions> options)
{
    public async Task<List<EmailMessage>> BuildAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var unorderedSince = today.AddDays(-options.Value.UnorderedLookbackDays);

        var lateOrders = await db.PurchaseOrders
            .Where(o => (o.Status == PurchaseOrderStatus.Sent || o.Status == PurchaseOrderStatus.PartiallyReceived)
                && o.ExpectedDeliveryDate < today)
            .OrderBy(o => o.ExpectedDeliveryDate)
            .Select(o => new LateOrder(
                o.Number,
                o.Supplier.Name,
                o.ExpectedDeliveryDate,
                o.Lines.Select(l => new MissingLine(
                    l.Product.Name,
                    l.Product.Unit,
                    l.QuantityOrdered,
                    db.GoodsReceiptLines
                        .Where(r => r.PurchaseOrderLineId == l.Id)
                        .Sum(r => r.QuantityReceived))).ToList()))
            .ToListAsync();

        var blockedInvoices = await db.Invoices
            .Where(i => i.Status == InvoiceStatus.Blocked)
            .OrderBy(i => i.InvoiceDate)
            .Select(i => new BlockedInvoice(i.Number, i.Supplier.Name, i.InvoiceDate, i.BlockReason ?? ""))
            .ToListAsync();

        var unorderedItems = await db.GoodsReceipts
            .Where(r => r.ReceivedDate >= unorderedSince)
            .OrderBy(r => r.ReceivedDate)
            .SelectMany(r => r.Lines
                .Where(l => l.PurchaseOrderLineId == null)
                .Select(l => new UnorderedItem(r.Id, r.ReceivedDate, r.Supplier.Name, l.Product.Name, l.Product.Unit, l.QuantityReceived)))
            .ToListAsync();

        return AlertComposer.Compose(
            today,
            lateOrders,
            blockedInvoices,
            unorderedItems,
            await EmailsInRoleAsync(Roles.Purchasing),
            await EmailsInRoleAsync(Roles.Accounting));
    }

    public async Task<int> SendAsync(CancellationToken cancellationToken = default)
    {
        var messages = await BuildAsync();
        foreach (var message in messages)
            await emailSender.SendAsync(message, cancellationToken);

        return messages.Count;
    }

    private async Task<List<string>> EmailsInRoleAsync(string role) =>
        (await userManager.GetUsersInRoleAsync(role))
            .Select(u => u.Email)
            .OfType<string>()
            .ToList();
}
