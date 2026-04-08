using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Persistence;
using PayItOff.Shared.Responses;

namespace PayItOff.Infrastructure.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly PayItOffDbContext _context;

    public GroupRepository(PayItOffDbContext context)
    {
        _context = context;
    }

    public async Task<List<Group?>> GetUserGroupsAsync(int userId)
    {
        return await _context.GroupMembers
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsFavorite)
            .Select(x => x.Group)
            .Where(x => x!.DeletedAt == null)
            .ToListAsync();
    }

    public async Task AddAsync(Group group)
    {
        _context.Groups.Add(group);
    }

    public async Task UpdateAsync(Group group)
    {
        _context.Groups.Update(group);
        await _context.SaveChangesAsync();
    }

    public async Task<Group?> GetGroupInfoByIdAsync(int groupId)
    {
        return await _context.Groups
            .Where(x => x.Id == groupId && x.DeletedAt == null)
            .FirstOrDefaultAsync();
    }
}
