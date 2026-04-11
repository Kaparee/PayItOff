using PayItOff.Domain.Enums;

namespace PayItOff.Shared.Requests
{
    public class GroupInviteUserRequest
    {
        public required int UserId { get; set; }
        public required int GroupId { get; set; }
        public required GroupMemberStatus Status { get; set; }
    }
}
