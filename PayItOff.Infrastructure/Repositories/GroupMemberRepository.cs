using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Persistence;
using PayItOff.Shared.Responses;

namespace PayItOff.Infrastructure.Repositories;

public class GroupMemberRepository : IGroupMemberRepository
{
    private readonly PayItOffDbContext _context;

    public GroupMemberRepository(PayItOffDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(GroupMember groupMember)
    {
        _context.GroupMembers.Add(groupMember);
    }

    public async Task UpdateAsync(GroupMember groupMember)
    {
        _context.GroupMembers.Update(groupMember);
    }
}
