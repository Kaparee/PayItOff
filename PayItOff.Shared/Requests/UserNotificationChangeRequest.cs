namespace PayItOff.Shared.Requests
{
    public class UserNotificationChangeRequest
    {
        public required UserNotificationSettingsRequest Notifications { get; set; }
    }
    public record UserNotificationSettingsRequest(
        bool DailySummary,
        bool NotifyOnGroupJoined,
        bool NotifyOnExpenseAdded,
        bool NotifyOnGroupRemoved,
        bool NotifyOnFriendRemoved,
        bool NotifyOnExpenseChanged,
        bool NotifyOnTransferConfirmed
    );
}
