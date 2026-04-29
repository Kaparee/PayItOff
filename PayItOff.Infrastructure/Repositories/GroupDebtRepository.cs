using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Pkcs;
using PayItOff.Domain.DomainServices;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Persistence;
using PayItOff.Shared.Responses;

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

    public async Task<List<(int UserId, string Name, string Surname, string? AvatarUrl, List<string> Categories, DateTime Date, decimal Amount)>> GetUserTotalIncomesAsync(int userId)
    {
        var groupedIncomes = await _context.ExpenseSplits
            .Where(s => s.ExpenseItem.Expense.PayerId == userId && s.UserId != userId)
            .GroupBy(s => new
            {
                s.User.Id,
                s.User.Name,
                s.User.Surname,
                s.User.AvatarUrl
            })
            .Select(g => new
            {
                g.Key.Id,
                g.Key.Name,
                g.Key.Surname,
                g.Key.AvatarUrl,
                Categories = g.Select(s => s.ExpenseItem.Category).Distinct().ToList(),
                LastExpenseDate = g.Max(s => s.ExpenseItem.Expense.PurchasedAt),
                TotalOwedAmount = g.Sum(s => s.OwedAmount)
            })
            .ToListAsync();

        return groupedIncomes.ConvertAll(x => (
            x.Id,
            x.Name,
            x.Surname,
            x.AvatarUrl,
            x.Categories,
            x.LastExpenseDate,
            x.TotalOwedAmount
        ));
    }

    public async Task<List<(int UserId, string Name, string Surname, string? AvatarUrl, List<string> Categories, DateTime Date, decimal Amount)>> GetUserTotalExpensesAsync(int userId)
    {
        var groupedExpenses = await _context.ExpenseSplits
            .Where(s => s.UserId == userId && s.ExpenseItem.Expense.PayerId != userId)
            .GroupBy(s => new
            {
                s.ExpenseItem.Expense.Payer.Id,
                s.ExpenseItem.Expense.Payer.Name,
                s.ExpenseItem.Expense.Payer.Surname,
                s.ExpenseItem.Expense.Payer.AvatarUrl
            })
            .Select(g => new
            {
                g.Key.Id,
                g.Key.Name,
                g.Key.Surname,
                g.Key.AvatarUrl,
                Categories = g.Select(s => s.ExpenseItem.Category).Distinct().ToList(),
                LastExpenseDate = g.Max(s => s.ExpenseItem.Expense.PurchasedAt),
                TotalOwedAmount = g.Sum(s => s.OwedAmount)
            })
            .ToListAsync();

        return groupedExpenses.ConvertAll(x => (
            x.Id,
            x.Name,
            x.Surname,
            x.AvatarUrl,
            x.Categories,
            x.LastExpenseDate,
            x.TotalOwedAmount
        ));
    }
}
