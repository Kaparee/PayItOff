using PayItOff.Domain.Entities;

namespace PayItOff.Domain.Interfaces;

public interface IGroupMemberRepository
{
    Task AddAsync(GroupMember groupMember);
    Task UpdateAsync(GroupMember groupMember);
    Task<bool> IsUserOwnerOrAdmin(int userId, int groupId);
    Task<bool> IsUserOwner(int userId, int groupId);
    Task<GroupMember?> GetActiveInvitationById(int invitationId);
    Task<GroupMember?> GetMemberAsync(int groupId, int userId);
    Task<List<GroupMember>> GetPendingInvitationsByUserIdAsync(int userId);
    Task<List<GroupMember>> GetAllActiveGroupMembersAsync(int groupId);
}
