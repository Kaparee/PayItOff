namespace PayItOff.Shared.Requests
{
    public class UserExpenseHistoryRequest
    {
        public int? TargetId { get; set; }
        public required string Type { get; set; }
        public required int Page { get; set; }
    }
}
