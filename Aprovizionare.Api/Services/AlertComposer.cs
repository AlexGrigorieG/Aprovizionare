using System.Globalization;
using System.Text;

namespace Aprovizionare.Api.Services;

public record MissingLine(string ProductName, string Unit, decimal OrderedQuantity, decimal ReceivedQuantity);

public record LateOrder(string Number, string SupplierName, DateOnly ExpectedDeliveryDate, List<MissingLine> Lines);

public record BlockedInvoice(string Number, string SupplierName, DateOnly InvoiceDate, string Reason);

public record UnorderedItem(int ReceiptId, DateOnly ReceivedDate, string SupplierName, string ProductName, string Unit, decimal Quantity);

public static class AlertComposer
{
    private static readonly CultureInfo Ro = CultureInfo.GetCultureInfo("ro-RO");

    public static List<EmailMessage> Compose(
        DateOnly today,
        IReadOnlyList<LateOrder> lateOrders,
        IReadOnlyList<BlockedInvoice> blockedInvoices,
        IReadOnlyList<UnorderedItem> unorderedItems,
        IReadOnlyList<string> purchasingEmails,
        IReadOnlyList<string> accountingEmails)
    {
        var messages = new List<EmailMessage>();

        if (purchasingEmails.Count > 0 && (lateOrders.Count > 0 || blockedInvoices.Count > 0 || unorderedItems.Count > 0))
        {
            var body = new StringBuilder();
            AppendLateOrders(body, today, lateOrders);
            AppendUnorderedItems(body, unorderedItems);
            AppendBlockedInvoices(body, blockedInvoices);

            messages.Add(new EmailMessage(
                purchasingEmails,
                $"Aprovizionare {Date(today)}: {Summary(lateOrders.Count, unorderedItems.Count, blockedInvoices.Count)}",
                body.ToString().TrimEnd()));
        }

        if (accountingEmails.Count > 0 && blockedInvoices.Count > 0)
        {
            var body = new StringBuilder();
            AppendBlockedInvoices(body, blockedInvoices);

            messages.Add(new EmailMessage(
                accountingEmails,
                $"Aprovizionare {Date(today)}: {Count(blockedInvoices.Count, "factură blocată", "facturi blocate")}",
                body.ToString().TrimEnd()));
        }

        return messages;
    }

    private static void AppendLateOrders(StringBuilder body, DateOnly today, IReadOnlyList<LateOrder> orders)
    {
        if (orders.Count == 0)
            return;

        body.AppendLine("COMENZI ÎNTÂRZIATE");
        foreach (var order in orders)
        {
            var daysLate = today.DayNumber - order.ExpectedDeliveryDate.DayNumber;
            body.AppendLine($"- {order.Number}, {order.SupplierName}: trebuia livrată pe {Date(order.ExpectedDeliveryDate)} ({Count(daysLate, "zi", "zile")} de întârziere)");

            foreach (var line in order.Lines.Where(l => l.ReceivedQuantity < l.OrderedQuantity))
                body.AppendLine($"    lipsesc {Quantity(line.OrderedQuantity - line.ReceivedQuantity)} {line.Unit} {line.ProductName} (primit {Quantity(line.ReceivedQuantity)} din {Quantity(line.OrderedQuantity)})");
        }
        body.AppendLine();
    }

    private static void AppendUnorderedItems(StringBuilder body, IReadOnlyList<UnorderedItem> items)
    {
        if (items.Count == 0)
            return;

        body.AppendLine("MARFĂ DE CLARIFICAT (necomandată sau peste cantitatea comandată)");
        foreach (var item in items)
            body.AppendLine($"- recepția {item.ReceiptId} din {Date(item.ReceivedDate)}, {item.SupplierName}: {Quantity(item.Quantity)} {item.Unit} {item.ProductName}");
        body.AppendLine();
    }

    private static void AppendBlockedInvoices(StringBuilder body, IReadOnlyList<BlockedInvoice> invoices)
    {
        if (invoices.Count == 0)
            return;

        body.AppendLine("FACTURI BLOCATE");
        foreach (var invoice in invoices)
            body.AppendLine($"- {invoice.Number}, {invoice.SupplierName}, din {Date(invoice.InvoiceDate)}: {invoice.Reason}");
        body.AppendLine();
    }

    private static string Summary(int late, int unordered, int blocked) =>
        string.Join(", ", new[]
        {
            late > 0 ? Count(late, "comandă întârziată", "comenzi întârziate") : null,
            unordered > 0 ? Count(unordered, "poziție de clarificat", "poziții de clarificat") : null,
            blocked > 0 ? Count(blocked, "factură blocată", "facturi blocate") : null
        }.Where(s => s is not null));

    private static string Count(int n, string singular, string plural) => n == 1 ? $"1 {singular}" : $"{n} {plural}";

    private static string Date(DateOnly date) => date.ToString("dd.MM.yyyy", Ro);

    private static string Quantity(decimal value) => value.ToString("0.###", Ro);
}
