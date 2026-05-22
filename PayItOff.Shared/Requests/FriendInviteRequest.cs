namespace PayItOff.Shared.Requests
{
    public class FriendInviteRequest
    {
        public int? TargetUserId { get; set; }
        public string? Nickname { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

}
