using Aprovizionare.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aprovizionare.Api.Controllers;

public record SupplierDto(int Id, string Name, string TaxId, string Email);

public record ProductDto(int Id, string Code, string Name, string Unit);

[ApiController]
[Route("api")]
[Authorize]
public class CatalogController(AppDbContext db) : ControllerBase
{
    [HttpGet("suppliers")]
    public Task<List<SupplierDto>> Suppliers() =>
        db.Suppliers.OrderBy(s => s.Name).Select(s => new SupplierDto(s.Id, s.Name, s.TaxId, s.Email)).ToListAsync();

    [HttpGet("products")]
    public Task<List<ProductDto>> Products() =>
        db.Products.OrderBy(p => p.Name).Select(p => new ProductDto(p.Id, p.Code, p.Name, p.Unit)).ToListAsync();
}
