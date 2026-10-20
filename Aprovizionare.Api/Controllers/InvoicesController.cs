using Aprovizionare.Api.Dtos;
using Aprovizionare.Api.Entities;
using Aprovizionare.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aprovizionare.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = $"{Roles.Accounting},{Roles.Purchasing},{Roles.Admin}")]
public class InvoicesController(InvoiceService service) : ControllerBase
{
    private const string CanEdit = $"{Roles.Accounting},{Roles.Admin}";

    [HttpGet]
    public Task<List<InvoiceDto>> GetAll() => service.GetAllAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceDto>> GetById(int id) => Result(await service.GetByIdAsync(id));

    [HttpPost]
    [Authorize(Roles = CanEdit)]
    public async Task<ActionResult<InvoiceDto>> Create(CreateInvoiceDto dto)
    {
        var invoice = await service.RegisterAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
    }

    [HttpPost("{id:int}/recheck")]
    [Authorize(Roles = CanEdit)]
    public async Task<ActionResult<InvoiceDto>> Recheck(int id) => Result(await service.RecheckAsync(id));

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = CanEdit)]
    public async Task<ActionResult<InvoiceDto>> Approve(int id, ApproveInvoiceDto dto) =>
        Result(await service.ApproveAsync(id, dto.Justification));

    [HttpPost("{id:int}/pay")]
    [Authorize(Roles = CanEdit)]
    public async Task<ActionResult<InvoiceDto>> MarkPaid(int id) => Result(await service.MarkPaidAsync(id));

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = CanEdit)]
    public async Task<ActionResult<InvoiceDto>> Reject(int id, RejectInvoiceDto dto) =>
        Result(await service.RejectAsync(id, dto.Reason));

    private ActionResult<InvoiceDto> Result(InvoiceDto? invoice) =>
        invoice is null ? NotFound() : invoice;
}
