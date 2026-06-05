using Microsoft.AspNetCore.Http;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Application.Interfaces
{
    public interface IGroupService
    {
        Task CreateAsync(CreateGroupRequest request, int userId, IFormFile? avatar);
        Task<List<GroupInfoResponse>> GetUserGroupsAsync(int userId);
        Task EditGroupInfoAsync(int userId, EditGroupInfoRequest request, IFormFile? avatar);
        Task DeleteGroupAsync(int userId, DeleteGroupRequest request);
        Task<List<GroupInfoResponse>> GetArchivedUserGroupsAsync(int userId);
        Task<List<ActiveGroupsDisplayResponse>> GetTop4UserActiveGroupsAsync(int userId);
        Task<GroupDetailsResponse> GetGroupDetailsAsync(int groupId, int userId);
        Task<List<AuditLogResponse>> GetGroupHistoryAsync(int groupId, int userId);
    }
}
