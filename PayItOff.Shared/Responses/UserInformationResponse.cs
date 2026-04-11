namespace PayItOff.Shared.Responses
{
    public class UserInformationResponse
    {
        public required string AvatarUrl { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public required string Email { get; set; }
        public required string Nickname { get; set; }
        public string? PhoneNumber { get; set; }
        public string? IBAN { get; set; }
        public required UserNotificationSettingsResponse Notifications { get; set; }
    }

    public record UserNotificationSettingsResponse(
        bool ReceiveEmail,
        bool DailySummary,
        bool NotifyOnGroupJoined,
        bool NotifyOnExpenseAdded,
        bool NotifyOnGroupRemoved,
        bool NotifyOnFriendRemoved,
        bool NotifyOnExpenseChanged,
        bool NotifyOnTransferConfirmed
    );
}
