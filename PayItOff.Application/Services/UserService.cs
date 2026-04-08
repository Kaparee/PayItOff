using BCrypt.Net;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Exceptions;
using PayItOff.Domain.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

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

    public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork, IValidator<RegisterRequest> validator, IJWTService jwtService, IEmailService emailService, IConfiguration configuration, IFileService fileService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _jwtService = jwtService;
        _emailService = emailService;
        _configuration = configuration;
        _fileService = fileService;
    }

    public async Task RegisterAsync(RegisterRequest request, IFormFile? avatar = null)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        var existingUser = await _userRepository.GetUserByEmailAsync(request.Email);
        if (existingUser != null) throw new UserAlreadyExistsException("Email", request.Email);

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        if (!string.IsNullOrWhiteSpace(request.IBAN))
        {
            request.IBAN = request.IBAN.Replace(" ", "").ToUpper();
        }

        var savedFileName = await _fileService.SaveAvatarAsync(avatar);
        if (savedFileName != null)
        {
            request.AvatarUrl = savedFileName;
        }

        var user = User.Register(
            request.Email,
            passwordHash,
            request.Nickname,
            request.Name,
            request.Surname,
            request.AvatarUrl,
            request.PhoneNumber,
            request.IBAN
        );

        await _unitOfWork.BeginTransactionAsync();
        try
        {

            await _userRepository.AddAsync(user);

            await _unitOfWork.SaveChangesAsync();

            var backendUrl = _configuration["AppUrls:BackendUrl"];
            await _emailService.SendEmailAsync(user.Email, "Witaj w PayItOff!", $"<h1>Cześć {user.Nickname}!</h1><p>Dzięki za rejestrację.<br/>Aby zweryfikować konto, kliknij tutaj -> <a href=\"{backendUrl}/api/User/verify?verificationToken={user.VerificationToken}\">Link</a></p>");
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw new Exception("Serwis pocztowy chwilowo niedostępny. Spróbuj ponownie później.");
        }
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetUserByEmailOrNicknameAsync(request.EmailOrNickname);
        if (user is null) { throw new UserNotFoundException(); }
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PassHash)) { throw new InvalidPasswordException(); }
        if (!user.IsActive || !user.IsVerified) { throw new UserNotActiveOrVerifiedException(); }

        var token = _jwtService.GenerateToken(user);

        return new LoginResponse { Token = token };
    }

    public async Task VerifyUserAsync(string token)
    {
        var user = await _userRepository.GetUserByVerificationTokenAsync(token);

        if (user == null)
            throw new Exception("Nieprawidłowy lub wygasły token weryfikacyjny.");

        user.ConfirmVerification(token);

        await _unitOfWork.SaveChangesAsync();
    }
}