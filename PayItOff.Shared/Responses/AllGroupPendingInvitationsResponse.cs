namespace PayItOff.Shared.Responses
{

    public class AllGroupPendingInvitationResponse
    {
        public required int UserId { get; set; }
        public required string AvatarUrl { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public required string Nickname { get; set; }
        public string FullName => $"{Name} {Surname}";
    }
}