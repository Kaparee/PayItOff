using Microsoft.AspNetCore.Http;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Application.Interfaces
{
    public interface IUserService
    {
        Task RegisterAsync(RegisterRequest request, IFormFile? avatar = null);
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task VerifyUserAsync(string token);
        Task<UserInformationResponse> GetUserInformationAsync(int userId);
        Task UpdateNotificationAsync(int userId, UserNotificationChangeRequest request);
        Task UpdateInfoAsync(int userId, UserInfoUpdateRequest request);
        Task UpdateAvatarAsync(int userId, IFormFile? avatar);
        Task ModifyPasswordAsync(int userId, ModifyPasswordRequest request);
        Task RequestPasswordResetAsync(string email);
        Task RequestEmailChangeAsync(int userId, string newEmail);
        Task ResetPasswordConfirmAsync(ResetPasswordRequest request);
        Task EmailChangeConfirmAsync(string token);
        Task DeleteUserAsync(int userId);
    }
}
