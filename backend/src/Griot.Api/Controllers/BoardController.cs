using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/boards")]
[Authorize]
public class BoardController : DomainControllerBase
{
    private readonly IDomainService _domain;
    public BoardController(IDomainService domain) { _domain = domain; }

    [HttpGet("/api/projects/{projectId}/boards")]
    public async Task<IActionResult> GetBoards(Guid projectId)
    { try { return Ok(await _domain.GetBoardsAsync(projectId, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpPost("/api/projects/{projectId}/boards")]
    public async Task<IActionResult> CreateBoard(Guid projectId, [FromBody] CreateBoardRequest request)
    { try { return StatusCode(StatusCodes.Status201Created, await _domain.CreateBoardAsync(projectId, request, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBoard(Guid id)
    { try { return Ok(await _domain.GetBoardAsync(id, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpPost("{id}/columns")]
    public async Task<IActionResult> CreateColumn(Guid id, [FromBody] CreateColumnRequest request)
    { try { return StatusCode(StatusCodes.Status201Created, await _domain.CreateColumnAsync(id, request, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }
}
