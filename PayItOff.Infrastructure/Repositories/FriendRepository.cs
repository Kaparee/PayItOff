using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Persistence;

namespace PayItOff.Infrastructure.Repositories;

public class FriendRepository : IFriendRepository
{
    private readonly PayItOffDbContext _context;

    public FriendRepository(PayItOffDbContext context)
    {
        _context = context;
    }

    public async Task<List<(User? Friend, int InviteId, decimal Balance, decimal Income, decimal Expense, List<(int GroupId, string Name, string? AvatarUrl)> SharedGroups)>> GetUserFriendListAsync(int userId)
    {
        var rawData = await _context.Friends
            .Where(x => x.InviterId == userId || x.ReceiverId == userId)
            .Where(x => x.AcceptedAt != null && x.DeletedAt == null && x.DeclinedAt == null)
            .Select(x => new
            {
                FriendEntity = x.InviterId == userId ? x.Receiver : x.Inviter,

                InviteId = x.Id,

                Balance = (_context.ExpenseSplits
                    .Where(s => s.ExpenseItem.Expense.DeletedAt == null &&
                                s.ExpenseItem.Expense.PayerId == userId &&
                                s.UserId == (x.InviterId == userId ? x.ReceiverId : x.InviterId))
                    .Sum(s => (decimal?)s.OwedAmount) ?? 0)
                    -
                    (_context.ExpenseSplits
                    .Where(s => s.ExpenseItem.Expense.DeletedAt == null &&
                                s.ExpenseItem.Expense.PayerId == (x.InviterId == userId ? x.ReceiverId : x.InviterId) &&
                                s.UserId == userId)
                    .Sum(s => (decimal?)s.OwedAmount) ?? 0),

                Income = _context.ExpenseSplits
                    .Where(s => s.ExpenseItem.Expense.DeletedAt == null &&
                                s.ExpenseItem.Expense.PayerId == userId &&
                                s.UserId == (x.InviterId == userId ? x.ReceiverId : x.InviterId))
                    .Sum(s => (decimal?)s.OwedAmount) ?? 0,

                Expense = _context.ExpenseSplits
                    .Where(s => s.ExpenseItem.Expense.DeletedAt == null &&
                                s.ExpenseItem.Expense.PayerId == (x.InviterId == userId ? x.ReceiverId : x.InviterId) &&
                                s.UserId == userId)
                    .Sum(s => (decimal?)s.OwedAmount) ?? 0,

                SharedGroups = _context.Groups
                    .Where(g => g.DeletedAt == null)
                    .Where(g => _context.GroupMembers.Any(gm => gm.GroupId == g.Id && gm.UserId == userId && gm.Status == PayItOff.Domain.Enums.GroupMemberStatus.Accepted))
                    .Where(g => _context.GroupMembers.Any(gm => gm.GroupId == g.Id && gm.UserId == (x.InviterId == userId ? x.ReceiverId : x.InviterId) && gm.Status == PayItOff.Domain.Enums.GroupMemberStatus.Accepted))
                    .OrderByDescending(g => g.CreatedAt)
                    .Select(g => new { GroupId = g.Id, Name = g.Name, AvatarUrl = g.AvatarUrl })
                    .ToList()
            })
            .ToListAsync();

        return rawData.ConvertAll(x => (x.FriendEntity, x.InviteId, x.Balance, x.Income, x.Expense, x.SharedGroups.Select(g => (g.GroupId, g.Name, (string?)g.AvatarUrl)).ToList()));
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

    public async Task<Friend?> GetUsersFriendshipAsync(int userId, int targetUserId)
    {
        return await _context.Friends
            .Where(x => (x.InviterId == userId && x.ReceiverId == targetUserId) || (x.InviterId == targetUserId && x.ReceiverId == userId))
            .FirstOrDefaultAsync();
    }

    public async Task<List<Friend>> GetPendingInvitationsByUserIdAsync(int userId)
    {
        return await _context.Friends
        .Include(x => x.Inviter)
        .Include(x => x.Receiver)
        .Where(x => x.InviterId == userId || x.ReceiverId == userId)
        .Where(x => x.AcceptedAt == null && x.DeclinedAt == null && x.DeletedAt == null)
        .ToListAsync();
    }
}