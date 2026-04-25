namespace PayItOff.Shared.Responses
{
    public class GlobalDebtSummaryResponse
    {
        public int UserId { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public required string AvatarUrl { get; set; }
        public decimal Amount { get; set; }
    }
}