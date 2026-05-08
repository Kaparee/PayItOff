namespace PayItOff.Shared.Responses
{
    public class ActiveGroupsDisplayResponse
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required string AvatarUrl { get; set; }
        public required string LastUpdate { get; set; }
    }
}
