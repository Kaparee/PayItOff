using PayItOff.Domain.Entities;

namespace PayItOff.Application.Interfaces
{
    public interface IJWTService
    {
        string GenerateToken(User user);
    }
}