using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Persistence;

namespace PayItOff.Infrastructure.Repositories;

public class GroupDebtRepository : IGroupDebtRepository
{
    private readonly PayItOffDbContext _context;

    public GroupDebtRepository(PayItOffDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(GroupDebt groupDebt)
    {
        _context.GroupDebts.Add(groupDebt);
    }

    public async Task UpdateAsync(GroupDebt groupDebt)
    {
        _context.GroupDebts.Update(groupDebt);
    }

    public async Task<bool> HasActiveGroupDebt(int groupId)
    {
        return await _context.GroupDebts
            .Where(x => x.GroupId == groupId && x.Amount > 0)
            .AnyAsync();
    }
}
