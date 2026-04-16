using PayItOff.Domain.Enums;

namespace PayItOff.Shared.Requests
{
    public class GroupMemberUpdateRequest
    {
        public required int TargetUserId { get; set; }
        public required int GroupId { get; set; }
        public required GroupMemberRole NewRole { get; set; }
    }
}
