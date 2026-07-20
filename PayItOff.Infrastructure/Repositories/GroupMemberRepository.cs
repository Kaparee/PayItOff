using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Persistence;
using System.Diagnostics.CodeAnalysis;

namespace PayItOff.Infrastructure.Repositories;

public class GroupMemberRepository : IGroupMemberRepository
{
    private readonly PayItOffDbContext _context;

    public GroupMemberRepository(PayItOffDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(GroupMember groupMember)
    {
        _context.GroupMembers.Add(groupMember);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(GroupMember groupMember)
    {
        _context.GroupMembers.Update(groupMember);
        return Task.CompletedTask;
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
            .Where(x => x.GroupId == groupId && x.UserId == userId && x.Status == GroupMemberStatus.Accepted && x.Role == GroupMemberRole.Owner)
            .AnyAsync();
    }

    public async Task<GroupMember?> GetActiveInvitationById(int invitationId)
    {
        return await _context.GroupMembers
            .Include(x => x.Group)
            .Where(x => x.Id == invitationId && x.Status == GroupMemberStatus.Pending)
            .FirstOrDefaultAsync();
    }

    public async Task<GroupMember?> GetMemberAsync(int groupId, int userId)
    {
        return await _context.GroupMembers
            .Include(x => x.Group)
            .Include(x => x.User)
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

    public async Task<bool> IsInviteAlreadyExistsAsync(int groupId, int userId)
    {
        return await _context.GroupMembers
            .Where(x => x.GroupId == groupId && x.UserId == userId && x.Status == GroupMemberStatus.Pending)
            .AnyAsync();
    }

    public async Task<GroupMember?> GetUserGroupInvitationAsync(int groupId, int userId)
    {
        return await _context.GroupMembers
            .Where(x => x.GroupId == groupId && x.UserId == userId && x.Status == GroupMemberStatus.Pending)
            .FirstOrDefaultAsync();
    }

    public async Task<List<GroupMember>> GetAllGroupPendingInvitationsAsync(int groupId)
    {
        return await _context.GroupMembers
            .Include(x => x.User)
            .Where(x => x.GroupId == groupId && x.Status == GroupMemberStatus.Pending)
            .ToListAsync();
    }
}
