using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;

namespace PayItOff.Infrastructure.Persistence.Interceptors;

public class AuditLogInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        LogChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        LogChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void LogChanges(DbContext? context)
    {
        if (context == null) return;

        var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out var userId)) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        foreach (var entry in entries)
        {
            var entityTypeEnum = GetEntityTypeEnum(entry.Entity);
            if (entityTypeEnum == null) continue;

            var action = GetAuditAction(entry.State);
            if (action == null) continue;

            string? oldValues = null;
            string? newValues = null;

            if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                oldValues = GetEntityValues(entry, true);
            }
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                newValues = GetEntityValues(entry, false);
            }
            
            var entityIdProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
            int entityId = 0;
            if (entityIdProperty != null && entityIdProperty.CurrentValue is int idVal)
            {
                entityId = idVal;
            }

            if (entityId == 0 && entry.State != EntityState.Added)
                continue;

            var auditLog = AuditLog.CreateWithUserId(
                entityTypeEnum.Value,
                entityId,
                userId,
                action.Value,
                oldValues,
                newValues
            );

            context.Add(auditLog);
        }
    }

    private EntityType? GetEntityTypeEnum(object entity)
    {
        return entity switch
        {
            Expense => EntityType.Expenses,
            Friend => EntityType.Friends,
            Group => EntityType.Groups,
            GroupMember => EntityType.GroupMembers,
            Settlement => EntityType.Settlements,
            User => EntityType.Users,
            GroupDebt => EntityType.GroupDebts,
            _ => null
        };
    }

    private AuditLogAction? GetAuditAction(EntityState state)
    {
        return state switch
        {
            EntityState.Added => AuditLogAction.Created,
            EntityState.Modified => AuditLogAction.Updated,
            EntityState.Deleted => AuditLogAction.Deleted,
            _ => null
        };
    }

    private string GetEntityValues(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, bool getOld)
    {
        var values = new Dictionary<string, object?>();
        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey()) continue;
            
            if (getOld)
            {
                if (entry.State == EntityState.Modified && !property.IsModified)
                    continue;

                values[property.Metadata.Name] = property.OriginalValue;
            }
            else
            {
                if (entry.State == EntityState.Modified && !property.IsModified)
                    continue;

                values[property.Metadata.Name] = property.CurrentValue;
            }
        }
        return JsonSerializer.Serialize(values);
    }
}
