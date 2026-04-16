using PayItOff.Domain.Entities;

namespace PayItOff.Domain.Interfaces;

public interface IGroupMemberRepository
{
    Task AddAsync(GroupMember groupMember);
    Task UpdateAsync(GroupMember groupMember);
    Task<bool> IsUserOwnerOrAdmin(int userId, int groupId);
    Task<bool> IsUserOwner(int userId, int groupId);
}
