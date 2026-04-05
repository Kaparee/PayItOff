namespace PayItOff.Domain.Entities
{
    public class Friend
    {
        public int Id { get; private set; }
        public User Inviter { get; private set; }
        public User Receiver { get; private set; }
        public int InviterId { get; private set; }
        public int ReceiverId { get; private set; }
        public DateTime SentAt { get; private set; }
        public DateTime? AcceptedAt { get; private set; }
        public DateTime? DeclinedAt { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        protected Friend() { }

        private Friend(User inviter, User receiver)
        {
            if (inviter == null) { throw new ArgumentNullException(nameof(inviter), "Error przy inviter"); }
            if (receiver == null) { throw new ArgumentNullException(nameof(receiver), "Error przy receiver"); }

            Inviter = inviter;
            Receiver = receiver;
            InviterId = inviter.Id;
            ReceiverId = receiver.Id;
            SentAt = DateTime.UtcNow;
        }

        public static Friend Invite(User inviter, User receiver)
        {
            if (inviter == null) { throw new ArgumentNullException(nameof(inviter), "Error przy inviter"); }
            if (receiver == null) { throw new ArgumentNullException(nameof(receiver), "Error przy receiver"); }
            if (inviter.Id == receiver.Id) throw new InvalidOperationException("You cannot invite yourself.");

            return new Friend(inviter, receiver);
        }

        public void Accept(int currentUserId)
        {
            if (currentUserId != ReceiverId) { throw new InvalidOperationException("Tylko odbiorca może zaakceptować zaproszenie."); }
            if (AcceptedAt != null || DeclinedAt != null) { throw new InvalidOperationException("Błąd akceptacji zaproszenia"); }
            AcceptedAt = DateTime.UtcNow;
        }

        public void Decline(int currentUserId)
        {
            if (currentUserId != ReceiverId) { throw new InvalidOperationException("Tylko odbiorca może odrzucić zaproszenie."); }
            if (AcceptedAt != null || DeclinedAt != null) { throw new InvalidOperationException("Nie można odrzucić już rozpatrzonego zaproszenia"); }
            DeclinedAt = DateTime.UtcNow;
        }

        public void Remove(int currentUserId)
        {
            if (currentUserId != InviterId && currentUserId != ReceiverId) { throw new InvalidOperationException("Tylko zainteresowani mogą się usunąć."); }
            if (DeletedAt != null) { throw new InvalidOperationException("Nie można usunąć usuniętego zaproszenia"); }
            DeletedAt = DateTime.UtcNow;
        }
    }
}