using PayItOff.Domain.Enums;

namespace PayItOff.Domain.Entities
{
    public class AuditLog
    {
        public int Id { get; private set; }
        public EntityType EntityType { get; private set; }
        public int EntityId { get; private set; }
        public User User { get; private set; }
        public int UserId { get; private set; }
        public AuditLogAction Action { get; private set; }
        public string? OldValues { get; private set; }
        public string? NewValues { get; private set; }
        public DateTime CreatedAt { get; private set; }

        protected AuditLog() { }

        private AuditLog(EntityType entityType, int entityId, User user, AuditLogAction action, string oldValues, string newValues)
        {
            if (entityId == 0) { throw new InvalidOperationException("Nie można przypisać do Audytu id = 0"); }
            if (user == null) { throw new ArgumentNullException(nameof(user), "Error przy user"); }

            EntityType = entityType;
            EntityId = entityId;
            User = user;
            UserId = user.Id;
            Action = action;
            OldValues = oldValues;
            NewValues = newValues;
            CreatedAt = DateTime.UtcNow;
        }

        public static AuditLog Create(EntityType entityType, int entityId, User user, AuditLogAction action, string oldValues, string newValues)
        {
            return new AuditLog(entityType, entityId, user, action, oldValues, newValues);
        }
    }
}