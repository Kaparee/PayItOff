namespace PayItOff.Shared.Responses
{
    public class GroupInfoResponse
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required string AvatarUrl { get; set; }
        public required DateTime UpdatedAt { get; set; }
        public required bool IsFavorite { get; set; }
        public required decimal Income { get; set; }
        public required decimal Expense { get; set; }
        public required decimal Balance { get; set; }
    }
}
