using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/tasks/{id}/attachments")]
[Authorize]
public class AttachmentController : DomainControllerBase
{
    private readonly IDomainService _domain;
    public AttachmentController(IDomainService domain) { _domain = domain; }

    [HttpGet]
    public async Task<IActionResult> GetAttachments(Guid id)
    { try { return Ok(await _domain.GetAttachmentsAsync(id, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpPost]
    public async Task<IActionResult> CreateAttachment(Guid id, [FromBody] CreateAttachmentRequest request)
    { try { return StatusCode(StatusCodes.Status201Created, await _domain.CreateAttachmentAsync(id, request, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpDelete("{attachmentId}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId)
    {
        if (ForbidIfAiCall() is IActionResult forbid) return forbid;
        try { return (await _domain.DeleteAttachmentAsync(id, attachmentId, CurrentUserId())) ? NoContent() : NotFound(new { message = "Attachment not found." }); } catch (DomainError e) { return Handle(e); }
    }
}
