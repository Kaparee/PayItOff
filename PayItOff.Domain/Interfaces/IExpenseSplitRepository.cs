using PayItOff.Domain.Entities;

namespace PayItOff.Domain.Interfaces;

public interface IExpenseSplitRepository
{
    Task<(List<ExpenseSplit> Splits, List<Settlement> Settlements, int TotalCount)> GetMixedHistoryAsync(int userId, int? targetId, string type, int page, int pageSize);
    Task<(int Total, int Incomes, int Expenses)> GetMixedHistoryCountsAsync(int userId, int? targetId);
    Task DistributePaymentAsync(int debtorId, int creditorId, int groupId, decimal amount);
}
