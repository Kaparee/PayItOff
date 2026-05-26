namespace PayItOff.Shared.Responses;

public class ExpenseDetailsResponse
{
    public int ExpenseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime Date { get; set; }
    
    public string PayerName { get; set; } = string.Empty;
    public string PayerAvatarUrl { get; set; } = string.Empty;
    
    public List<ExpenseParticipantDto> Participants { get; set; } = new();
}

public class ExpenseParticipantDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public decimal OwedAmount { get; set; }
}
