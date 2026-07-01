using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Persistence;

namespace PayItOff.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly PayItOffDbContext _context;

    public AuditLogRepository(PayItOffDbContext context)
    {
        _context = context;
    }

    public async Task<List<AuditLog>> GetAuditLogsForGroupAsync(int groupId)
    {

        var expenseIds = await _context.Expenses
            .IgnoreQueryFilters()
            .Where(e => e.GroupId == groupId)
            .Select(e => e.Id)
            .ToListAsync();

        var memberIds = await _context.GroupMembers
            .IgnoreQueryFilters()
            .Where(m => m.GroupId == groupId)
            .Select(m => m.Id)
            .ToListAsync();

        var logs = await _context.AuditLogs
            .Include(a => a.User)
            .Where(a =>
                (a.EntityType == EntityType.Groups && a.EntityId == groupId) ||
                (a.EntityType == EntityType.Expenses && expenseIds.Contains(a.EntityId)) ||
                (a.EntityType == EntityType.GroupMembers && memberIds.Contains(a.EntityId))
            )
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return logs;
    }
}
