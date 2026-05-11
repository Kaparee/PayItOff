public class UserDebtComponentResponse
{
    public int ExpenseId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public bool AmIDebtor { get; set; }
    public required List<string> Categories { get; set; }
    public required string GroupName { get; set; }
    public required string OtherName { get; set; }
    public required string OtherSurname { get; set; }
    public required string OtherAvatarUrl { get; set; }
}