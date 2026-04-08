namespace PayItOff.Shared.Requests
{
    public class CreateGroupRequest
    {
        public required string Name { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
