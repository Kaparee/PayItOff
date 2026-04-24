namespace PayItOff.Domain.Entities
{
    public class ExpenseSplit
    {
        public int Id { get; private set; }
        public ExpenseItem ExpenseItem { get; private set; }
        public int ExpenseItemId { get; private set; }
        public User User { get; private set; }
        public int UserId { get; private set; }
        public decimal OwedAmount { get; private set; }

        protected ExpenseSplit() { }

        private ExpenseSplit(ExpenseItem expenseItem, User user, decimal owedAmount)
        {
            if (user == null) { throw new ArgumentNullException(nameof(user), "Nie może być null"); }
            if (owedAmount <= 0) { throw new InvalidOperationException("Dług musi być większy od zera"); }

            ExpenseItem = expenseItem;
            ExpenseItemId = expenseItem.Id;
            User = user;
            UserId = user.Id;
            OwedAmount = owedAmount;
        }

        public static ExpenseSplit Create(ExpenseItem expenseItem, User user, decimal owedAmount)
        {
            return new ExpenseSplit(expenseItem, user, owedAmount);
        }

        public void UpdateAmount(decimal newAmount)
        {
            if (newAmount <= 0) { throw new InvalidOperationException("Dług musi być większy od zera"); }
            OwedAmount = newAmount;
        }
    }
}