public class CreateSettlementRequest
{
    public required int ReceiverId { get; set; }
    public required int GroupId { get; set; }
    public required decimal Amount { get; set; }
    public string? Description { get; set; }
}