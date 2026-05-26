using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;
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

    public Task AddAsync(GroupDebt groupDebt)
    {
        _context.GroupDebts.Add(groupDebt);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(GroupDebt groupDebt)
    {
        _context.GroupDebts.Update(groupDebt);
        return Task.CompletedTask;
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

    public void ApplyDirectDebtReduction(GroupDebt debt, decimal reduction)
    {
        if (reduction <= 0) return;

        debt.DecreaseAmount(reduction);

        if (debt.Amount <= 0)
            _context.GroupDebts.Remove(debt);
        else
            _context.GroupDebts.Update(debt);
    }

    public async Task ApplyDebtChangeAsync(Group group, User debtor, User creditor, decimal amountChange)
    {
        if (amountChange == 0) return;

        if (amountChange < 0)
        {
            var forward = await GetDebtAsync(group.Id, debtor.Id, creditor.Id);
            if (forward is null || forward.Amount + amountChange < 0)
                throw new InvalidOperationException("Niewystarczające saldo długu do zmniejszenia (kwota większa niż zapisany dług).");
        }

        var directDebt = await GetDebtAsync(group.Id, debtor.Id, creditor.Id);

        if (directDebt != null)
        {
            directDebt.ChangeAmount(amountChange);

            if (directDebt.Amount <= 0)
                _context.GroupDebts.Remove(directDebt);
            else
                _context.GroupDebts.Update(directDebt);

            return;
        }

        var reverseDebt = await GetDebtAsync(group.Id, creditor.Id, debtor.Id);

        if (reverseDebt != null)
        {
            if (reverseDebt.Amount > amountChange)
            {
                reverseDebt.ChangeAmount(-amountChange);
                _context.GroupDebts.Update(reverseDebt);
            }
            else if (reverseDebt.Amount == amountChange)
            {
                _context.GroupDebts.Remove(reverseDebt);
            }
            else
            {
                decimal remainingAmount = amountChange - reverseDebt.Amount;
                _context.GroupDebts.Remove(reverseDebt);

                var newDebt = GroupDebt.Create(group, debtor, creditor, remainingAmount);
                await _context.GroupDebts.AddAsync(newDebt);
            }
            return;
        }

        if (amountChange > 0)
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
        var allDebts = await _context.GroupDebts
            .Include(d => d.Debtor)
            .Include(d => d.Creditor)
            .Where(d => d.DebtorId == userId || d.CreditorId == userId)
            .ToListAsync();

        var globalBalances = allDebts
            .GroupBy(d => d.DebtorId == userId ? d.CreditorId : d.DebtorId)
            .Select(g => new
            {
                OtherUserId = g.Key,
                NetBalance = g.Sum(d => d.CreditorId == userId ? d.Amount : -d.Amount),
                User = g.First().DebtorId == userId ? g.First().Creditor : g.First().Debtor
            })
            .Where(x => x.NetBalance > 0)
            .ToList();

        return await MapToResponseWithMetadata(userId, globalBalances.Select(x => (x.User, x.NetBalance)).ToList());
    }

    public async Task<List<(int UserId, string Name, string Surname, string? AvatarUrl, List<string> Categories, DateTime Date, decimal Amount)>> GetUserTotalExpensesAsync(int userId)
    {
        var allDebts = await _context.GroupDebts
            .Include(d => d.Debtor)
            .Include(d => d.Creditor)
            .Where(d => d.DebtorId == userId || d.CreditorId == userId)
            .ToListAsync();

        var globalBalances = allDebts
            .GroupBy(d => d.DebtorId == userId ? d.CreditorId : d.DebtorId)
            .Select(g => new
            {
                OtherUserId = g.Key,
                NetBalance = g.Sum(d => d.CreditorId == userId ? d.Amount : -d.Amount),
                User = g.First().DebtorId == userId ? g.First().Creditor : g.First().Debtor
            })
            .Where(x => x.NetBalance < 0)
            .ToList();

        return await MapToResponseWithMetadata(userId, globalBalances.Select(x => (x.User, Math.Abs(x.NetBalance))).ToList());
    }

    private async Task<List<(int UserId, string Name, string Surname, string? AvatarUrl, List<string> Categories, DateTime Date, decimal Amount)>> MapToResponseWithMetadata(int userId, List<(User User, decimal Amount)> balances)
    {
        var result = new List<(int, string, string, string?, List<string>, DateTime, decimal)>();

        foreach (var item in balances)
        {
            var metadata = await _context.ExpenseSplits
                .Where(s => (s.UserId == userId && s.ExpenseItem.Expense.PayerId == item.User.Id) ||
                            (s.UserId == item.User.Id && s.ExpenseItem.Expense.PayerId == userId))
                .OrderByDescending(s => s.ExpenseItem.Expense.PurchasedAt)
                .Select(s => new { s.ExpenseItem.Category, s.ExpenseItem.Expense.PurchasedAt })
                .Take(5)
                .ToListAsync();

            result.Add((
                item.User.Id,
                item.User.Name,
                item.User.Surname,
                item.User.AvatarUrl,
                metadata.Select(m => m.Category).Distinct().ToList(),
                metadata.FirstOrDefault()?.PurchasedAt ?? DateTime.Now,
                item.Amount
            ));
        }

        return result;
    }

    public async Task<GroupDebt?> GetSpecificDebtAsync(int debtorId, int creditorId, int groupId)
    {
        return await _context.GroupDebts
            .FirstOrDefaultAsync(gd => gd.DebtorId == debtorId && gd.CreditorId == creditorId && gd.GroupId == groupId);
    }

    public async Task<List<(int GroupId, string GroupName, int CreditorId, string CreditorName, string CreditorSurname, decimal Amount)>> GetOpenDebtLinesForDebtorAsync(int debtorId)
    {
        var rows = await _context.GroupDebts
            .Where(gd => gd.DebtorId == debtorId && gd.Amount > 0)
            .Select(gd => new
            {
                gd.GroupId,
                GroupName = gd.Group.Name,
                gd.CreditorId,
                CreditorName = gd.Creditor.Name,
                CreditorSurname = gd.Creditor.Surname,
                gd.Amount
            })
            .ToListAsync();

        return rows.ConvertAll(x => (x.GroupId, x.GroupName, x.CreditorId, x.CreditorName, x.CreditorSurname, x.Amount));
    }

    public async Task<List<GroupDebt>> GetBilateralActiveDebtsBetweenUsersAsync(int userId1, int userId2)
    {
        return await _context.GroupDebts
            .Include(gd => gd.Group)
            .Include(gd => gd.Debtor)
            .Include(gd => gd.Creditor)
            .Where(gd => gd.Amount > 0
                && ((gd.DebtorId == userId1 && gd.CreditorId == userId2)
                    || (gd.DebtorId == userId2 && gd.CreditorId == userId1)))
            .ToListAsync();
    }

    public async Task<List<GroupDebt>> GetGroupDebtsByGroupIdAsync(int groupId)
    {
        return await _context.GroupDebts
            .Include(gd => gd.Debtor)
            .Include(gd => gd.Creditor)
            .Where(gd => gd.GroupId == groupId && gd.Amount > 0)
            .ToListAsync();
    }
}
