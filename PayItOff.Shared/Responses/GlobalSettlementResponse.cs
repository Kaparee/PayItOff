namespace PayItOff.Shared.Responses;

public class GlobalSettlementResponse
{
    public List<GlobalDebtSummaryResponse> Items { get; set; } = [];
    public decimal TotalAmount { get; set; }
}