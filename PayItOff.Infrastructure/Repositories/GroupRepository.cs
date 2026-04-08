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
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.IsFavorite)
            .Select(m => m.Group)
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
}
