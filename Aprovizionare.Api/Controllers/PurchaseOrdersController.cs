using Aprovizionare.Api.Dtos;
using Aprovizionare.Api.Entities;
using Aprovizionare.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aprovizionare.Api.Controllers;

[ApiController]
[Route("api/purchase-orders")]
[Authorize]
public class PurchaseOrdersController(PurchaseOrderService service) : ControllerBase
{
    private const string CanEdit = $"{Roles.Purchasing},{Roles.Admin}";

    private bool IncludePrices => !User.IsInRole(Roles.Warehouse);

    [HttpGet]
    public Task<List<PurchaseOrderDto>> GetAll() => service.GetAllAsync(IncludePrices);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(int id)
    {
        var order = await service.GetByIdAsync(id, IncludePrices);
        if (order is null)
            return NotFound();

        return order;
    }

    [HttpPost]
    [Authorize(Roles = CanEdit)]
    public async Task<ActionResult<PurchaseOrderDto>> Create(CreatePurchaseOrderDto dto)
    {
        var order = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = CanEdit)]
    public async Task<IActionResult> Cancel(int id) =>
        await service.CancelAsync(id) ? NoContent() : NotFound();
}
