using PayItOff.Domain.Enums;

namespace PayItOff.Shared.Responses
{
    public class NotificationResponse
    {
        public required int NotificationId { get; set; }
        public required NotificationType NotificationType { get; set; }
        public required int ActorId { get; set; }
        public required string ActorAvatarUrl { get; set; }
        public required string ActorFullName { get; set; }
        public required string Body { get; set; }
        public required NotificationStatus NotificationStatus { get; set; }
        public required int EntityId { get; set; }
        public required EntityType EntityType { get; set; }
        public required string CreatedAt { get; set; }
    }
}
