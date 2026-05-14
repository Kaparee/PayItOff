using PayItOff.Domain.Entities;

namespace PayItOff.Domain.Interfaces;

public interface ISettlementRepository
{
    Task<Settlement?> GetSettlementByIdAsync(int userId, int settId);
    Task AddAsync(Settlement settlement);
    Task UpdateAsync(Settlement settlement);
    Task<bool> HasPendingSettlementAsync(int senderId, int receiverId, int groupId);
}
