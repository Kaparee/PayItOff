namespace PayItOff.Shared.Responses
{
    public class GroupInfoResponse
    {
        public required string Name { get; set; }
        public required string AvatarUrl { get; set; }
        public required DateTime UpdatedAt { get; set; }
    }
}
