using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.DomainServices;
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

    public async Task<GroupDebt?> GetDebtAsync(int groupId, int debtorId, int creditorId)
    {
        return await _context.GroupDebts
            .Where(x => x.GroupId == groupId && x.DebtorId == debtorId && x.CreditorId == creditorId)
            .FirstOrDefaultAsync();
    }

    public async Task ApplyDebtChangeAsync(Group group, User debtor, User creditor, decimal amountChange)
    {
        var existingDebt = await GetDebtAsync(group.Id, debtor.Id, creditor.Id);

        if (existingDebt != null)
        {
            existingDebt.ChangeAmount(amountChange);
            _context.GroupDebts.Update(existingDebt);
        }
        else if (amountChange > 0)
        {
            var newDebt = GroupDebt.Create(group, debtor, creditor, amountChange);

            await _context.GroupDebts.AddAsync(newDebt);
        }
    }
}
