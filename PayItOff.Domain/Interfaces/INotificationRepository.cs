using PayItOff.Domain.Entities;

namespace PayItOff.Domain.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task UpdateAsync(Notification notification);
    Task<bool> HasDebtReminderSinceAsync(int creditorId, int debtorId, int groupDebtId, DateTime sinceUtc);
}
