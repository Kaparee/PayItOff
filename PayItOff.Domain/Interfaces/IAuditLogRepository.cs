using PayItOff.Domain.Entities;

namespace PayItOff.Domain.Interfaces;

public interface IAuditLogRepository
{
    Task<List<AuditLog>> GetAuditLogsForGroupAsync(int groupId);
}
