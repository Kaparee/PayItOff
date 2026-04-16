using PayItOff.Domain.Enums;

namespace PayItOff.Shared.Responses
{
    public class GroupMemberResponse
    {
        public required string AvatarUrl { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public required string Email { get; set; }
        public required string Nickname { get; set; }
        public required GroupMemberRole Role { get; set; }
    }
}
