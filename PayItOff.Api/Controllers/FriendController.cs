using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayItOff.Application.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendController : ControllerBase
{
    private readonly IFriendService _friendService;
    private int GetUserId()
        => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
           ?? throw new UnauthorizedAccessException());
    public FriendController(IFriendService friendService) { _friendService = friendService; }

    [HttpGet("friends-list")]
    public async Task<ActionResult<FriendListResponse>> GetUserFriendsList()
    {
        var result = await _friendService.GetUserFriendListAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("all-pending-invitation")]
    public async Task<ActionResult<FriendPendingInvitationResponse>> GetPendingInvitationsAsync()
    {
        var result = await _friendService.GetPendingInvitationsAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost("invite")]
    public async Task<IActionResult> Invite([FromBody] FriendInviteRequest request)
    {
        await _friendService.InviteAsync(GetUserId(), request);
        return NoContent();
    }

    [HttpPatch("accept")]
    public async Task<IActionResult> Accept([FromBody] UpdateInviteRequest request)
    {
        await _friendService.AcceptInviteAsync(GetUserId(), request);
        return NoContent();
    }

    [HttpPatch("decline")]
    public async Task<IActionResult> Decline([FromBody] UpdateInviteRequest request)
    {
        await _friendService.DeclineInviteAsync(GetUserId(), request);
        return NoContent();
    }

    [HttpDelete("remove")]
    public async Task<IActionResult> Remove([FromBody] UpdateInviteRequest request)
    {
        await _friendService.RemoveFriendAsync(GetUserId(), request);
        return NoContent();
    }
}