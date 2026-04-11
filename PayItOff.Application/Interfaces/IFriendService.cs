using Microsoft.AspNetCore.Http;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Runtime.CompilerServices;

namespace PayItOff.Application.Interfaces
{
    public interface IFriendService
    {
        Task<List<FriendListResponse>> GetUserFriendListAsync(int userId);
        Task InviteAsync(int userId, FriendInviteRequest request);
        Task AcceptInviteAsync(int userId, UpdateInviteRequest request);
        Task DeclineInviteAsync(int userId, UpdateInviteRequest request);
        Task RemoveFriendAsync(int userId, UpdateInviteRequest request);
    }
}
