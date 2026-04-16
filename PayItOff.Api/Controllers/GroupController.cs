using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using PayItOff.Application.Interfaces;
using PayItOff.Application.Services;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Security.Claims;

namespace PayItOff.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupController : ControllerBase
{
    private readonly IGroupService _groupService;
    private int GetUserId()
        => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
           ?? throw new UnauthorizedAccessException());
    public GroupController(IGroupService groupService) { _groupService = groupService; }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromForm] CreateGroupRequest request, IFormFile? avatar = null)
    {
        await _groupService.CreateAsync(request, GetUserId(), avatar);
        return Ok();
    }

    [HttpGet("groups")]
    public async Task<ActionResult<GroupInfoResponse>> Info()
    {
        var result = await _groupService.GetUserGroupsAsync(GetUserId());
        return Ok(result);
    }

    [HttpPatch("group-edit")]
    public async Task<IActionResult> Edit([FromForm] EditGroupInfoRequest request, IFormFile? avatar)
    {
        await _groupService.EditGroupInfoAsync(GetUserId(), request, avatar);
        return NoContent();
    }

    [HttpDelete("group-delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteGroupRequest request)
    {
        await _groupService.DeleteGroupAsync(GetUserId(), request);
        return NoContent();
    }
}

