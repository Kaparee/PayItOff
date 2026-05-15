namespace PayItOff.Shared.Requests
{
    public class CreateSettlementRequest
    {
        public int GroupId { get; set; }
        public int ReceiverId { get; set; }
        public decimal Amount { get; set; }
    }
}