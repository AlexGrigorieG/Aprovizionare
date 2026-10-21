using Aprovizionare.Api.Data;
using Aprovizionare.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aprovizionare.Api.Controllers;

public record AuditLogDto(
    int Id, DateTime Timestamp, string UserName, string Action, string EntityName, string EntityId, string Changes);

[ApiController]
[Route("api/audit")]
[Authorize(Roles = Roles.Admin)]
public class AuditController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public Task<List<AuditLogDto>> Get(string? entityName, string? entityId, string? userName, int take = 200)
    {
        var logs = db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
            logs = logs.Where(l => l.EntityName == entityName);
        if (!string.IsNullOrWhiteSpace(entityId))
            logs = logs.Where(l => l.EntityId == entityId);
        if (!string.IsNullOrWhiteSpace(userName))
            logs = logs.Where(l => l.UserName.Contains(userName));

        return logs
            .OrderByDescending(l => l.Id)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(l => new AuditLogDto(
                l.Id,
                DateTime.SpecifyKind(l.Timestamp, DateTimeKind.Utc),
                l.UserName,
                l.Action,
                l.EntityName,
                l.EntityId,
                l.Changes))
            .ToListAsync();
    }
}
