using PayItOff.Domain.Entities;

namespace PayItOff.Domain.Interfaces;

public interface IGroupMemberRepository
{
    Task AddAsync(GroupMember groupMember);
    Task UpdateAsync(GroupMember groupMember);

}
