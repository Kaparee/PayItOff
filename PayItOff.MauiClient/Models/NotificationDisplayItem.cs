using PayItOff.Domain.Enums;
using PayItOff.Shared.Responses;

namespace PayItOff.MauiClient.Models;

public class NotificationDisplayItem
{
    public int NotificationId { get; set; }
    public NotificationType NotificationType { get; set; }
    public int ActorId { get; set; }
    public string ActorAvatarUrl { get; set; } = string.Empty;
    public string ActorFullName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationStatus NotificationStatus { get; set; }
    public int EntityId { get; set; }
    public EntityType EntityType { get; set; }
    public string CreatedAt { get; set; } = string.Empty;

    public bool IsRead => NotificationStatus == NotificationStatus.Read;
    public bool IsActionRequired => NotificationType == NotificationType.NeedAction;
    public bool IsDailySummary => NotificationType == NotificationType.DailySummary;

    public string DisplayText => IsDailySummary
        ? "Kliknij, aby zobaczyć podsumowanie powiadomień"
        : Body;

    public double Opacity => IsRead ? 0.6 : 1.0;

    public string BackgroundColor => IsRead ? "#121826" : "#1E232D";
    public string TextColor => IsRead ? "#9CA3AF" : "#F3F4F6";

    public string LeftBarColor
    {
        get
        {
            if (IsRead)
            {
                return "#374151";
            }

            return NotificationType switch
            {
                NotificationType.Deleting => "#EF4444",
                NotificationType.NeedAction => "#F59E0B",
                NotificationType.DailySummary => "#8B5CF6",
                NotificationType.Adding => "#10B981",
                _ => "#10B981"
            };
        }
    }

    public string IconSource
    {
        get
        {
            if (NotificationType == NotificationType.DailySummary)
            {
                return "calendar_icon.png";
            }

            if (EntityType == EntityType.Friends)
            {
                if (NotificationType == NotificationType.Deleting || Body.Contains("odrzucone") || Body.Contains("Odrzucono") || Body.Contains("odrzucił") || Body.Contains("usunięte"))
                {
                    return "friend_remove_icon.png";
                }

                if (NotificationType == NotificationType.NeedAction || NotificationType == NotificationType.Adding || Body.Contains("Zaakceptowano") || Body.Contains("zaakceptował"))
                {
                    return "friends_icon_green.png";
                }

                return "friends_icon.png";
            }
            if (EntityType == EntityType.Expenses)
            {
                return "new_expense_icon.png";
            }

            if (EntityType is EntityType.Settlements or EntityType.NetSettlements)
            {
                return "new_settlement_icon.png";
            }

            if (EntityType == EntityType.GroupDebts)
            {
                return "wallet_icon.png";
            }

            if (EntityType is EntityType.Groups or EntityType.GroupMembers)
            {
                if (Body.Contains("wyrzucony") || Body.Contains("usunął z grupy"))
                {
                    return "red_leave_icon.png";
                }

                if (Body.Contains("opuścił") || Body.Contains("usunął"))
                {
                    return "leave_icon.png";
                }

                return "groups_icon.png";
            }
            return "notifications_icon.png";
        }
    }

    public string TimeDisplay
    {
        get
        {
            return CreatedAt;
        }
    }

    public static NotificationDisplayItem FromResponse(NotificationResponse response)
    {
        return new NotificationDisplayItem
        {
            NotificationId = response.NotificationId,
            NotificationType = response.NotificationType,
            ActorId = response.ActorId,
            ActorAvatarUrl = response.ActorAvatarUrl,
            ActorFullName = response.ActorFullName,
            Body = response.Body,
            NotificationStatus = response.NotificationStatus,
            EntityId = response.EntityId,
            EntityType = response.EntityType,
            CreatedAt = response.CreatedAt
        };
    }
}
