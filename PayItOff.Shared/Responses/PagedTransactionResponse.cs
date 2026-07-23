namespace PayItOff.Shared.Responses;

public class PagedTransactionResponse
{
    public List<UserDebtComponentResponse> Items { get; set; } = [];
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }


    public int TotalTransactionsCount { get; set; }
    public int TotalIncomesCount { get; set; }
    public int TotalExpensesCount { get; set; }
}