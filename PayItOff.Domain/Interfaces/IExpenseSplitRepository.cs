using PayItOff.Domain.Entities;

namespace PayItOff.Domain.Interfaces;

public interface IExpenseSplitRepository
{
    Task<(List<ExpenseSplit> Items, int TotalCount)> GetFilteredSplitsAsync(int userId, int? targetId, string type, int page, int pageSize);
}
