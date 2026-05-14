using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Persistence;

namespace PayItOff.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly PayItOffDbContext _context;

    public NotificationRepository(PayItOffDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task<bool> HasDebtReminderSinceAsync(int creditorId, int debtorId, int groupDebtId, DateTime sinceUtc)
    {
        return _context.Notifications.AnyAsync(n =>
            n.ActorId == creditorId
            && n.UserId == debtorId
            && n.EntityType == EntityType.GroupDebts
            && n.EntityId == groupDebtId
            && n.CreatedAt >= sinceUtc);
    }
}
