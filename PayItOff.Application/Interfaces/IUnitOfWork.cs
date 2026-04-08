namespace PayItOff.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
    Task<int> SaveChangesAsync();
}