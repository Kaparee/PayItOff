using Microsoft.AspNetCore.Http;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Application.Interfaces
{
    public interface IGroupMemberService
    {
        Task InviteUserAsync(GroupInviteUserRequest request);
        Task AcceptInviteAsync(int userId, int invitationId);
        Task DeclineInviteAsync(int userId, int invitationId);
        Task UpdateRoleAsync()
    }
}
