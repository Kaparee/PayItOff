using PayItOff.Shared.Requests;

namespace PayItOff.Application.Interfaces
{
    public interface IExpenseService
    {
        Task CreateExpenseBatch(int userId, CreateExpenseBatchRequest request);
    }
}