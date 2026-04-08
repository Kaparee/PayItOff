using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using PayItOff.Application.Interfaces;
using PayItOff.Shared.Requests;
using System.Security.Claims;

namespace PayItOff.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupController : ControllerBase
{
    private readonly IGroupService _service;
    private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
    public GroupController(IGroupService service) { _service = service; }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateGroupRequest request)
    {
        await _service.CreateAsync(request, CurrentUserId);
        return Ok();
    }

    [HttpGet("groups")]
    public async Task<IActionResult> Info()
    {
        var result = await _service.GetUserGroupsAsync(CurrentUserId);
        return Ok(result);
    }
}

