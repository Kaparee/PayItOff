using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Persistence;

namespace PayItOff.Infrastructure.Repositories;

public class SettlementRepository : ISettlementRepository
{
    private readonly PayItOffDbContext _context;
    public SettlementRepository(PayItOffDbContext context) { _context = context; }

    public async Task<Settlement?> GetSettlementByIdAsync(int userId, int settId)
    {
        return await _context.Settlements
            .Include(x => x.Sender)
            .Include(x => x.Receiver)
            .Include(x => x.Group)
            .Where(x => (x.SenderId == userId || x.ReceiverId == userId) && x.Id == settId)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(Settlement settlement)
    {
        _context.Settlements.Add(settlement);
    }

    public async Task UpdateAsync(Settlement settlement)
    {
        _context.Settlements.Update(settlement);
    }

    public Task<bool> HasPendingSettlementAsync(int senderId, int receiverId, int groupId) =>
        _context.Settlements.AnyAsync(s =>
            s.SenderId == senderId
            && s.ReceiverId == receiverId
            && s.GroupId == groupId
            && s.Status == SettlementStatus.Pending);

    public async Task<HashSet<(int SenderId, int ReceiverId, int GroupId)>> GetPendingSettlementKeysForUserPairInGroupsAsync(
        int userId1,
        int userId2,
        IReadOnlyCollection<int> groupIds)
    {
        if (groupIds.Count == 0)
            return new HashSet<(int SenderId, int ReceiverId, int GroupId)>();

        var rows = await _context.Settlements
            .AsNoTracking()
            .Where(s => s.Status == SettlementStatus.Pending)
            .Where(s => groupIds.Contains(s.GroupId))
            .Where(s =>
                (s.SenderId == userId1 && s.ReceiverId == userId2)
                || (s.SenderId == userId2 && s.ReceiverId == userId1))
            .Select(s => new { s.SenderId, s.ReceiverId, s.GroupId })
            .ToListAsync();

        return rows.Select(r => (r.SenderId, r.ReceiverId, r.GroupId)).ToHashSet();
    }

    public async Task<List<int>> GetPendingSettlementIdsAsync(int senderId, int receiverId)
    {
        return await _context.Settlements
            .AsNoTracking()
            .Where(s => s.SenderId == senderId && s.ReceiverId == receiverId && s.Status == SettlementStatus.Pending)
            .Select(s => s.Id)
            .ToListAsync();
    }
}