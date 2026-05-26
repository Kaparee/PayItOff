using PayItOff.Shared.Requests;

namespace PayItOff.Application.Interfaces
{
    public interface IExpenseService
    {
        Task CreateExpenseBatch(int userId, CreateExpenseBatchRequest request);
        Task<PayItOff.Shared.Responses.ExpenseDetailsResponse> GetExpenseDetailsAsync(int userId, int expenseId);
        Task<PayItOff.Shared.Responses.ExpenseDetailsResponse> GetExpenseItemDetailsAsync(int userId, int expenseId, int itemId);
    }
}