namespace PayItOff.Domain.Entities
{
    public class GroupDebt
    {
        public int Id { get; private set; }
        public Group Group { get; private set; }
        public User Debtor { get; private set; }
        public User Creditor { get; private set; }
        public int GroupId { get; private set; }
        public int DebtorId { get; private set; }
        public int CreditorId { get; private set; }
        public decimal Amount { get; private set; }

        protected GroupDebt() { }

        private GroupDebt(Group group, User debtor, User creditor, decimal amount)
        {
            if (group == null) { throw new ArgumentNullException(nameof(group), "Error przy group"); }
            if (debtor == null) { throw new ArgumentNullException(nameof(debtor), "Error przy debtor"); }
            if (creditor == null) { throw new ArgumentNullException(nameof(creditor), "Error przy creditor"); }
            if (debtor.Id == creditor.Id) { throw new InvalidOperationException("Nie można być winnym pieniędzy samemu sobie"); }
            if (amount < 0) { throw new InvalidOperationException("Nie można mieć długu mniejszego od 0"); }

            Group = group;
            Debtor = debtor;
            Creditor = creditor;
            GroupId = group.Id;
            DebtorId = debtor.Id;
            CreditorId = creditor.Id;
            Amount = amount;
        }

        public static GroupDebt Create(Group group, User debtor, User creditor, decimal amount)
        {
            return new GroupDebt(group, debtor, creditor, amount);
        }

        public void IncreaseAmount(decimal amountToAdd)
        {
            if (amountToAdd < 0) { throw new InvalidOperationException("Nie można mieć długu mniejszego od 0"); }
            Amount += amountToAdd;
        }

        public void DecreaseAmount(decimal amountToSubtract)
        {
            if (amountToSubtract < 0) { throw new InvalidOperationException("Nie można odjąć od długu mniej niż 0"); }
            if (amountToSubtract > Amount) { throw new InvalidOperationException("Nie można odjąć od długu więcej niż on wynosi"); }
            Amount -= amountToSubtract;
        }
    }
}