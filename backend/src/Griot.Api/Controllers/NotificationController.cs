using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{

    [HttpGet]
    public IActionResult GetNotifications()
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpPost]
    public IActionResult CreateNotification()
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpPost("read-all")]
    public IActionResult ReadAll()
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpGet("unread-count")]
    public IActionResult GetUnreadCount()
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

}
