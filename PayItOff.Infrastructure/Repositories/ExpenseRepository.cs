using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Persistence;

namespace PayItOff.Infrastructure.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly PayItOffDbContext _context;

    public ExpenseRepository(PayItOffDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Expense expense)
    {
        _context.Expenses.Add(expense);
    }
    public async Task UpdateAsync(Expense expense)
    {
        _context.Expenses.Update(expense);
    }
    public async Task<Expense?> GetExpenseWithSplitsAsync(int expenseId)
    {
        return await _context.Expenses
            .Include(e => e.Items)
                .ThenInclude(i => i.Splits)
            .Include(e => e.Groups)
                .ThenInclude(g => g.Items)
            .FirstOrDefaultAsync(x => x.Id == expenseId && x.DeletedAt == null);
    }

    public async Task<List<Expense>> GetExpensesByGroupIdAsync(int groupId)
    {
        return await _context.Expenses
            .Include(e => e.Payer)
            .Include(e => e.Items)
                .ThenInclude(i => i.Splits)
                    .ThenInclude(s => s.User)
            .Where(e => e.GroupId == groupId && e.DeletedAt == null)
            .OrderByDescending(e => e.PurchasedAt)
            .ToListAsync();
    }
}