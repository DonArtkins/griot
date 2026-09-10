using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/tasks/{taskId}/comments")]
[Authorize]
public class CommentController : DomainControllerBase
{
    private readonly IDomainService _domain;
    public CommentController(IDomainService domain) { _domain = domain; }

    [HttpGet]
    public async Task<IActionResult> GetComments(Guid taskId)
    { try { return Ok(await _domain.GetCommentsAsync(taskId, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpPost]
    public async Task<IActionResult> CreateComment(Guid taskId, [FromBody] CreateCommentRequest request)
    { try { return StatusCode(StatusCodes.Status201Created, await _domain.CreateCommentAsync(taskId, request, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpPut("{commentId}")]
    public async Task<IActionResult> UpdateComment(Guid taskId, Guid commentId, [FromBody] UpdateCommentRequest request)
    { try { var r = await _domain.UpdateCommentAsync(taskId, commentId, request, CurrentUserId()); return r is null ? NotFound(new { message = "Comment not found." }) : Ok(r); } catch (DomainError e) { return Handle(e); } }

    [HttpDelete("{commentId}")]
    public async Task<IActionResult> DeleteComment(Guid taskId, Guid commentId)
    { try { return (await _domain.DeleteCommentAsync(taskId, commentId, CurrentUserId())) ? NoContent() : NotFound(new { message = "Comment not found." }); } catch (DomainError e) { return Handle(e); } }
}
