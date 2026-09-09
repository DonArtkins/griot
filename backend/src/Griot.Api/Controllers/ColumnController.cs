using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/columns")]
[Authorize]
public class ColumnController : DomainControllerBase
{
    private readonly IDomainService _domain;
    public ColumnController(IDomainService domain) { _domain = domain; }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateColumn(Guid id, [FromBody] UpdateColumnRequest request)
    { try { return Ok(await _domain.UpdateColumnAsync(id, request, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteColumn(Guid id)
    { try { return (await _domain.DeleteColumnAsync(id, CurrentUserId())) ? NoContent() : NotFound(new { message = "Column not found." }); } catch (DomainError e) { return Handle(e); } }
}
