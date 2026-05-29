using Microsoft.AspNetCore.Http;
using PayItOff.Domain.Enums;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Application.Interfaces
{
    public interface INotificationService
    {
        Task<List<NotificationResponse>> GetUserNotificationAsync(int userId, List<string> filters);
        Task SetNotificationAsReadAsync(int userId, int notificationId);
        Task DeleteNotificationAsync(int userId, int notificationId);
        Task SetAllNotificationsAsReadAsync(int userId);
        Task DeleteAllNotificationsAsync(int userId);
        Task CreateNotificationAsync(int userId, int actorId, NotificationType notificationType, string body, int entityId, EntityType entityType);
        Task<List<NotificationResponse>> GetUserLast5Notifications(int userId);
        Task ResolveActionNotificationAsync(int userId, int entityId, EntityType entityType, bool isAccepted);
    }
}
