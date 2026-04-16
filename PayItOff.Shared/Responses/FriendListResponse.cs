namespace PayItOff.Shared.Responses
{
    public class FriendListResponse
    {
        public required int FriendId { get; set; }
        public required int InviteId { get; set; }
        public required string AvatarUrl { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public required string Nickname { get; set; }
        public string? PhoneNumber { get; set; }
        public required List<string> SharedGroupAvatars { get; set; }
        public required decimal Balance { get; set; }
    }
}
