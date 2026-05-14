using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Persistence;

namespace PayItOff.Infrastructure.Repositories;

/// <summary>
/// repo do historii portfela - laczy wydatki i splaty
/// </summary>
public class ExpenseSplitRepository : IExpenseSplitRepository
{
    private readonly PayItOffDbContext _context;

    public ExpenseSplitRepository(PayItOffDbContext context)
    {
        _context = context;
    }

    // -------------------------------------------------------------------------
    // glowna metoda - pobiera historie
    // -------------------------------------------------------------------------
    public async Task<(List<ExpenseSplit> Splits, List<Settlement> Settlements, int TotalCount)> GetMixedHistoryAsync(
        int userId, int? targetId, string type, int page, int pageSize)
    {
        IQueryable<ExpenseSplit> queryForSplits = _context.ExpenseSplits;
        queryForSplits = queryForSplits.Where(s => s.UserId != s.ExpenseItem.Expense.PayerId);

        queryForSplits = queryForSplits.Where(s => s.UserId == userId || s.ExpenseItem.Expense.PayerId == userId);

        IQueryable<Settlement> queryForSettlements = _context.Settlements;
        queryForSettlements = queryForSettlements.Where(s => s.SenderId == userId || s.ReceiverId == userId);

        if (targetId != null)
        {
            int tid = targetId.Value;

            queryForSplits = queryForSplits.Where(s => s.UserId == tid || s.ExpenseItem.Expense.PayerId == tid);

            queryForSettlements = queryForSettlements.Where(s =>
                (s.SenderId == userId && s.ReceiverId == tid)
                || (s.ReceiverId == userId && s.SenderId == tid));
        }

        if (type == "Income")
        {
            queryForSplits = queryForSplits.Where(s => s.ExpenseItem.Expense.PayerId == userId);
            queryForSettlements = queryForSettlements.Where(s => s.ReceiverId == userId);
        }
        else if (type == "Expense")
        {
            queryForSplits = queryForSplits.Where(s => s.UserId == userId);
            queryForSettlements = queryForSettlements.Where(s => s.SenderId == userId);
        }
        else
        {
        }

        var expensePart = queryForSplits.Select(s => new TransactionKeyModel
        {
            IsSettlement = false,
            Id = s.ExpenseItem.ExpenseId,
            TargetUserId = s.UserId == userId ? s.ExpenseItem.Expense.PayerId : s.UserId,
            Date = s.ExpenseItem.Expense.PurchasedAt,
            AmIDebtor = s.UserId == userId
        }).Distinct();

        var settlementPart = queryForSettlements.Select(s => new TransactionKeyModel
        {
            IsSettlement = true,
            Id = s.Id,
            TargetUserId = s.SenderId == userId ? s.ReceiverId : s.SenderId,
            Date = s.CreatedAt,
            AmIDebtor = s.SenderId == userId
        });

        var everythingMixedTogether = expensePart.Concat(settlementPart);

        int howManyTotal = await everythingMixedTogether.CountAsync();

        int skipHowMany = (page - 1) * pageSize;
        var pageOfKeys = await everythingMixedTogether
            .OrderByDescending(x => x.Date)
            .Skip(skipHowMany)
            .Take(pageSize)
            .ToListAsync();

        if (pageOfKeys.Count == 0)
        {
            return (new List<ExpenseSplit>(), new List<Settlement>(), 0);
        }

        List<int> expenseIdsList = new List<int>();
        List<int> settlementIdsList = new List<int>();
        foreach (var key in pageOfKeys)
        {
            if (key.IsSettlement == false)
            {
                expenseIdsList.Add(key.Id);
            }
            else
            {
                settlementIdsList.Add(key.Id);
            }
        }

        List<ExpenseSplit> splitsResult = new List<ExpenseSplit>();
        if (expenseIdsList.Count > 0)
        {
            splitsResult = await _context.ExpenseSplits
                .Include(s => s.User)
                .Include(s => s.ExpenseItem).ThenInclude(ei => ei.Expense).ThenInclude(e => e.Payer)
                .Include(s => s.ExpenseItem).ThenInclude(ei => ei.Expense).ThenInclude(e => e.Group)
                .Where(s => expenseIdsList.Contains(s.ExpenseItem.ExpenseId))
                .Where(s => s.UserId == userId || s.ExpenseItem.Expense.PayerId == userId)
                .ToListAsync();
        }

        List<Settlement> settlementsResult = new List<Settlement>();
        if (settlementIdsList.Count > 0)
        {
            settlementsResult = await _context.Settlements
                .Include(s => s.Sender)
                .Include(s => s.Receiver)
                .Include(s => s.Group)
                .Where(s => settlementIdsList.Contains(s.Id))
                .ToListAsync();
        }

        return (splitsResult, settlementsResult, howManyTotal);
    }

    // -------------------------------------------------------------------------
    // druga metoda - liczniki do kafelkow na gorze
    // -------------------------------------------------------------------------
    public async Task<(int Total, int Incomes, int Expenses)> GetMixedHistoryCountsAsync(int userId, int? targetId)
    {
        IQueryable<ExpenseSplit> qSplits = _context.ExpenseSplits;
        qSplits = qSplits.Where(s => s.UserId != s.ExpenseItem.Expense.PayerId);
        qSplits = qSplits.Where(s => s.UserId == userId || s.ExpenseItem.Expense.PayerId == userId);

        IQueryable<Settlement> qSet = _context.Settlements;
        qSet = qSet.Where(s => s.SenderId == userId || s.ReceiverId == userId);

        if (targetId != null)
        {
            int t = targetId.Value;
            qSplits = qSplits.Where(s => s.UserId == t || s.ExpenseItem.Expense.PayerId == t);
            qSet = qSet.Where(s =>
                (s.SenderId == userId && s.ReceiverId == t)
                || (s.ReceiverId == userId && s.SenderId == t));
        }

        var keysFromExpenses = qSplits.Select(s => new TransactionKeyModel
        {
            IsSettlement = false,
            Id = s.ExpenseItem.ExpenseId,
            TargetUserId = s.UserId == userId ? s.ExpenseItem.Expense.PayerId : s.UserId,
            Date = s.ExpenseItem.Expense.PurchasedAt,
            AmIDebtor = s.UserId == userId
        }).Distinct();

        var keysFromSettlements = qSet.Select(s => new TransactionKeyModel
        {
            IsSettlement = true,
            Id = s.Id,
            TargetUserId = s.SenderId == userId ? s.ReceiverId : s.SenderId,
            Date = s.CreatedAt,
            AmIDebtor = s.SenderId == userId
        });

        var allKeys = keysFromExpenses.Concat(keysFromSettlements);

        int incomes = await allKeys.CountAsync(k => k.AmIDebtor == false);
        int expenses = await allKeys.CountAsync(k => k.AmIDebtor == true);
        int total = incomes + expenses;

        return (total, incomes, expenses);
    }
}

// model pomocniczy - uzywany tylko w tym pliku
internal class TransactionKeyModel
{
    public bool IsSettlement { get; set; }
    public int Id { get; set; }
    public int TargetUserId { get; set; }
    public DateTime Date { get; set; }
    public bool AmIDebtor { get; set; }
}
