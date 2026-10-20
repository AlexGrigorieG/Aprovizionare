using Aprovizionare.Api.Enums;

namespace Aprovizionare.Api.Entities;

public class ExchangeRate
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public Currency Currency { get; set; }
    public decimal Rate { get; set; }
}