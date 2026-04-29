using PayItOff.Shared.Responses;

namespace PayItOff.Application.Interfaces
{
    public interface ISettlementService
    {
        Task<GlobalSettlementResponse> GetUserAllIncomesSummaryAsync(int userId);
        Task<GlobalSettlementResponse> GetUserAllExpensesSummaryAsync(int userId);
    }
}