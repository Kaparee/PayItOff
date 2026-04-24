using PayItOff.Domain.Entities;

namespace PayItOff.Domain.Interfaces;

public interface IExpenseRepository
{
    Task AddAsync(Expense expense);
    Task UpdateAsync(Expense expense);
    Task<Expense?> GetExpenseWithSplitsAsync(int expenseId);
}
