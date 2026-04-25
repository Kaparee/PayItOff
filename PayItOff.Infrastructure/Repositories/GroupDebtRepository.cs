using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Pkcs;
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

    public async Task<Dictionary<int, (decimal Income, decimal Expense)>> GetUserGroupBalancesAsync(int userId)
    {
        var balances = await _context.GroupDebts
            .Where(x => x.DebtorId == userId || x.CreditorId == userId)
            .GroupBy(x => x.GroupId)
            .Select(g => new
            {
                GroupId = g.Key,
                Income = g.Where(x => x.CreditorId == userId).Sum(x => x.Amount),
                Expense = g.Where(x => x.DebtorId == userId).Sum(x => x.Amount)
            })
            .ToDictionaryAsync(x => x.GroupId, x => (x.Income, x.Expense));

        return balances;
    }

    public async Task<List<(int UserId, string Name, string Surname, string? AvatarUrl, decimal Amount)>> GetUserTotalIncomesAsync(int userId)
    {
        var incomes = await _context.GroupDebts
            .Where(x => x.CreditorId == userId)
            .GroupBy(x => new { x.Debtor!.Id, x.Debtor.Name, x.Debtor.Surname, x.Debtor.AvatarUrl })
            .Select(g => new
            {
                g.Key.Id,
                g.Key.Name,
                g.Key.Surname,
                g.Key.AvatarUrl,
                TotalAmount = g.Sum(x => x.Amount)
            })
            .ToListAsync();

        return incomes.ConvertAll(x => (x.Id, x.Name, x.Surname, x.AvatarUrl, x.TotalAmount));
    }

    public async Task<List<(int UserId, string Name, string Surname, string? AvatarUrl, decimal Amount)>> GetUserTotalExpensesAsync(int userId)
    {
        var expenses = await _context.GroupDebts
            .Where(x => x.DebtorId == userId)
            .GroupBy(x => new { x.Creditor!.Id, x.Creditor.Name, x.Creditor.Surname, x.Creditor.AvatarUrl })
            .Select(g => new
            {
                g.Key.Id,
                g.Key.Name,
                g.Key.Surname,
                g.Key.AvatarUrl,
                TotalAmount = g.Sum(x => x.Amount)
            })
            .ToListAsync();

        return expenses.ConvertAll(x => (x.Id, x.Name, x.Surname, x.AvatarUrl, x.TotalAmount));
    }
}
