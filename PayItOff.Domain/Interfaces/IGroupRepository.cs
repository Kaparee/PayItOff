using PayItOff.Domain.Entities;

namespace PayItOff.Domain.Interfaces;

public interface IGroupRepository
{
    Task<List<GroupMember>> GetUserGroupsAsync(int userId);
    Task AddAsync(Group group);
    Task UpdateAsync(Group group);
    Task<Group?> GetGroupInfoByIdAsync(int groupId);
}
