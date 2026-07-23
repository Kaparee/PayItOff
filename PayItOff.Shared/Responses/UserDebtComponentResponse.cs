namespace PayItOff.Shared.Responses;

public class UserDebtComponentResponse
{
    public int ExpenseId { get; set; }
    public int GroupId { get; set; }
    public DateTime Date { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool AmIDebtor { get; set; }
    public int OtherUserId { get; set; }
    public string OtherName { get; set; } = string.Empty;
    public string OtherSurname { get; set; } = string.Empty;
    public string? OtherAvatarUrl { get; set; }
    public List<string> Categories { get; set; } = [];
    public bool IsSettlement { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TransferReference { get; set; } = string.Empty;
    public bool CanSendDebtReminder { get; set; }
}