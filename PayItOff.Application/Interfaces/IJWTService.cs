using PayItOff.Domain.Entities;
using System.Security.Claims;

namespace PayItOff.Application.Interfaces
{
    public interface IJWTService
    {
        string GenerateToken(User user);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}