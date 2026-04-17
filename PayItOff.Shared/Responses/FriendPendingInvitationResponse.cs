namespace PayItOff.Shared.Responses
{

    public class FriendPendingInvitationResponse
    {
        public required int InvitationId { get; set; }
        public required int FriendId { get; set; }
        public required string AvatarUrl { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public required string Nickname { get; set; }
        public required DateTime SentAt { get; set; }
        public bool IsIncoming { get; set; }
    }
}