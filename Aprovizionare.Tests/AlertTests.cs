using Aprovizionare.Api.Services;

namespace Aprovizionare.Tests;

public class AlertTests
{
    private static readonly DateOnly Today = new(2026, 10, 20);
    private static readonly string[] Purchasing = ["andrei@firma.ro"];
    private static readonly string[] Accounting = ["elena@firma.ro"];

    private static readonly LateOrder LateOrder = new(
        "PO-2026-0001", "Agro Parts SRL", new DateOnly(2026, 10, 12),
        [
            new MissingLine("Motocoasă", "buc", OrderedQuantity: 100, ReceivedQuantity: 60),
            new MissingLine("Lanț", "buc", OrderedQuantity: 50, ReceivedQuantity: 50)
        ]);

    private static readonly BlockedInvoice BlockedInvoice = new(
        "F-778", "Agro Parts SRL", new DateOnly(2026, 10, 14), "Motocoasă: facturat în total 100, recepționat 60.");

    [Fact]
    public void Fara_probleme_nu_pleaca_niciun_email()
    {
        var messages = AlertComposer.Compose(Today, [], [], [], Purchasing, Accounting);

        Assert.Empty(messages);
    }

    [Fact]
    public void Comanda_intarziata_ajunge_doar_la_achizitii_cu_ce_lipseste()
    {
        var message = Assert.Single(AlertComposer.Compose(Today, [LateOrder], [], [], Purchasing, Accounting));

        Assert.Equal(Purchasing, message.To);
        Assert.Contains("PO-2026-0001", message.Body);
        Assert.Contains("8 zile de întârziere", message.Body);
        Assert.Contains("lipsesc 40 buc Motocoasă", message.Body);
        Assert.DoesNotContain("Lanț", message.Body);
    }

    [Fact]
    public void Factura_blocata_ajunge_la_contabilitate_si_la_achizitii()
    {
        var messages = AlertComposer.Compose(Today, [], [BlockedInvoice], [], Purchasing, Accounting);

        Assert.Equal(2, messages.Count);
        Assert.All(messages, m => Assert.Contains("F-778", m.Body));
        Assert.Contains(messages, m => m.To.SequenceEqual(Accounting) && m.Subject.EndsWith("1 factură blocată"));
    }

    [Fact]
    public void Marfa_necomandata_apare_la_clarificat()
    {
        var item = new UnorderedItem(3, Today, "Agro Parts SRL", "Mănuși", "pereche", 10);

        var message = Assert.Single(AlertComposer.Compose(Today, [], [], [item], Purchasing, Accounting));

        Assert.Contains("MARFĂ DE CLARIFICAT", message.Body);
        Assert.Contains("10 pereche Mănuși", message.Body);
    }

    [Theory]
    [InlineData("2026-10-20 07:00", 1)]
    [InlineData("2026-10-20 08:00", 24)]
    [InlineData("2026-10-20 09:00", 23)]
    public void Jobul_ruleaza_la_ora_configurata(string now, int expectedHours)
    {
        var delay = DailyAlertsJob.TimeUntilNextRun(DateTime.Parse(now), new TimeOnly(8, 0));

        Assert.Equal(TimeSpan.FromHours(expectedHours), delay);
    }
}
