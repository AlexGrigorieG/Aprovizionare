using Aprovizionare.Api.Services;

namespace Aprovizionare.Tests;

public class InvoiceMatcherTests
{
    private static Dictionary<int, OrderLineFacts> Order(params OrderLineFacts[] lines) =>
        lines.ToDictionary(l => l.LineId);

    private static readonly OrderLineFacts Motocoase =
        new(LineId: 1, "Motocoasă", OrderedUnitPrice: 120, ReceivedQuantity: 60, AlreadyInvoicedQuantity: 0);

    private static readonly OrderLineFacts Lanturi =
        new(LineId: 2, "Lanț", OrderedUnitPrice: 8, ReceivedQuantity: 50, AlreadyInvoicedQuantity: 0);

    [Fact]
    public void Factura_corecta_trece()
    {
        var result = InvoiceMatcher.Match(
            [new InvoiceLineToMatch(1, 60, 120), new InvoiceLineToMatch(2, 50, 8)],
            Order(Motocoase, Lanturi));

        Assert.True(result.IsValid);
        Assert.Empty(result.Problems);
    }

    [Fact]
    public void Se_blocheaza_cand_se_factureaza_mai_mult_decat_s_a_receptionat()
    {
        var result = InvoiceMatcher.Match([new InvoiceLineToMatch(1, 100, 120)], Order(Motocoase));

        Assert.False(result.IsValid);
        Assert.Contains("recepționat 60", Assert.Single(result.Problems));
    }

    [Fact]
    public void Se_blocheaza_cand_pretul_difera_de_comanda()
    {
        var result = InvoiceMatcher.Match([new InvoiceLineToMatch(2, 50, 8.50m)], Order(Lanturi));

        Assert.False(result.IsValid);
        Assert.Contains("preț facturat 8,50", Assert.Single(result.Problems));
    }

    [Fact]
    public void Diferenta_de_rotunjire_este_acceptata()
    {
        var result = InvoiceMatcher.Match([new InvoiceLineToMatch(2, 50, 8.005m)], Order(Lanturi));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Cantitatile_deja_facturate_se_aduna()
    {
        var dejaFacturat = Motocoase with { ReceivedQuantity = 100, AlreadyInvoicedQuantity = 100 };

        var result = InvoiceMatcher.Match([new InvoiceLineToMatch(1, 10, 120)], Order(dejaFacturat));

        Assert.False(result.IsValid);
        Assert.Contains("facturat în total 110", Assert.Single(result.Problems));
    }

    [Fact]
    public void Aceeasi_linie_de_doua_ori_pe_factura_se_aduna()
    {
        var result = InvoiceMatcher.Match(
            [new InvoiceLineToMatch(1, 40, 120), new InvoiceLineToMatch(1, 30, 120)],
            Order(Motocoase));

        Assert.False(result.IsValid);
        Assert.Contains("facturat în total 70", Assert.Single(result.Problems));
    }

    [Fact]
    public void Linia_din_alta_comanda_este_semnalata()
    {
        var result = InvoiceMatcher.Match([new InvoiceLineToMatch(99, 1, 1)], Order(Motocoase));

        Assert.False(result.IsValid);
        Assert.Contains("nu aparține comenzii", Assert.Single(result.Problems));
    }
}
