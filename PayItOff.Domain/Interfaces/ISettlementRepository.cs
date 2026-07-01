using PayItOff.Domain.Entities;

namespace PayItOff.Domain.Interfaces;

public interface ISettlementRepository
{
    Task<Settlement?> GetSettlementByIdAsync(int userId, int settId);
    Task AddAsync(Settlement settlement);
    Task UpdateAsync(Settlement settlement);
    Task<bool> HasPendingSettlementAsync(int senderId, int receiverId, int groupId);

    Task<HashSet<(int SenderId, int ReceiverId, int GroupId)>> GetPendingSettlementKeysForUserPairInGroupsAsync(
        int userId1,
        int userId2,
        IReadOnlyCollection<int> groupIds);

    Task<List<int>> GetPendingSettlementIdsAsync(int senderId, int receiverId);
}
