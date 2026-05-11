public class PagedTransactionResponse
{
    public List<UserDebtComponentResponse> Items { get; set; } = new();
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}