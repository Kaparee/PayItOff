using PayItOff.Application.Interfaces;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;
using PayItOff.Domain.Interfaces;
using System.Text;

namespace PayItOff.Application.Services;

public class DailySummaryJob : IDailySummaryJob
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DailySummaryJob(IUserRepository userRepository, INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync()
    {
        var users = await _userRepository.GetAllUsersWithDailySummaryAsync();

        foreach (var user in users)
        {
            var hiddenNotifications = await _notificationRepository.GetHiddenNotificationsFromTodayAsync(user.Id);

            if (hiddenNotifications.Any())
            {
                var sb = new StringBuilder();
                sb.AppendLine("Podsumowanie z dzisiaj:");
                
                foreach (var notif in hiddenNotifications)
                {
                    sb.AppendLine($"- {notif.Body}");
                    notif.Delete();
                    await _notificationRepository.UpdateAsync(notif);
                }

                var summaryNotification = Notification.Create(user.Id, user.Id, NotificationType.DailySummary, sb.ToString(), user.Id, EntityType.Users);
                
                await _notificationRepository.AddAsync(summaryNotification);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
