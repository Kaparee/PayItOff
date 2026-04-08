using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Application.Interfaces
{
    public interface IUserService
    {
        Task RegisterAsync(RegisterRequest request);
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task VerifyUserAsync(string token);
    }
}
