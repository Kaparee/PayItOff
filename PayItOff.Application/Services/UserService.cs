using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PayItOff.Application.Helpers;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Exceptions;
using PayItOff.Domain.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PayItOff.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RegisterRequest> _validator;
    private readonly IJWTService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IFileService _fileService;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork, IValidator<RegisterRequest> validator, IJWTService jwtService, IEmailService emailService, IConfiguration configuration, IFileService fileService, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _jwtService = jwtService;
        _emailService = emailService;
        _configuration = configuration;
        _fileService = fileService;
        _passwordHasher = passwordHasher;
    }

    public async Task RegisterAsync(RegisterRequest request, IFormFile? avatar)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var existingUser = await _userRepository.GetUserByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new UserAlreadyExistsException("Email", request.Email);
        }

        var existingNickname = await _userRepository.GetUserByNicknameAsync(request.Nickname);
        if (existingNickname != null)
        {
            throw new UserAlreadyExistsException("Nickname", request.Nickname);
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            request.PhoneNumber = PhoneNumberHelper.FormatPhoneNumber(request.PhoneNumber)!;
            var existingPhone = await _userRepository.GetUserByPhoneNumberAsync(request.PhoneNumber);
            if (existingPhone != null)
            {
                throw new UserAlreadyExistsException("PhoneNumber", request.PhoneNumber);
            }
        }

        string passwordHash = _passwordHasher.Hash(request.Password);

        var savedFileName = await _fileService.SaveAvatarAsync(avatar);

        var user = User.Register(
            request.Email,
            passwordHash,
            request.Nickname,
            request.Name,
            request.Surname,
            savedFileName,
            request.PhoneNumber,
            request.IBAN
        );

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _userRepository.AddAsync(user);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetUserByEmailOrNicknameAsync(request.EmailOrNickname);
        if (user is null) { throw new UserNotFoundException(); }
        if (!_passwordHasher.Verify(request.Password, user.PassHash)) { throw new InvalidPasswordException(); }
        if (!user.IsActive || !user.IsVerified) { throw new UserNotActiveOrVerifiedException(); }
        if (request.RememberMe is true)
        {
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(128));

            user.UpdateRefreshToken(refreshToken, DateTime.UtcNow.AddDays(30));
        }

        await _userRepository.UpdateAsync(user);

        await _unitOfWork.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        return new LoginResponse { Token = token, RefreshToken = user.RefreshToken };
    }

    public async Task VerifyUserAsync(string token)
    {
        var user = await _userRepository.GetUserByVerificationTokenAsync(token);

        if (user == null)
        {
            throw new Exception("Nieprawidłowy lub wygasły token weryfikacyjny.");
        }

        user.ConfirmVerification(token);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<UserInformationResponse> GetUserInformationAsync(int userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) { throw new UserNotFoundException(); }

        var baseUrl = _configuration["AppUrls:BackendUrl"];

        return new UserInformationResponse
        {
            Id = user.Id,
            AvatarUrl = UrlHelper.BuildUserAvatarUrl(baseUrl!, user.AvatarUrl),
            Name = user.Name,
            Surname = user.Surname,
            Email = user.Email,
            Nickname = user.Nickname,
            PhoneNumber = user.PhoneNumber,
            IBAN = user.IBAN,
            Notifications = new UserNotificationSettingsResponse(
                user.NotificationsSettings.DailySummary,
                user.NotificationsSettings.NotifyOnGroupJoined,
                user.NotificationsSettings.NotifyOnExpenseAdded,
                user.NotificationsSettings.NotifyOnGroupRemoved,
                user.NotificationsSettings.NotifyOnFriendRemoved,
                user.NotificationsSettings.NotifyOnExpenseChanged,
                user.NotificationsSettings.NotifyOnTransferConfirmed
                )
        };
    }

    public async Task UpdateNotificationAsync(int userId, UserNotificationChangeRequest request)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) { throw new UserNotFoundException(); }

        var newSettings = new NotificationsSettings(
            request.Notifications.DailySummary,
            request.Notifications.NotifyOnGroupJoined,
            request.Notifications.NotifyOnExpenseAdded,
            request.Notifications.NotifyOnGroupRemoved,
            request.Notifications.NotifyOnFriendRemoved,
            request.Notifications.NotifyOnExpenseChanged,
            request.Notifications.NotifyOnTransferConfirmed
        );

        user.UpdateNotifications(newSettings);

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateInfoAsync(int userId, UserInfoUpdateRequest request)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) { throw new UserNotFoundException(); }

        if (request.Nickname != user.Nickname)
        {
            var existingNickname = await _userRepository.GetUserByNicknameAsync(request.Nickname);
            if (existingNickname != null)
            {
                throw new UserAlreadyExistsException("Nickname", request.Nickname);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            request.PhoneNumber = PhoneNumberHelper.FormatPhoneNumber(request.PhoneNumber)!;
            if (request.PhoneNumber != user.PhoneNumber)
            {
                var existingPhone = await _userRepository.GetUserByPhoneNumberAsync(request.PhoneNumber);
                if (existingPhone != null)
                {
                    throw new UserAlreadyExistsException("PhoneNumber", request.PhoneNumber);
                }
            }
        }

        user.UpdateInfo(request.Nickname, request.Name, request.Surname, request.PhoneNumber, request.IBAN);

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateAvatarAsync(int userId, IFormFile? avatar)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) { throw new UserNotFoundException(); }

        var savedFileName = await _fileService.SaveAvatarAsync(avatar);
        if (savedFileName != null)
        {
            if (user!.AvatarUrl != null && user.AvatarUrl != "default_user_avatar.png")
            {
                _fileService.DeleteFile(user.AvatarUrl);
            }
            user.UpdateAvatar(savedFileName);
        }

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ModifyPasswordAsync(int userId, ModifyPasswordRequest request)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) { throw new UserNotFoundException(); }

        if (!_passwordHasher.Verify(request.OldPassword, user.PassHash))
        {
            throw new InvalidPasswordException();
        }

        if (request.OldPassword == request.NewPassword)
        {
            throw new Exception("New password cannot be same as the old password.");
        }

        var passHash = _passwordHasher.Hash(request.NewPassword);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            user.ModifyPassword(passHash);
            await _userRepository.UpdateAsync(user);

            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task RequestPasswordResetAsync(string email)
    {
        var user = await _userRepository.GetUserByEmailOrNicknameAsync(email);
        if (user is null) { throw new UserNotFoundException(); }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            user.GeneratePasswordResetToken();
            await _userRepository.UpdateAsync(user);

            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task ResetPasswordConfirmAsync(ResetPasswordRequest request)
    {
        var user = await _userRepository.GetUserByPasswordResetTokenAsync(request.Token);

        if (user == null || user.ResetTokenExpiresAt < DateTime.UtcNow)
        {
            throw new Exception("Invalid or expired token.");
        }

        var passwordHash = _passwordHasher.Hash(request.NewPassword);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            user.ResetPassword(user.PasswordResetToken!, passwordHash);
            await _userRepository.UpdateAsync(user);

            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task RequestEmailChangeAsync(int userId, string newEmail)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) { throw new UserNotFoundException(); }

        if (await _userRepository.IsEmailTakenAsync(newEmail))
        {
            throw new Exception("This email is already taken.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            user.ChangeEmailDirectly(newEmail);
            await _userRepository.UpdateAsync(user);

            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task EmailChangeConfirmAsync(string token)
    {
        var user = await _userRepository.GetUserByEmailChangeTokenAsync(token);

        if (user == null)
        {
            throw new Exception("Invalid email change token.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            user.EmailChange(token);
            await _userRepository.UpdateAsync(user);

            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteUserAsync(int userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) { throw new UserNotFoundException(); }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (user!.AvatarUrl != null && user.AvatarUrl != "default_user_avatar.png")
            {
                _fileService.DeleteFile(user.AvatarUrl);
            }

            user.Delete();
            await _userRepository.UpdateAsync(user);

            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshRequest request)
    {
        var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);

        var userIdStr = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var userId = int.Parse(userIdStr!);

        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) { throw new UserNotFoundException(); }

        if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime < DateTime.UtcNow) { throw new UnauthorizedAccessException("Nieprawidłowy token odświeżający."); }

        var newToken = _jwtService.GenerateToken(user);

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(128));

        user.UpdateRefreshToken(refreshToken, DateTime.UtcNow.AddDays(30));

        await _unitOfWork.SaveChangesAsync();

        return new LoginResponse { Token = newToken, RefreshToken = refreshToken };
    }
}
