using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
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

    public Task UpdateAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        return Task.CompletedTask;
    }

    public async Task<bool> HasDebtReminderSinceAsync(int creditorId, int debtorId, int groupDebtId, DateTime sinceUtc)
    {
        return await _context.Notifications.AnyAsync(n =>
            n.ActorId == creditorId
            && n.UserId == debtorId
            && n.EntityType == EntityType.GroupDebts
            && n.EntityId == groupDebtId
            && n.CreatedAt >= sinceUtc);
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(int userId, List<string> filters)
    {

        IQueryable<Notification> notifications = _context.Notifications
            .Include(n => n.Actor);
        notifications = notifications.Where(x => x.UserId == userId && x.DeletedAt == null && x.Status != NotificationStatus.Hidden);

        if (filters != null)
        {
            if (filters.Contains("Unread"))
            {
                notifications = notifications.Where(x => x.ReadAt == null);
            }
            if (filters.Contains("NeedAction"))
            {
                notifications = notifications.Where(x => x.Type == NotificationType.NeedAction);
            }
        }

        return await notifications
            .OrderBy(x => x.ReadAt == null ? 0 : 1)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Notification?> GetUserNotificationByIdAsync(int userId, int notificationId)
    {
        return await _context.Notifications
            .Where(x => x.UserId == userId && x.Id == notificationId && x.DeletedAt == null)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Notification>> GetLast5UserNotificationsAsync(int userId)
    {
        return await _context.Notifications
            .Include(n => n.Actor)
            .Where(x => x.UserId == userId && x.DeletedAt == null && x.Status != NotificationStatus.Hidden)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .ToListAsync();
    }

    public async Task<Notification?> GetActionNotificationAsync(int userId, int entityId, EntityType entityType)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && n.EntityId == entityId && n.EntityType == entityType && n.Type == NotificationType.NeedAction && n.DeletedAt == null)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Notification>> GetHiddenNotificationsFromTodayAsync(int userId)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.Notifications
            .Where(n => n.UserId == userId && n.Status == NotificationStatus.Hidden && n.DeletedAt == null && n.CreatedAt >= today)
            .ToListAsync();
    }
}
