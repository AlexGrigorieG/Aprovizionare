using Aprovizionare.Api.Dtos;
using Aprovizionare.Api.Entities;
using Aprovizionare.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aprovizionare.Api.Controllers;

[ApiController]
[Route("api/goods-receipts")]
[Authorize]
public class GoodsReceiptsController(GoodsReceiptService service) : ControllerBase
{
    private const string CanEdit = $"{Roles.Warehouse},{Roles.Admin}";

    [HttpGet]
    public Task<List<GoodsReceiptDto>> GetAll() => service.GetAllAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GoodsReceiptDto>> GetById(int id)
    {
        var receipt = await service.GetByIdAsync(id);
        if (receipt is null)
            return NotFound();

        return receipt;
    }

    [HttpPost]
    [Authorize(Roles = CanEdit)]
    public async Task<ActionResult<GoodsReceiptDto>> Create(CreateGoodsReceiptDto dto)
    {
        var receipt = await service.RegisterAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, receipt);
    }
}
