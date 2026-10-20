using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aprovizionare.Api.Entities;
using Aprovizionare.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Aprovizionare.Api.Data;

public class AuditInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    private List<(EntityEntry Entry, EntityState State, string? Changes)> _pending = [];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            _pending = eventData.Context.ChangeTracker.Entries()
                .Where(IsAudited)
                .Select(e => (e, e.State, e.State == EntityState.Added ? null : DescribeChanges(e, e.State)))
                .ToList();
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null || _pending.Count == 0)
            return result;

        var logs = _pending.Select(p => new AuditLog
        {
            UserName = currentUser.UserName,
            Action = p.State.ToString(),
            EntityName = p.Entry.Metadata.ClrType.Name,
            EntityId = KeyOf(p.Entry),
            Changes = p.Changes ?? DescribeChanges(p.Entry, EntityState.Added),
            Timestamp = DateTime.UtcNow
        }).ToList();

        _pending = [];
        eventData.Context.Set<AuditLog>().AddRange(logs);
        await eventData.Context.SaveChangesAsync(cancellationToken);

        return result;
    }

    private static bool IsAudited(EntityEntry e) =>
        e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
        && e.Entity is not (AuditLog or AppUser)
        && e.Entity.GetType().Namespace == typeof(Supplier).Namespace;

    private static string KeyOf(EntityEntry e) =>
        string.Join(",", e.Properties.Where(p => p.Metadata.IsPrimaryKey()).Select(p => p.CurrentValue));

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() } 
    };

    private static string DescribeChanges(EntityEntry e, EntityState state)
    {
        var changes = e.Properties
            .Where(p => !p.Metadata.IsPrimaryKey())
            .Where(p => state != EntityState.Modified || p.IsModified)
            .ToDictionary(
                p => p.Metadata.Name,
                p => state switch
                {
                    EntityState.Added => new { Old = (object?)null, New = p.CurrentValue },
                    EntityState.Deleted => new { Old = p.OriginalValue, New = (object?)null },
                    _ => new { Old = p.OriginalValue, New = p.CurrentValue }
                });

        return JsonSerializer.Serialize(changes, JsonOptions);
    }
}