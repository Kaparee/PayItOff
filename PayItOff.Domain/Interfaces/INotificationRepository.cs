using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;

namespace PayItOff.Domain.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task UpdateAsync(Notification notification);
    Task<bool> HasDebtReminderSinceAsync(int creditorId, int debtorId, int groupDebtId, DateTime sinceUtc);
    Task<List<Notification>> GetUserNotificationsAsync(int userId, List<string> filters);
    Task<Notification?> GetUserNotificationByIdAsync(int userId, int notificationId);
    Task<List<Notification>> GetLast5UserNotificationsAsync(int userId);
    Task<Notification?> GetActionNotificationAsync(int userId, int entityId, EntityType entityType);
}
