using PayItOff.Domain.Entities;

namespace PayItOff.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByEmailAsync(string email);
    Task AddAsync(User entity);
    Task UpdateAsync(User user);
    Task<User?> GetUserByEmailOrNicknameAsync(string eon);
    Task<User?> GetUserByVerificationTokenAsync(string token);
    Task<User?> GetUserByIdAsync(int userId);
    Task<bool> IsEmailTakenAsync(string email);
    Task<User?> GetUserByPasswordResetTokenAsync(string token);
    Task<User?> GetUserByEmailChangeTokenAsync(string token);
}
