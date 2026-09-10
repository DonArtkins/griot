using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : DomainControllerBase
{
    private readonly IDomainService _domain;
    public NotificationController(IDomainService domain) { _domain = domain; }

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    { try { return Ok(await _domain.GetNotificationsAsync(CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    { try { return Ok(new { count = await _domain.GetUnreadNotificationCountAsync(CurrentUserId()) }); } catch (DomainError e) { return Handle(e); } }

    [HttpPost("read-all")]
    public async Task<IActionResult> ReadAll()
    { try { await _domain.MarkAllNotificationsReadAsync(CurrentUserId()); return Ok(new { message = "All notifications marked as read." }); } catch (DomainError e) { return Handle(e); } }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> ReadOne(Guid id)
    { try { var ok = await _domain.MarkNotificationReadAsync(id, CurrentUserId()); return ok ? Ok(new { message = "Notification marked as read." }) : NotFound(new { message = "Notification not found." }); } catch (DomainError e) { return Handle(e); } }
}
