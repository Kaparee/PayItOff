using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Application.Interfaces
{
    public interface ISettlementService
    {
        Task<GlobalSettlementResponse> GetUserAllIncomesSummaryAsync(int userId);
        Task<GlobalSettlementResponse> GetUserAllExpensesSummaryAsync(int userId);
        Task<PagedTransactionResponse> GetHistoryAsync(int userId, UserExpenseHistoryRequest request);
        Task<decimal> GetUserCurrentTotalDebtAsync(int userId, int? targetId = null);
        Task<List<PayableDebtOptionResponse>> GetPayableDebtOptionsAsync(int userId);
        Task<int> CreateSettlementAsync(int userId, CreateSettlementRequest request);
        Task<PayNetDebtResponse> CreateNetDebtSettlementsAsync(int userId, PayNetDebtRequest request);
        Task<bool> AcceptSettlementAsync(int userId, int settlementId);
        Task<bool> RejectSettlementAsync(int userId, int settlementId);
        Task SendDebtReminderAsync(int creditorUserId, RemindDebtRequest request);
        Task CompensateMutualDebtsAsync(int userId, CompensateDebtsRequest request);
    }
}