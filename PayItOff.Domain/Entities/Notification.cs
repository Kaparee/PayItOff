using PayItOff.Domain.Enums;

namespace PayItOff.Domain.Entities
{
    public class Notification
    {
        public int Id { get; private set; }
        public User User { get; private set; }
        public User Actor { get; private set; }
        public int UserId { get; private set; }
        public int ActorId { get; private set; }
        public NotificationType Type { get; private set; }
        public string Body { get; private set; }
        public NotificationStatus Status { get; private set; }
        public int EntityId { get; private set; }
        public EntityType EntityType { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? ReadAt { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        protected Notification() { }

        private Notification(User user, User actor, NotificationType type, string body, NotificationStatus status, int entityId, EntityType entityType)
        {
            if (entityId == 0) { throw new InvalidOperationException("Nie można przypisać do Powiadomien id = 0"); }
            if (user == null) { throw new ArgumentNullException(nameof(user), "Error przy user"); }
            if (actor == null) { throw new ArgumentNullException(nameof(actor), "Error przy actor"); }
            if (string.IsNullOrWhiteSpace(body)) { throw new ArgumentException(nameof(body)); }

            User = user;
            Actor = actor;
            UserId = user.Id;
            ActorId = actor.Id;
            Type = type;
            Body = body;
            Status = status;
            EntityId = entityId;
            EntityType = entityType;
            CreatedAt = DateTime.UtcNow;
        }

        public static Notification Create(User user, User actor, NotificationType type, string body, int entityId, EntityType entityType)
        {
            return new Notification(user, actor, type, body, NotificationStatus.Unread, entityId, entityType);
        }

        public void MarkAsRead()
        {
            if (Status == NotificationStatus.Unread)
            {
                Status = NotificationStatus.Read;
                ReadAt = DateTime.UtcNow;
            }
        }

        public void Delete()
        {
            if (Type != NotificationType.NeedAction)
            {
                DeletedAt = DateTime.UtcNow;
            }
        }
    }
}