using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using PayItOff.Application.Interfaces;
using PayItOff.Shared.Requests;

namespace PayItOff.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _service;

    public UserController(IUserService service) { _service = service; }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] RegisterRequest request)
    {
        await _service.RegisterAsync(request);
        return Ok();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _service.LoginAsync(request);
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
        await _service.VerifyUserAsync(verificationToken);
        return Ok("Konto zostało zweryfikowane. Możesz się zalogować.");
    }
}

