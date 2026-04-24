namespace PayItOff.Domain.Entities
{
    public class Expense
    {
        public int Id { get; private set; }
        public Group Group { get; private set; }
        public User Payer { get; private set; }
        public int GroupId { get; private set; }
        public int PayerId { get; private set; }
        public decimal TotalAmount { get; private set; }
        public string Name { get; private set; }
        public DateTime PurchasedAt { get; private set; }
        public string? ReceiptImageUrl { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public DateTime? DeletedAt { get; private set; }
        private readonly List<ExpenseItem> _items = new();
        public IReadOnlyCollection<ExpenseItem> Items => _items.AsReadOnly();
        private readonly List<ExpenseGroup> _groups = new();
        public IReadOnlyCollection<ExpenseGroup> Groups => _groups.AsReadOnly();

        protected Expense() { }

        private Expense(Group group, User payer, string name, string? receiptImageUrl, DateTime purchasedAt)
        {
            if (group == null) { throw new ArgumentNullException(nameof(group), "Error przy group"); }
            if (payer == null) { throw new ArgumentNullException(nameof(payer), "Error przy payer"); }
            if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException(nameof(name)); }

            Group = group;
            Payer = payer;
            GroupId = group.Id;
            PayerId = payer.Id;
            TotalAmount = 0;
            Name = name;
            PurchasedAt = purchasedAt;
            ReceiptImageUrl = string.IsNullOrWhiteSpace(receiptImageUrl) ? null : receiptImageUrl;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public static Expense Create(Group group, User payer, string name, string? receiptImageUrl, DateTime purchasedAt)
        {
            var finalReceiptImageUrl = string.IsNullOrWhiteSpace(receiptImageUrl) ? null : receiptImageUrl;
            return new Expense(group, payer, name, finalReceiptImageUrl, purchasedAt);
        }

        public void AddItem(ExpenseItem item)
        {
            if (item == null) { throw new ArgumentNullException(nameof(item), "Error przy item"); }
            _items.Add(item);
            TotalAmount += item.TotalPrice;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddGroup(ExpenseGroup group)
        {
            if (group == null) { throw new ArgumentNullException(nameof(group), "Error przy group"); }
            _groups.Add(group);
            UpdatedAt = DateTime.UtcNow;
        }

        public void Delete()
        {
            DeletedAt = DateTime.UtcNow;
        }

        public Dictionary<int, decimal> CalculateDebts()
        {
            var debts = new Dictionary<int, decimal>();

            foreach (ExpenseItem item in _items)
            {
                foreach (ExpenseSplit split in item.Splits)
                {
                    AddDebtToDictionary(debts, split);
                }
            }

            foreach (ExpenseGroup group in _groups)
            {
                foreach (var item in group.Items)
                {
                    foreach (ExpenseSplit split in item.Splits)
                    {
                        AddDebtToDictionary(debts, split);
                    }
                }
            }

            return debts;
        }


        private void AddDebtToDictionary(Dictionary<int, decimal> debts, ExpenseSplit split)
        {
            if (split.UserId != PayerId)
            {
                if (debts.ContainsKey(split.UserId))
                {
                    debts[split.UserId] += split.OwedAmount;
                }
                else
                {
                    debts.Add(split.UserId, split.OwedAmount);
                }
            }
        }
        public void RecalculateTotal()
        {
            decimal itemsSum = _items.Sum(item => item.TotalPrice);
            decimal groupsSum = _groups.Sum(group => group.TotalAmount);

            TotalAmount = itemsSum + groupsSum;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}