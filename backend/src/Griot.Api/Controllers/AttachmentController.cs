using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [HttpDelete("{attachmentId}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId)
    { try { return (await _domain.DeleteAttachmentAsync(id, attachmentId, CurrentUserId())) ? NoContent() : NotFound(new { message = "Attachment not found." }); } catch (DomainError e) { return Handle(e); } }
}
