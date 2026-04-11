using PayItOff.Domain.Entities;

namespace PayItOff.Domain.Interfaces;

public interface IFriendRepository
{
    Task<List<(User? Friend, int InviteId, decimal Balance, List<string> SharedAvatars)>> GetUserFriendListAsync(int userId);
    Task<bool> IsFriendInviteExistAsync(int userId, int targetUserId);
    Task AddAsync(Friend friend);
    Task UpdateAsync(Friend friend);
    Task<Friend?> GetInviteByIdAsync(int userId, int inviteId);

}
