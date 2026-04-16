using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayItOff.Application.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupMemberController : ControllerBase
{
    private readonly IGroupMemberService _groupMemberService;
    private int GetUserId()
        => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
           ?? throw new UnauthorizedAccessException());
    public GroupMemberController(IGroupMemberService groupMemberService) { _groupMemberService = groupMemberService; }

    [HttpGet("all-pending-invitation")]
    public async Task<ActionResult<List<PendingInvitationResponse>>> AllPendingInvitations()
    {
        var result = await _groupMemberService.GetPendingInvitationsAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("{groupId}/all-group-members")]
    public async Task<ActionResult<List<GroupMemberResponse>>> AllActiveGroupMembers([FromRoute] int groupId)
    {
        var result = await _groupMemberService.GetAllActiveGroupMembersAsync(groupId);
        return Ok(result);
    }

    [HttpPost("invite")]
    public async Task<IActionResult> Invite([FromBody] GroupInviteUserRequest request)
    {
        await _groupMemberService.InviteUserAsync(request);
        return Ok();
    }

    [HttpPatch("accept")]
    public async Task<IActionResult> Accept([FromQuery] int invitationId)
    {
        await _groupMemberService.AcceptInviteAsync(GetUserId(), invitationId);
        return Ok();
    }

    [HttpPatch("decline")]
    public async Task<IActionResult> Decline([FromQuery] int invitationId)
    {
        await _groupMemberService.DeclineInviteAsync(GetUserId(), invitationId);
        return Ok();
    }

    [HttpPatch("update-role")]
    public async Task<IActionResult> UpdateRole([FromBody] GroupMemberUpdateRequest request)
    {
        await _groupMemberService.UpdateRoleAsync(GetUserId(), request);
        return Ok();
    }

    [HttpPatch("{groupId}/set-fav")]
    public async Task<IActionResult> SetGroupAsFav([FromRoute] int groupId)
    {
        await _groupMemberService.SetGroupAsFavoriteAsync(GetUserId(), groupId);
        return Ok();
    }

    [HttpDelete("{groupId}/leave")]
    public async Task<IActionResult> LeaveGroup([FromRoute] int groupId)
    {
        await _groupMemberService.LeaveGroupAsync(GetUserId(), groupId);
        return NoContent();
    }

    [HttpDelete("{groupId}/kick/{targetUserId}")]
    public async Task<IActionResult> KickMember([FromRoute] int groupId, [FromRoute] int targetUserId)
    {
        await _groupMemberService.KickUserFromGroupAsync(GetUserId(), groupId, targetUserId);
        return NoContent();
    }
}