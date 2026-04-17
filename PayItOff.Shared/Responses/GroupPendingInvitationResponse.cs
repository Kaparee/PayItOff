using PayItOff.Domain.Enums;

namespace PayItOff.Shared.Responses
{

    public class GroupPendingInvitationResponse
    {
        public required int InvitationId { get; set; }
        public required int GroupId { get; set; }
        public required string GroupName { get; set; } = string.Empty;
        public required string GroupAvatarUrl { get; set; } = string.Empty;
        public required GroupMemberRole Role { get; set; }
        public required DateTime InvitedAt { get; set; }
    }
}