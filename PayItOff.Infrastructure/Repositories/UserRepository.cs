using Microsoft.EntityFrameworkCore;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Interfaces;
using PayItOff.Infrastructure.Persistence;

namespace PayItOff.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly PayItOffDbContext _context;

    public UserRepository(PayItOffDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
    }

    public async Task AddAsync(User user)
    {
        _context.Users.Add(user);
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<User?> GetUserByEmailOrNicknameAsync(string eon)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(x => x.DeletedAt == null)
            .FirstOrDefaultAsync(x => x.Email == eon || x.Nickname == eon);
    }

    public async Task<User?> GetUserByVerificationTokenAsync(string token)
    {
        return await _context.Users.FirstOrDefaultAsync(x => x.VerificationToken == token);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
    }
}
