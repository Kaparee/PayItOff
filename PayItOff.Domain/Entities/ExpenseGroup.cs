namespace PayItOff.Domain.Entities
{
    public class ExpenseGroup
    {
        public int Id { get; private set; }
        public Expense Expense { get; private set; }
        public int ExpenseId { get; private set; }
        public string Name { get; private set; }
        public decimal TotalAmount { get; private set; }
        private readonly List<ExpenseSplit> _splits = new();
        public IReadOnlyCollection<ExpenseSplit> Splits => _splits.AsReadOnly();

        protected ExpenseGroup() { }

        private ExpenseGroup(Expense expense, string name, decimal totalAmount)
        {
            if(expense == null) { throw new ArgumentNullException(nameof(expense), "Nie może być null"); }
            if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException("Name cannot be empty", nameof(name)); }
            if(totalAmount < 0 ) { throw new InvalidOperationException("Nie można mieć wydatku mniejszego od 0"); }

            Expense = expense;
            ExpenseId = expense.Id;
            Name = name;
            TotalAmount = totalAmount;
        }

        public static ExpenseGroup Create(Expense expense, string name, decimal totalAmount)
        {
            return new ExpenseGroup(expense, name, totalAmount);
        }

        public void Edit(string newName)
        {
            if(string.IsNullOrWhiteSpace(newName)) { throw new ArgumentException("Name cannot be empty", nameof(newName)); }
            Name = newName;
        }

        public void UpdateAmount(decimal newAmount)
        {
            if (newAmount < 0) { throw new InvalidOperationException("Nie można mieć wydatku mniejszego od 0"); }
            TotalAmount = newAmount;
        }

        public void AddSplit(ExpenseSplit split)
        {
            if(split == null) { throw new ArgumentNullException(nameof(split), "Nie może być null"); }
            _splits.Add(split);
        }
    }
}