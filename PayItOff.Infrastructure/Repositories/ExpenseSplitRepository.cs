using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Persistence;

namespace PayItOff.Infrastructure.Repositories;

public class ExpenseSplitRepository : IExpenseSplitRepository
{
    private readonly PayItOffDbContext _context;
    public ExpenseSplitRepository(PayItOffDbContext context) { _context = context; }

    public async Task<(List<ExpenseSplit> Items, int TotalCount)> GetFilteredSplitsAsync(
        int userId, int? targetId, string type, int page, int pageSize)
    {
        var query = _context.ExpenseSplits
            .Include(s => s.User)
            .Include(s => s.ExpenseItem)
                .ThenInclude(ei => ei.Expense)
                    .ThenInclude(e => e.Payer)
            .Include(s => s.ExpenseItem)
                .ThenInclude(ei => ei.Expense)
                    .ThenInclude(e => e.Group)
            .Where(s => s.UserId == userId || s.ExpenseItem.Expense.PayerId == userId)
            .AsQueryable();

        if (targetId.HasValue)
        {
            query = query.Where(s => s.UserId == targetId || s.ExpenseItem.Expense.PayerId == targetId);
        }

        if (type == "Income")
            query = query.Where(s => s.ExpenseItem.Expense.PayerId == userId && s.UserId != userId);
        else if (type == "Expense")
            query = query.Where(s => s.UserId == userId && s.ExpenseItem.Expense.PayerId != userId);
        else if (type == "All")
            query = query.Where(s => (s.UserId == userId && s.ExpenseItem.Expense.PayerId != userId)
                                  || (s.ExpenseItem.Expense.PayerId == userId && s.UserId != userId));

        var items = await query
            .OrderByDescending(x => x.ExpenseItem.Expense.PurchasedAt)
            .ToListAsync();

        return (items, items.Count);
    }
}