using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayItOff.Application.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private int GetUserId()
        => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
           ?? throw new UnauthorizedAccessException());
    public NotificationController(INotificationService notificationService) { _notificationService = notificationService; }

    [HttpGet("get-all-notifications")]
    public async Task<ActionResult<NotificationResponse>> GetAllUserNotifications([FromQuery] string? type1 = null, string? type2 = null)
    {
        var filters = new List<string>();
        if (!string.IsNullOrEmpty(type1)) filters.Add(type1);
        if (!string.IsNullOrEmpty(type2)) filters.Add(type2);

        var result = await _notificationService.GetUserNotificationAsync(GetUserId(), filters);
        return Ok(result);
    }

    [HttpGet("get-last-5-notifications")]
    public async Task<ActionResult<NotificationResponse>> GetLast5Notifications()
    {
        var result = await _notificationService.GetUserLast5Notifications(GetUserId());
        return Ok(result);
    }

    [HttpPatch("set-as-read")]
    public async Task<IActionResult> SetNotificationAsRead([FromQuery] int notificationId)
    {
        await _notificationService.SetNotificationAsReadAsync(GetUserId(), notificationId);
        return Ok();
    }

    [HttpPatch("set-all-as-read")]
    public async Task<IActionResult> SetAllNotificationsAsRead()
    {
        await _notificationService.SetAllNotificationsAsReadAsync(GetUserId());
        return Ok();
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteNotification([FromQuery] int notificationId)
    {
        await _notificationService.DeleteNotificationAsync(GetUserId(), notificationId);
        return Ok();
    }

    [HttpDelete("delete-all")]
    public async Task<IActionResult> DeleteAllNotifications()
    {
        await _notificationService.DeleteAllNotificationsAsync(GetUserId());
        return Ok();
    }
}