using PayItOff.Domain.Enums;

namespace PayItOff.Domain.Entities
{
    public class Settlement
    {
        public int Id { get; private set; }
        public string TransferReference { get; private set; }
        public User Sender { get; private set; }
        public User Receiver { get; private set; }
        public Group Group { get; private set; }
        public int SenderId { get; private set; }
        public int ReceiverId { get; private set; }
        public int GroupId { get; private set; }
        public decimal Amount { get; private set; }
        public string Description { get; private set; }
        public SettlementStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public DateTime? DeletedAt { get; private set; }
        public DateTime? RemindedAt { get; private set; }

        protected Settlement() { }

        private Settlement(string transferReference, User sender, User receiver, Group group, decimal amount, string description, SettlementStatus status)
        {
            if (string.IsNullOrWhiteSpace(transferReference)) { throw new ArgumentException("Invalid Transfer Reference."); }
            if (sender == null) { throw new ArgumentNullException(nameof(sender), "Error przy sender"); }
            if (receiver == null) { throw new ArgumentNullException(nameof(receiver), "Error przy receiver"); }
            if (group == null) { throw new ArgumentNullException(nameof(group), "Error przy group"); }
            if (amount <= 0) { throw new InvalidOperationException("Nie można mieć długu mniejszego od 0"); }
            if (string.IsNullOrWhiteSpace(description)) { throw new ArgumentException("Invalid Description."); }
            if (sender.Id == receiver.Id) { throw new InvalidOperationException("Nie można być winnym pieniędzy samemu sobie"); }

            TransferReference = transferReference;
            Sender = sender;
            Receiver = receiver;
            Group = group;
            SenderId = sender.Id;
            GroupId = group.Id;
            ReceiverId = receiver.Id;
            Amount = amount;
            Description = description;
            Status = status;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public static Settlement Create(User sender, User receiver, Group group, decimal amount, string description)
        {
            var transferReference = group.Name.Trim() + GenerateTransferReference(10);
            return new Settlement(transferReference, sender, receiver, group, amount, description, SettlementStatus.Pending);
        }

        private static string GenerateTransferReference(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public void Confirm()
        {
            if (Status != SettlementStatus.Pending) { throw new InvalidOperationException("Można procesować tylko oczekujące rozliczenia."); }
            Status = SettlementStatus.Confirmed;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reject()
        {
            if (Status != SettlementStatus.Pending) { throw new InvalidOperationException("Można procesować tylko oczekujące rozliczenia."); }
            Status = SettlementStatus.Rejected;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Remind()
        {
            RemindedAt = DateTime.UtcNow;
        }
    }
}