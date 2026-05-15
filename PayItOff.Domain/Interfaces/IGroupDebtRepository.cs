using PayItOff.Domain.Entities;

namespace PayItOff.Domain.Interfaces;

public interface IGroupDebtRepository
{
    Task AddAsync(GroupDebt groupDebt);
    Task UpdateAsync(GroupDebt groupDebt);
    Task<bool> HasActiveGroupDebt(int groupId);
    Task<GroupDebt?> GetDebtAsync(int groupId, int debtorId, int creditorId);
    Task ApplyDebtChangeAsync(Group group, User debtor, User creditor, decimal amountChange);
    Task<Dictionary<int, (decimal Income, decimal Expense)>> GetUserGroupBalancesAsync(int userId);
    Task<List<(int UserId, string Name, string Surname, string? AvatarUrl, List<string> Categories, DateTime Date, decimal Amount)>> GetUserTotalIncomesAsync(int userId);
    Task<List<(int UserId, string Name, string Surname, string? AvatarUrl, List<string> Categories, DateTime Date, decimal Amount)>> GetUserTotalExpensesAsync(int userId);
    Task<GroupDebt?> GetSpecificDebtAsync(int debtorId, int creditorId, int groupId);

    Task<List<(int GroupId, string GroupName, int CreditorId, string CreditorName, string CreditorSurname, decimal Amount)>> GetOpenDebtLinesForDebtorAsync(int debtorId);
    Task<List<GroupDebt>> GetBilateralActiveDebtsBetweenUsersAsync(int userId1, int userId2);
}
