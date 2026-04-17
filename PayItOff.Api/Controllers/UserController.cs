using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayItOff.Application.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    private int GetUserId()
        => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
           ?? throw new UnauthorizedAccessException());

    public UserController(IUserService userService)
    { _userService = userService; }

    [HttpGet("verify")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify([FromQuery] string verificationToken)
    {
        await _userService.VerifyUserAsync(verificationToken);
        return Ok("Konto zostało zweryfikowane. Możesz się zalogować.");
    }

    [HttpGet("info")]
    public async Task<ActionResult<UserInformationResponse>> Info()
    {
        var result = await _userService.GetUserInformationAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromForm] RegisterRequest request, IFormFile? avatar = null)
    {
        await _userService.RegisterAsync(request, avatar);
        return Ok();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _userService.LoginAsync(request);
        return Ok(result);
    }

    //"email": "jakub@plocica.com",
    //"password": "JakubPlocica123!",
    //"nickname": "JakubPlocica",
    //"name": "Jakub",
    //"surname": "Płocica"
    [HttpPost("avatar")]
    public async Task<IActionResult> UpdateAvatar(IFormFile? avatar = null)
    {
        await _userService.UpdateAvatarAsync(GetUserId(), avatar);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("request-password-reset")]
    public async Task<IActionResult> RequestPasswordReset(PasswordRequest request)
    {
        await _userService.RequestPasswordResetAsync(request.Email);
        return Ok(new { message = "Na podany adres został wysłany mail z resetem hasła" });
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("confirm-password-reset")]
    public async Task<IActionResult> ConfirmPasswordReset([FromBody] ResetPasswordRequest request)
    {
        await _userService.ResetPasswordConfirmAsync(request);
        return Ok();
    }

    [HttpPost]
    [Route("request-email-change")]
    public async Task<IActionResult> RequestEmailChange(EmailRequest request)
    {
        await _userService.RequestEmailChangeAsync(GetUserId(), request.NewEmail);
        return Ok();
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("confirm-email-change")]
    public async Task<IActionResult> ConfirmEmailChange([FromQuery] string token)
    {
        await _userService.EmailChangeConfirmAsync(token);
        return Ok();
    }

    [HttpPatch("notifications")]
    public async Task<IActionResult> UpdateNotification([FromBody] UserNotificationChangeRequest request)
    {
        await _userService.UpdateNotificationAsync(GetUserId(), request);
        return NoContent();
    }

    [HttpPatch("profile")]
    public async Task<IActionResult> UpdateInfo([FromBody] UserInfoUpdateRequest request)
    {
        await _userService.UpdateInfoAsync(GetUserId(), request);
        return NoContent();
    }

    [HttpPatch]
    [Route("modify-password")]
    public async Task<IActionResult> ModifyPassword([FromBody] ModifyPasswordRequest request)
    {
        await _userService.ModifyPasswordAsync(GetUserId(), request);
        return Ok();
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteAccount()
    {
        await _userService.DeleteUserAsync(GetUserId());
        return NoContent();
    }
}