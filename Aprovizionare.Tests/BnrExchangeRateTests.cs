using Aprovizionare.Api.Services;

namespace Aprovizionare.Tests;

public class BnrExchangeRateTests
{
    private const string Xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <DataSet xmlns="http://www.bnr.ro/xsd">
          <Header><Publisher>National Bank of Romania</Publisher></Header>
          <Body>
            <Subject>Reference rates</Subject>
            <OrigCurrency>RON</OrigCurrency>
            <Cube date="2026-10-15">
              <Rate currency="EUR">4.9701</Rate>
              <Rate currency="HUF" multiplier="100">1.2500</Rate>
            </Cube>
            <Cube date="2026-10-16">
              <Rate currency="EUR">4.9712</Rate>
            </Cube>
          </Body>
        </DataSet>
        """;

    [Fact]
    public void Citeste_cursurile_din_xml_bnr()
    {
        var rates = BnrExchangeRateProvider.ParseRates(Xml);

        Assert.Contains(new BnrRate(new DateOnly(2026, 10, 16), "EUR", 4.9712m), rates);
        Assert.Equal(3, rates.Count);
    }

    [Fact]
    public void Multiplicatorul_este_aplicat()
    {
        var huf = Assert.Single(BnrExchangeRateProvider.ParseRates(Xml), r => r.Currency == "HUF");

        Assert.Equal(0.0125m, huf.Rate);
    }

    [Fact]
    public void Duminica_foloseste_cursul_de_vineri()
    {
        var rates = BnrExchangeRateProvider.ParseRates(Xml)
            .Where(r => r.Currency == "EUR")
            .Select(r => (r.Date, r.Rate));

        var rate = BnrExchangeRateProvider.FindRateForDate(rates, new DateOnly(2026, 10, 18));

        Assert.Equal(4.9712m, rate);
    }

    [Fact]
    public void Fara_curs_in_ultimele_zile_nu_intoarce_nimic()
    {
        var rates = BnrExchangeRateProvider.ParseRates(Xml)
            .Where(r => r.Currency == "EUR")
            .Select(r => (r.Date, r.Rate));

        Assert.Null(BnrExchangeRateProvider.FindRateForDate(rates, new DateOnly(2026, 11, 30)));
    }
}
