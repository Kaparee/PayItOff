using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Application.Interfaces
{
    public interface IGroupMemberService
    {
        Task InviteUserAsync(GroupInviteUserRequest request);
        Task AcceptInviteAsync(int userId, int invitationId);
        Task DeclineInviteAsync(int userId, int invitationId);
        Task UpdateRoleAsync(int userId, GroupMemberUpdateRequest request);
        Task SetGroupAsFavoriteAsync(int userId, int groupId);
        Task LeaveGroupAsync(int userId, int groupId);
        Task KickUserFromGroupAsync(int userId, int groupId, int targetUserId);
        Task<List<GroupPendingInvitationResponse>> GetPendingInvitationsAsync(int userId);
        Task<List<GroupMemberResponse>> GetAllActiveGroupMembersAsync(int groupId);
    }
}
