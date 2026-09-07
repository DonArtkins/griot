using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/attachments")]
public class AttachmentController : ControllerBase
{

    [HttpGet("/api/tasks/{id}/attachments")]
    public IActionResult GetAttachments(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpPost("/api/tasks/{id}/attachments")]
    public IActionResult CreateAttachment(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpDelete("/api/tasks/{id}/attachments/{attachmentId}")]
    public IActionResult DeleteAttachment(Guid id, Guid attachmentId)
    {
        return Ok(new { message = "Not implemented yet" });
    }

}
