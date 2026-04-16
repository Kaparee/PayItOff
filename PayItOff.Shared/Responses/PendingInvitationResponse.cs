using PayItOff.Domain.Enums;

namespace PayItOff.Shared.Responses;

public class PendingInvitationResponse
{
    public int InvitationId { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string GroupAvatarUrl { get; set; } = string.Empty;
    public GroupMemberRole Role { get; set; }
    public DateTime InvitedAt { get; set; }
}