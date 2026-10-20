using System.Globalization;

namespace Aprovizionare.Api.Services;

public record InvoiceLineToMatch(int PurchaseOrderLineId, decimal Quantity, decimal UnitPrice);

public record OrderLineFacts(
    int LineId,
    string ProductName,
    decimal OrderedUnitPrice,
    decimal ReceivedQuantity,
    decimal AlreadyInvoicedQuantity);

public record MatchResult(bool IsValid, List<string> Problems);

public static class InvoiceMatcher
{
    public const decimal PriceTolerance = 0.01m;

    private static readonly CultureInfo Ro = CultureInfo.GetCultureInfo("ro-RO");

    public static MatchResult Match(
        IEnumerable<InvoiceLineToMatch> invoiceLines, IReadOnlyDictionary<int, OrderLineFacts> orderLines)
    {
        var problems = new List<string>();

        foreach (var group in invoiceLines.GroupBy(l => l.PurchaseOrderLineId))
        {
            if (!orderLines.TryGetValue(group.Key, out var line))
            {
                problems.Add($"Linia de comandă {group.Key} nu aparține comenzii facturate.");
                continue;
            }

            var invoicedTotal = line.AlreadyInvoicedQuantity + group.Sum(l => l.Quantity);
            if (invoicedTotal > line.ReceivedQuantity)
            {
                problems.Add(
                    $"{line.ProductName}: facturat în total {Quantity(invoicedTotal)} " +
                    $"(din care {Quantity(line.AlreadyInvoicedQuantity)} pe facturi anterioare), " +
                    $"recepționat {Quantity(line.ReceivedQuantity)}.");
            }

            foreach (var price in group.Select(l => l.UnitPrice).Distinct())
            {
                if (Math.Abs(price - line.OrderedUnitPrice) > PriceTolerance)
                    problems.Add(
                        $"{line.ProductName}: preț facturat {Price(price)}, preț din comandă {Price(line.OrderedUnitPrice)}.");
            }
        }

        return new MatchResult(problems.Count == 0, problems);
    }

    private static string Quantity(decimal value) => value.ToString("0.###", Ro);

    private static string Price(decimal value) => value.ToString("0.00##", Ro);
}
