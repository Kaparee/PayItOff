using Humanizer;
using Microsoft.Extensions.Configuration;
using PayItOff.Application.Helpers;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;
using PayItOff.Domain.Exceptions;
using PayItOff.Domain.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Buffers.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PayItOff.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public NotificationService(INotificationRepository notificationRepository, IUserRepository userRepository, IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<List<NotificationResponse>> GetUserNotificationAsync(int userId, List<string> filters)
    {
        var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, filters);

        var plCulture = new CultureInfo("pl-PL");

        var baseUrl = _configuration["AppUrls:BackendUrl"];

        return notifications.Select(notification =>
        {
        var diff = DateTime.UtcNow - notification.CreatedAt;

        var lastUpdateText = diff.TotalMinutes < 1
            ? "Teraz"
            : $"{diff.Humanize(precision: 1, culture: plCulture)} temu";

            return new NotificationResponse
            {
                NotificationId = notification.Id,
                NotificationType = notification.Type,
                ActorId = notification.ActorId,
                ActorAvatarUrl = AvatarUrlHelper.BuildUserAvatarUrl(baseUrl!, notification.Actor.AvatarUrl!),
                ActorFullName = notification.Actor.FullName,
                Body = notification.Body,
                NotificationStatus = notification.ReadAt == null ? NotificationStatus.Unread : NotificationStatus.Read,
                EntityId = notification.EntityId,
                EntityType = notification.EntityType,
                CreatedAt = lastUpdateText
            };
        }).ToList();

    }

    public async Task SetNotificationAsReadAsync(int userId, int notificationId)
    {
        var notification = await _notificationRepository.GetUserNotificationByIdAsync(userId, notificationId);
        if (notification == null) { throw new NotificationNotFoundException(); }

        notification.MarkAsRead();
        await _notificationRepository.UpdateAsync(notification);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteNotificationAsync(int userId, int notificationId)
    {
        var notification = await _notificationRepository.GetUserNotificationByIdAsync(userId, notificationId);
        if (notification == null) { throw new NotificationNotFoundException(); }

        notification.Delete();
        await _notificationRepository.UpdateAsync(notification);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task SetAllNotificationsAsReadAsync(int userId)
    {
        var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, null!);

        foreach (var notification in notifications)
        {
            if(notification.ReadAt == null)
            {
                notification.MarkAsRead();
                await _notificationRepository.UpdateAsync(notification);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAllNotificationsAsync(int userId)
    {
        var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, null!);

        foreach (var notification in notifications)
        {
            if (notification.DeletedAt == null)
            {
                notification.Delete();
                await _notificationRepository.UpdateAsync(notification);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task CreateNotificationAsync(int userId, int actorId, NotificationType notificationType, string body, int entityId, EntityType entityType)
    {
        var newNotification = Notification.Create(userId, actorId, notificationType, body, entityId, entityType);

        await _notificationRepository.AddAsync(newNotification);
    }

    public async Task<List<NotificationResponse>> GetUserLast5Notifications(int userId)
    {
        var notifications = await _notificationRepository.GetLast5UserNotificationsAsync(userId);

        var plCulture = new CultureInfo("pl-PL");

        var baseUrl = _configuration["AppUrls:BackendUrl"];
        return notifications.Select(notification =>
        {
            var diff = DateTime.UtcNow - notification.CreatedAt;

            var lastUpdateText = diff.TotalMinutes < 1
                ? "Teraz"
                : $"{diff.Humanize(precision: 1, culture: plCulture)} temu";

            return new NotificationResponse
            {
                NotificationId = notification.Id,
                NotificationType = notification.Type,
                ActorId = notification.ActorId,
                ActorAvatarUrl = AvatarUrlHelper.BuildUserAvatarUrl(baseUrl!, notification.Actor.AvatarUrl!),
                ActorFullName = notification.Actor.FullName,
                Body = notification.Body,
                NotificationStatus = notification.ReadAt == null ? NotificationStatus.Unread : NotificationStatus.Read,
                EntityId = notification.EntityId,
                EntityType = notification.EntityType,
                CreatedAt = lastUpdateText
            };
        }).ToList();
    }

    public async Task ResolveActionNotificationAsync(int userId, int entityId, EntityType entityType, bool isAccepted)
    {
        var notification = await _notificationRepository.GetActionNotificationAsync(userId, entityId, entityType);
        if (notification != null)
        {
            notification.ChangeTypeToNormal();
            notification.AppendToBody(isAccepted ? " (ZAAKCEPTOWANE)" : " (ODRZUCONE)");
            await _notificationRepository.UpdateAsync(notification);
        }
    }
}


//using PayItOff.Domain.Enums;

//namespace PayItOff.Shared.Requests
//{
//    public class CreateNotificationRequest
//    {
//        public required int UserId { get; set; }
//        public required int ActorId { get; set; }
//        public required NotificationType NotificationType { get; set; }
//        public required string Body { get; set; }
//        public required int EntityId { get; set; }
//        public required EntityType EntityType { get; set; }
//    }
//}