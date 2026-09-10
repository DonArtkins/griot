using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;

namespace Griot.Api.Controllers;

[ApiController]
[Authorize]
public class BoardController : DomainControllerBase
{
    private readonly IDomainService _domain;
    public BoardController(IDomainService domain) { _domain = domain; }

    [HttpGet]
    [Route("api/projects/{projectId}/boards")]
    public async Task<IActionResult> GetBoards(Guid projectId)
    { try { return Ok(await _domain.GetBoardsAsync(projectId, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpPost]
    [Route("api/projects/{projectId}/boards")]
    public async Task<IActionResult> CreateBoard(Guid projectId, [FromBody] CreateBoardRequest request)
    { try { return StatusCode(StatusCodes.Status201Created, await _domain.CreateBoardAsync(projectId, request, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpGet]
    [Route("api/boards/{id}")]
    public async Task<IActionResult> GetBoard(Guid id)
    { try { return Ok(await _domain.GetBoardAsync(id, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpPut]
    [Route("api/boards/{id}")]
    public async Task<IActionResult> UpdateBoard(Guid id, [FromBody] UpdateBoardRequest request)
    { try { var r = await _domain.UpdateBoardAsync(id, request, CurrentUserId()); return r is null ? NotFound(new { message = "Board not found." }) : Ok(r); } catch (DomainError e) { return Handle(e); } }

    [HttpDelete]
    [Route("api/boards/{id}")]
    public async Task<IActionResult> DeleteBoard(Guid id)
    { try { return (await _domain.DeleteBoardAsync(id, CurrentUserId())) ? NoContent() : NotFound(new { message = "Board not found." }); } catch (DomainError e) { return Handle(e); } }

    [HttpPost]
    [Route("api/boards/{id}/columns")]
    public async Task<IActionResult> CreateColumn(Guid id, [FromBody] CreateColumnRequest request)
    { try { return StatusCode(StatusCodes.Status201Created, await _domain.CreateColumnAsync(id, request, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }
}
