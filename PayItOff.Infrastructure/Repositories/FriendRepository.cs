using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Persistence;
using PayItOff.Shared.Responses;

namespace PayItOff.Infrastructure.Repositories;

public class FriendRepository : IFriendRepository
{
    private readonly PayItOffDbContext _context;

    public FriendRepository(PayItOffDbContext context)
    {
        _context = context;
    }

    public async Task<List<(User? Friend, int InviteId, decimal Balance, List<string> SharedAvatars)>> GetUserFriendListAsync(int userId)
    {
        var rawData = await _context.Friends
            .Where(x => x.InviterId == userId || x.ReceiverId == userId)
            .Where(x => x.AcceptedAt != null && x.DeletedAt == null && x.DeclinedAt == null)
            .Select(x => new
            {
                FriendEntity = x.InviterId == userId ? x.Receiver : x.Inviter,

                InviteId = x.Id,

                Balance = _context.GroupDebts
                    .Where(gd => (gd.CreditorId == userId && gd.DebtorId == (x.InviterId == userId ? x.ReceiverId : x.InviterId)) ||
                                 (gd.DebtorId == userId && gd.CreditorId == (x.InviterId == userId ? x.ReceiverId : x.InviterId)))
                    .Sum(gd => gd.CreditorId == userId ? gd.Amount : -gd.Amount),

                SharedAvatars = _context.Groups
                    .Where(g => g.DeletedAt == null)
                    .Where(g => _context.GroupMembers.Any(gm => gm.GroupId == g.Id && gm.UserId == userId))
                    .Where(g => _context.GroupMembers.Any(gm => gm.GroupId == g.Id && gm.UserId == (x.InviterId == userId ? x.ReceiverId : x.InviterId)))
                    .Select(g => g.AvatarUrl)
                    .ToList()
            })
            .ToListAsync();

        return rawData.ConvertAll(x => (x.FriendEntity, x.InviteId, x.Balance, x.SharedAvatars));
    }

    public async Task<bool> IsFriendInviteExistAsync(int userId, int targetUserId)
    {
        return await _context.Friends
            .Where(x => (x.InviterId == userId && x.ReceiverId == targetUserId) || (x.InviterId == targetUserId && x.ReceiverId == userId))
            .Where(x => x.DeletedAt == null && x.DeclinedAt == null)
            .AnyAsync();
    }

    public async Task AddAsync(Friend friend)
    {
        _context.Friends.Add(friend);
    }

    public async Task UpdateAsync(Friend friend)
    {
        _context.Friends.Update(friend);
    }

    public async Task<Friend?> GetInviteByIdAsync(int userId, int inviteId)
    {
        return await _context.Friends
            .Where(x => x.Id == inviteId && (x.InviterId == userId || x.ReceiverId == userId))
            .Where(x => x.DeletedAt == null && x.DeclinedAt == null)
            .FirstOrDefaultAsync();
    }
}