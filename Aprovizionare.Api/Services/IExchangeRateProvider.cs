using Aprovizionare.Api.Enums;

namespace Aprovizionare.Api.Services;

public interface IExchangeRateProvider
{
    Task<decimal> GetRateAsync(Currency currency, DateOnly date);
}
