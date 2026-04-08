using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Application.Interfaces
{
    public interface IGroupService
    {
        Task CreateAsync(CreateGroupRequest request, int userId);
        Task<List<GroupInfoResponse>> GetUserGroupsAsync(int userId);
    }
}
