using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayItOff.Application.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

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

    [HttpGet("groups")]
    public async Task<ActionResult<GroupInfoResponse>> Info()
    {
        var result = await _groupService.GetUserGroupsAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("last-active-groups")]
    public async Task<ActionResult<ActiveGroupsDisplayResponse>> Get4ActiveGroups()
    {
        var result = await _groupService.GetTop4UserActiveGroupsAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("{groupId}/details")]
    public async Task<ActionResult<GroupDetailsResponse>> GetGroupDetails(int groupId)
    {
        var response = await _groupService.GetGroupDetailsAsync(groupId, GetUserId());
        return Ok(response);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromForm] CreateGroupRequest request, IFormFile? avatar = null)
    {
        await _groupService.CreateAsync(request, GetUserId(), avatar);
        return Ok();
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
    [HttpGet("archived")]
    [EndpointSummary("Pobieranie usuniętych (zarchiwizowanych) grup")]
    [EndpointDescription("Endpoint zwracający listę grup, które zostały usunięte i znajdują się w archiwum.")]
    public async Task<IActionResult> GetArchivedGroups()
    {
        var response = await _groupService.GetArchivedUserGroupsAsync(GetUserId());
        return Ok(response);
    }

    [HttpGet("{groupId}/history")]
    [EndpointSummary("Historia zmian w grupie")]
    [EndpointDescription("Zwraca listę logów audytowych dla danej grupy.")]
    public async Task<IActionResult> GetGroupHistory(int groupId)
    {
        var response = await _groupService.GetGroupHistoryAsync(groupId, GetUserId());
        return Ok(response);
    }
}

