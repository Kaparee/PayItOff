using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using PayItOff.Application.Interfaces;
using PayItOff.Application.Services;
using PayItOff.Shared.Requests;

namespace PayItOff.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService) { _userService = userService; }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromForm] RegisterRequest request, IFormFile? avatar)
    {
        await _userService.RegisterAsync(request, avatar);
        return Ok();
    }


    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _userService.LoginAsync(request);
        return Ok(result);
    }

    //"email": "jakub@plocica.com",
    //"password": "JakubPlocica123!",
    //"nickname": "JakubPlocica",
    //"name": "Jakub",
    //"surname": "Płocica"

    [HttpGet("verify")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify([FromQuery] string verificationToken)
    {
        await _userService.VerifyUserAsync(verificationToken);
        return Ok("Konto zostało zweryfikowane. Możesz się zalogować.");
    }
}

