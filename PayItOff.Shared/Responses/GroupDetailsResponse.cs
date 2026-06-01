namespace PayItOff.Shared.Responses;

public class GroupDetailsResponse
{
    public int GroupId { get; set; }
    public string GroupName { get; set; }
    public string UserRole { get; set; }
    public List<GroupMemberBalanceDto> Members { get; set; } = new();
    public List<ExpenseSummaryDto> Expenses { get; set; } = new();
    public bool IsArchived { get; set; }
}

public class GroupMemberDebtLineDto
{
    public string CounterpartyName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool MemberOwes { get; set; }
}

public class MemberExpenseLineDto
{

    public string ExpenseName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public decimal OwedAmount { get; set; }
    public DateTime Date { get; set; }
}

public class GroupMemberBalanceDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public decimal OverallBalance { get; set; }
    public bool IsCurrentUser { get; set; }
    public bool IsCreditorToCurrentUser { get; set; }
    public List<GroupMemberDebtLineDto> Lines { get; set; } = new();
    public decimal LinesTotal { get; set; }
    public List<MemberExpenseLineDto> Expenses { get; set; } = new();
    public decimal ExpensesTotal { get; set; }
}

public class ExpenseSummaryDto
{
    public int ExpenseId { get; set; }
    public int ItemId { get; set; }
    public string Title { get; set; }
    public string PayerName { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime Date { get; set; }
}