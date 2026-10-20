using Aprovizionare.Api.Entities;
using Aprovizionare.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aprovizionare.Api.Controllers;

[ApiController]
[Route("api/alerts")]
[Authorize(Roles = Roles.Admin)]
public class AlertsController(AlertService service) : ControllerBase
{
    [HttpGet("preview")]
    public Task<List<EmailMessage>> Preview() => service.BuildAsync();

    [HttpPost("send-now")]
    public async Task<IActionResult> SendNow() => Ok(new { sent = await service.SendAsync(HttpContext.RequestAborted) });
}
