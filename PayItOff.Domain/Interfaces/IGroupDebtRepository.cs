using PayItOff.Domain.Entities;

namespace PayItOff.Domain.Interfaces;

public interface IGroupDebtRepository
{
    Task AddAsync(GroupDebt groupDebt);
    Task UpdateAsync(GroupDebt groupDebt);
    Task<bool> HasActiveGroupDebt(int groupId);
    Task<GroupDebt?> GetDebtAsync(int groupId, int debtorId, int creditorId);
    Task ApplyDebtChangeAsync(int groupId, int debtorId, int creditorId, decimal amountChange);
}
