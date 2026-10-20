using Aprovizionare.Api.Dtos;
using Aprovizionare.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aprovizionare.Api.Controllers;

[ApiController]
[Route("api/stock")]
[Authorize]
public class StockController(GoodsReceiptService service) : ControllerBase
{
    [HttpGet]
    public Task<List<StockItemDto>> Get() => service.GetStockAsync();
}
