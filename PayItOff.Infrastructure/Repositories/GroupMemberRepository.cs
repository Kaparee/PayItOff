using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;
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

    public async Task<bool> IsUserOwnerOrAdmin(int userId, int groupId)
    {
        return await _context.GroupMembers
            .Where(x => x.GroupId == groupId && x.UserId == userId && x.Status == GroupMemberStatus.Accepted && x.Role != GroupMemberRole.Member)
            .AnyAsync();
    }

    public async Task<bool> IsUserOwner(int userId, int groupId)
    {
        return await _context.GroupMembers
            .Where(x => x.GroupId == groupId && x.UserId == userId && x.Status == GroupMemberStatus.Accepted&& x.Role == GroupMemberRole.Owner)
            .AnyAsync();
    }

    public async Task<GroupMember?> GetActiveInvitationById(int invitationId)
    {
        return await _context.GroupMembers
            .Where(x => x.Id == invitationId && x.Status == GroupMemberStatus.Pending)
            .FirstOrDefaultAsync();
    }

    public async Task<GroupMember?> GetMemberAsync(int groupId, int userId)
    {
        return await _context.GroupMembers
            .Where(x => x.GroupId == groupId && x.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<GroupMember>> GetPendingInvitationsByUserIdAsync(int userId)
    {
        return await _context.GroupMembers
        .Include(x => x.Group)
        .Where(x => x.UserId == userId && x.Status == GroupMemberStatus.Pending)
        .ToListAsync();
    }

    public async Task<List<GroupMember>> GetAllActiveGroupMembersAsync(int groupId)
    {
        return await _context.GroupMembers
            .Include(x => x.Group)
            .Include(x => x.User)
            .Where(x => x.GroupId == groupId && x.Status == GroupMemberStatus.Accepted)
            .ToListAsync();
    }

}
