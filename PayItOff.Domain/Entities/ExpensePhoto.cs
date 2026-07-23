namespace PayItOff.Domain.Entities;

public class ExpensePhoto
{
    public int Id { get; private set; }
    public int ExpenseId { get; private set; }
    public Expense Expense { get; private set; } = null!;
    public string PhotoUrl { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    protected ExpensePhoto() { }

    public ExpensePhoto(int expenseId, string photoUrl)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
        {
            throw new ArgumentException("PhotoUrl cannot be empty", nameof(photoUrl));
        }

        ExpenseId = expenseId;
        PhotoUrl = photoUrl;
        CreatedAt = DateTime.UtcNow;
    }

    public static ExpensePhoto Create(int expenseId, string photoUrl)
    {
        return new ExpensePhoto(expenseId, photoUrl);
    }
}
