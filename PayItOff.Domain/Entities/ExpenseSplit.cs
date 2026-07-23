namespace PayItOff.Domain.Entities
{
    public class ExpenseSplit
    {
        public int Id { get; private set; }
        public ExpenseItem ExpenseItem { get; private set; } = null!;
        public int ExpenseItemId { get; private set; }
        public User User { get; private set; } = null!;
        public int UserId { get; private set; }
        public decimal OwedAmount { get; private set; }
        public decimal PaidAmount { get; private set; }
        public bool IsSettled => (OwedAmount - PaidAmount) <= 0;

        protected ExpenseSplit() { }

        private ExpenseSplit(ExpenseItem expenseItem, User user, decimal owedAmount)
        {
            if (owedAmount <= 0) { throw new InvalidOperationException("Dług musi być większy od zera"); }

            ExpenseItem = expenseItem;
            ExpenseItemId = expenseItem.Id;
            User = user ?? throw new ArgumentNullException(nameof(user), "Nie może być null");
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

        public decimal ApplyPayment(decimal amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            decimal remainingOwed = OwedAmount - PaidAmount;

            if (amount >= remainingOwed)
            {
                decimal leftover = amount - remainingOwed;
                PaidAmount = OwedAmount;
                return leftover;
            }
            else
            {
                PaidAmount += amount;
                return 0;
            }
        }
    }
}