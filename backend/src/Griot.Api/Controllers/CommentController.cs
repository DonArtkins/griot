using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/tasks/{id}/comments")]
[Authorize]
public class CommentController : DomainControllerBase
{
    private readonly IDomainService _domain;
    public CommentController(IDomainService domain) { _domain = domain; }

    [HttpGet]
    public async Task<IActionResult> GetComments(Guid id)
    { try { return Ok(await _domain.GetCommentsAsync(id, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpPost]
    public async Task<IActionResult> CreateComment(Guid id, [FromBody] CreateCommentRequest request)
    { try { return StatusCode(StatusCodes.Status201Created, await _domain.CreateCommentAsync(id, request, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }
}
