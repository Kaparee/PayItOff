using PayItOff.Shared.Responses;

namespace PayItOff.Application.Interfaces
{
    public interface ISettlementService
    {
        Task<List<GlobalDebtSummaryResponse>> GetUserAllIncomesSummaryAsync(int userId);
        Task<List<GlobalDebtSummaryResponse>> GetUserAllExpensesSummaryAsync(int userId);
    }
}