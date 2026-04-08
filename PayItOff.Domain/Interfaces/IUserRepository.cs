using PayItOff.Domain.Entities;

namespace PayItOff.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User entity);
    Task UpdateAsync(User user);
    Task<User?> GetUserByEmailOrNicknameAsync(string eon);
    Task<User?> GetUserByVerificationTokenAsync(string token);

}
