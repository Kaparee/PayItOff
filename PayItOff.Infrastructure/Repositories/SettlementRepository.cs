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
}