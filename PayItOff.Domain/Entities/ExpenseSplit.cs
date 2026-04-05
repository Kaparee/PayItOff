namespace PayItOff.Domain.Entities
{
    public class ExpenseSplit
    {
        public int Id { get; private set; }
        public ExpenseItem? ExpenseItem { get; private set; }
        public ExpenseGroup? ExpenseGroup { get; private set; }
        public int? ExpenseItemId { get; private set; }
        public int? ExpenseGroupId { get; private set; }
        public User User { get; private set; }
        public int UserId { get; private set; }
        public decimal OwedAmount { get; private set; }

        protected ExpenseSplit() { }

        private ExpenseSplit(ExpenseItem? expenseItem, ExpenseGroup? expenseGroup, User user, decimal owedAmount)
        {
            if (expenseItem == null && expenseGroup == null) { throw new InvalidOperationException("Obie kolumny nie mogą być null"); }
            if (expenseItem != null && expenseGroup != null) { throw new InvalidOperationException("Obie kolumny nie mogą być przypisane"); }
            if (user == null) { throw new ArgumentNullException(nameof(user), "Nie może być null"); }
            if (owedAmount <= 0) { throw new InvalidOperationException("Dług musi być większy od zera"); }

            ExpenseItem = expenseItem;
            ExpenseGroup = expenseGroup;
            ExpenseItemId = (expenseItem == null) ? null : expenseItem.Id;
            ExpenseGroupId = (expenseGroup == null) ? null : expenseGroup.Id;
            User = user;
            UserId = user.Id;
            OwedAmount = owedAmount; 
        }

        public static ExpenseSplit Create(ExpenseItem? expenseItem, ExpenseGroup? expenseGroup, User user, decimal owedAmount)
        {
            return new ExpenseSplit(expenseItem, expenseGroup, user, owedAmount);
        }

        public void UpdateAmount(decimal newAmount)
        {
            if (newAmount <= 0) { throw new InvalidOperationException("Dług musi być większy od zera"); }
            OwedAmount = newAmount;
        }
    }
}