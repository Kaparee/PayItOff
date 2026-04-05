namespace PayItOff.Domain.Entities
{
    public class ExpenseItem
    {
        public int Id { get; private set; }
        public Expense Expense { get; private set; }
        public ExpenseGroup? ExpenseGroup { get; private set; }
        public int ExpenseId { get; private set; }
        public int? ExpenseGroupId { get; private set; }
        public string Name { get; private set; }
        public string Category { get; private set; }
        public decimal Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal TotalPrice { get; private set; }
        private readonly List<ExpenseSplit> _splits = new();
        public IReadOnlyCollection<ExpenseSplit> Splits => _splits.AsReadOnly();

        protected ExpenseItem() { }

        private ExpenseItem(Expense expense, ExpenseGroup? expenseGroup, string name, string category, decimal quantity, decimal unitPrice)
        {
            if(expense == null) { throw new ArgumentNullException(nameof(expense), "Nie może być null"); }
            if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException("Name cannot be empty", nameof(name)); }
            if (string.IsNullOrWhiteSpace(category)) { throw new ArgumentException("Category cannot be empty", nameof(category)); }
            if (quantity <= 0) { throw new InvalidOperationException("Nie można kupić mniej niż 0 rzeczy"); }
            if (unitPrice < 0) { throw new InvalidOperationException("Nie można kupić za mniej niż 0"); }

            Expense = expense;
            ExpenseId = expense.Id;
            ExpenseGroup = expenseGroup;
            ExpenseGroupId = (expenseGroup == null) ? null : expenseGroup.Id;
            Name = name;
            Category = category;
            Quantity = quantity;
            UnitPrice = unitPrice;
            TotalPrice = UnitPrice * Quantity;
        }

        public static ExpenseItem Create(Expense expense, ExpenseGroup? expenseGroup, string name, string category, decimal quantity, decimal unitPrice)
        {
            return new ExpenseItem(expense, expenseGroup, name, category, quantity, unitPrice);
        }

        public void UpdateQuantity(decimal newQuantity)
        {
            if (newQuantity <= 0) { throw new InvalidOperationException("Nie można kupić mniej niż 0 rzeczy"); }
            Quantity = newQuantity;
            TotalPrice = UnitPrice * newQuantity;
        }

        public void UpdateUnitPrice(decimal newUnitPrice)
        {
            if (newUnitPrice < 0) { throw new InvalidOperationException("Nie można kupić za mniej niż 0"); }
            UnitPrice = newUnitPrice;
            TotalPrice = newUnitPrice * Quantity;
        }

        public void AddSplit(ExpenseSplit split)
        {
            if (split == null) { throw new ArgumentNullException(nameof(split), "Nie może być null"); }
            _splits.Add(split);
        }
    }
}