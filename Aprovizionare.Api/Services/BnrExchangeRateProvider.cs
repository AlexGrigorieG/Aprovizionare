using System.Globalization;
using System.Xml.Linq;
using Aprovizionare.Api.Data;
using Aprovizionare.Api.Entities;
using Aprovizionare.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace Aprovizionare.Api.Services;

public record BnrRate(DateOnly Date, string Currency, decimal Rate);

public class BnrExchangeRateProvider(HttpClient http, AppDbContext db) : IExchangeRateProvider
{
    private const int MaxDaysWithoutRate = 7;

    public async Task<decimal> GetRateAsync(Currency currency, DateOnly date)
    {
        if (currency == Currency.RON)
            return 1m;

        var exact = await db.ExchangeRates
            .Where(r => r.Currency == currency && r.Date == date)
            .Select(r => (decimal?)r.Rate)
            .FirstOrDefaultAsync();
        if (exact is not null)
            return exact.Value;

        await DownloadAsync(currency, date);

        var cached = await db.ExchangeRates
            .Where(r => r.Currency == currency && r.Date <= date && r.Date > date.AddDays(-MaxDaysWithoutRate))
            .Select(r => new { r.Date, r.Rate })
            .ToListAsync();

        return FindRateForDate(cached.Select(r => (r.Date, r.Rate)), date)
            ?? throw new BusinessRuleException(
                $"BNR nu a publicat un curs {currency} pentru {date:dd.MM.yyyy} sau pentru zilele dinainte.");
    }

    public static decimal? FindRateForDate(IEnumerable<(DateOnly Date, decimal Rate)> rates, DateOnly date) =>
        rates
            .Where(r => r.Date <= date && r.Date > date.AddDays(-MaxDaysWithoutRate))
            .OrderByDescending(r => r.Date)
            .Select(r => (decimal?)r.Rate)
            .FirstOrDefault();

    public static List<BnrRate> ParseRates(string xml)
    {
        var rates = new List<BnrRate>();

        foreach (var cube in XDocument.Parse(xml).Descendants().Where(e => e.Name.LocalName == "Cube"))
        {
            var date = DateOnly.ParseExact(cube.Attribute("date")!.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            foreach (var rate in cube.Elements().Where(e => e.Name.LocalName == "Rate"))
            {
                var value = decimal.Parse(rate.Value, CultureInfo.InvariantCulture);
                var multiplier = rate.Attribute("multiplier") is { } m ? int.Parse(m.Value, CultureInfo.InvariantCulture) : 1;
                rates.Add(new BnrRate(date, rate.Attribute("currency")!.Value, value / multiplier));
            }
        }

        return rates;
    }

    private async Task DownloadAsync(Currency currency, DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var urls = new List<string>();
        if (today.DayNumber - date.DayNumber < 10)
            urls.Add("nbrfxrates10days.xml");
        else
        {
            urls.Add($"files/xml/years/nbrfxrates{date.Year}.xml");
            if (date.DayOfYear <= MaxDaysWithoutRate)
                urls.Add($"files/xml/years/nbrfxrates{date.Year - 1}.xml");
        }

        var downloaded = new List<BnrRate>();
        try
        {
            foreach (var url in urls)
                downloaded.AddRange(ParseRates(await http.GetStringAsync(url)));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Xml.XmlException)
        {
            throw new BusinessRuleException("Cursul BNR nu a putut fi descărcat. Încearcă din nou peste câteva minute.");
        }

        var code = currency.ToString();
        var known = (await db.ExchangeRates.Where(r => r.Currency == currency).Select(r => r.Date).ToListAsync())
            .ToHashSet();

        db.ExchangeRates.AddRange(downloaded
            .Where(r => r.Currency == code && !known.Contains(r.Date))
            .DistinctBy(r => r.Date)
            .Select(r => new ExchangeRate { Date = r.Date, Currency = currency, Rate = r.Rate }));

        await db.SaveChangesAsync();
    }
}
