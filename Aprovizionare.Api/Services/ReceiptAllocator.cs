namespace Aprovizionare.Api.Services;

public record OpenOrderLine(int LineId, int ProductId, decimal OrderedQuantity, decimal OpenQuantity);

public record Allocation(int? PurchaseOrderLineId, decimal Quantity);

public static class ReceiptAllocator
{
    public static List<Allocation> Allocate(List<OpenOrderLine> openLines, int productId, decimal quantity)
    {
        var allocations = new List<Allocation>();
        var remaining = quantity;

        for (int i = 0; i < openLines.Count && remaining > 0; i++)
        {
            var line = openLines[i];
            if (line.ProductId != productId || line.OpenQuantity <= 0)
                continue;

            var taken = Math.Min(line.OpenQuantity, remaining);
            allocations.Add(new Allocation(line.LineId, taken));
            openLines[i] = line with { OpenQuantity = line.OpenQuantity - taken };
            remaining -= taken;
        }

        if (remaining > 0)
            allocations.Add(new Allocation(null, remaining));

        return allocations;
    }
}
