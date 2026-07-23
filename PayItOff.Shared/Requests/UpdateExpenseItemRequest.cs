namespace PayItOff.Shared.Requests;

public class UpdateExpenseItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public List<ExpenseSplitDto> Splits { get; set; } = [];
}

public class ExpenseSplitDto
{
    public int UserId { get; set; }
    public decimal Amount { get; set; }
}
