using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Persistence;

namespace PayItOff.Infrastructure.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly PayItOffDbContext _context;

    public GroupRepository(PayItOffDbContext context)
    {
        _context = context;
    }

    public async Task<List<GroupMember>> GetUserGroupsAsync(int userId)
    {
        return await _context.GroupMembers
            .Include(x => x.Group)
            .Where(x => x.UserId == userId && x.Status == GroupMemberStatus.Accepted)
            .Where(x => x.Group!.DeletedAt == null)
            .OrderByDescending(x => x.IsFavorite)
            .ThenByDescending(x => x.Group!.UpdatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Group group)
    {
        _context.Groups.Add(group);
    }

    public async Task UpdateAsync(Group group)
    {
        _context.Groups.Update(group);
    }

    public async Task<Group?> GetGroupInfoByIdAsync(int groupId)
    {
        return await _context.Groups
            .Where(x => x.Id == groupId && x.DeletedAt == null)
            .FirstOrDefaultAsync();
    }

    public async Task<Group?> GetGroupInfoIncludingArchivedByIdAsync(int groupId)
    {
        return await _context.Groups
            .Where(x => x.Id == groupId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<GroupMember>> GetTop4UserActiveGroupsAsync(int userId)
    {
        return await _context.GroupMembers
            .Include(x => x.Group)
            .Where(x => x.UserId == userId && x.Status == GroupMemberStatus.Accepted)
            .Where(x => x.Group!.DeletedAt == null)
            .OrderByDescending(x => x.Group!.UpdatedAt)
            .Take(4)
            .ToListAsync();
    }

    public async Task<List<GroupMember>> GetArchivedUserGroupsAsync(int userId)
    {
        return await _context.GroupMembers
            .Include(x => x.Group)
            .Where(x => x.UserId == userId && x.Status == GroupMemberStatus.Accepted)
            .Where(x => x.Group!.DeletedAt != null)
            .OrderByDescending(x => x.Group!.DeletedAt)
            .ToListAsync();
    }
}
