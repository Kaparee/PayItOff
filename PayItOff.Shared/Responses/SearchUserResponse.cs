namespace PayItOff.Shared.Responses
{
    public class SearchUserResponse
    {
        public required int Id { get; set; }
        public required string AvatarUrl { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public required string Nickname { get; set; }
    }
}
